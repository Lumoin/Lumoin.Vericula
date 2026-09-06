using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Hand-written tests asserting the exact data-model shape <see cref="MessageFormatReader.TryParse(string)"/>
/// builds for one representative source per grammar production. Field-by-field rather than whole-
/// record comparison throughout, because <see cref="System.Collections.Immutable.ImmutableArray{T}"/>'s
/// own equality compares the underlying array by reference, not by content, so comparing two
/// separately-built model graphs with <c>Assert.AreEqual</c> would not do what it looks like it does.
/// </summary>
[TestClass]
public sealed class MessageParserModelShapeTests
{
    /// <summary>Parses <paramref name="source"/> and asserts it succeeded with no diagnostic, returning the model as a <see cref="PatternMessage"/>.</summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The parsed pattern message.</returns>
    private static PatternMessage ParsesAsPatternMessage(string source)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.HasCount(0, result.Diagnostics);
        var message = result.Message as PatternMessage;
        Assert.IsNotNull(message);

        return message;
    }

    /// <summary>Parses <paramref name="source"/> and asserts it succeeded with no diagnostic, returning the model as a <see cref="SelectMessage"/>.</summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The parsed select message.</returns>
    private static SelectMessage ParsesAsSelectMessage(string source)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.HasCount(0, result.Diagnostics);
        var message = result.Message as SelectMessage;
        Assert.IsNotNull(message);

        return message;
    }

    /// <summary>Adjacent text and escapes in a simple message fold into exactly one <see cref="TextPart"/>, with the escapes cooked.</summary>
    [TestMethod]
    public void SimpleMessageWithEscapesFoldsIntoOneTextPart()
    {
        PatternMessage message = ParsesAsPatternMessage("""hello \{world\} and \|pipe\|""");

        Assert.HasCount(0, message.Declarations);
        Assert.HasCount(1, message.Pattern.Parts);
        var text = message.Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("hello {world} and |pipe|", text.Text);
    }

    /// <summary>A simple message's leading and trailing whitespace is ordinary pattern text, not stripped.</summary>
    [TestMethod]
    public void SimpleMessageKeepsLeadingAndTrailingWhitespaceAsText()
    {
        PatternMessage message = ParsesAsPatternMessage("   padded text   ");

        Assert.HasCount(1, message.Pattern.Parts);
        var text = message.Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("   padded text   ", text.Text);
    }

    /// <summary>A simple message may start with a bidi mark immediately followed by '.', with the bidi mark and the '.' both ordinary pattern text.</summary>
    [TestMethod]
    public void SimpleMessageMayStartWithABidiMarkFollowedByAFullStop()
    {
        //Named killer: MessageParser.cs, ParseMessage's bidi-then-'.' retry (the `sawBidi` check and
        //the reset-and-retry it guards). Removing either one treats a failed complex attempt on
        //"‎.hello" as final: TryParse returns a null Message with one VFX200 instead of this
        //valid PatternMessage, and the assertions below see the difference.
        PatternMessage message = ParsesAsPatternMessage("‎.hello");

        Assert.HasCount(0, message.Declarations);
        Assert.HasCount(1, message.Pattern.Parts);
        var text = message.Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("‎.hello", text.Text);
    }

    /// <summary>The optional whitespace surrounding a complex message's quoted pattern is consumed by <c>o</c>, not carried into the pattern's own text.</summary>
    [TestMethod]
    public void ComplexMessageDropsWhitespaceSurroundingTheQuotedPattern()
    {
        PatternMessage message = ParsesAsPatternMessage("  {{hello}}  ");

        Assert.HasCount(0, message.Declarations);
        Assert.HasCount(1, message.Pattern.Parts);
        var text = message.Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("hello", text.Text);
    }

    /// <summary>A literal expression with no function and no attributes.</summary>
    [TestMethod]
    public void ALiteralExpressionWithoutAFunctionKeepsItsValue()
    {
        PatternMessage message = ParsesAsPatternMessage("{|hello world|}");

        Assert.HasCount(1, message.Pattern.Parts);
        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as LiteralExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("hello world", expression.Literal.Value);
        Assert.IsNull(expression.Function);
        Assert.HasCount(0, expression.Attributes);
    }

    /// <summary>A variable expression with a function whose options carry both a literal and a variable value.</summary>
    [TestMethod]
    public void AVariableExpressionsFunctionKeepsLiteralAndVariableOptionValues()
    {
        PatternMessage message = ParsesAsPatternMessage("{$amount :number minimumFractionDigits=2 currency=$curr}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("amount", expression.Variable.Name);
        Assert.IsNotNull(expression.Function);
        Assert.AreEqual("number", expression.Function.Name);
        Assert.HasCount(2, expression.Function.Options);

        Assert.AreEqual("minimumFractionDigits", expression.Function.Options[0].Name);
        var fractionDigits = expression.Function.Options[0].Value as Literal;
        Assert.IsNotNull(fractionDigits);
        Assert.AreEqual("2", fractionDigits.Value);

        Assert.AreEqual("currency", expression.Function.Options[1].Name);
        var currency = expression.Function.Options[1].Value as Variable;
        Assert.IsNotNull(currency);
        Assert.AreEqual("curr", currency.Name);
    }

    /// <summary>A bare function expression, with no operand.</summary>
    [TestMethod]
    public void ABareFunctionExpressionHasNoOperandAndNoOptions()
    {
        PatternMessage message = ParsesAsPatternMessage("{:now}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as FunctionExpression;
        Assert.IsNotNull(expression);
        FunctionRef? function = expression.Function;
        Assert.IsNotNull(function);
        Assert.AreEqual("now", function.Name);
        Assert.HasCount(0, function.Options);
    }

    /// <summary>An expression's attributes, one valueless and one with a literal value, in source order.</summary>
    [TestMethod]
    public void KeepsAttributesValuelessAndValuedInSourceOrder()
    {
        PatternMessage message = ParsesAsPatternMessage("{$x @valueless @withvalue=|val|}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.HasCount(2, expression.Attributes);

        Assert.AreEqual("valueless", expression.Attributes[0].Name);
        Assert.IsNull(expression.Attributes[0].Value);

        Assert.AreEqual("withvalue", expression.Attributes[1].Name);
        Literal? withValue = expression.Attributes[1].Value;
        Assert.IsNotNull(withValue);
        Assert.AreEqual("val", withValue.Value);
    }

    /// <summary>Duplicate attribute names are kept in source order, with no diagnostic: the parser does not deduplicate or reject a repeated attribute.</summary>
    [TestMethod]
    public void DuplicateAttributesAreKeptInSourceOrderWithoutADiagnostic()
    {
        MessageParseResult result = MessageFormatReader.TryParse("{$x @a @a=1}");

        Assert.HasCount(0, result.Diagnostics);
        var message = result.Message as PatternMessage;
        Assert.IsNotNull(message);
        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.HasCount(2, expression.Attributes);

        Assert.AreEqual("a", expression.Attributes[0].Name);
        Assert.IsNull(expression.Attributes[0].Value);

        Assert.AreEqual("a", expression.Attributes[1].Name);
        Literal? secondValue = expression.Attributes[1].Value;
        Assert.IsNotNull(secondValue);
        Assert.AreEqual("1", secondValue.Value);
    }

    /// <summary>Open and close markup wrap plain text, each carrying its element name.</summary>
    [TestMethod]
    public void MarkupOpenAndCloseWrapText()
    {
        PatternMessage message = ParsesAsPatternMessage("{#b}bold{/b}");

        Assert.HasCount(3, message.Pattern.Parts);

        var open = message.Pattern.Parts[0] as MarkupPart;
        Assert.IsNotNull(open);
        Assert.AreEqual(MarkupKind.Open, open.Kind);
        Assert.AreEqual("b", open.Name);

        var text = message.Pattern.Parts[1] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("bold", text.Text);

        var close = message.Pattern.Parts[2] as MarkupPart;
        Assert.IsNotNull(close);
        Assert.AreEqual(MarkupKind.Close, close.Kind);
        Assert.AreEqual("b", close.Name);
    }

    /// <summary>Standalone markup with an option and an attribute.</summary>
    [TestMethod]
    public void StandaloneMarkupKeepsItsOptionAndItsAttribute()
    {
        PatternMessage message = ParsesAsPatternMessage("{#img src=|cat.png| @decorative/}");

        var markup = message.Pattern.Parts[0] as MarkupPart;
        Assert.IsNotNull(markup);
        Assert.AreEqual(MarkupKind.Standalone, markup.Kind);
        Assert.AreEqual("img", markup.Name);

        Assert.HasCount(1, markup.Options);
        Assert.AreEqual("src", markup.Options[0].Name);
        var src = markup.Options[0].Value as Literal;
        Assert.IsNotNull(src);
        Assert.AreEqual("cat.png", src.Value);

        Assert.HasCount(1, markup.Attributes);
        Assert.AreEqual("decorative", markup.Attributes[0].Name);
        Assert.IsNull(markup.Attributes[0].Value);
    }

    /// <summary>An input declaration and a local declaration referencing it, in source order.</summary>
    [TestMethod]
    public void KeepsAnInputDeclarationAndALocalDeclarationInSourceOrder()
    {
        PatternMessage message = ParsesAsPatternMessage(".input {$x :number} .local $y = {$x :number style=percent} {{{$y}}}");

        Assert.HasCount(2, message.Declarations);

        var input = message.Declarations[0] as InputDeclaration;
        Assert.IsNotNull(input);
        Assert.AreEqual("x", input.Name);
        Assert.AreEqual("x", input.Value.Variable.Name);
        Assert.IsNotNull(input.Value.Function);
        Assert.AreEqual("number", input.Value.Function.Name);

        var local = message.Declarations[1] as LocalDeclaration;
        Assert.IsNotNull(local);
        Assert.AreEqual("y", local.Name);
        var localValue = local.Value as VariableExpression;
        Assert.IsNotNull(localValue);
        Assert.AreEqual("x", localValue.Variable.Name);
        Assert.IsNotNull(localValue.Function);
        Assert.HasCount(1, localValue.Function.Options);
        Assert.AreEqual("style", localValue.Function.Options[0].Name);

        var body = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(body);
        var bodyExpression = body.Expression as VariableExpression;
        Assert.IsNotNull(bodyExpression);
        Assert.AreEqual("y", bodyExpression.Variable.Name);
    }

    /// <summary>
    /// A matcher with two selectors and four variants: two ordinary literal-versus-catch-all
    /// combinations, one whose first key is the quoted literal <c>|*|</c> (distinct from the unquoted
    /// catch-all <c>*</c>), plus a genuine catch-all-on-both-selectors variant, without which the
    /// message would have no fallback variant and so not be a valid data model.
    /// </summary>
    [TestMethod]
    public void AMatcherWithTwoSelectorsKeepsFourVariantsInSourceOrder()
    {
        SelectMessage message = ParsesAsSelectMessage(
            ".input {$a :number} .input {$b :number} .match $a $b "
            + "1 * {{one any}} "
            + "* 1 {{any one}} "
            + "|*| * {{literal-star any}} "
            + "* * {{fallback}}");

        Assert.HasCount(2, message.Selectors);
        Assert.AreEqual("a", message.Selectors[0].Name);
        Assert.AreEqual("b", message.Selectors[1].Name);

        Assert.HasCount(4, message.Variants);

        Assert.HasCount(2, message.Variants[0].Keys);
        var firstKey = message.Variants[0].Keys[0] as LiteralKey;
        Assert.IsNotNull(firstKey);
        Assert.AreEqual("1", firstKey.Literal.Value);
        Assert.IsInstanceOfType<CatchallKey>(message.Variants[0].Keys[1]);
        var firstPattern = message.Variants[0].Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(firstPattern);
        Assert.AreEqual("one any", firstPattern.Text);

        Assert.IsInstanceOfType<CatchallKey>(message.Variants[1].Keys[0]);
        var secondKey = message.Variants[1].Keys[1] as LiteralKey;
        Assert.IsNotNull(secondKey);
        Assert.AreEqual("1", secondKey.Literal.Value);

        var thirdKey = message.Variants[2].Keys[0] as LiteralKey;
        Assert.IsNotNull(thirdKey);
        Assert.AreEqual("*", thirdKey.Literal.Value);
        Assert.IsInstanceOfType<CatchallKey>(message.Variants[2].Keys[1]);
        var thirdPattern = message.Variants[2].Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(thirdPattern);
        Assert.AreEqual("literal-star any", thirdPattern.Text);

        Assert.IsInstanceOfType<CatchallKey>(message.Variants[3].Keys[0]);
        Assert.IsInstanceOfType<CatchallKey>(message.Variants[3].Keys[1]);
    }

    /// <summary>A namespaced identifier is kept whole, as <c>namespace:name</c>, without the introducing sigil.</summary>
    [TestMethod]
    public void NamespacedIdentifierKeptWhole()
    {
        PatternMessage message = ParsesAsPatternMessage("{:u:datetime}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as FunctionExpression;
        Assert.IsNotNull(expression);
        FunctionRef? function = expression.Function;
        Assert.IsNotNull(function);
        Assert.AreEqual("u:datetime", function.Name);
    }

    /// <summary>A name written with a combining mark is stored NFC-normalized (the precomposed form), not as the two separate code points the source wrote.</summary>
    [TestMethod]
    public void NameWithCombiningMarkStoredNfcNormalized()
    {
        PatternMessage message = ParsesAsPatternMessage("{$é}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);

        //"e" (U+0065) + COMBINING ACUTE ACCENT (U+0301) NFC-normalizes to the single precomposed
        //"é" (U+00E9); the raw two-code-point form must not survive into the model.
        Assert.AreEqual("é", expression.Variable.Name);
    }

    /// <summary>A quoted literal's value is kept verbatim: escapes processed, otherwise unaltered, and never NFC-normalized the way a name's value is.</summary>
    [TestMethod]
    public void QuotedLiteralValueIsKeptVerbatimNotNfcNormalized()
    {
        //"e" (U+0065) followed by COMBINING ACUTE ACCENT (U+0301) NFC-normalizes to the single
        //precomposed "é" (U+00E9), exactly as in NameWithCombiningMarkStoredNfcNormalized above; but
        //a literal's value is never normalized, so the two-code-unit decomposed form must survive here.
        PatternMessage message = ParsesAsPatternMessage("{|é|}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as LiteralExpression;
        Assert.IsNotNull(expression);
        Assert.HasCount(2, expression.Literal.Value);
        Assert.AreEqual("é", expression.Literal.Value);
    }
    /// <summary>Bidi marks surrounding a name are stripped from the stored value.</summary>
    [TestMethod]
    public void BidiMarksAroundAVariableNameAreStripped()
    {
        PatternMessage message = ParsesAsPatternMessage("{$‎name‎}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("name", expression.Variable.Name);
    }

    /// <summary>Bidi marks inside a quoted literal are ordinary content and are kept, unlike the marks surrounding a name.</summary>
    [TestMethod]
    public void BidiMarksInsideAQuotedLiteralAreKept()
    {
        PatternMessage message = ParsesAsPatternMessage("{|‎text‎|}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as LiteralExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("‎text‎", expression.Literal.Value);
    }

    /// <summary>A trailing bidi mark after a namespace name is consumed before the colon that separates it from the name half of a namespaced identifier.</summary>
    [TestMethod]
    public void TrailingBidiMarkAfterANamespaceNameIsConsumedBeforeTheColon()
    {
        //Named killer: MessageParser.Literals.cs line 73, deleting ParseName's trailing
        //ConsumeOptionalSingleBidi() call (the one after the name-char loop). Without it,
        //ParseIdentifier's raw PeekCharRaw() != Colon check (no whitespace/bidi skip) sees the bidi
        //mark left at the cursor instead of ':', returns just "ns" for the namespace half, and
        //leaves the mark unconsumed; TryParseFunctionAndAttributesTail's own whitespace-skip then
        //eats it and finds ':' with a function already set, failing with "at most one function"
        //instead of succeeding, so the assertions below (a valid FunctionExpression) see it (M-246).
        PatternMessage message = ParsesAsPatternMessage("{:ns‎:name}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as FunctionExpression;
        Assert.IsNotNull(expression);
        FunctionRef? function = expression.Function;
        Assert.IsNotNull(function);
        Assert.AreEqual("ns:name", function.Name);
    }

    /// <summary>An escaped character inside a quoted literal is appended to the literal's value.</summary>
    [TestMethod]
    public void EscapedCharacterInAQuotedLiteralIsAppendedToItsValue()
    {
        //Named killer: MessageParser.Literals.cs line 177, ParseQuotedLiteral's
        //`text.Append(escaped.Value);` call right after a successful escape. Removing it silently
        //drops the escaped character instead of appending it, so this quoted literal's value would
        //come out empty instead of holding the escaped '{' (M-259).
        PatternMessage message = ParsesAsPatternMessage("{|\\{|}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as LiteralExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("{", expression.Literal.Value);
    }

    /// <summary>IDEOGRAPHIC SPACE (U+3000) is accepted wherever <c>ws</c> is required, such as after the <c>.local</c> keyword.</summary>
    [TestMethod]
    public void IdeographicSpaceAcceptedAsWhitespace()
    {
        PatternMessage message = ParsesAsPatternMessage(".local　$x = {1} {{{$x}}}");

        Assert.HasCount(1, message.Declarations);
        var local = message.Declarations[0] as LocalDeclaration;
        Assert.IsNotNull(local);
        Assert.AreEqual("x", local.Name);
    }

    /// <summary>An empty source is a valid simple message with an empty pattern.</summary>
    [TestMethod]
    public void EmptyMessageHasAnEmptyPattern()
    {
        PatternMessage message = ParsesAsPatternMessage("");

        Assert.HasCount(0, message.Declarations);
        Assert.HasCount(0, message.Pattern.Parts);
    }

    /// <summary>An empty quoted pattern (<c>{{}}</c>) is a valid complex message with an empty pattern.</summary>
    [TestMethod]
    public void EmptyQuotedPatternHasAnEmptyPattern()
    {
        PatternMessage message = ParsesAsPatternMessage("{{}}");

        Assert.HasCount(0, message.Declarations);
        Assert.HasCount(0, message.Pattern.Parts);
    }

    /// <summary>The uppercase ASCII letter 'Z', the upper end of the name-start 'A'-'Z' range, starts a name.</summary>
    [TestMethod]
    public void UppercaseZIsAValidNameStartCharacter()
    {
        //Named killer: MessageFormatCharacterClasses.cs line 43, IsNameStart's 'A'-'Z' range upper
        //bound. Changing <= 'Z' to <= 'Y' excludes 'Z' from name-start; no other test's source names
        //anything starting with 'Z', so that mutant survived until this test's variable name (which
        //starts with 'Z') made the parse fail and this assertion (that it parses) see it.
        PatternMessage message = ParsesAsPatternMessage("{$Zulu}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("Zulu", expression.Variable.Name);
    }

    /// <summary>The underscore sigil is a valid name-start character.</summary>
    [TestMethod]
    public void UnderscoreIsAValidNameStartCharacter()
    {
        //Named killer: MessageFormatCharacterClasses.cs line 44, IsNameStart's 0x5F ('_') literal.
        //Changing it to 0x60 excludes '_' from name-start; no other test's source names anything
        //starting with '_', so that mutant survived until this test's variable name (which starts
        //with '_') made the parse fail and this assertion (that it parses) see it.
        PatternMessage message = ParsesAsPatternMessage("{$_private}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("_private", expression.Variable.Name);
    }

    /// <summary>LEFT SQUARE BRACKET (<c>[</c>, U+005B) is ordinary text-char content, the upper end of <c>text-char</c>'s first range.</summary>
    [TestMethod]
    public void LeftSquareBracketIsOrdinaryText()
    {
        //Named killer: MessageFormatCharacterClasses.cs line 88, IsTextChar's first range upper
        //bound. Changing <= 0x5B to <= 0x5A excludes '[' from text-char; no other test's source has
        //a '[' in plain text, so that mutant survived until this test's source (which does) made the
        //pattern stop early and the part-count assertion below see it.
        PatternMessage message = ParsesAsPatternMessage("array[0]");

        Assert.HasCount(1, message.Pattern.Parts);
        var text = message.Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("array[0]", text.Text);
    }

    /// <summary>A quoted literal may contain an unescaped LEFT CURLY BRACKET (<c>{</c>, U+007B), the upper end of <c>quoted-char</c>'s second range.</summary>
    [TestMethod]
    public void QuotedLiteralAllowsAnUnescapedOpenBrace()
    {
        //Named killer: MessageFormatCharacterClasses.cs line 104, IsQuotedChar's second range upper
        //bound. Changing <= 0x7B to <= 0x7A excludes '{' from quoted-char; no other test's quoted
        //literal has an unescaped '{', so that mutant survived until this test's literal (which
        //does) made the quoted literal fail to close and the value assertion below see it.
        PatternMessage message = ParsesAsPatternMessage("{|a{b|}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as LiteralExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual("a{b", expression.Literal.Value);
    }

    /// <summary>A supplementary-plane scalar value (encoded as a UTF-16 surrogate pair, two code units wide) is a valid name-start character and the whole pair is consumed as one name.</summary>
    [TestMethod]
    public void SupplementaryPlaneScalarIsAValidNameStartCharacter()
    {
        //Named killer: MessageParser.Literals.cs line 51, ParseName's advance past the name-start
        //scalar. Changing _index += width to _index += 1 advances by only one code unit for a
        //two-code-unit scalar, leaving the cursor on the dangling low surrogate; no other test's
        //name starts with a supplementary-plane character, so that mutant survived until this test's
        //variable name (which does) made the parse fail on the stranded surrogate and the assertion
        //below see it.
        string grinningFace = char.ConvertFromUtf32(0x1F600);
        PatternMessage message = ParsesAsPatternMessage($"{{${grinningFace}}}");

        var part = message.Pattern.Parts[0] as ExpressionPart;
        Assert.IsNotNull(part);
        var expression = part.Expression as VariableExpression;
        Assert.IsNotNull(expression);
        Assert.AreEqual(grinningFace, expression.Variable.Name);
    }

    /// <summary>Every one of <c>MessageFormatCharacterClasses.IsNameStart</c>'s declared range boundaries (lines 45-71) accepts its own edge scalar value as a name-start character.</summary>
    [TestMethod]
    public void NameStartAcceptsEachDeclaredRangeBoundary()
    {
        //Named killers, all in MessageFormatCharacterClasses.cs IsNameStart (lines 45-71). Each value
        //below sits on exactly one declared range's own edge; mutating that edge's comparison (<= to
        //<, or >= to >) or flipping the arm's own =>true to =>false excludes the scalar from
        //name-start, so "{$<scalar>b}" fails to parse where it must succeed, and the loop's
        //assertions (a valid PatternMessage whose sole part's Variable.Name is "<scalar>b") see it.
        //  U+061B   line 45 <=0x61B (M-019)
        //  U+167F   line 46 <=0x167F (M-020); its =>false arm mutant (M-021) too
        //  U+1FFF   line 47 <=0x1FFF (M-022)
        //  U+2027   line 49 <=0x2027 (M-023); its =>false arm mutant (M-024) too
        //  U+205E   line 50 <=0x205E (M-025); its =>false arm mutant (M-026) too
        //  U+2065   line 51 <=0x2065 (M-027); its =>false arm mutant (M-028) too
        //  U+2FFF   line 52 <=0x2FFF (M-029); its =>false arm mutant (M-030) too
        //  U+D7FF   line 53 <=0xD7FF (M-031); its =>false arm mutant (M-032) too
        //  U+FDCF   line 54 <=0xFDCF (M-033); its =>false arm mutant (M-034) too
        //  U+FFFD   line 55 <=0xFFFD (M-035); its =>false arm mutant (M-036) too
        //  U+10000  line 56 >=0x10000 (M-037)
        //  U+1FFFD  line 56 <=0x1FFFD (M-038); its =>false arm mutant (M-039) too
        //  U+20000  line 57 >=0x20000 (M-040)
        //  U+2FFFD  line 57 <=0x2FFFD (M-041); its =>false arm mutant (M-042) too
        //  U+30000  line 58 >=0x30000 (M-043)
        //  U+3FFFD  line 58 <=0x3FFFD (M-044); its =>false arm mutant (M-045) too
        //  U+40000  line 59 >=0x40000 (M-046)
        //  U+4FFFD  line 59 <=0x4FFFD (M-047); its =>false arm mutant (M-048) too
        //  U+50000  line 60 >=0x50000 (M-049)
        //  U+5FFFD  line 60 <=0x5FFFD (M-050); its =>false arm mutant (M-051) too
        //  U+60000  line 61 >=0x60000 (M-052)
        //  U+6FFFD  line 61 <=0x6FFFD (M-053); its =>false arm mutant (M-054) too
        //  U+70000  line 62 >=0x70000 (M-055)
        //  U+7FFFD  line 62 <=0x7FFFD (M-056); its =>false arm mutant (M-057) too
        //  U+80000  line 63 >=0x80000 (M-058)
        //  U+8FFFD  line 63 <=0x8FFFD (M-059); its =>false arm mutant (M-060) too
        //  U+90000  line 64 >=0x90000 (M-061)
        //  U+9FFFD  line 64 <=0x9FFFD (M-062); its =>false arm mutant (M-063) too
        //  U+A0000  line 65 >=0xA0000 (M-064)
        //  U+AFFFD  line 65 <=0xAFFFD (M-065); its =>false arm mutant (M-066) too
        //  U+B0000  line 66 >=0xB0000 (M-067)
        //  U+BFFFD  line 66 <=0xBFFFD (M-068); its =>false arm mutant (M-069) too
        //  U+C0000  line 67 >=0xC0000 (M-070)
        //  U+CFFFD  line 67 <=0xCFFFD (M-071); its =>false arm mutant (M-072) too
        //  U+D0000  line 68 >=0xD0000 (M-073)
        //  U+DFFFD  line 68 <=0xDFFFD (M-074); its =>false arm mutant (M-075) too
        //  U+E0000  line 69 >=0xE0000 (M-076)
        //  U+EFFFD  line 69 <=0xEFFFD (M-077); its =>false arm mutant (M-078) too
        //  U+F0000  line 70 >=0xF0000 (M-079)
        //  U+FFFFD  line 70 <=0xFFFFD (M-080); its =>false arm mutant (M-081) too
        //  U+100000 line 71 >=0x100000 (M-082)
        //  U+10FFFD line 71 <=0x10FFFD, both its >0x10FFFD (M-083) and <0x10FFFD (M-084) mutants, and
        //           its =>false arm mutant (M-085)
        int[] boundaries =
        [
            0x061B, 0x167F, 0x1FFF, 0x2027, 0x205E, 0x2065, 0x2FFF, 0xD7FF, 0xFDCF, 0xFFFD,
            0x10000, 0x1FFFD, 0x20000, 0x2FFFD, 0x30000, 0x3FFFD, 0x40000, 0x4FFFD,
            0x50000, 0x5FFFD, 0x60000, 0x6FFFD, 0x70000, 0x7FFFD, 0x80000, 0x8FFFD,
            0x90000, 0x9FFFD, 0xA0000, 0xAFFFD, 0xB0000, 0xBFFFD, 0xC0000, 0xCFFFD,
            0xD0000, 0xDFFFD, 0xE0000, 0xEFFFD, 0xF0000, 0xFFFFD, 0x100000, 0x10FFFD
        ];

        foreach(int scalarValue in boundaries)
        {
            string description = $"U+{scalarValue:X}";
            string name = char.ConvertFromUtf32(scalarValue) + "b";
            MessageParseResult result = MessageFormatReader.TryParse($"{{${name}}}");

            Assert.HasCount(0, result.Diagnostics, description);
            var message = result.Message as PatternMessage;
            Assert.IsNotNull(message, description);
            var part = message.Pattern.Parts[0] as ExpressionPart;
            Assert.IsNotNull(part, description);
            var expression = part.Expression as VariableExpression;
            Assert.IsNotNull(expression, description);
            Assert.AreEqual(name, expression.Variable.Name, description);
        }
    }

    /// <summary>Every one of <c>MessageFormatCharacterClasses.IsTextChar</c>'s declared range boundaries (lines 88-91) accepts its own edge scalar value as ordinary pattern text.</summary>
    [TestMethod]
    public void TextCharAcceptsEachDeclaredRangeBoundary()
    {
        //Named killers, all in MessageFormatCharacterClasses.cs IsTextChar:
        //  U+0001   line 88 >=0x01, the first range's own bottom edge (M-086). Mutating to >0x01
        //           rejects it; "ab" then stops the text scan early and the Text assertion sees it.
        //  U+007C   ('|') line 90's singleton arm (M-087, NoCoverage: no existing test has an
        //           unescaped '|' in plain text). Flipping =>true to =>false rejects it; "a|b" then
        //           stops the text scan at '|' and the Text assertion sees it.
        //  U+007E   ('~') line 91 >=0x7E, the last range's own bottom edge (M-088). Mutating to >0x7E
        //           rejects it; "a~b" then stops the text scan early and the Text assertion sees it.
        //  U+10FFFF line 91 <=0x10FFFF, the maximum scalar value and the last range's own top edge
        //           (M-089). Mutating to <0x10FFFF rejects it; the scan stops early and the Text
        //           assertion sees it.
        int[] boundaries = [0x0001, 0x007C, 0x007E, 0x10FFFF];

        foreach(int scalarValue in boundaries)
        {
            string description = $"U+{scalarValue:X}";
            string expected = "a" + char.ConvertFromUtf32(scalarValue) + "b";
            MessageParseResult result = MessageFormatReader.TryParse(expected);

            Assert.HasCount(0, result.Diagnostics, description);
            var message = result.Message as PatternMessage;
            Assert.IsNotNull(message, description);
            Assert.HasCount(1, message.Pattern.Parts, description);
            var text = message.Pattern.Parts[0] as TextPart;
            Assert.IsNotNull(text, description);
            Assert.AreEqual(expected, text.Text, description);
        }
    }

    /// <summary>Every one of <c>MessageFormatCharacterClasses.IsQuotedChar</c>'s declared range boundaries (lines 103-105) accepts its own edge scalar value inside a quoted literal.</summary>
    [TestMethod]
    public void QuotedCharAcceptsEachDeclaredRangeBoundary()
    {
        //Named killers, all in MessageFormatCharacterClasses.cs IsQuotedChar:
        //  U+0001   line 103 >=0x01, the first range's own bottom edge (M-091). Mutating to >0x01
        //           rejects it; "{|ab|}" then fails the quoted-literal scan and the Value
        //           assertion sees it.
        //  U+005B   ('[') line 103 <=0x5B, the first range's own top edge (M-092). Mutating to <0x5B
        //           rejects it; "{|a[b|}" fails the scan at '[' and the Value assertion sees it.
        //  U+005D   (']') line 104 >=0x5D, the second range's own bottom edge (M-093; the second
        //           range's own top edge, '{', is already killed by QuotedLiteralAllowsAnUnescapedOpenBrace
        //           above). Mutating to >0x5D rejects it; "{|a]b|}" fails the scan at ']'.
        //  U+007D   ('}') line 105 >=0x7D, the third range's own bottom edge (M-094). Mutating to
        //           >0x7D rejects it; "{|a}b|}" fails the scan at '}'.
        //  U+10FFFF line 105 <=0x10FFFF, the maximum scalar value and the third range's own top edge
        //           (M-095). Mutating to <0x10FFFF rejects it; the scan fails at U+10FFFF.
        int[] boundaries = [0x0001, 0x005B, 0x005D, 0x007D, 0x10FFFF];

        foreach(int scalarValue in boundaries)
        {
            string description = $"U+{scalarValue:X}";
            string expectedValue = "a" + char.ConvertFromUtf32(scalarValue) + "b";
            MessageParseResult result = MessageFormatReader.TryParse($"{{|{expectedValue}|}}");

            Assert.HasCount(0, result.Diagnostics, description);
            var message = result.Message as PatternMessage;
            Assert.IsNotNull(message, description);
            var part = message.Pattern.Parts[0] as ExpressionPart;
            Assert.IsNotNull(part, description);
            var expression = part.Expression as LiteralExpression;
            Assert.IsNotNull(expression, description);
            Assert.AreEqual(expectedValue, expression.Literal.Value, description);
        }
    }

    /// <summary>The character right after a placeholder is scanned fresh, not mistaken for the placeholder's own stale opening '{'.</summary>
    [TestMethod]
    public void TreatsTheCharacterAfterAPlaceholderAsAFreshTokenNotTheStaleOpeningBrace()
    {
        //Named killer: MessageParser.cs line 305, removing the `continue;` right after
        //`parts.Add(part);` in ParsePattern's placeholder branch. Without it, the loop falls
        //through using the stale `c` read before the placeholder was parsed (still the opening
        //'{'), so the character that actually follows the placeholder never gets its own Backslash
        //check: "{$x}\{" places an escape right after a placeholder, and the mutant instead runs
        //IsTextChar('\\') (false) on it and breaks the loop early instead of consuming the "\{"
        //escape, turning a valid two-part pattern into a trailing-content syntax error; this test's
        //shape assertions see the difference (M-129).
        PatternMessage message = ParsesAsPatternMessage("{$x}\\{");

        Assert.HasCount(2, message.Pattern.Parts);
        Assert.IsInstanceOfType<ExpressionPart>(message.Pattern.Parts[0]);
        var text = message.Pattern.Parts[1] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("{", text.Text);
    }

    /// <summary>An input declaration's variable expression skips optional whitespace right after its own opening '{', per the grammar's <c>o</c> there (unlike <c>local-declaration</c>'s required <c>s</c> after the keyword).</summary>
    [TestMethod]
    public void InputDeclarationSkipsWhitespaceAfterTheVariableExpressionsOpeningBrace()
    {
        //Named killer: MessageParser.Declarations.cs line 48, deleting
        //ParseVariableExpressionOnly's SkipOptionalWhitespace() call right after its own '{'.
        //Without it, the space in "{ $x}" below leaves ParseVariable's ConsumeChar('$') looking at
        //' ' instead of '$', failing the whole message; this test's shape assertions see the
        //difference (M-143).
        PatternMessage message = ParsesAsPatternMessage(".input { $x} {{y}}");

        Assert.HasCount(1, message.Declarations);
        var input = message.Declarations[0] as InputDeclaration;
        Assert.IsNotNull(input);
        Assert.AreEqual("x", input.Name);
        Assert.HasCount(1, message.Pattern.Parts);
        var text = message.Pattern.Parts[0] as TextPart;
        Assert.IsNotNull(text);
        Assert.AreEqual("y", text.Text);
    }
}
