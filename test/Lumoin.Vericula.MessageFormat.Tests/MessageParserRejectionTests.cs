using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Hand-written tests asserting that specific malformed sources fail to parse with a single
/// <see cref="WellKnownMessageFormatDiagnostics.SyntaxError"/> diagnostic at a specific offset: an
/// unpaired surrogate in each of four regions (text, a quoted literal, a name, and a whitespace
/// region), at the end of input, and as a lone low surrogate; U+0000; an unknown escape; a stray
/// closing brace; and content trailing a complex body. Also asserts the exact offset and exact
/// message text of every fail site in the lexical and low-level cursor productions
/// (<c>MessageParser.Literals.cs</c> and <c>MessageParser.Scanning.cs</c>).
/// </summary>
[TestClass]
public sealed class MessageParserRejectionTests
{
    /// <summary>Parses <paramref name="source"/>, asserting it failed with exactly one syntax-error diagnostic, and returns that diagnostic.</summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The sole diagnostic.</returns>
    private static MessageFormatDiagnostic ParsesToOneSyntaxError(string source)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.IsNull(result.Message);
        Assert.HasCount(1, result.Diagnostics);
        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsSyntaxError(result.Diagnostics[0].Id));

        return result.Diagnostics[0];
    }

    /// <summary>Parses <paramref name="source"/>, asserting the single syntax-error diagnostic sits at <paramref name="offset"/>, on line 1.</summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="offset">The expected zero-based UTF-16 offset of the diagnostic.</param>
    private static void AssertRejectedAt(string source, int offset)
    {
        MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError(source);

        Assert.AreEqual(offset, diagnostic.Offset);
        Assert.AreEqual(1, diagnostic.Line);
        Assert.AreEqual(offset + 1, diagnostic.Position);
    }

    /// <summary>An unpaired high surrogate in plain pattern text is rejected, at the surrogate's own offset.</summary>
    [TestMethod]
    public void RejectsAnUnpairedHighSurrogateInText()
    {
        AssertRejectedAt("abc\uD800def", 3);
    }

    /// <summary>An unpaired low surrogate in plain pattern text is rejected, at the surrogate's own offset.</summary>
    [TestMethod]
    public void RejectsAnUnpairedLowSurrogateInText()
    {
        AssertRejectedAt("abc\uDC00def", 3);
    }

    /// <summary>An unpaired high surrogate at the very end of input is rejected rather than treated as needing more data.</summary>
    /// <remarks>
    /// Named killer: MessageParser.Scanning.cs line 48, changing <c>!= OperationStatus.Done</c> to
    /// <c>== OperationStatus.InvalidData</c> in <c>PeekScalar</c> would let a trailing high surrogate
    /// decode as <c>NeedMoreData</c> instead of being rejected, since <c>Rune.DecodeFromUtf16</c> only
    /// returns <c>InvalidData</c> for a surrogate that has more input after it to prove it unpaired;
    /// this test has no more input after the surrogate, so it is the one case that distinguishes the
    /// two statuses and would silently accept the message instead of failing.
    /// </remarks>
    [TestMethod]
    public void RejectsAnUnpairedHighSurrogateAtTheEndOfInput()
    {
        AssertRejectedAt("abc\uD800", 3);
    }

    /// <summary>An unpaired high surrogate inside a quoted literal is rejected, even though the grammar otherwise tolerates almost any code point there.</summary>
    [TestMethod]
    public void RejectsAnUnpairedHighSurrogateInAQuotedLiteral()
    {
        AssertRejectedAt("{|abc\uD800def|}", 5);
    }

    /// <summary>An unpaired high surrogate inside a name is rejected.</summary>
    [TestMethod]
    public void RejectsAnUnpairedHighSurrogateInAName()
    {
        AssertRejectedAt("{$abc\uD800}", 5);
    }

    /// <summary>An unpaired high surrogate in an optional-whitespace region is rejected.</summary>
    [TestMethod]
    public void RejectsAnUnpairedHighSurrogateInAWhitespaceRegion()
    {
        AssertRejectedAt("{$x \uD800:number}", 4);
    }

    /// <summary>U+0000 is never permitted, anywhere in the source.</summary>
    [TestMethod]
    public void RejectsNulCharacter()
    {
        AssertRejectedAt("abc" + char.MinValue + "def", 3);
    }

    /// <summary>A backslash followed by anything other than <c>\</c>, <c>{</c>, <c>|</c> or <c>}</c> is not a valid escape, and the offset points at the offending character, not the backslash.</summary>
    [TestMethod]
    public void RejectsAnUnknownEscape()
    {
        AssertRejectedAt("abc\\xdef", 4);
    }

    /// <summary>The unknown-escape diagnostic names each of the four valid escape targets with a single backslash, not a doubled one.</summary>
    [TestMethod]
    public void UnknownEscapeMessageNamesTheFourValidEscapeTargetsWithASingleBackslash()
    {
        //Named killer: MessageParser.Literals.cs, ParseEscapeChar's ExpectedButFound argument. The
        //literal "'\\\\', '{', '|', or '}' after the escaping backslash" (four backslashes in source)
        //renders as two backslash characters between the first pair of quotes; this test's exact-text
        //assertion sees the doubled backslash a reader would otherwise have to notice by eye.
        MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError("abc\\xdef");

        Assert.AreEqual("Expected '\\', '{', '|', or '}' after the escaping backslash but found 'x'.", diagnostic.Message);
    }

    /// <summary>A closing brace with no opening placeholder or quoted pattern is a syntax error, since <c>}</c> is not itself a valid <c>text-char</c>.</summary>
    [TestMethod]
    public void RejectsAStrayClosingBrace()
    {
        AssertRejectedAt("abc}def", 3);
    }

    /// <summary>Content trailing a complex body's final <c>o</c> is a syntax error: a complex message must end there.</summary>
    [TestMethod]
    public void RejectsContentAfterAComplexBody()
    {
        AssertRejectedAt("{{hello}}extra", 9);
    }

    /// <summary>A leading space (not a bidi mark) followed by '.' still fails: only a bidi mark immediately before '.' is ambiguous with a simple message, and plain whitespace is not.</summary>
    [TestMethod]
    public void RejectsALeadingSpaceThenFullStopWithNoBidiMark()
    {
        //Pins the boundary next to SimpleMessageMayStartWithABidiMarkFollowedByAFullStop
        //(MessageParserModelShapeTests.cs): 'o' here is a plain space, not a bidi mark, so ParseMessage's
        //retry condition (sawBidi) is false and this source is never re-tried as simple. '.' is not a
        //valid simple-start-char, so " .hello" has no valid derivation either way.
        AssertRejectedAt(" .hello", 1);
    }

    /// <summary>A function is rejected after an attribute, in every operand-bearing expression form: the grammar puts the optional function strictly before every attribute.</summary>
    [TestMethod]
    public void RejectsAFunctionAfterAnAttribute()
    {
        //Named killer: MessageParser.Expressions.cs, TryParseFunctionAndAttributesTail's Colon arm.
        //Removing the `builder.Count > 0` guard (added ahead of the existing `function is not null`
        //check) lets a function follow an attribute instead of being rejected; each source below would
        //then parse successfully instead of failing, and this test's null-model assertion sees it.
        ParsesToOneSyntaxError("{$x @a :f}");
        ParsesToOneSyntaxError("{|x| @a :f}");
        ParsesToOneSyntaxError(".input {$x @a :f} {{}}");
    }

    /// <summary>Every expression, markup, function, option and attribute production names the specific grammar rule it enforces in its diagnostic message, at the first place a source can genuinely violate that rule.</summary>
    [TestMethod]
    public void TheFirstFailSiteReachedNamesTheGrammarRuleItEnforces()
    {
        //Named killers, all in MessageParser.Expressions.cs: each source below drives the parser to the
        //one Fail call named beside it as the first syntax error a well-formed prefix can reach; a
        //String mutation blanking that call's message argument empties diagnostic.Message, so
        //Assert.Contains below sees it. A blanked Fail() call itself (a Statement mutation, replacing
        //the call with `;`) skips setting _syntaxError while the parse still fails structurally further
        //up, so MessageParser.Parse's own `_syntaxError is MessageFormatDiagnostic` check is false and
        //it falls into MessageDataModelValidator.Validate with a null message instead of returning the
        //diagnostic cleanly; this same source, through ParsesToOneSyntaxError's assertions, sees that
        //mutant too, whether it throws or merely reports the wrong diagnostic count.
        //  ".local $x=y"             line 17  ParseExpression's own '{' guard, reached (unlike from
        //                                     ParsePlaceholder) with no guarantee of '{' (M-160)
        //  "{$x @a :f}"              line 131 TryParseFunctionAndAttributesTail's Colon arm, once an
        //                                     attribute already started the tail (M-179)
        //  "{$x@a}"                  line 163 TryParseFunctionAndAttributesTail's At arm (M-189)
        //  "{$x !}"                  line 179 TryParseFunctionAndAttributesTail's catch-all (M-193)
        //  "{#foo\u200e\u200ebar=1}" line 245 ParseMarkup's options loop; the name's own
        //                                     trailing [bidi] swallows the first mark, leaving the
        //                                     second right before an identifier start with no real
        //                                     whitespace between them (M-203, M-204)
        //  "{#foo@a}"                line 278 ParseMarkup's attributes loop (M-208, M-209)
        //  "{#foo!}"                 line 305 ParseMarkup's closing '}' (M-213)
        //  "{:foo\u200e\u200ebar=1}" line 349 ParseFunction's options loop, same bidi
        //                                     mechanism as the markup case above (M-220, M-221)
        //  "{:f a}"                  line 384 ParseOption's '=' guard (M-227)
        (string source, string fragment)[] cases =
        [
            (".local $x=y", "start an expression"),
            ("{$x @a :f}", "before every attribute"),
            ("{$x@a}", "before the attribute"),
            ("{$x !}", "a function, or an attribute"),
            ("{#foo\u200e\u200ebar=1}", "before the option"),
            ("{#foo@a}", "before the attribute"),
            ("{#foo!}", "close the markup"),
            ("{:foo\u200e\u200ebar=1}", "before the option"),
            ("{:f a}", "after the option name")
        ];

        foreach((string source, string fragment) in cases)
        {
            MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError(source);

            Assert.Contains(fragment, diagnostic.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>Every top-level message, pattern, quoted-pattern, declaration and matcher production names the specific grammar rule it enforces in its diagnostic message, at the first place a source can genuinely violate that rule.</summary>
    [TestMethod]
    public void MessageAndMatcherProductionsNameTheGrammarRuleTheyEnforce()
    {
        //Named killers, all in MessageParser.cs unless noted: each source below drives the parser
        //to the one Fail call named beside it as the first syntax error a well-formed prefix can
        //reach; a String mutation blanking that call's message argument empties diagnostic.Message
        //(or, where noted, an AND/OR swap routes execution through a different Fail call entirely),
        //so Assert.Contains below sees it.
        //  "a}"              line 145 ParseSimpleMessage's trailing-content guard (M-114)
        //  ".{"              line 228's quoted-pattern-vs-matcher lookahead in ParseComplexBody: an
        //                    AND-to-OR swap wrongly enters ParseQuotedPattern, whose own '{' guard
        //                    fails with a different message (M-115), and line 245's catch-all,
        //                    reached only when the lookahead is correctly false (M-118)
        //  ".match $x 1 x"   line 254 ParseQuotedPattern's first '{' (reached, unlike from
        //                    ParseComplexBody's already-brace-guaranteed call, with no guarantee of
        //                    '{' from ParseVariant's call: M-119)
        //  ".match $x 1 {a}" line 255 ParseQuotedPattern's second '{' (M-120)
        //  "{{a"             line 267 ParseQuotedPattern's first '}' (M-124)
        //  "{{a}"            line 268 ParseQuotedPattern's second '}' (M-125)
        //  "a\uD800"         line 324 ParsePattern's invalid-scalar Fail, the very first Fail call
        //                    reached since the leading SkipOptionalWhitespace never inspects past
        //                    the leading 'a' (M-132)
        //  ".input x"        MessageParser.Declarations.cs line 42, ParseVariableExpressionOnly's
        //                    own '{' guard (M-141)
        //  ".local$x"        MessageParser.Declarations.cs line 81, ParseLocalDeclaration's
        //                    RequireWhitespace right after the keyword (M-150)
        //  ".local $x #"     MessageParser.Declarations.cs line 98, ParseLocalDeclaration's '='
        //                    guard (M-155)
        //  ".match$x"        MessageParser.Matcher.cs line 27, ParseMatcher's RequireWhitespace
        //                    before a selector (M-267)
        //  ".match $x*"      MessageParser.Matcher.cs line 50, ParseMatcher's RequireWhitespace
        //                    before a variant (M-270)
        (string source, string fragment)[] cases =
        [
            ("a}", "the end of the message"),
            (".{", "an input or local declaration, a quoted pattern, or '.match'"),
            (".match $x 1 x", "start a quoted pattern"),
            (".match $x 1 {a}", "a second '{' to start a quoted pattern"),
            ("{{a", "close the quoted pattern"),
            ("{{a}", "a second '}' to close the quoted pattern"),
            ("a\uD800", "pattern text, an escape, or a placeholder"),
            (".input x", "'{' to start a variable expression"),
            (".local$x", "whitespace after '.local'"),
            (".local $x #", "'=' after the local variable"),
            (".match$x", "whitespace before a selector"),
            (".match $x*", "whitespace before a variant")
        ];

        foreach((string source, string fragment) in cases)
        {
            MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError(source);

            Assert.Contains(fragment, diagnostic.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>Every fail site in <c>MessageParser.Literals.cs</c> and <c>MessageParser.Scanning.cs</c> reports both the exact offset and the exact message text of the grammar rule it enforces.</summary>
    [TestMethod]
    public void EveryFailSiteReportsItsExactOffsetAndMessage()
    {
        //Named killers: each source below drives the parser to the one Fail call named beside it,
        //exercising a String mutation that blanks that call's message argument (caught by the
        //exact-message assertion below) and, where noted, a Statement mutation that drops the Fail
        //call entirely while a structural failure still propagates a null Message with _syntaxError
        //left null; ParsesToOneSyntaxError calls MessageFormatReader.TryParse with no try/catch, so
        //that mutant is caught by an unhandled MessageParser.Parse -> MessageDataModelValidator.Validate
        //ArgumentNullException instead of a clean single diagnostic.
        //  "{$}"              Literals.cs:45  ParseName's first-scalar check (M-243)
        //  "{$abc\uD800}"     Literals.cs:58  ParseName's loop Invalid-status check (M-244)
        //  ".input {y}"       Literals.cs:117 ParseVariable's ConsumeChar('$'), reached from
        //                     ParseVariableExpressionOnly with no preceding '$' check (M-251)
        //  "{|abc\uD800def|}" Literals.cs:185 ParseQuotedLiteral's non-pipe/backslash/escape
        //                     check (M-260)
        //  "{a\uD800}"        Literals.cs:214 ParseUnquotedLiteral's Invalid-status check; its
        //                     Statement mutation (M-263) crashes instead of failing cleanly, its
        //                     String mutation (M-264) blanks the message
        //  "{$x @a=}"         Literals.cs:229 ParseUnquotedLiteral's zero-consumed check (M-266)
        //  "{|\x|}"           Literals.cs:27  ParseEscapeChar's unknown-target check, reached from
        //                     inside a quoted literal (unlike
        //                     UnknownEscapeMessageNamesTheFourValidEscapeTargetsWithASingleBackslash
        //                     above, which reaches it from plain text); ParseQuotedLiteral's own
        //                     Statement mutation right after the call (M-258, Literals.cs:173) would
        //                     crash on a null `escaped.Value` instead of failing cleanly
        //  "\0abc"            Scanning.cs:143 SkipOptionalWhitespaceTrackingRealWhitespace's
        //                     Invalid-status check, the first check ParseMessage makes; its Statement
        //                     mutation (M-294) leaves a different Fail call, in ParsePattern's own
        //                     simple-message retry, to report a different message at the same
        //                     offset; its String mutation (M-295) blanks this one's message
        //  ".local\0"         Scanning.cs:185 RequireWhitespace's bidi-loop Invalid-status check,
        //                     reached from ParseLocalDeclaration right after '.local'; a Block
        //                     removal on PeekScalar's own NUL handling (M-289, Scanning.cs:54)
        //                     reaches this same offset through the generic branch below instead,
        //                     with the wrong "found '\0'" text; this check's own Statement mutation
        //                     (M-298) crashes instead of failing cleanly, its String mutation
        //                     (M-299) blanks the message
        //  ".match$a"         Scanning.cs:201 RequireWhitespace's ws-required check, reached from
        //                     ParseMatcher right after '.match'; an OR-to-AND mutation on the
        //                     condition guarding it (M-304, Scanning.cs:199) skips the check
        //                     entirely and reports a different message at a different offset
        //                     instead; this call's own String mutation (M-305) blanks the message
        //  "{$"               Scanning.cs:305 FoundSuffix's EndOfInput case, reached through
        //                     ParseName's first-scalar check finding the end of input (M-311)
        //  "{$\uD800}"        Scanning.cs:306 FoundSuffix's Invalid case, reached the same way
        //                     but finding an unpaired surrogate instead (M-312)
        (string source, int offset, string message)[] cases =
        [
            ("{$}", 2, "Expected a name but found '}'."),
            ("{$abc\uD800}", 5, "Expected a name character but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted)."),
            (".input {y}", 8, "Expected '$' to start a variable but found 'y'."),
            ("{|abc\uD800def|}", 5, "Expected quoted literal text, an escape, or the closing '|' but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted)."),
            ("{a\uD800}", 2, "Expected a literal but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted)."),
            ("{$x @a=}", 7, "Expected a literal but found '}'."),
            ("{|\\x|}", 3, "Expected '\\', '{', '|', or '}' after the escaping backslash but found 'x'."),
            (char.MinValue + "abc", 0, "Expected whitespace, a bidi mark, or the next token but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted)."),
            (".local" + char.MinValue, 6, "Expected whitespace after '.local' but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted)."),
            (".match$a", 6, "Expected whitespace before a selector but found '$'."),
            ("{$", 2, "Expected a name but reached the end of the message."),
            ("{$\uD800}", 2, "Expected a name but found an invalid code point (an unpaired surrogate, or U+0000, which is never permitted).")
        ];

        foreach((string source, int offset, string message) in cases)
        {
            MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError(source);

            Assert.AreEqual(offset, diagnostic.Offset, source);
            Assert.AreEqual(message, diagnostic.Message, source);
        }
    }
}
