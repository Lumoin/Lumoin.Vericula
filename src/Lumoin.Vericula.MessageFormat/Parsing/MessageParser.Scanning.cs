using System.Buffers;
using System.Text;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>Low-level cursor primitives shared by every production: scalar decoding, whitespace, and the single-error-slot failure protocol.</summary>
internal ref partial struct MessageParser
{
    /// <summary>The outcome of peeking one Unicode scalar value at the cursor.</summary>
    private enum ScalarStatus
    {
        /// <summary>The cursor is at or past the end of the source text.</summary>
        EndOfInput = 0,

        /// <summary>The next code unit(s) do not decode to a valid, non-NULL scalar value: an unpaired surrogate, or U+0000.</summary>
        Invalid = 1,

        /// <summary>A valid, non-NULL scalar value was decoded.</summary>
        Ok = 2
    }

    /// <summary>Whether a syntax error has already been recorded; once true, it stays true for the life of the parser.</summary>
    private readonly bool Failed => _syntaxError is not null;

    /// <summary>Whether the cursor has reached the end of the source text.</summary>
    private readonly bool AtEnd => _index >= _source.Length;

    /// <summary>
    /// Decodes the Unicode scalar value at the cursor without advancing it, rejecting an unpaired
    /// surrogate or U+0000 exactly like the grammar does everywhere: neither is a valid <c>text-char</c>,
    /// <c>quoted-char</c>, <c>name-start</c>, <c>name-char</c>, <c>ws</c> or <c>bidi</c>, so no
    /// production ever accepts one.
    /// </summary>
    /// <param name="rune">The decoded scalar value, valid only when the result is <see cref="ScalarStatus.Ok"/>.</param>
    /// <param name="width">The scalar's width in UTF-16 code units (1 or 2), valid only when the result is <see cref="ScalarStatus.Ok"/>.</param>
    /// <returns>Whether the cursor is at the end of input, on an invalid code point, or on a valid scalar value.</returns>
    private readonly ScalarStatus PeekScalar(out Rune rune, out int width)
    {
        if(_index >= _source.Length)
        {
            rune = default;
            width = 0;

            return ScalarStatus.EndOfInput;
        }

        if(Rune.DecodeFromUtf16(_source.AsSpan(_index), out rune, out width) != OperationStatus.Done)
        {
            return ScalarStatus.Invalid;
        }

        if(rune.Value == 0)
        {
            return ScalarStatus.Invalid;
        }

        return ScalarStatus.Ok;
    }

    /// <summary>Reads the raw UTF-16 code unit at the cursor without decoding it as a scalar value, for matching single-code-unit ASCII sigils.</summary>
    /// <returns>The code unit at the cursor, or <c>'\0'</c> at the end of input (never itself a valid sigil, since U+0000 is never permitted).</returns>
    private readonly char PeekCharRaw() => _index < _source.Length ? _source[_index] : '\0';

    /// <summary>Reads the raw UTF-16 code unit at an arbitrary index, for lookahead that must not move the cursor.</summary>
    /// <param name="index">The index to read.</param>
    /// <returns>The code unit at <paramref name="index"/>, or <c>'\0'</c> when it is at or past the end of the source.</returns>
    private readonly char PeekCharAt(int index) => index < _source.Length ? _source[index] : '\0';

    /// <summary>
    /// Finds how far <c>o = *(ws / bidi)</c> would advance from <paramref name="index"/>, without
    /// moving the cursor or recording a failure; used only to decide between two alternatives before
    /// either has committed to consuming input (see <see cref="ParsePlaceholder"/>).
    /// </summary>
    /// <param name="index">The index to start looking from.</param>
    /// <returns>The index of the first code point that is not <c>ws</c> or <c>bidi</c>, or the source's length; an invalid code point also stops the search there, leaving it for the real, failing scan to report.</returns>
    private readonly int PeekPastOptionalWhitespaceFrom(int index)
    {
        while(index < _source.Length)
        {
            if(Rune.DecodeFromUtf16(_source.AsSpan(index), out Rune rune, out int width) != OperationStatus.Done)
            {
                return index;
            }

            if(!MessageFormatCharacterClasses.IsWhitespace(rune.Value) && !MessageFormatCharacterClasses.IsBidi(rune.Value))
            {
                return index;
            }

            index += width;
        }

        return index;
    }

    /// <summary>
    /// Finds how far a run of plain <c>ws</c> alone, with no <c>bidi</c> marks, would advance from
    /// <paramref name="index"/>, without moving the cursor; <see cref="ParseMessage"/> compares this
    /// bound against the cursor position after <see cref="SkipOptionalWhitespace"/> to tell whether
    /// the leading <c>o</c> it just skipped consumed at least one bidi mark.
    /// </summary>
    /// <param name="index">The index to start looking from.</param>
    /// <returns>The index of the first code point that is not <c>ws</c>, or the source's length; an invalid code point also stops the search there, leaving it for the real, failing scan to report.</returns>
    private readonly int PeekPastPureWhitespaceFrom(int index)
    {
        while(index < _source.Length)
        {
            if(Rune.DecodeFromUtf16(_source.AsSpan(index), out Rune rune, out int width) != OperationStatus.Done)
            {
                return index;
            }

            if(!MessageFormatCharacterClasses.IsWhitespace(rune.Value))
            {
                return index;
            }

            index += width;
        }

        return index;
    }

    /// <summary>Parses <c>o = *(ws / bidi)</c>, discarding whether any <c>ws</c> character (as opposed to only <c>bidi</c> marks) was found.</summary>
    private void SkipOptionalWhitespace() => SkipOptionalWhitespaceTrackingRealWhitespace();

    /// <summary>
    /// Parses <c>o = *(ws / bidi)</c>, reporting whether at least one true <c>ws</c> character (not
    /// only <c>bidi</c> marks) was consumed; several productions use this to tell a required <c>s</c>
    /// apart from an optional <c>o</c> that merely happens to contain bidi marks.
    /// </summary>
    /// <returns><see langword="true"/> if a <c>ws</c> character was consumed; otherwise, <see langword="false"/>.</returns>
    private bool SkipOptionalWhitespaceTrackingRealWhitespace()
    {
        bool sawWhitespace = false;

        while(true)
        {
            ScalarStatus status = PeekScalar(out Rune rune, out int width);
            if(status == ScalarStatus.Invalid)
            {
                Fail(_index, ExpectedButFound("whitespace, a bidi mark, or the next token"));

                return sawWhitespace;
            }

            if(status == ScalarStatus.EndOfInput)
            {
                return sawWhitespace;
            }

            if(MessageFormatCharacterClasses.IsWhitespace(rune.Value))
            {
                sawWhitespace = true;
                _index += width;

                continue;
            }

            if(MessageFormatCharacterClasses.IsBidi(rune.Value))
            {
                _index += width;

                continue;
            }

            return sawWhitespace;
        }
    }

    /// <summary>
    /// Parses <c>s = *bidi ws o</c>: any number of leading bidi marks, then a required <c>ws</c>
    /// character, then a trailing <c>o</c>.
    /// </summary>
    /// <param name="context">A short phrase naming where the whitespace is required, folded into the syntax-error message when it is missing.</param>
    /// <returns><see langword="true"/> on success; <see langword="false"/> when the required <c>ws</c> character is missing, after recording the syntax error.</returns>
    private bool RequireWhitespace(string context)
    {
        while(true)
        {
            ScalarStatus status = PeekScalar(out Rune rune, out int width);
            if(status == ScalarStatus.Invalid)
            {
                Fail(_index, ExpectedButFound($"whitespace {context}"));

                return false;
            }

            if(status != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsBidi(rune.Value))
            {
                break;
            }

            _index += width;
        }

        ScalarStatus wsStatus = PeekScalar(out Rune wsRune, out int wsWidth);
        if(wsStatus != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsWhitespace(wsRune.Value))
        {
            Fail(_index, ExpectedButFound($"whitespace {context}"));

            return false;
        }

        _index += wsWidth;

        SkipOptionalWhitespace();

        return !Failed;
    }

    /// <summary>
    /// Peeks whether the cursor could start a <c>name</c>: a <c>name-start</c> code point at the
    /// cursor. Used to decide, without committing, whether another option follows a function's or
    /// markup's options list; both callers have just run <see cref="SkipOptionalWhitespaceTrackingRealWhitespace"/>,
    /// whose own loop already consumes every leading <c>bidi</c> mark (not only a single one), so no
    /// bidi mark can still be at the cursor here.
    /// </summary>
    /// <returns><see langword="true"/> if a name could start here; otherwise, <see langword="false"/>.</returns>
    private readonly bool NextLooksLikeIdentifierStart()
    {
        return Rune.DecodeFromUtf16(_source.AsSpan(_index), out Rune nameStartRune, out _) == OperationStatus.Done
            && MessageFormatCharacterClasses.IsNameStart(nameStartRune.Value);
    }

    /// <summary>Peeks whether the cursor could start a <c>key</c>: <c>*</c>, or a <c>literal</c> (quoted or unquoted).</summary>
    /// <returns><see langword="true"/> if a key could start here; otherwise, <see langword="false"/>.</returns>
    private readonly bool LooksLikeKeyStart()
    {
        char c = PeekCharRaw();
        if(c is MessageFormatSigils.Pipe or MessageFormatSigils.Asterisk)
        {
            return true;
        }

        return Rune.DecodeFromUtf16(_source.AsSpan(_index), out Rune rune, out _) == OperationStatus.Done
            && MessageFormatCharacterClasses.IsNameChar(rune.Value);
    }

    /// <summary>Consumes one expected single-code-unit sigil, or records a syntax error naming what was expected instead.</summary>
    /// <param name="expected">The sigil the grammar requires at the cursor.</param>
    /// <param name="expectedDescription">A phrase describing <paramref name="expected"/>, used only when it is missing.</param>
    /// <returns><see langword="true"/> if <paramref name="expected"/> was consumed; otherwise, <see langword="false"/>.</returns>
    private bool ConsumeChar(char expected, string expectedDescription)
    {
        if(_index < _source.Length && _source[_index] == expected)
        {
            _index++;

            return true;
        }

        Fail(_index, ExpectedButFound(expectedDescription));

        return false;
    }

    /// <summary>Consumes a fixed, case-sensitive ASCII keyword when the cursor starts with it exactly; otherwise leaves the cursor untouched.</summary>
    /// <param name="keyword">The keyword to try, from <see cref="WellKnownMessageFormatKeywords"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="keyword"/> was consumed; otherwise, <see langword="false"/>.</returns>
    private bool TryConsumeKeyword(string keyword)
    {
        if(_index + keyword.Length <= _source.Length
            && string.CompareOrdinal(_source, _index, keyword, 0, keyword.Length) == 0)
        {
            _index += keyword.Length;

            return true;
        }

        return false;
    }

    /// <summary>Records the first syntax error at <paramref name="offset"/>; a later call is ignored, since only the first syntax error is ever reported.</summary>
    /// <param name="offset">The zero-based UTF-16 offset of the offending code point, or the source's length at the end of input.</param>
    /// <param name="message">A human-readable description of what was expected and what was found.</param>
    private void Fail(int offset, string message)
    {
        if(_syntaxError is not null)
        {
            return;
        }

        (int line, int position) = MessageFormatPosition.Locate(_source, offset);
        _syntaxError = new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.SyntaxError, message, offset, line, position);
    }

    /// <summary>Composes a syntax-error message describing what the grammar expects at the cursor and what is actually there.</summary>
    /// <param name="expected">A phrase describing what the grammar allows at the cursor.</param>
    /// <returns>The composed message, ending in a period.</returns>
    private readonly string ExpectedButFound(string expected)
    {
        ScalarStatus status = PeekScalar(out Rune rune, out _);

        return $"Expected {expected}{FoundSuffix(status, rune)}.";
    }

    /// <summary>Describes what was actually found at the cursor, for <see cref="ExpectedButFound(string)"/>.</summary>
    /// <param name="status">The outcome of peeking the cursor's scalar value.</param>
    /// <param name="rune">The decoded scalar value; meaningful only when <paramref name="status"/> is <see cref="ScalarStatus.Ok"/>.</param>
    /// <returns>A clause starting with " but", to be appended after "Expected &lt;something&gt;".</returns>
    private static string FoundSuffix(ScalarStatus status, Rune rune) => status switch
    {
        ScalarStatus.EndOfInput => " but reached the end of the message",
        ScalarStatus.Invalid => " but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted)",
        _ => $" but found '{rune}'"
    };
}
