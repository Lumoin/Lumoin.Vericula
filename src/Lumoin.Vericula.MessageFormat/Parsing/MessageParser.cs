using System.Collections.Immutable;
using System.Text;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// A single-pass, backtracking-free recursive-descent parser for MessageFormat 2.0 source text,
/// driven by a cursor (<see cref="_index"/>) over the source string. Not thread-safe and not
/// reusable: <see cref="Parse(string)"/> creates one instance per call. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "ABNF Grammar" (<c>message.abnf</c>), production
/// <c>message</c>.
/// </summary>
/// <remarks>
/// Every production method returns <see langword="null"/> exactly when parsing has failed (checked
/// via <see cref="Failed"/>, set once by <see cref="Fail(int, string)"/> and never cleared): there is
/// no backtracking past a genuine syntax error, only the small amount of lookahead a handful of
/// productions need to decide between two alternatives before either has consumed input
/// irreversibly. Every caller of a sub-production checks <see cref="Failed"/> (or the sub-production's
/// null result, which implies it) before using the result or advancing further.
/// </remarks>
internal ref partial struct MessageParser
{
    /// <summary>The complete source text being parsed.</summary>
    private readonly string _source;

    /// <summary>The zero-based UTF-16 cursor position: the next code unit to be examined.</summary>
    private int _index;

    /// <summary>The first syntax error encountered, if any; once set, it is never replaced or cleared.</summary>
    private MessageFormatDiagnostic? _syntaxError;

    /// <summary>
    /// The offset side table this parser records a declaration's, a variant's, an option's, a select
    /// message's and each selector variable's starting offset into as it builds them, for
    /// <see cref="MessageDataModelValidator"/> to locate the diagnostics it raises against the model
    /// this parser hands back.
    /// </summary>
    private readonly MessageFormatOffsets _offsets;

    /// <summary>Initializes a new instance of the <see cref="MessageParser"/> struct positioned at the start of <paramref name="source"/>.</summary>
    /// <param name="source">The source text to parse.</param>
    private MessageParser(string source)
    {
        _source = source;
        _index = 0;
        _syntaxError = null;
        _offsets = new MessageFormatOffsets();
    }

    /// <summary>
    /// Parses a MessageFormat 2.0 message, the sole entry point <see cref="MessageFormatReader.TryParse(string)"/>
    /// delegates to: a syntax error stops parsing immediately and is the sole diagnostic reported, but a
    /// syntactically complete parse always goes on to <see cref="MessageDataModelValidator.Validate"/>,
    /// which collects every data model diagnostic rather than stopping at the first.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>
    /// On a syntax error, a <see langword="null"/> model and exactly one
    /// <see cref="WellKnownMessageFormatDiagnostics.SyntaxError"/> diagnostic; otherwise, the parsed
    /// model and every data model diagnostic found against it (empty for a valid message).
    /// </returns>
    internal static MessageParseResult Parse(string source)
    {
        var parser = new MessageParser(source);
        Message? message = parser.ParseMessage();

        if(parser._syntaxError is MessageFormatDiagnostic diagnostic)
        {
            return new MessageParseResult(null, [diagnostic]);
        }

        ImmutableArray<MessageFormatDiagnostic> diagnostics = MessageDataModelValidator.Validate(message!, source, parser._offsets);

        return new MessageParseResult(message, diagnostics);
    }

    /// <summary>
    /// Parses <c>message = simple-message / complex-message</c>: skips the leading <c>o</c>, then
    /// decides between the two forms by whether a <c>.</c> or a <c>{{</c> comes next. A simple
    /// message re-parses the whole source as its pattern (see <see cref="ParseSimpleMessage"/>) rather
    /// than resuming after the whitespace this method already skipped, so that leading whitespace
    /// (and, for the bidi retry below, a leading bidi mark) is ordinary pattern text rather than a
    /// separately tracked prefix.
    /// </summary>
    /// <returns>The parsed message, or <see langword="null"/> on a syntax error.</returns>
    private Message? ParseMessage()
    {
        int pureWhitespaceEnd = PeekPastPureWhitespaceFrom(0);

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        bool sawBidi = _index > pureWhitespaceEnd;
        bool isDot = PeekCharRaw() == MessageFormatSigils.Dot;
        bool isComplex = (PeekCharRaw() == MessageFormatSigils.OpenBrace && PeekCharAt(_index + 1) == MessageFormatSigils.OpenBrace) || isDot;

        if(isComplex)
        {
            Message? complexMessage = ParseComplexMessage();
            if(!Failed || !isDot || !sawBidi)
            {
                return complexMessage;
            }

            //A bidi mark immediately before '.' is ambiguous: the leading o could have consumed
            //the bidi mark, leaving '.' to start a declaration or matcher (what the branch above
            //just tried and failed at), or o could be empty with the bidi mark itself matching
            //simple-start-char, leaving the '.' and everything after it as ordinary pattern text.
            //A complex-valid message can never also be simple-valid (a real declaration or matcher
            //needs a '{', which is not a valid text-char), so retrying as simple here can only ever
            //turn a correct failure into a correct success, never a correct success into a failure.
            _index = 0;
            _syntaxError = null;

            return ParseSimpleMessage();
        }

        _index = 0;

        return ParseSimpleMessage();
    }

    /// <summary>
    /// Parses <c>simple-message</c> as the whole source text run through <c>pattern</c>'s grammar
    /// (<c>*(text-char / escaped-char / placeholder)</c>): every <c>ws</c> character is itself a
    /// valid <c>text-char</c>, so re-running <c>pattern</c> from offset 0 reproduces exactly what
    /// <c>o [simple-start pattern]</c> would, without a separate leading-whitespace step.
    /// </summary>
    /// <returns>The parsed message, or <see langword="null"/> on a syntax error.</returns>
    private PatternMessage? ParseSimpleMessage()
    {
        Pattern? pattern = ParsePattern();
        if(Failed || pattern is null)
        {
            return null;
        }

        if(!AtEnd)
        {
            Fail(_index, ExpectedButFound("the end of the message"));

            return null;
        }

        return new PatternMessage(ImmutableArray<Declaration>.Empty, pattern);
    }

    /// <summary>
    /// Parses <c>complex-message = o *(declaration o) complex-body o</c> from the current position
    /// (the leading <c>o</c> already consumed by <see cref="ParseMessage"/>): zero or more
    /// declarations, each followed by <c>o</c>, then the complex body, then a final <c>o</c>, then
    /// nothing but the end of input.
    /// </summary>
    /// <returns>The parsed message, or <see langword="null"/> on a syntax error.</returns>
    private Message? ParseComplexMessage()
    {
        var declarations = ImmutableArray.CreateBuilder<Declaration>();

        while(true)
        {
            int declarationStart = _index;

            Declaration? declaration = null;
            if(TryConsumeKeyword(WellKnownMessageFormatKeywords.Input))
            {
                declaration = ParseInputDeclaration();
            }
            else if(TryConsumeKeyword(WellKnownMessageFormatKeywords.Local))
            {
                declaration = ParseLocalDeclaration();
            }
            else
            {
                break;
            }

            if(Failed || declaration is null)
            {
                return null;
            }

            _offsets.Record(declaration, declarationStart);

            declarations.Add(declaration);

            SkipOptionalWhitespace();
            if(Failed)
            {
                return null;
            }
        }

        Message? body = ParseComplexBody(declarations.ToImmutable());
        if(Failed || body is null)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        if(!AtEnd)
        {
            Fail(_index, ExpectedButFound("the end of the message"));

            return null;
        }

        return body;
    }

    /// <summary>
    /// Parses <c>complex-body = quoted-pattern / matcher</c>: a <c>{{</c> starts a quoted pattern, and
    /// the <c>.match</c> keyword starts a matcher; anything else is a syntax error.
    /// </summary>
    /// <param name="declarations">The declarations already parsed, carried into the resulting message.</param>
    /// <returns>The parsed message, or <see langword="null"/> on a syntax error.</returns>
    private Message? ParseComplexBody(ImmutableArray<Declaration> declarations)
    {
        if(PeekCharRaw() == MessageFormatSigils.OpenBrace && PeekCharAt(_index + 1) == MessageFormatSigils.OpenBrace)
        {
            Pattern? pattern = ParseQuotedPattern();
            if(Failed || pattern is null)
            {
                return null;
            }

            return new PatternMessage(declarations, pattern);
        }

        int matchStart = _index;
        if(TryConsumeKeyword(WellKnownMessageFormatKeywords.Match))
        {
            return ParseMatcher(declarations, matchStart);
        }

        Fail(_index, ExpectedButFound("an input or local declaration, a quoted pattern, or '.match'"));

        return null;
    }

    /// <summary>Parses <c>quoted-pattern = "{{" pattern "}}"</c>.</summary>
    /// <returns>The wrapped pattern, or <see langword="null"/> on a syntax error.</returns>
    private Pattern? ParseQuotedPattern()
    {
        ConsumeChar(MessageFormatSigils.OpenBrace, "'{' to start a quoted pattern");
        ConsumeChar(MessageFormatSigils.OpenBrace, "a second '{' to start a quoted pattern");
        if(Failed)
        {
            return null;
        }

        Pattern? pattern = ParsePattern();
        if(Failed || pattern is null)
        {
            return null;
        }

        ConsumeChar(MessageFormatSigils.CloseBrace, "'}' to close the quoted pattern");
        ConsumeChar(MessageFormatSigils.CloseBrace, "a second '}' to close the quoted pattern");
        if(Failed)
        {
            return null;
        }

        return pattern;
    }

    /// <summary>
    /// Parses <c>pattern = *(text-char / escaped-char / placeholder)</c>, folding every run of plain
    /// text and escapes into one <see cref="TextPart"/>. Stops, without consuming or failing, at the
    /// first code point that is none of the three alternatives (end of input, a stray <c>}</c>, or
    /// anything else); the caller decides whether stopping there is legitimate (end of a simple
    /// message, or the <c>}}</c> that closes a quoted pattern) or a syntax error.
    /// </summary>
    /// <returns>The parsed pattern; never <see langword="null"/> unless a syntax error occurred.</returns>
    private Pattern? ParsePattern()
    {
        var parts = ImmutableArray.CreateBuilder<PatternPart>();
        var text = new StringBuilder();

        while(true)
        {
            char c = PeekCharRaw();
            if(c == MessageFormatSigils.OpenBrace)
            {
                FlushText(text, parts);

                PatternPart? part = ParsePlaceholder();
                if(Failed || part is null)
                {
                    return null;
                }

                parts.Add(part);

                continue;
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
            if(status == ScalarStatus.Invalid)
            {
                Fail(_index, ExpectedButFound("pattern text, an escape, or a placeholder"));

                return null;
            }

            if(status != ScalarStatus.Ok || !MessageFormatCharacterClasses.IsTextChar(rune.Value))
            {
                break;
            }

            text.Append(_source, _index, width);
            _index += width;
        }

        FlushText(text, parts);

        return new Pattern(parts.ToImmutable());
    }

    /// <summary>Appends <paramref name="text"/> as one non-empty <see cref="TextPart"/> to <paramref name="parts"/>, and clears it, when it holds any text.</summary>
    /// <param name="text">The accumulated cooked text of the current run.</param>
    /// <param name="parts">The pattern's parts collected so far.</param>
    private static void FlushText(StringBuilder text, ImmutableArray<PatternPart>.Builder parts)
    {
        if(text.Length > 0)
        {
            parts.Add(new TextPart(text.ToString()));
            text.Clear();
        }
    }

    /// <summary>
    /// Parses a placeholder: <c>placeholder = expression / markup</c>. Looks ahead past the opening
    /// <c>{</c> and an optional <c>o</c>, without consuming, to decide whether a <c>#</c> or <c>/</c>
    /// makes this markup; either alternative then re-consumes its own opening <c>{</c> and <c>o</c>.
    /// </summary>
    /// <returns>The parsed pattern part, or <see langword="null"/> on a syntax error.</returns>
    private PatternPart? ParsePlaceholder()
    {
        int afterWhitespace = PeekPastOptionalWhitespaceFrom(_index + 1);
        char next = PeekCharAt(afterWhitespace);

        if(next is MessageFormatSigils.Hash or MessageFormatSigils.Slash)
        {
            MarkupPart? markup = ParseMarkup();
            if(Failed || markup is null)
            {
                return null;
            }

            return markup;
        }

        Expression? expression = ParseExpression();
        if(Failed || expression is null)
        {
            return null;
        }

        return new ExpressionPart(expression);
    }
}
