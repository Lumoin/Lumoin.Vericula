using System.Collections.Immutable;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Linting;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class LinterTests
{
    [TestMethod]
    public void ReturnsEmptyForACleanDocument()
    {
        XliffFile file = FileWithRules(RuleSet(new PresenceRule("Cancel")), Unit("A", "x", "Cancel now"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReturnsEmptyWhenNoValidationRulesAndNoTargetLanguage()
    {
        XliffFile file = new(
            "wallet",
            new LanguageTag("en"),
            TargetLanguage: null,
            ToneProfile: null,
            Glossary: null,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Home", null)));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsPresenceRuleViolationAsWarning()
    {
        XliffFile file = FileWithRules(RuleSet(new PresenceRule("Cancel")), Unit("A", "Cancel", "Peruuta"));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX100", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Warning, diagnostic.Severity);
        Assert.AreEqual("wallet", diagnostic.Location?.FilePath);
        Assert.AreEqual("A", diagnostic.Location?.UnitId);
        Assert.AreEqual(0, diagnostic.Location?.LineNumber);

        //Linter.cs:313, segmentId is null ? ... : ... => (false ? ... : ...) - the unit's lone
        //segment (built by XliffUnit.FromText) carries a null id, so the true branch is the only one
        //that should fire here; forcing the false branch would append a spurious "segment ''".
        Assert.DoesNotContain("segment", diagnostic.Message, StringComparison.Ordinal);

        //Linter.cs:313, $"unit '{unit.Id}'" => $"" - emptying the null-segmentId branch's own text
        //would drop the "unit 'A'" label from the message entirely.
        Assert.Contains("unit 'A'", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void PresenceRuleIsSatisfiedWhenTextIsPresent()
    {
        XliffFile file = FileWithRules(RuleSet(new PresenceRule("Cancel")), Unit("A", "Cancel", "Cancel now"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsAbsenceRuleViolationAsError()
    {
        XliffFile file = FileWithRules(RuleSet(new AbsenceRule("TODO")), Unit("A", "x", "TODO translate"));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX101", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);

        //Linter.cs:230, the absence-violation message => "" - the forbidden text is the caller's
        //only clue to what the target must not contain, so an emptied message would lose it.
        Assert.Contains("TODO", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AbsenceRuleIsSatisfiedWhenTextIsMissing()
    {
        XliffFile file = FileWithRules(RuleSet(new AbsenceRule("TODO")), Unit("A", "x", "Valmis"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsLengthBudgetViolationAsError()
    {
        //Kills the mutant that swaps target.Length and budget.MaximumLength in the message text:
        //a single-digit length (the old "6") would still match if the two numbers were swapped
        //with a single-digit maximum, so this uses two different multi-digit numbers and asserts
        //both appear, with the length before the maximum, exactly where the format string puts them.
        XliffFile file = FileWithRules(RuleSet(new LengthBudgetRule(12)), Unit("A", "x", new string('x', 23)));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX102", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);
        Assert.Contains("23", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("12", diagnostic.Message, StringComparison.Ordinal);
        Assert.IsTrue(
            diagnostic.Message.IndexOf("23", StringComparison.Ordinal) < diagnostic.Message.IndexOf("12", StringComparison.Ordinal),
            diagnostic.Message);
    }

    [TestMethod]
    public void LengthBudgetRuleAllowsTargetAtExactlyTheMaximum()
    {
        XliffFile file = FileWithRules(RuleSet(new LengthBudgetRule(5)), Unit("A", "x", "12345"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsRegexRuleViolationAsError()
    {
        XliffFile file = FileWithRules(RuleSet(new RegexRule("^[0-9]+$")), Unit("A", "x", "abc"));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX103", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);

        //Linter.cs:292, the regex-violation message => "" - the required pattern is the caller's
        //only clue to what format the target must match, so an emptied message would lose it.
        Assert.Contains("^[0-9]+$", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RegexRuleIsSatisfiedWhenTargetMatches()
    {
        XliffFile file = FileWithRules(RuleSet(new RegexRule("^[0-9]+$")), Unit("A", "x", "12345"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsInvalidRegexPatternOnTheFileWithoutThrowing()
    {
        XliffFile file = FileWithRules(RuleSet(new RegexRule("[unterminated")), Unit("A", "x", "anything"));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX104", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("wallet", diagnostic.Location?.FilePath);
        Assert.IsNull(diagnostic.Location?.UnitId);

        //Linter.cs:147, the invalid-pattern message => "" - only the message text names the
        //offending pattern, so emptying it would hide the one thing this diagnostic exists to report.
        Assert.Contains("[unterminated", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReportsMissingTargetWhenFileDeclaresATargetLanguage()
    {
        XliffFile file = new(
            "wallet",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            Glossary: null,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Home", null)));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX105", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Warning, diagnostic.Severity);
        Assert.AreEqual("A", diagnostic.Location?.UnitId);
    }

    [TestMethod]
    public void DoesNotReportMissingTargetForAWhitespaceOnlyCompleteTarget()
    {
        //r1-34: VFX105 must fire only when XliffUnit.Target is null, i.e. the translation is not yet
        //complete; it must not fire for a present target that happens to be whitespace-only. XLIFF
        //2.1 Core's <target> content model allows zero or more characters, so a segment carrying
        //" " with segment state final is a legal, complete translation, not a missing one - this test
        //used to expect VFX105 here, which the old whitespace check raised regardless of completeness.
        XliffFile file = new(
            "wallet",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            Glossary: null,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Home", "   ")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void DoesNotReportMissingTargetWhenFileDeclaresNoTargetLanguage()
    {
        XliffFile file = new(
            "wallet",
            new LanguageTag("en"),
            TargetLanguage: null,
            ToneProfile: null,
            Glossary: null,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Home", null)));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsGlossaryRenderingMissingForAPreferredTerm()
    {
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Open your wallet", "Avaa kukkarosi")));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Warning, diagnostic.Severity);

        //Linter.cs:369, the glossary-rendering-missing message => "" - the required rendering is the
        //caller's only clue to what the target is missing, so an emptied message would lose it.
        Assert.Contains("lompakko", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void DoesNotReportGlossaryWhenTheRequiredRenderingIsPresent()
    {
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Open your wallet", "Avaa lompakkosi")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void DoesNotReportGlossaryRenderingMissingWhenOnlyTheNormalizationFormDiffers()
    {
        //f-11: ContainsTerm normalizes both source and term to Form C, but the adjacent
        //target/translation half of the same check compared raw text with StringComparison.Ordinal.
        //The entry's translation is precomposed "café" (U+00E9); the target holds the canonically
        //equal decomposed form (e + U+0301 combining acute). Only normalizing both sides keeps this
        //correct rendering from being reported as missing.
        string decomposedRendering = "café";
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("cafe", "café", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Meet at the cafe", $"Tavataan {decomposedRendering}ssa")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void DoesNotReportGlossaryForAllowedOrForbiddenEntries()
    {
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("wallet", "kukkaro", null, GlossaryEntryStatus.Allowed, ImmutableArray<Scope>.Empty, null),
            new GlossaryEntry("wallet", "pussi", null, GlossaryEntryStatus.Forbidden, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Open your wallet", "Avaa lompakkosi")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void TraversesUnitsNestedInGroups()
    {
        XliffGroup inner = new(
            "Inner",
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            ImmutableArray.Create(Unit("Deep", "x", null)),
            ImmutableArray<XliffGroup>.Empty);

        XliffGroup outer = new(
            "Outer",
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            ImmutableArray<XliffUnit>.Empty,
            ImmutableArray.Create(inner));

        XliffFile file = new(
            "wallet",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            Glossary: null,
            ValidationRules: null,
            ImmutableArray.Create(outer),
            ImmutableArray<XliffUnit>.Empty);

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("Deep", diagnostic.Location?.UnitId);
        Assert.AreEqual("VFX105", diagnostic.Id);
    }

    [TestMethod]
    public void OrdersDiagnosticsByFileThenUnitTraversalThenRule()
    {
        ValidationRuleSet rules = RuleSet(new PresenceRule("Cancel"), new AbsenceRule("TODO"));

        XliffGroup group = new(
            "G1",
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            ImmutableArray.Create(Unit("U2", "x", "TODO")),
            ImmutableArray<XliffGroup>.Empty);

        XliffFile fileA = new(
            "a",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            Glossary: null,
            rules,
            ImmutableArray.Create(group),
            ImmutableArray.Create(Unit("U1", "x", "TODO")));

        XliffFile fileB = new(
            "b",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            Glossary: null,
            rules,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("U3", "x", "TODO")));

        ImmutableArray<LintDiagnostic> diagnostics = Linter.Lint(DocumentOf(fileA, fileB));

        string[] actual = diagnostics
            .Select(diagnostic => $"{diagnostic.Location?.FilePath}:{diagnostic.Location?.UnitId}:{diagnostic.Id}")
            .ToArray();
        string[] expected =
        [
            "a:U1:VFX100",
            "a:U1:VFX101",
            "a:U2:VFX100",
            "a:U2:VFX101",
            "b:U3:VFX100",
            "b:U3:VFX101"
        ];

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void InvalidRegexDiagnosticPrecedesUnitDiagnosticsForTheSameFile()
    {
        ValidationRuleSet rules = RuleSet(new RegexRule("[unterminated"), new AbsenceRule("TODO"));
        XliffFile file = FileWithRules(rules, Unit("A", "x", "TODO"));

        ImmutableArray<LintDiagnostic> diagnostics = Linter.Lint(DocumentOf(file));

        Assert.HasCount(2, diagnostics);
        Assert.AreEqual("VFX104", diagnostics[0].Id);
        Assert.AreEqual("VFX101", diagnostics[1].Id);
    }

    [TestMethod]
    public void ReportsStartsWithViolationAsError()
    {
        XliffFile file = FileWithRules(RuleSet(new StartsWithRule("Hei")), Unit("A", "Hello", "Moi"));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX107", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);

        //Linter.cs:246, the starts-with-violation message => "" - the required prefix text is the
        //caller's only clue to what the target must start with, so an emptied message would lose it.
        Assert.Contains("Hei", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StartsWithRuleIsSatisfiedWhenTheTargetStartsWithTheText()
    {
        XliffFile file = FileWithRules(RuleSet(new StartsWithRule("Hei")), Unit("A", "Hello", "Hei maailma"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsEndsWithViolationAsError()
    {
        XliffFile file = FileWithRules(RuleSet(new EndsWithRule("!")), Unit("A", "Hello!", "Moi."));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX108", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);

        //Linter.cs:253, the ends-with-violation message => "" - the required suffix text is the
        //caller's only clue to what the target must end with, so an emptied message would lose it.
        Assert.Contains("required text '!'", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void EndsWithRuleIsSatisfiedWhenTheTargetEndsWithTheText()
    {
        XliffFile file = FileWithRules(RuleSet(new EndsWithRule("!")), Unit("A", "Hello!", "Moi!"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void ReportsGlossaryRenderingMissingForAUnitLevelGlossary()
    {
        var glossary = new Glossary([new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)]);
        XliffUnit unit = Unit("A", "Open the wallet", "Avaa kukkaro") with { Glossary = glossary };
        XliffFile file = FileWithRules(RuleSet(), unit);

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
        Assert.AreEqual("A", diagnostic.Location?.UnitId);
    }

    [TestMethod]
    public void PresenceRuleNormalizesToNfcByDefaultSoADecomposedTargetSatisfiesAPrecomposedRule()
    {
        //r1-31: XLIFF 2.1 §5.8.5.8 normalization defaults to nfc: an absent normalization attribute
        //means both rule text and target must be compared after folding to Unicode Normalization
        //Form C. "Café" (precomposed U+00E9) in the rule must match a target holding the decomposed
        //form (e + U+0301 combining acute), which a raw ordinal Contains would miss.
        string decomposedTarget = "Café is open";
        XliffFile file = FileWithRules(RuleSet(new PresenceRule("Café")), Unit("A", "x", decomposedTarget));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void NormalizationNoneComparesRawCodePointsWithoutFoldingToNfc()
    {
        //r1-31: normalization="none" must skip the NFC fold entirely, per XLIFF 2.1 §5.8.5.8's "none:
        //No normalization SHOULD be done." A rule explicitly set to None must therefore still flag a
        //target that differs only in normalization form from the rule text; a mutant that always
        //applies the NFC fold regardless of this property would make the two forms compare equal and
        //miss the violation.
        string decomposedTarget = "Café";
        AbsenceRule rule = new AbsenceRule("Café") with { Normalization = TextNormalization.None };
        XliffFile file = FileWithRules(RuleSet(rule), Unit("A", "x", decomposedTarget));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void RegexRuleNormalizesToNfcByDefaultSoADecomposedTargetSatisfiesAPrecomposedPattern()
    {
        //f-8: RegexRule's own Normalization was never honoured - EvaluateRegex matched the raw,
        //unnormalized target, unlike every sibling rule kind in Evaluate's switch. A target holding
        //the decomposed form of "café" (e + U+0301 combining acute) must still satisfy a pattern
        //written with the precomposed "café" (U+00E9), since the default normalization is nfc.
        string decomposedTarget = "café";
        XliffFile file = FileWithRules(RuleSet(new RegexRule("^café$")), Unit("A", "x", decomposedTarget));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void LengthBudgetRuleNormalizesToNfcByDefaultSoADecomposedTargetIsMeasuredComposed()
    {
        //f-10: LengthBudgetRule compared target.Length raw, ignoring its own Normalization. The
        //decomposed form of "café" (e + U+0301 combining acute) is 5 UTF-16 code units long but is
        //canonically the same 4-code-unit text as the precomposed form, which is exactly at the
        //budget; only measuring the normalized target keeps it within budget.
        string decomposedTarget = "café";
        XliffFile file = FileWithRules(RuleSet(new LengthBudgetRule(4)), Unit("A", "x", decomposedTarget));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void RegexRuleTimeoutBecomesADiagnosticInsteadOfThrowing()
    {
        //r1-32: a RegexMatchTimeoutException from catastrophic backtracking used to escape Lint
        //entirely, discarding every diagnostic already collected for the document; matching against a
        //pathological pattern-target pair must instead surface as a VFX109 diagnostic on the
        //offending unit and segment, per the linter's "never throws on document content" contract.
        string target = new string('a', 30) + "!";
        XliffFile file = FileWithRules(RuleSet(new RegexRule("^(a+)+$")), Unit("A", "x", target));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX109", diagnostic.Id);
        Assert.AreEqual(LintSeverity.Error, diagnostic.Severity);

        //Linter.cs:279, the timeout message => "" - the offending pattern is the caller's only clue
        //to which rule timed out, so an emptied message would lose it.
        Assert.Contains("^(a+)+$", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RulesAreEvaluatedAgainstEachSegmentsOwnTargetRatherThanTheUnitsFoldedTarget()
    {
        //(a): XLIFF 2.1 §5.8.4.2 validation's Processing Requirements say a file's rules "MUST be
        //applied to all <target> elements within the scope" of the enclosing element, not to one
        //folded string. A presence rule whose text spans a segment boundary in the folded target
        //("AB") but is absent from either segment's own target ("A", "B") must still be reported,
        //once per offending segment, with each diagnostic naming its segment - evaluating against
        //XliffUnit.Target instead would see "AB" as satisfied and report nothing.
        var unit = new XliffUnit(
            "A",
            [
                new XliffSegment("s1", SegmentKind.Translatable, InlineContent.FromText("x"), InlineContent.FromText("A"), SegmentState.Final, null),
                new XliffSegment("s2", SegmentKind.Translatable, InlineContent.FromText("y"), InlineContent.FromText("B"), SegmentState.Final, null)
            ],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);
        XliffFile file = FileWithRules(RuleSet(new PresenceRule("AB")), unit);

        ImmutableArray<LintDiagnostic> diagnostics = Linter.Lint(DocumentOf(file));

        Assert.HasCount(2, diagnostics);
        Assert.AreEqual("s1", diagnostics[0].SegmentId);
        Assert.AreEqual("s2", diagnostics[1].SegmentId);
        Assert.Contains("segment 's1'", diagnostics[0].Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GlossaryTermDoesNotMatchInsideAnUnrelatedWord()
    {
        //r1-35: a raw substring test lets a short term match inside an unrelated word - "art" inside
        //"Start" - and raise a false VFX106. Term detection must respect Unicode word boundaries, so a
        //source that merely contains the term's letters as part of a longer word must lint clean.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("art", "taide", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Start the app", "Käynnistä sovellus")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void GlossaryTermMatchesMidSentenceInAScriptWithNoInterWordSpaces()
    {
        //f-9: char.IsLetter is true for CJK ideographs (Unicode category Lo), which have no
        //inter-word whitespace, so the neighbour of a mid-sentence term is always itself "a letter"
        //and the boundary test could only ever be satisfied at the very start or end of the whole
        //source string. Here the term sits flanked by other Chinese characters on both sides, and
        //the target lacks the required rendering "shop", so VFX106 must still fire.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("商店", "shop", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("zh"),
            new LanguageTag("en"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "请到网上商店购买", "buy it online")));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
        Assert.Contains("shop", diagnostic.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GlossaryEntryScopedToOneAreaDoesNotApplyToAUnitInAnotherArea()
    {
        //r1-35/(c): a glossary entry with scopes must be enforced only on units whose scopes
        //intersect it. An entry scoped to "finance" must not flag a unit scoped only to "marketing",
        //even though the term is present in the source and the required rendering is absent from the
        //target - without the scope check this would wrongly raise VFX106.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("balance", "saldo", null, GlossaryEntryStatus.Preferred, ImmutableArray.Create(new Scope("finance")), null)));

        XliffUnit unit = Unit("A", "Check your balance", "Tarkista tilisi") with { Scopes = ImmutableArray.Create(new Scope("marketing")) };
        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(unit));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void GlossaryEntryScopedToAnAreaAppliesToAUnitInThatArea()
    {
        //r1-35/(c): the mirror of the above - an entry scoped to "finance" must still be enforced on a
        //unit that shares that scope, proving the scope check filters rather than disabling
        //enforcement entirely.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("balance", "saldo", null, GlossaryEntryStatus.Preferred, ImmutableArray.Create(new Scope("finance")), null)));

        XliffUnit unit = Unit("A", "Check your balance", "Tarkista tilisi") with { Scopes = ImmutableArray.Create(new Scope("finance")) };
        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(unit));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
    }

    [TestMethod]
    public void ThrowsForANullDocument()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Linter.Lint(null!));
    }

    [TestMethod]
    public void GlossaryEntryAppliesWhenAnyOfItsMultipleScopesMatchesTheUnit()
    {
        //Linter.cs:381, entryScopes.Any => entryScopes.All - the entry carries two scopes,
        //"finance" and "marketing", but the unit is scoped only to "marketing". The real Any check
        //applies as soon as one entry scope matches; an All-based check would find "finance"
        //unmatched against the unit's scopes and wrongly suppress the diagnostic.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("balance", "saldo", null, GlossaryEntryStatus.Preferred, ImmutableArray.Create(new Scope("finance"), new Scope("marketing")), null)));

        XliffUnit unit = Unit("A", "Check your balance", "Tarkista tilisi") with { Scopes = ImmutableArray.Create(new Scope("marketing")) };
        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(unit));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
    }

    [TestMethod]
    public void GlossaryEntryAppliesWhenTheUnitHasAnAdditionalScopeBesidesTheMatchingOne()
    {
        //Linter.cs:381, unitScopes.Any => unitScopes.All - the entry carries one scope, "finance",
        //and the unit carries both "finance" and "marketing". The real Any check applies as soon as
        //one unit scope matches; an All-based check would find "marketing" unequal to "finance" and
        //wrongly suppress the diagnostic.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("balance", "saldo", null, GlossaryEntryStatus.Preferred, ImmutableArray.Create(new Scope("finance")), null)));

        XliffUnit unit = Unit("A", "Check your balance", "Tarkista tilisi") with { Scopes = ImmutableArray.Create(new Scope("finance"), new Scope("marketing")) };
        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(unit));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
    }

    [TestMethod]
    public void GlossaryTermAtTheVeryStartOfTheSourceIsStillDetected()
    {
        //Linter.cs:403 Equality mutant flips the loop condition from >= 0 to > 0, so an IndexOf match
        //at position 0 - the very first character of the source - is treated the same as "not found"
        //and the loop body, which checks the word boundaries and can report a match, never runs. A
        //source that begins with the glossary term, and contains it nowhere else, must still raise
        //VFX106 when the target lacks the required rendering.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("Cancel", "peruuta", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Cancel now", "Nyt")));

        LintDiagnostic diagnostic = Only(Linter.Lint(DocumentOf(file)));

        Assert.AreEqual("VFX106", diagnostic.Id);
    }

    [TestMethod]
    public void GlossaryTermAtTheStartOfALongerWordDoesNotMatchAsAWholeWord()
    {
        //Linter.cs:407 LogicalNotExpression mutant flips !char.IsLetter(...) to char.IsLetter(...), so
        //the right boundary reports true exactly when the following character IS a letter instead of
        //when it is not. "Cancel" begins "Cancellation" (left boundary satisfied at index 0) but is
        //followed by another letter, so it is not a standalone word; the mutant would treat that letter
        //as a boundary and wrongly report the term as used, raising VFX106 even though the source never
        //uses the whole word "Cancel".
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry("Cancel", "peruuta", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Cancellation policy", "Käytäntö")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void DisabledValidationRuleIsSkippedEntirely()
    {
        //Linter.cs:133, continue; => ; : removing the continue lets a disabled rule fall through to
        //be compiled and evaluated instead of skipped entirely, per XLIFF 2.1 §5.8.5.9 disabled. A
        //PresenceRule with Disabled = true and a target missing the required text would, if
        //evaluated, report VFX100; an empty result proves the rule was never compiled.
        PresenceRule rule = new PresenceRule("Cancel") with { Disabled = true };
        XliffFile file = FileWithRules(RuleSet(rule), Unit("A", "x", "no match here"));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    [TestMethod]
    public void GlossaryEntryWithAnEmptyTermNeverMatchesTheSource()
    {
        //Linter.cs:396, return false; => return true; in ContainsTerm's term.Length == 0 guard: an
        //empty term must never be treated as present in the source, or a preferred glossary entry
        //with an empty Term would spuriously raise VFX106 for a target missing the required
        //rendering, regardless of source content.
        var glossary = new Glossary(ImmutableArray.Create(
            new GlossaryEntry(string.Empty, "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)));

        XliffFile file = new(
            "app",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            glossary,
            ValidationRules: null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(Unit("A", "Open your wallet", "Avaa kukkarosi")));

        Assert.HasCount(0, Linter.Lint(DocumentOf(file)));
    }

    /// <summary>
    /// Builds a single-segment translatable unit with the given source and target text.
    /// </summary>
    /// <param name="id">The unit's id.</param>
    /// <param name="source">The segment's source text.</param>
    /// <param name="target">The segment's target text, or null for no target.</param>
    /// <returns>The built unit.</returns>
    private static XliffUnit Unit(string id, string source, string? target)
    {
        return XliffUnit.FromText(id, source, target);
    }

    /// <summary>
    /// Builds a validation rule set from the given rules, in order.
    /// </summary>
    /// <param name="rules">The rules to include.</param>
    /// <returns>The built rule set.</returns>
    private static ValidationRuleSet RuleSet(params ValidationRule[] rules)
    {
        return new ValidationRuleSet(ImmutableArray.Create(rules));
    }

    /// <summary>
    /// Builds a file named "wallet", from "en" to "fi", carrying the given validation rules and units.
    /// </summary>
    /// <param name="rules">The file's validation rules.</param>
    /// <param name="units">The file's units.</param>
    /// <returns>The built file.</returns>
    private static XliffFile FileWithRules(ValidationRuleSet rules, params XliffUnit[] units)
    {
        return new XliffFile(
            "wallet",
            new LanguageTag("en"),
            new LanguageTag("fi"),
            ToneProfile: null,
            Glossary: null,
            rules,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray.Create(units));
    }

    /// <summary>
    /// Builds a document at <see cref="XliffVersion.V20"/> carrying the given files.
    /// </summary>
    /// <param name="files">The document's files.</param>
    /// <returns>The built document.</returns>
    private static XliffDocument DocumentOf(params XliffFile[] files)
    {
        return new XliffDocument(XliffVersion.V20, ImmutableArray.Create(files));
    }

    /// <summary>
    /// Returns the single diagnostic in <paramref name="diagnostics"/>, failing the test if there is
    /// not exactly one.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to pick the one element from.</param>
    /// <returns>The single diagnostic.</returns>
    private static LintDiagnostic Only(ImmutableArray<LintDiagnostic> diagnostics)
    {
        return diagnostics.Single();
    }
}
