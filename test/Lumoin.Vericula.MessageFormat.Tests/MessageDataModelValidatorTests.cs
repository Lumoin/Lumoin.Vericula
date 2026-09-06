using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Hand-written tests for the data model checks <see cref="MessageFormatReader.TryParse(string)"/> runs
/// after a syntactically complete parse, covering cases the vendored suite does not exercise: more than
/// one error kind in a single message (two, and separately three), the no-cascade rule, normalization-
/// and quoting-only duplicate variants, a duplicate option in markup, in a declaration's own function
/// and in a variant pattern, the three self- and implicit-reference shapes of Duplicate Declaration (for
/// both a local and an input declaration), a multi-declaration selector chain (a valid one, one that
/// dead-ends, and one that cycles back on itself without ever reaching a function), an entirely
/// undeclared selector, a valid two-selector matcher with a genuine fallback, a missing fallback with
/// two selectors, multi-line diagnostic positions, and <see cref="MessageFormatException"/>'s
/// two-diagnostics-on-two-lines message.
/// </summary>
[TestClass]
public sealed class MessageDataModelValidatorTests
{
    /// <summary>Parses <paramref name="source"/> and asserts it reached a non-null model with exactly the given set of distinct diagnostic ids.</summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="expectedIds">The distinct diagnostic ids expected, in any order.</param>
    /// <returns>The parse result, for a test that needs to inspect more than just the id set.</returns>
    private static MessageParseResult AssertDataModelDiagnostics(string source, params string[] expectedIds)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.IsNotNull(result.Message, $"'{source}' was expected to parse to a model despite its data model diagnostics.");

        var actualIds = new HashSet<string>(result.Diagnostics.Select(static d => d.Id), StringComparer.Ordinal);
        var expected = new HashSet<string>(expectedIds, StringComparer.Ordinal);

        Assert.IsTrue(actualIds.SetEquals(expected),
            $"'{source}' reported [{string.Join(", ", actualIds.Order(StringComparer.Ordinal))}], expected [{string.Join(", ", expected.Order(StringComparer.Ordinal))}].");

        return result;
    }

    /// <summary>Parses <paramref name="source"/> and asserts it reached a valid model: non-null, with no diagnostics.</summary>
    /// <param name="source">The source text to parse.</param>
    private static void AssertValid(string source)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.IsNotNull(result.Message, $"'{source}' was expected to parse to a valid model.");
        Assert.IsTrue(result.Diagnostics.IsEmpty,
            $"'{source}' reported {result.Diagnostics.Length} unexpected diagnostic(s): {string.Join(", ", result.Diagnostics.Select(d => d.Id))}.");
    }

    /// <summary>A message with a duplicate declaration and a duplicate option name reports both, distinct diagnostics.</summary>
    [TestMethod]
    public void TwoDifferentDataModelErrorsInOneMessageAreBothReported()
    {
        AssertDataModelDiagnostics(
            ".input {$x} .input {$x} {{{:f opt=1 opt=2}}}",
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
            WellKnownMessageFormatDiagnostics.DuplicateOptionName);
    }

    /// <summary>
    /// A matcher whose single variant's key count mismatches the selector count, and which as a result
    /// has no remaining variant at all, reports only the mismatch: the no-cascade rule stops the
    /// mismatch from also being reported, spuriously, as a missing fallback variant.
    /// </summary>
    [TestMethod]
    public void AMismatchedVariantDoesNotAlsoTriggerMissingFallback()
    {
        AssertDataModelDiagnostics(
            ".input {$a :x} .input {$b :x} .match $a $b 1 2 3 {{v}}",
            WellKnownMessageFormatDiagnostics.VariantKeyMismatch);
    }

    /// <summary>A matcher with two selectors, both annotated, and a genuine all-catch-all variant among its others, is entirely valid.</summary>
    [TestMethod]
    public void ATwoSelectorMatcherWithAGenuineFallbackIsValid()
    {
        AssertValid(".input {$a :x} .input {$b :y} .match $a $b 1 2 {{v}} * * {{w}}");
    }

    /// <summary>Two variants whose keys differ only in Unicode normalization form (decomposed versus precomposed) are the same key, so a duplicate.</summary>
    [TestMethod]
    public void VariantsThatDifferOnlyInNormalizationFormAreDuplicates()
    {
        //U+1E0A U+0323 ("Ḋ" + combining dot below) and U+1E0C U+0307 ("Ḍ" + combining dot above) both
        //NFC-normalize to the same single precomposed code point sequence, so these two unquoted keys
        //are the same key under the spec's NFC comparison rule.
        AssertDataModelDiagnostics(
            ".local $x = {Ḍ̇ :string} .match $x Ḍ̇ {{a}} Ḍ̇ {{b}} * {{c}}",
            WellKnownMessageFormatDiagnostics.DuplicateVariant);
    }

    /// <summary>Two literal keys that differ only in letter case are distinct keys: the spec's key comparison is by exact string value, not case-insensitively.</summary>
    [TestMethod]
    public void VariantKeysThatDifferOnlyByCaseAreNotDuplicates()
    {
        //Named killer: MessageDataModelValidator.Matcher.cs line 113, KeysEqual's
        //StringComparison.Ordinal changed to StringComparison.OrdinalIgnoreCase. "Foo" and "foo" would
        //then compare equal, adding a spurious DuplicateVariant diagnostic that AssertValid's
        //zero-diagnostics expectation sees.
        AssertValid(".input {$x :s} .match $x Foo {{a}} foo {{b}} * {{c}}");
    }

    /// <summary>An unquoted key and a quoted key with the same string value are the same key, since keys compare by string value, not syntactic form.</summary>
    [TestMethod]
    public void VariantsThatDifferOnlyInQuotingStyleAreDuplicates()
    {
        AssertDataModelDiagnostics(
            ".input {$x :string} .match $x foo {{a}} |foo| {{b}} * {{c}}",
            WellKnownMessageFormatDiagnostics.DuplicateVariant);
    }

    /// <summary>Two options named "color" on the same markup element are a duplicate option name, exactly as they would be on a function call.</summary>
    [TestMethod]
    public void ReportsDuplicateOptionNameInMarkup()
    {
        AssertDataModelDiagnostics(
            "{#b color=red color=blue}text{/b}",
            WellKnownMessageFormatDiagnostics.DuplicateOptionName);
    }

    /// <summary>A local declaration whose function call passes its own bound variable as an option value refers to itself, a Duplicate Declaration error distinct from the plainer "operand is itself" shape.</summary>
    [TestMethod]
    public void ALocalDeclarationPassingItsOwnVariableAsAnOptionValueIsADuplicateDeclaration()
    {
        AssertDataModelDiagnostics(
            ".local $x = {1 :func opt=$x} {{_}}",
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration);
    }

    /// <summary>A variable used as an earlier declaration's option value is implicitly declared there; explicitly declaring it afterward redeclares it.</summary>
    [TestMethod]
    public void AVariableImplicitlyDeclaredByAnOptionValueIsRedeclaredByALaterExplicitInput()
    {
        AssertDataModelDiagnostics(
            ".local $foo = {1 :func opt=$bar} .input {$bar} {{_}}",
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration);
    }

    /// <summary>An input declaration whose own function call passes its own bound variable as an option value refers to itself, the input-declaration shape of self-reference: the mandatory operand (always the same name) is not itself the problem, the option value is.</summary>
    [TestMethod]
    public void InputDeclarationSelfReferenceThroughAnOptionValueIsADuplicateDeclaration()
    {
        AssertDataModelDiagnostics(
            ".input {$x :f opt=$x} {{_}}",
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration);
    }

    /// <summary>
    /// A declaration that is both an already-used name (rule 1) and self-referencing (rule 3) reports
    /// exactly one diagnostic, the first rule that applies, never two: <c>$b</c> is used by <c>$a</c>'s
    /// own expression (making <c>$b</c>'s own later declaration a rule-1 redeclaration) and its
    /// expression names itself (which would independently be a rule-3 self-reference).
    /// </summary>
    [TestMethod]
    public void ADeclarationThatIsBothAlreadyUsedAndSelfReferencingReportsExactlyOneDiagnostic()
    {
        MessageParseResult result = MessageFormatReader.TryParse(".local $a = {$b} .local $b = {$b} {{_}}");

        Assert.IsNotNull(result.Message);
        Assert.HasCount(1, result.Diagnostics);
        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsDuplicateDeclaration(result.Diagnostics[0].Id));
    }

    /// <summary>
    /// A selector that reaches, through three local declarations each rebinding to the next bare
    /// variable, an input declaration whose function is present, is validly annotated: the chain need
    /// not end in a local declaration, and its length is not itself a problem.
    /// </summary>
    [TestMethod]
    public void SelectorChainOfThreeLocalDeclarationsEndingInAFunctionBearingInputIsValid()
    {
        AssertValid(".input {$d :func} .local $c = {$d} .local $b = {$c} .local $a = {$b} .match $a 1 {{x}} * {{y}}");
    }

    /// <summary>The same three-local-declaration chain, but ending in an input declaration with no function, dead-ends: the selector is not annotated.</summary>
    [TestMethod]
    public void SelectorChainOfThreeLocalDeclarationsEndingInAFunctionlessInputIsAMiss()
    {
        AssertDataModelDiagnostics(
            ".input {$d} .local $c = {$d} .local $b = {$c} .local $a = {$b} .match $a 1 {{x}} * {{y}}",
            WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation);
    }

    /// <summary>
    /// Two local declarations that rebind to each other (<c>$a</c> to <c>$b</c>, <c>$b</c> to <c>$a</c>)
    /// form a cycle that never reaches a function: the selector chain walk detects the cycle and
    /// reports a miss, distinct from and alongside the ordinary Duplicate Declaration the second
    /// declaration's binding of <c>$b</c> (already used, as <c>$a</c>'s operand, by the first
    /// declaration) triggers on its own.
    /// </summary>
    [TestMethod]
    public void SelectorChainThatCyclesBackOnItselfIsAMissAndADuplicateDeclaration()
    {
        AssertDataModelDiagnostics(
            ".local $a = {$b} .local $b = {$a} .match $a * {{x}}",
            WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation,
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration);
    }

    /// <summary>
    /// When a name is declared twice, the selector chain walk consults the later declaration (the one
    /// that actually took effect), not the earlier one it superseded: redeclaring a function-less local
    /// as a function-bearing one still leaves the selector validly annotated, alongside the Duplicate
    /// Declaration the redeclaration itself is.
    /// </summary>
    [TestMethod]
    public void TheSelectorChainConsultsTheLaterOfTwoDeclarationsSharingAName()
    {
        //Named killer: MessageDataModelValidator.Matcher.cs line 134, DeclarationsByName's last-wins
        //overwrite (declarationsByName[declaration.Name] = declaration). Changing it to a first-wins
        //assignment (for example TryAdd) would make the selector chain consult the first, function-less
        //"$x" instead of the second, function-bearing one, adding a spurious MissingSelectorAnnotation
        //that AssertDataModelDiagnostics's exact-set expectation sees.
        AssertDataModelDiagnostics(
            ".local $x = {1} .local $x = {2 :f} .match $x 1 {{a}} * {{b}}",
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration);
    }

    /// <summary>A selector variable with no declaration at all (not even an undecorated one) is unannotated.</summary>
    [TestMethod]
    public void ASelectorThatIsNotDeclaredAtAllIsAMiss()
    {
        AssertDataModelDiagnostics(
            ".match $foo 1 {{a}} * {{b}}",
            WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation);
    }

    /// <summary>A duplicate option inside a variant's own pattern (not a declaration or the selector's own expression) is still caught.</summary>
    [TestMethod]
    public void ReportsADuplicateOptionInAVariantPatternsPlaceholder()
    {
        AssertDataModelDiagnostics(
            ".input {$x :string} .match $x 1 {{{:f opt=1 opt=2}}} * {{other}}",
            WellKnownMessageFormatDiagnostics.DuplicateOptionName);
    }

    /// <summary>A duplicate option on a declaration's own function call (not one reached through a placeholder or a variant pattern) is caught the same way.</summary>
    [TestMethod]
    public void ReportsADuplicateOptionInADeclarationsFunction()
    {
        AssertDataModelDiagnostics(
            ".local $x = {1 :func opt=1 opt=2} {{_}}",
            WellKnownMessageFormatDiagnostics.DuplicateOptionName);
    }

    /// <summary>A duplicate option on an input declaration's own function call is caught exactly like on a local declaration's, exercising the input arm of the same options walk.</summary>
    [TestMethod]
    public void ReportsADuplicateOptionOnAnInputDeclarationsFunction()
    {
        //Named killer: MessageDataModelValidator.cs line 76, FunctionOf's `InputDeclaration input =>
        //input.Value.Function` arm. Changing it to return null for an input declaration (the local arm
        //stays correct) would drop this input declaration's options from AllOptionLists entirely, so
        //the duplicate opt= pair would never be checked and no diagnostic would be reported, which
        //AssertDataModelDiagnostics's exact-set expectation sees.
        AssertDataModelDiagnostics(
            ".input {$x :f opt=1 opt=2} {{_}}",
            WellKnownMessageFormatDiagnostics.DuplicateOptionName);
    }

    /// <summary>
    /// A message combining a redeclared input, a duplicate option in a variant's placeholder, and a
    /// selector left unannotated (both the original and the redundant <c>.input {$x}</c> carry no
    /// function, so the selector chain walk misses regardless of which of the two declarations it
    /// consults) reports all three, distinct, error kinds together.
    /// </summary>
    [TestMethod]
    public void ThreeDifferentDataModelErrorsInOneMessageAreAllReported()
    {
        AssertDataModelDiagnostics(
            ".input {$x} .input {$x} .match $x 1 {{{:f opt=1 opt=2}}} * {{y}}",
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
            WellKnownMessageFormatDiagnostics.DuplicateOptionName,
            WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation);
    }

    /// <summary>
    /// A duplicate declaration on the second line of a three-line message reports the offset, line and
    /// position of that second declaration, not of the first (already-valid) one or of anything on the
    /// third line.
    /// </summary>
    [TestMethod]
    public void ReportsADataModelDiagnosticsPositionInAMultiLineMessage()
    {
        MessageParseResult result = MessageFormatReader.TryParse(".input {$x :func}\n.input {$x :func}\n{{_}}");

        Assert.IsNotNull(result.Message);
        Assert.HasCount(1, result.Diagnostics);
        MessageFormatDiagnostic diagnostic = result.Diagnostics[0];

        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsDuplicateDeclaration(diagnostic.Id));
        Assert.AreEqual(18, diagnostic.Offset);
        Assert.AreEqual(2, diagnostic.Line);
        Assert.AreEqual(1, diagnostic.Position);
    }

    /// <summary>A Duplicate Option Name diagnostic's offset is the start of the second, repeated option, not the first.</summary>
    [TestMethod]
    public void DuplicateOptionNamePositionPointsAtTheSecondOption()
    {
        const string source = "{42 :number style=percent style=decimal}";
        int expectedOffset = source.IndexOf("style=decimal", StringComparison.Ordinal);

        MessageParseResult result = AssertDataModelDiagnostics(source, WellKnownMessageFormatDiagnostics.DuplicateOptionName);
        MessageFormatDiagnostic diagnostic = result.Diagnostics[0];

        Assert.AreEqual(expectedOffset, diagnostic.Offset);
        Assert.AreEqual(1, diagnostic.Line);
        Assert.AreEqual(expectedOffset + 1, diagnostic.Position);
    }

    /// <summary>A Variant Key Mismatch diagnostic's offset is the start of the offending variant itself, not the matcher or the correctly sized variant before it.</summary>
    [TestMethod]
    public void VariantKeyMismatchPositionPointsAtTheOffendingVariant()
    {
        const string source = ".input {$a :x} .input {$b :x} .match $a $b * * {{ok}} 3 {{bad}}";
        int expectedOffset = source.IndexOf("3 {{bad}}", StringComparison.Ordinal);

        MessageParseResult result = AssertDataModelDiagnostics(source, WellKnownMessageFormatDiagnostics.VariantKeyMismatch);
        MessageFormatDiagnostic diagnostic = result.Diagnostics[0];

        Assert.AreEqual(expectedOffset, diagnostic.Offset);
        Assert.AreEqual(1, diagnostic.Line);
        Assert.AreEqual(expectedOffset + 1, diagnostic.Position);
    }

    /// <summary>A Missing Fallback Variant diagnostic's offset is the start of the <c>.match</c> keyword, blaming the matcher as a whole rather than any one variant.</summary>
    [TestMethod]
    public void MissingFallbackVariantPositionPointsAtTheMatchKeyword()
    {
        const string source = ".input {$a :x} .input {$b :x} .match $a $b 1 2 {{v}} 3 4 {{w}}";
        int expectedOffset = source.IndexOf(".match", StringComparison.Ordinal);

        MessageParseResult result = AssertDataModelDiagnostics(source, WellKnownMessageFormatDiagnostics.MissingFallbackVariant);
        MessageFormatDiagnostic diagnostic = result.Diagnostics[0];

        Assert.AreEqual(expectedOffset, diagnostic.Offset);
        Assert.AreEqual(1, diagnostic.Line);
        Assert.AreEqual(expectedOffset + 1, diagnostic.Position);
    }

    /// <summary>A Duplicate Variant diagnostic's offset is the start of the second, repeating variant, not the first.</summary>
    [TestMethod]
    public void DuplicateVariantPositionPointsAtTheSecondVariant()
    {
        const string source = ".input {$x :s} .match $x foo {{a}} foo {{b}} * {{c}}";
        int expectedOffset = source.IndexOf("foo {{b}}", StringComparison.Ordinal);

        MessageParseResult result = AssertDataModelDiagnostics(source, WellKnownMessageFormatDiagnostics.DuplicateVariant);
        MessageFormatDiagnostic diagnostic = result.Diagnostics[0];

        Assert.AreEqual(expectedOffset, diagnostic.Offset);
        Assert.AreEqual(1, diagnostic.Line);
        Assert.AreEqual(expectedOffset + 1, diagnostic.Position);
    }

    /// <summary>A Missing Selector Annotation diagnostic's offset is the start of the selector variable in the <c>.match</c> clause, not of its declaration earlier in the message.</summary>
    [TestMethod]
    public void MissingSelectorAnnotationPositionPointsAtTheSelectorNotItsDeclaration()
    {
        const string source = ".local $x = {|v|} .match $x 1 {{a}} * {{b}}";
        int expectedOffset = source.LastIndexOf("$x", StringComparison.Ordinal);

        MessageParseResult result = AssertDataModelDiagnostics(source, WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation);
        MessageFormatDiagnostic diagnostic = result.Diagnostics[0];

        Assert.AreEqual(expectedOffset, diagnostic.Offset);
        Assert.AreEqual(1, diagnostic.Line);
        Assert.AreEqual(expectedOffset + 1, diagnostic.Position);
        Assert.AreNotEqual(source.IndexOf("$x", StringComparison.Ordinal), diagnostic.Offset,
            "the declaration's own $x and the selector's $x are different occurrences; the diagnostic must point at the selector.");
    }

    /// <summary>
    /// <see cref="MessageFormatReader.Parse(string)"/> throws with a message that lists two data model
    /// diagnostics on two lines (joined by <see cref="Environment.NewLine"/>, as its implementation
    /// documents), and both diagnostics are present on <see cref="MessageFormatException.Diagnostics"/>.
    /// </summary>
    [TestMethod]
    public void ParseThrowsListingTwoDiagnosticsOnTwoLines()
    {
        MessageFormatException exception = Assert.ThrowsExactly<MessageFormatException>(
            () => MessageFormatReader.Parse(".input {$x} .input {$x} {{{:f opt=1 opt=2}}}"));

        Assert.HasCount(2, exception.Diagnostics);
        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsDuplicateDeclaration(exception.Diagnostics[0].Id));
        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsDuplicateOptionName(exception.Diagnostics[1].Id));

        string[] lines = exception.Message.Split(Environment.NewLine);
        Assert.HasCount(2, lines);
        Assert.Contains(
            $"{exception.Diagnostics[0].Id} at line {exception.Diagnostics[0].Line}, position {exception.Diagnostics[0].Position}: ",
            lines[0],
            StringComparison.Ordinal);
        Assert.Contains(
            $"{exception.Diagnostics[1].Id} at line {exception.Diagnostics[1].Line}, position {exception.Diagnostics[1].Position}: ",
            lines[1],
            StringComparison.Ordinal);
    }

    /// <summary>Option names that differ only by letter case are distinct option identifiers, not a duplicate: option-name comparison is by exact string value, not case-insensitively.</summary>
    [TestMethod]
    public void OptionNamesThatDifferOnlyByCaseAreNotDuplicates()
    {
        //Named killer: MessageDataModelValidator.Options.cs line 23, ValidateDuplicateOptionNames'
        //seenNames set changed from StringComparer.Ordinal to StringComparer.OrdinalIgnoreCase. "style"
        //and "Style" would then collide, adding a spurious DuplicateOptionName that AssertValid's
        //zero-diagnostics expectation sees.
        AssertValid("{42 :number style=percent Style=decimal}");
    }

    /// <summary>Declaration names that differ only by letter case are distinct declarations, not a duplicate: declaration-name comparison is by exact string value, not case-insensitively.</summary>
    [TestMethod]
    public void DeclarationNamesThatDifferOnlyByCaseAreNotDuplicates()
    {
        //Named killer: MessageDataModelValidator.Declarations.cs line 28, ValidateDeclarations'
        //seenNames set changed from StringComparer.Ordinal to StringComparer.OrdinalIgnoreCase. "$a" and
        //"$A" would then collide, adding a spurious DuplicateDeclaration that AssertValid's
        //zero-diagnostics expectation sees.
        AssertValid(".local $a = {1} .local $A = {2} {{_}}");
    }

    /// <summary>A duplicate-variant key's own literal value is kept verbatim, not NFC-normalized, even though the duplicate was found by comparing NFC-normalized values.</summary>
    [TestMethod]
    public void DuplicateVariantKeysLiteralValueIsKeptVerbatimNotNfcNormalized()
    {
        //"e" (U+0065) followed by COMBINING ACUTE ACCENT (U+0301, spelled as an escape so it cannot
        //be silently re-composed) NFC-normalizes to the single precomposed accented "e"; both keys
        //below use the same decomposed, two-code-unit spelling, which the NFC comparison still finds
        //equal to itself, but the surviving model must keep the original, unnormalized two-code-unit
        //value.
        const string decomposedE = "e\u0301";
        MessageParseResult result = AssertDataModelDiagnostics(
            $".input {{$x :s}} .match $x {decomposedE} {{{{a}}}} {decomposedE} {{{{b}}}} * {{{{c}}}}",
            WellKnownMessageFormatDiagnostics.DuplicateVariant);

        var selectMessage = result.Message as SelectMessage;
        Assert.IsNotNull(selectMessage);
        var firstKey = selectMessage.Variants[0].Keys[0] as LiteralKey;
        Assert.IsNotNull(firstKey);
        Assert.HasCount(2, firstKey.Literal.Value);
        Assert.AreEqual(decomposedE, firstKey.Literal.Value);
    }

    /// <summary>Every data model diagnostic's own message text names the offending construct, not an empty string.</summary>
    [TestMethod]
    public void DataModelDiagnosticMessageNamesTheOffendingConstruct()
    {
        //Named killers: MessageDataModelValidator.Declarations.cs line 35 (M-006, the rule-1
        //already-used-name message) and line 40 (M-007, the rule-3 self-reference message);
        //MessageDataModelValidator.Matcher.cs line 30 (M-008, VariantKeyMismatch), line 51
        //(M-010, DuplicateVariant), line 65 (M-012, MissingSelectorAnnotation);
        //MessageDataModelValidator.Options.cs line 30 (M-016, DuplicateOptionName). Every one of
        //these lines is a diagnostic's own message string; blanking it to "" leaves the
        //diagnostic's id and offset unchanged but empties its Message, which no other test in
        //this file inspects. Each row drives the parser to exactly one of these diagnostics and
        //asserts its exact message text.
        (string source, string expectedId, string expectedMessage)[] cases =
        [
            (".local $x = {1} .local $x = {2} {{_}}", WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
                "The variable '$x' is already declared."),
            (".local $x = {$x} {{_}}", WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
                "The declaration of '$x' refers to itself."),
            (".input {$a :x} .match $a 1 2 {{bad}} * {{ok}}", WellKnownMessageFormatDiagnostics.VariantKeyMismatch,
                "This variant has 2 key(s) but the matcher has 1 selector(s)."),
            (".input {$x :s} .match $x foo {{a}} foo {{b}} * {{c}}", WellKnownMessageFormatDiagnostics.DuplicateVariant,
                "Another variant already uses this list of keys."),
            (".match $foo 1 {{a}} * {{b}}", WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation,
                "The selector '$foo' does not directly or indirectly reference a declaration with a function."),
            ("{42 :number style=percent style=decimal}", WellKnownMessageFormatDiagnostics.DuplicateOptionName,
                "The option 'style' is repeated.")
        ];

        foreach((string source, string expectedId, string expectedMessage) in cases)
        {
            MessageParseResult result = AssertDataModelDiagnostics(source, expectedId);

            Assert.AreEqual(expectedMessage, result.Diagnostics[0].Message);
        }
    }
}
