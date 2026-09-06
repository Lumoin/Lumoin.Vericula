using System.Text;
using Lumoin.Vericula.MessageFormat.DataModel;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>Names, identifiers, variables, literals and character escapes: the lexical productions every other production is built from.</summary>
internal ref partial struct MessageParser
{
    /// <summary>Parses <c>escaped-char = backslash (backslash / "{" / "|" / "}")</c>. The leading backslash must already be at the cursor.</summary>
    /// <returns>The single escaped character, or <see langword="null"/> on a syntax error.</returns>
    private char? ParseEscapeChar()
    {
        ConsumeChar(MessageFormatSigils.Backslash, "a backslash to start an escape");
        if(Failed)
        {
            return null;
        }

        char next = PeekCharRaw();
        if(next is MessageFormatSigils.Backslash or MessageFormatSigils.OpenBrace or MessageFormatSigils.Pipe or MessageFormatSigils.CloseBrace)
        {
            _index++;

            return next;
        }

        Fail(_index, ExpectedButFound("'\\', '{', '|', or '}' after the escaping backslash"));

        return null;
    }

    /// <summary>
    /// Parses <c>name = [bidi] name-start *name-char [bidi]</c>: strips at most one leading and one
    /// trailing bidi mark from the stored value, and NFC-normalizes it (checking
    /// <see cref="string.IsNormalized()"/> first, normalizing only when it is not already normalized).
    /// </summary>
    /// <returns>The cooked, NFC-normalized name, or <see langword="null"/> on a syntax error.</returns>
    private string? ParseName()
    {
        ConsumeOptionalSingleBidi();

        ScalarStatus status = PeekScalar(out Rune rune, out int width);
        if(status != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsNameStart(rune.Value))
        {
            Fail(_index, ExpectedButFound("a name"));

            return null;
        }

        int contentStart = _index;
        _index += width;

        while(true)
        {
            status = PeekScalar(out rune, out width);
            if(status == ScalarStatus.Invalid)
            {
                Fail(_index, ExpectedButFound("a name character"));

                return null;
            }

            if(status != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsNameChar(rune.Value))
            {
                break;
            }

            _index += width;
        }

        string raw = _source[contentStart.._index];

        ConsumeOptionalSingleBidi();

        return MessageFormatCharacterClasses.NormalizeToNfc(raw);
    }

    /// <summary>Consumes at most one leading or trailing <c>[bidi]</c> mark of a <c>name</c>; an invalid code point here is left for the caller's own check to report, since nothing is consumed.</summary>
    private void ConsumeOptionalSingleBidi()
    {
        if(PeekScalar(out Rune rune, out int width) == ScalarStatus.Ok && MessageFormatCharacterClasses.IsBidi(rune.Value))
        {
            _index += width;
        }
    }

    /// <summary>Parses <c>identifier = [namespace ":"] name</c>, where <c>namespace = name</c>, joining the two halves with <c>:</c> when a namespace is present.</summary>
    /// <returns>The full identifier text (<c>name</c>, or <c>namespace:name</c>), or <see langword="null"/> on a syntax error.</returns>
    private string? ParseIdentifier()
    {
        string? first = ParseName();
        if(Failed || first is null)
        {
            return null;
        }

        if(PeekCharRaw() != MessageFormatSigils.Colon)
        {
            return first;
        }

        _index++;

        string? second = ParseName();
        if(Failed || second is null)
        {
            return null;
        }

        return $"{first}{MessageFormatSigils.Colon}{second}";
    }

    /// <summary>Parses <c>variable = "$" name</c>.</summary>
    /// <returns>The parsed variable, or <see langword="null"/> on a syntax error.</returns>
    private Variable? ParseVariable()
    {
        ConsumeChar(MessageFormatSigils.Dollar, "'$' to start a variable");
        if(Failed)
        {
            return null;
        }

        string? name = ParseName();
        if(Failed || name is null)
        {
            return null;
        }

        return new Variable(name);
    }

    /// <summary>Parses <c>literal = quoted-literal / unquoted-literal</c>.</summary>
    /// <returns>The parsed literal, or <see langword="null"/> on a syntax error.</returns>
    private Literal? ParseLiteral()
    {
        if(PeekCharRaw() == MessageFormatSigils.Pipe)
        {
            return ParseQuotedLiteral();
        }

        return ParseUnquotedLiteral();
    }

    /// <summary>
    /// Parses <c>quoted-literal = "|" *(quoted-char / escaped-char) "|"</c>. Unlike pattern text, every
    /// code point here is either quoted content, an escape, or the closing <c>|</c>: there is no
    /// legitimate reason to stop other than the closing delimiter, so any other code point is an
    /// immediate syntax error.
    /// </summary>
    /// <returns>The literal, its value kept verbatim (escapes processed, otherwise unaltered, and not NFC-normalized), or <see langword="null"/> on a syntax error.</returns>
    private Literal? ParseQuotedLiteral()
    {
        ConsumeChar(MessageFormatSigils.Pipe, "'|' to open a quoted literal");
        if(Failed)
        {
            return null;
        }

        var text = new StringBuilder();

        while(true)
        {
            char c = PeekCharRaw();
            if(c == MessageFormatSigils.Pipe)
            {
                break;
            }

            if(c == MessageFormatSigils.Backslash)
            {
                char? escaped = ParseEscapeChar();
                if(Failed || escaped is null)
                {
                    return null;
                }

                text.Append(escaped.Value);

                continue;
            }

            ScalarStatus status = PeekScalar(out Rune rune, out int width);
            if(status != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsQuotedChar(rune.Value))
            {
                Fail(_index, ExpectedButFound("quoted literal text, an escape, or the closing '|'"));

                return null;
            }

            text.Append(_source, _index, width);
            _index += width;
        }

        ConsumeChar(MessageFormatSigils.Pipe, "'|' to close the quoted literal");
        if(Failed)
        {
            return null;
        }

        return new Literal(text.ToString());
    }

    /// <summary>Parses <c>unquoted-literal = 1*name-char</c>: one or more name-characters, with no bidi stripping and no restriction on the first character.</summary>
    /// <returns>The literal, its value kept verbatim (not NFC-normalized), or <see langword="null"/> on a syntax error.</returns>
    private Literal? ParseUnquotedLiteral()
    {
        int start = _index;

        while(true)
        {
            ScalarStatus status = PeekScalar(out Rune rune, out int width);
            if(status == ScalarStatus.Invalid)
            {
                Fail(_index, ExpectedButFound("a literal"));

                return null;
            }

            if(status != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsNameChar(rune.Value))
            {
                break;
            }

            _index += width;
        }

        if(_index == start)
        {
            Fail(_index, ExpectedButFound("a literal"));

            return null;
        }

        return new Literal(_source[start.._index]);
    }
}
