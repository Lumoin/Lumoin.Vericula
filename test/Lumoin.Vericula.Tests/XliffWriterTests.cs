using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text;
using System.Xml.Linq;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Tone;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class XliffWriterTests
{
    [TestMethod]
    public void RoundTripsMinimalDocumentWithoutTargetLanguage()
    {
        XliffUnit unit = NewUnit("Intro", "The wallet stores the claims you hold.");
        XliffFile file = NewFile("wallet", "en", null, [], [unit]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.IsFalse(xml.Contains("trgLang", StringComparison.Ordinal));
        Assert.IsFalse(xml.Contains("<target", StringComparison.Ordinal));
        AssertDocumentsEqual(document, roundTripped);
    }

    [TestMethod]
    public void RoundTripsDocumentWithTargetLanguageNestedGroupsAndNotes()
    {
        XliffUnit leaf = NewUnit("TabHome", "Home", "Koti", "Bottom navigation label.");
        XliffUnit untranslated = NewUnit("TabIntro", "The wallet stores the claims you hold.");
        var innerGroup = new XliffGroup("Cards", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [leaf], ImmutableArray<XliffGroup>.Empty);
        var outerGroup = new XliffGroup("Shell", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [untranslated], [innerGroup]);
        XliffUnit topLevel = NewUnit("AppTitle", "Wallet", "Lompakko");
        XliffFile file = NewFile("wallet", "en", "fi", [outerGroup], [topLevel]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        XliffDocument roundTripped = Read(WriteToBytes(document));

        AssertDocumentsEqual(document, roundTripped);
    }

    [TestMethod]
    public void RoundTripsReaderFixtureString()
    {
        XliffDocument original = Read(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));

        XliffDocument roundTripped = Read(WriteToBytes(original));

        AssertDocumentsEqual(original, roundTripped);
    }

    [TestMethod]
    public void RoundTripsToneProfileGlossariesValidationRulesScopesAndMetadata()
    {
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "brand voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty.Add("channel", "mobile").Add("audience", "adults"));
        var fileGlossary = new Glossary([
            new GlossaryEntry("wallet", "lompakko", "a place to store claims", GlossaryEntryStatus.Preferred, [new Scope("shell")], "brand term"),
            new GlossaryEntry("claim", "väite", null, GlossaryEntryStatus.Forbidden, ImmutableArray<Scope>.Empty, null)
        ]);
        var unitGlossary = new Glossary([
            new GlossaryEntry("home", "koti", "the start screen", GlossaryEntryStatus.Preferred, [new Scope("shell"), new Scope("cards")], "navigation term"),
            new GlossaryEntry("home", "etusivu", "the start screen", GlossaryEntryStatus.Allowed, [new Scope("shell"), new Scope("cards")], "navigation term")
        ]);
        var validationRules = new ValidationRuleSet([
            new PresenceRule("{0}"),
            new AbsenceRule("TODO"),
            new StartsWithRule("Hei"),
            new EndsWithRule("!"),
            new LengthBudgetRule(40),
            new RegexRule("^[A-Z]")
        ]);
        XliffUnit unit = NewUnit("TabHome", "Home", "Koti", "Bottom navigation label.") with
        {
            Scopes = [new Scope("shell")],
            Metadata = ImmutableDictionary<string, string>.Empty.Add("owner", "growth").Add("tier", "1"),
            Glossary = unitGlossary
        };
        var group = new XliffGroup(
            "Shell",
            [new Scope("shell"), new Scope("navigation")],
            ImmutableDictionary<string, string>.Empty.Add("owner", "growth"),
            [unit],
            ImmutableArray<XliffGroup>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), new LanguageTag("fi"), toneProfile, fileGlossary, validationRules, [group], ImmutableArray<XliffUnit>.Empty);
        var document = new XliffDocument(XliffVersion.V21, [file]);

        byte[] bytes = WriteToBytes(document);
        XliffDocument roundTripped = Read(bytes);

        AssertDocumentsEqual(document, roundTripped);
        string xml = Encoding.UTF8.GetString(bytes);
        Assert.IsTrue(xml.Contains("<val:rule isPresent=\"{0}\" />", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("vericula:maxLength=\"40\"", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("<gls:glossEntry vericula:rationale=\"navigation term\" vericula:scopes=\"shell cards\"", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("category=\"vericula:tone\"", StringComparison.Ordinal));

        //Kills the mutant that drops WriteTone's r1 WriteMeta call for the orientation field at
        //XliffWriter.cs:387: TextOrientation.Horizontal is the enum's own default value, so a round
        //trip that only compared the parsed ToneProfile back to the original would still pass even
        //with the <mda:meta type="orientation"> element missing entirely, since the reader falls back
        //to the same default when the element is absent; only a direct check for the element catches
        //it.
        Assert.Contains("<mda:meta type=\"orientation\">Horizontal</mda:meta>", xml);

        //Kills the mutant that drops WriteTone's r1 WriteMeta call for the digit style field at
        //XliffWriter.cs:390, for the same reason as the orientation check above: DigitStyle.HalfWidth
        //is the enum's default, so only a direct check for the element, not the round-tripped value,
        //catches the missing <mda:meta type="digitStyle"> element.
        Assert.Contains("<mda:meta type=\"digitStyle\">HalfWidth</mda:meta>", xml);
    }

    [TestMethod]
    public void RoundTripsSegmentsWithIdsStatesSubStatesAndIgnorables()
    {
        var unit = new XliffUnit(
            "A",
            [
                new XliffSegment("s1", SegmentKind.Translatable, "First.", "Eka.", SegmentState.Translated, "tool:draft"),
                new XliffSegment("i1", SegmentKind.Ignorable, " ", " ", SegmentState.Initial, null),
                new XliffSegment("s2", SegmentKind.Translatable, "Second.", "Toka.", SegmentState.Reviewed, null),
                new XliffSegment(null, SegmentKind.Translatable, "Third.", "Kolmas.", SegmentState.Final, null),
                new XliffSegment(null, SegmentKind.Translatable, "Fourth.", null, SegmentState.NeedsTranslation, null),
                new XliffSegment(null, SegmentKind.Translatable, "Fifth.", null, SegmentState.Initial, null),
                new XliffSegment(null, SegmentKind.Translatable, "Sixth.", null, SegmentState.Initial, "tool:x")
            ],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);
        XliffFile file = NewFile("wallet", "en", "fi", [], [unit]);
        var document = new XliffDocument(XliffVersion.V21, [file]);

        byte[] bytes = WriteToBytes(document);
        XliffDocument roundTripped = Read(bytes);

        AssertDocumentsEqual(document, roundTripped);
        string xml = Encoding.UTF8.GetString(bytes);
        Assert.IsTrue(xml.Contains("<segment id=\"s1\" state=\"translated\" subState=\"tool:draft\">", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("<ignorable id=\"i1\">", StringComparison.Ordinal));

        //Kills the mutant that reverts r1-20: XLIFF 2.1 §4.3.1.35 subState, Constraints, "If the
        //attribute subState is used, the attribute state MUST be explicitly set", so the
        //NeedsTranslation segment (state implicit, sub-state present) must carry an explicit
        //state="initial", while the plain Initial segment (no sub-state) must stay fully implicit.
        Assert.IsTrue(xml.Contains("<segment state=\"initial\" subState=\"vericula:needsTranslation\">", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("<segment>\n        <source>Fifth.", StringComparison.Ordinal));

        //Kills the mutant that turns StateAttributes' r1-20 SegmentState.Initial arm ternary,
        //segment.SubState is null ? null : StateInitial, into an unconditional null at
        //XliffWriter.cs:333: "Sixth." carries the same subState-requires-explicit-state rule as the
        //NeedsTranslation case above, but through the plain Initial arm instead, so state="initial"
        //must still be written even though the mutant would leave it implicit.
        Assert.Contains("<segment state=\"initial\" subState=\"tool:x\">", xml);
    }

    [TestMethod]
    public void PreservesBareLineFeedInSourceAndTargetText()
    {
        XliffUnit unit = NewUnit("Body", "First line.\nSecond line.", "Eka rivi.\nToka rivi.");
        XliffFile file = NewFile("wallet", "en", "fi", [], [unit]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        string xml = Encoding.UTF8.GetString(bytes);

        Assert.IsTrue(xml.Contains("<source>First line.\nSecond line.</source>", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("<target>Eka rivi.\nToka rivi.</target>", StringComparison.Ordinal));

        XliffDocument roundTripped = Read(bytes);
        Assert.AreEqual(unit.Source, roundTripped.Files[0].Units[0].Source);
        Assert.AreEqual(unit.Target, roundTripped.Files[0].Units[0].Target);
    }

    [TestMethod]
    public void PreservesCarriageReturnsInTextAndNotes()
    {
        XliffUnit unit = NewUnit("Body", "a\r\nb", "x\ry", "line1\r\nline2");
        XliffFile file = NewFile("wallet", "en", "fi", [], [unit]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.IsTrue(xml.Contains("&#xD;", StringComparison.Ordinal));
        Assert.AreEqual("a\r\nb", roundTripped.Files[0].Units[0].Source);
        Assert.AreEqual("x\ry", roundTripped.Files[0].Units[0].Target);
        Assert.AreEqual("line1\r\nline2", roundTripped.Files[0].Units[0].Notes.Single());
    }

    [TestMethod]
    public void ProducesTheExactGoldenStringForASmallDocument()
    {
        XliffUnit unit = NewUnit("TabHome", "Home", "Koti", "Bottom navigation label.");
        XliffFile file = NewFile("wallet", "en", "fi", [], [unit]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        string xml = Encoding.UTF8.GetString(WriteToBytes(document));

        string expected = string.Join(
            "\n",
            """<?xml version="1.0" encoding="utf-8"?>""",
            """<xliff version="2.0" srcLang="en" trgLang="fi" xml:space="preserve" xmlns="urn:oasis:names:tc:xliff:document:2.0">""",
            """  <file id="wallet">""",
            """    <unit id="TabHome">""",
            """      <notes>""",
            """        <note>Bottom navigation label.</note>""",
            """      </notes>""",
            """      <segment>""",
            """        <source>Home</source>""",
            """        <target>Koti</target>""",
            """      </segment>""",
            """    </unit>""",
            """  </file>""",
            "</xliff>");

        Assert.AreEqual(expected, xml);
    }

    private static readonly string[] UnsortedMetadataKeys = ["kilo", "delta", "whiskey", "alpha", "romeo", "golf", "yankee", "charlie"];

    private static readonly string[] OrdinalSortedMetadataKeys = ["alpha", "charlie", "delta", "golf", "kilo", "romeo", "whiskey", "yankee"];

    [TestMethod]
    public void WritesMetadataKeysInOrdinalOrderRegardlessOfInsertionOrder()
    {
        //Kills the mutant that writes WriteDictionary's entries in the dictionary's own enumeration
        //order instead of StringComparer.Ordinal order: ImmutableDictionary enumerates by an internal
        //hash order unrelated to key text, so with 8 keys the chance that hash order alone reproduces
        //the exact alphabetical sequence below is negligible; only an explicit ordinal sort does.
        ImmutableDictionary<string, string>.Builder metadata = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach(string key in UnsortedMetadataKeys)
        {
            metadata[key] = key;
        }

        XliffUnit unit = NewUnit("A", "Home") with { Metadata = metadata.ToImmutable() };
        XliffFile file = NewFile("wallet", "en", null, [], [unit]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        string xml = Encoding.UTF8.GetString(WriteToBytes(document));

        string[] actualOrder = [.. System.Text.RegularExpressions.Regex.Matches(xml, "type=\"([a-z]+)\"").Select(match => match.Groups[1].Value)];
        CollectionAssert.AreEqual(OrdinalSortedMetadataKeys, actualOrder);
    }

    [TestMethod]
    public void WritesVersion20And21Attributes()
    {
        var version20 = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);
        var version21 = new XliffDocument(XliffVersion.V21, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);

        string xml20 = Encoding.UTF8.GetString(WriteToBytes(version20));
        string xml21 = Encoding.UTF8.GetString(WriteToBytes(version21));

        Assert.IsTrue(xml20.Contains("version=\"2.0\"", StringComparison.Ordinal));
        Assert.IsTrue(xml21.Contains("version=\"2.1\"", StringComparison.Ordinal));
        Assert.AreEqual(XliffVersion.V20, Read(Encoding.UTF8.GetBytes(xml20)).Version);
        Assert.AreEqual(XliffVersion.V21, Read(Encoding.UTF8.GetBytes(xml21)).Version);
    }

    [TestMethod]
    public async Task WriteAsyncThrowsWhenCancellationIsAlreadyRequestedAndWritesNothing()
    {
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);
        using var stream = new MemoryStream();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => XliffWriter.WriteAsync(document, stream, new CancellationToken(canceled: true)));
        Assert.AreEqual(0, stream.Length);

        //Kills the mutant that drops XliffWriter.cs:77's
        //cancellationToken.ThrowIfCancellationRequested() in WriteAsync(XliffDocument, PipeWriter,
        //CancellationToken): the assertions above only exercise the Stream overload's identical
        //guard at line 114, so without line 77 a pre-cancelled token would let the write proceed on
        //a pipe instead of throwing before any byte reaches it.
        var pipe = new Pipe();
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => XliffWriter.WriteAsync(document, pipe.Writer, new CancellationToken(canceled: true)));
    }

    [TestMethod]
    public void ProducesIdenticalBytesForRepeatedWrites()
    {
        XliffDocument document = Read(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));

        byte[] first = WriteToBytes(document);
        byte[] second = WriteToBytes(document);

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public async Task WriteAsyncProducesTheSameBytesAsWrite()
    {
        XliffDocument document = Read(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));

        byte[] sync = WriteToBytes(document);
        using var asyncStream = new MemoryStream();
        await XliffWriter.WriteAsync(document, asyncStream, TestContext.CancellationToken);

        CollectionAssert.AreEqual(sync, asyncStream.ToArray());
    }

    [TestMethod]
    public async Task WritesToAPipeAndCompletesIt()
    {
        XliffDocument document = Read(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));
        var pipe = new Pipe();

        XliffWriter.Write(document, pipe.Writer);
        byte[] piped = await DrainAsync(pipe.Reader, TestContext.CancellationToken);

        CollectionAssert.AreEqual(WriteToBytes(document), piped);
    }

    [TestMethod]
    public async Task WritesToAPipeAsynchronouslyAndCompletesIt()
    {
        XliffDocument document = Read(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));
        var pipe = new Pipe();

        await XliffWriter.WriteAsync(document, pipe.Writer, TestContext.CancellationToken);
        byte[] piped = await DrainAsync(pipe.Reader, TestContext.CancellationToken);

        CollectionAssert.AreEqual(WriteToBytes(document), piped);
    }

    [TestMethod]
    public void WrittenOutputIsWellFormedXmlAndDeclaresPreservedWhitespaceOnTheRoot()
    {
        XliffDocument document = Read(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));

        using var stream = new MemoryStream(WriteToBytes(document));
        XDocument parsed = TestXml.Load(stream);

        Assert.AreEqual("xliff", parsed.Root?.Name.LocalName);
        Assert.AreEqual("preserve", parsed.Root?.Attribute(XNamespace.Xml + "space")?.Value);
    }

    [TestMethod]
    public void RejectsADocumentWithoutFiles()
    {
        AssertRejected(new XliffDocument(XliffVersion.V20, ImmutableArray<XliffFile>.Empty), "at least one file");
    }

    [TestMethod]
    public void RejectsAFileWithoutUnitsOrGroups()
    {
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [])]), "neither units nor groups");
    }

    [TestMethod]
    public void RejectsAUnitWithoutSegments()
    {
        XliffUnit unit = NewUnit("A", "Home") with { Segments = ImmutableArray<XliffSegment>.Empty };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "no translatable segment");
    }

    [TestMethod]
    public void RejectsAUnitWithOnlyIgnorableSegments()
    {
        //Kills the mutant that reverts r1-24 to unit.Segments.IsEmpty: XLIFF 2.1 §4.2.2.5 requires a
        //<unit> to contain at least one <segment>, so a unit holding only <ignorable> content must
        //still be refused even though it is not empty.
        var unit = new XliffUnit(
            "A",
            [new XliffSegment(null, SegmentKind.Ignorable, " ", null, SegmentState.Initial, null)],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "no translatable segment");
    }

    [TestMethod]
    public void RejectsDuplicateSegmentIdsWithinAUnit()
    {
        //Kills the mutant that drops the r1-25 duplicate-segment-id check: XLIFF 2.1 §4.3.1.21 id
        //requires a segment or ignorable id to be unique among all of them within the enclosing unit.
        var unit = new XliffUnit(
            "A",
            [
                new XliffSegment("s1", SegmentKind.Translatable, "One", null, SegmentState.Initial, null),
                new XliffSegment("s1", SegmentKind.Translatable, "Two", null, SegmentState.Initial, null)
            ],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "Duplicate segment id");
    }

    [TestMethod]
    public void RejectsALengthBudgetRuleBelowOne()
    {
        //Kills the mutant that drops the r1-26 MaximumLength check: the writer used to emit
        //vericula:maxLength="-1" (or "0") and let the reader's own NumberStyles.None parse refuse it
        //later, rather than reporting the problem itself.
        var rules = new ValidationRuleSet([new LengthBudgetRule(0)]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "length budget 0");
    }

    private sealed record UnknownRule: ValidationRule;

    [TestMethod]
    public void RejectsAValidationRuleKindTheWriterDoesNotKnow()
    {
        //Kills the mutant that drops the r1-26 "unknown rule kind" check: the writer switch used to
        //throw ArgumentOutOfRangeException instead of reporting the problem through Problems().
        var rules = new ValidationRuleSet([new UnknownRule()]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "rule kind the writer does not know");
    }

    [TestMethod]
    public void RejectsAnEmptyValidationRuleSet()
    {
        //r1-21: XLIFF 2.1 §5.8.4.2 <validation> "Contains: One or more <rule> elements", so a rule set
        //with none is not legal to emit. Without this check the writer produced a bare
        //<val:validation /> - well-formed XML but invalid against the module's own content model -
        //because the reader's disabled-rule handling (see r1-13) can legally hand it an empty set.
        var rules = new ValidationRuleSet(ImmutableArray<ValidationRule>.Empty);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "no rules");
    }

    [TestMethod]
    public void RejectsAnEmptyRuleText()
    {
        //(b): an isNotPresent rule with an empty value would flag every target, since the empty
        //string is present in every string. The reader refuses this on the way in (see
        //XliffModuleReaderTests), but the writer must refuse it independently too, since the model can
        //be built directly without going through XliffReader.
        var rules = new ValidationRuleSet([new AbsenceRule("")]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "empty value");
    }

    [TestMethod]
    public void RoundTripsADisabledValidationRule()
    {
        //r1-13: XLIFF 2.1 §5.8.4.3 <rule> Processing Requirements say "Modifiers MUST NOT remove
        //either <rule> elements or their attributes defined in this module", so a disabled rule must
        //round-trip with disabled="yes" preserved rather than vanish, and the model must come back
        //with Disabled true rather than the rule being dropped.
        var rules = new ValidationRuleSet([new PresenceRule("store") with { Disabled = true }]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.IsTrue(xml.Contains("disabled=\"yes\"", StringComparison.Ordinal));
        AssertDocumentsEqual(document, roundTripped);
    }

    [TestMethod]
    public void RoundTripsAnExplicitNormalizationNoneAttribute()
    {
        //r1-15: XLIFF 2.1 §5.8.5.8 normalization defaults to nfc, so an explicit normalization="none"
        //changes the semantics a conforming validator applies to the rule; the writer must re-emit it
        //rather than silently falling back to the schema default, which would re-declare NFC
        //semantics the source document never asked for.
        var rules = new ValidationRuleSet([new AbsenceRule("TODO") with { Normalization = TextNormalization.None }]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.IsTrue(xml.Contains("normalization=\"none\"", StringComparison.Ordinal));
        AssertDocumentsEqual(document, roundTripped);
    }

    [TestMethod]
    public void RejectsAnUndefinedSegmentState()
    {
        //Kills the mutant that drops the r1-26 undefined-SegmentState check: the writer switch used to
        //throw ArgumentOutOfRangeException instead of reporting the problem through Problems().
        XliffUnit unit = XliffUnit.FromText("A", "Home") with
        {
            Segments = [new XliffSegment(null, SegmentKind.Translatable, "Home", null, (SegmentState)99, null)]
        };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "not a known segment state");
    }

    [TestMethod]
    public void RejectsAnIgnorableSegmentCarryingAStateOrSubState()
    {
        //Kills the mutant that drops the r1-30 check: XLIFF 2.1 §4.2.2.7 <ignorable> has no state or
        //subState attribute, so a model value the writer cannot express must be refused, not dropped.
        var unit = new XliffUnit(
            "A",
            [new XliffSegment("i1", SegmentKind.Ignorable, " ", " ", SegmentState.Translated, "tool:x")],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", "fi", [], [unit])]), "ignorable segment");
    }

    [TestMethod]
    public void RejectsANeedsTranslationSegmentCarryingItsOwnSubState()
    {
        //f-5: SegmentState.NeedsTranslation writes as state="initial" with the reserved
        //vericula:needsTranslation sub-state (StateAttributes' NeedsTranslation arm hardcodes it), so a
        //segment that is legally constructible with a different, real SubState used to be written with
        //that sub-state silently discarded and no diagnostic. The writer must refuse it instead.
        var unit = new XliffUnit(
            "A",
            [new XliffSegment("s1", SegmentKind.Translatable, "Home", null, SegmentState.NeedsTranslation, "acme:source-changed")],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "NeedsTranslation and a sub-state");
    }

    [TestMethod]
    public void RejectsAMalformedFileSourceOrTargetLanguage()
    {
        //Kills the mutant that drops the r1-27/r1-8 BCP 47 shape check on the writer side: an
        //unvalidated LanguageTag must not reach the output as an invalid srcLang or trgLang.
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en US", null, [], [NewUnit("A", "Home")])]), "not a well-formed BCP 47");

        XliffFile targetFile = NewFile("wallet", "en", "fi", [], [NewUnit("A", "Home", "Koti")]) with { TargetLanguage = new LanguageTag("") };
        AssertRejected(new XliffDocument(XliffVersion.V20, [targetFile]), "not a well-formed BCP 47");
    }

    [TestMethod]
    public void RejectsFilesThatDisagreeOnSourceOrTargetLanguage()
    {
        //Kills the mutant that drops the r1-23 check: XLIFF has one srcLang/trgLang pair per document,
        //taken from the first file, so a second file with a different pair must be refused rather than
        //silently rewritten to the first file's languages on the next read.
        XliffFile english = NewFile("en-file", "en", "fi", [], [NewUnit("A", "Home", "Koti")]);
        XliffFile german = NewFile("de-file", "de", "fi", [], [NewUnit("B", "Haus", "Talo")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [english, german]), "declares source language 'de'");

        XliffFile swedish = NewFile("sv-file", "en", "sv", [], [NewUnit("C", "Hem", "Hem")]);
        AssertRejected(new XliffDocument(XliffVersion.V20, [english, swedish]), "declares target language 'sv'");
    }

    [TestMethod]
    public void RejectsATargetBearingFileWithNoTargetLanguageWhenAnotherFileSetsTheDocumentsTrgLang()
    {
        //p1-5: f1 declares no target language itself but carries a translated unit; f2 declares "fi"
        //and is what RootLanguages picks as the document's trgLang. Write(x) must be refused here rather
        //than silently rewriting f1 with trgLang="fi" on the next read, which is what would happen if
        //this problem went unreported: write(x) then read must give back x's per-file TargetLanguage,
        //null for f1, or refuse to write at all.
        XliffFile f1 = NewFile("f1", "en", null, [], [NewUnit("u1", "Home", "Koti")]);
        XliffFile f2 = NewFile("f2", "en", "fi", [], [NewUnit("u2", "Wallet", "Lompakko")]);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => XliffWriter.Write(new XliffDocument(XliffVersion.V20, [f1, f2]), new MemoryStream()));

        //The file-level check (Problems(XliffDocument)) and the per-segment check (Problems(XliffSegment,
        //...), now compared against f1's own TargetLanguage rather than the document root's) each catch
        //this independently; both messages must be present so reverting either fix alone still fails here.
        Assert.Contains("File 'f1' declares no target language, but it carries target text", exception.Message);
        Assert.Contains("Unit 'u1' in file 'f1' carries a target but file 'f1' declares no target language.", exception.Message);
    }

    [TestMethod]
    public void RejectsDuplicateFileIds()
    {
        //Kills the mutant that drops the duplicate-<file>-id check behind r1-38: two files sharing an
        //id would otherwise merge into one resource for any consumer keyed by file id.
        XliffFile first = NewFile("wallet", "en", null, [], [NewUnit("A", "One")]);
        XliffFile second = NewFile("wallet", "en", null, [], [NewUnit("B", "Two")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [first, second]), "Duplicate file id");
    }

    [TestMethod]
    public void RejectsGroupNestingDeeperThan64Levels()
    {
        //Kills the mutant that drops the r1-95 depth cap on the writer side, mirroring the reader's own
        //bound so a document the writer would emit is one the reader can read back without a risk of
        //unbounded recursion.
        XliffGroup group = new("g65", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);
        for(int level = 0; level < 64; level++)
        {
            group = new XliffGroup($"g{level}", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, ImmutableArray<XliffUnit>.Empty, [group]);
        }

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), "nests more than 64 levels");
    }

    [TestMethod]
    public void RejectsAnIdThatIsNotAnXmlNameToken()
    {
        //Kills the mutant at XliffWriter.cs:875 that empties the "what" fragment NameTokenProblems
        //receives for a unit's id ($"unit id '{unit.Id}' in file '{file.Id}'" => $""), which would
        //otherwise still satisfy a bare "not an XML name token" check.
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("Tab\nHome", "Home")])]), "unit id 'Tab\nHome' in file 'wallet' is not an XML name token");
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("Tab Home", "Home")])]), "unit id 'Tab Home' in file 'wallet' is not an XML name token");

        //Kills the mutant at XliffWriter.cs:688 that empties the "what" fragment passed for a file's
        //own id ($"file id '{file.Id}'" => $""), for the same reason as above.
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wal let", "en", null, [], [NewUnit("A", "Home")])]), "The file id 'wal let' is not an XML name token.");
    }

    [TestMethod]
    public void RejectsAnEmptyUnitId()
    {
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("", "Home")])]);
        using var stream = new MemoryStream();

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffWriter.Write(document, stream));

        //Kills the mutant at XliffWriter.cs:1091 that turns NameTokenProblems' r1
        //yield return $"The {what} is empty."; into a no-op (";"): without it, an empty unit id
        //yields no problem at all (the yield break at line 1093 still runs), so Problems() would stay
        //empty and Write would succeed instead of throwing; ThrowsExactly above already fails then.
        //Also kills the mutant at XliffWriter.cs:1091 that empties the message text itself
        //($"The {what} is empty." => $""): the exact message below would be "" instead.
        //Also kills the mutant at XliffWriter.cs:1093 that turns yield break; into a no-op: without
        //it, execution falls through to the IsNameToken check for the same empty value, which also
        //fails XmlConvert.VerifyNMTOKEN, so the message would gain a second, redundant "is not an
        //XML name token" sentence instead of stopping after the one sentence asserted here.
        Assert.Contains("The unit id '' in file 'wallet' is empty.", exception.Message);
        Assert.IsFalse(exception.Message.Contains("is not an XML name token", StringComparison.Ordinal), exception.Message);
        Assert.AreEqual("document", exception.ParamName);
        Assert.AreEqual(0, stream.Length);
    }

    [TestMethod]
    public void RejectsDuplicateUnitIdsWithinAFile()
    {
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "One"), NewUnit("A", "Two")])]), "Duplicate unit id");
    }

    [TestMethod]
    public void RejectsATargetWithoutATargetLanguage()
    {
        //p1-5: the message names the file itself, since the check now compares a target-bearing segment
        //against its own file's TargetLanguage rather than the document root's.
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "One", "Yksi")])]), "file 'wallet' declares no target language");
    }

    [TestMethod]
    public void RejectsAScopeContainingWhitespace()
    {
        //Strengthened to assert the full message rather than only "contains whitespace": kills the
        //mutant at XliffWriter.cs:890 that empties the "what" fragment ScopeProblems receives for a
        //unit's own scopes, which would otherwise still satisfy a bare "contains whitespace" check.
        XliffUnit unit = NewUnit("A", "One") with { Scopes = [new Scope("two words")] };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "The scope 'two words' of unit 'A' is empty or contains whitespace.");
    }

    [TestMethod]
    public void RejectsTextWithCharactersXmlCannotCarry()
    {
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "bad\u0001char")])]);
        using var stream = new MemoryStream();

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffWriter.Write(document, stream));

        Assert.Contains("cannot carry", exception.Message);

        //Kills XliffWriter.cs:941: without the unit id in the "what" fragment passed to
        //XmlTextProblems for a segment's source, the message reads "The text of  contains a
        //character XML cannot carry." for every unit alike, so asserting the source's own unit
        //context survives in the text tells the mutant apart from the original.
        Assert.Contains("the source of unit 'A'", exception.Message);
        Assert.AreEqual("document", exception.ParamName);
        Assert.AreEqual(0, stream.Length);
    }

    [TestMethod]
    public void RejectsAnUnknownXliffVersionValue()
    {
        //Kills the mutant that drops the Enum.IsDefined check on XliffDocument.Version: a version the
        //writer's VersionText switch does not know would otherwise throw ArgumentOutOfRangeException
        //instead of being reported through Problems().
        AssertRejected(new XliffDocument((XliffVersion)99, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]), "not one the writer can emit");
    }

    [TestMethod]
    public void RejectsAnEmptyMetadataKey()
    {
        //Kills the mutant that drops the empty-key check from the metadata dictionary's Problems():
        //an empty key would still be text XML can carry, so only a dedicated check catches it.
        XliffUnit unit = NewUnit("A", "Home") with { Metadata = ImmutableDictionary<string, string>.Empty.Add("", "value") };
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]);
        using var stream = new MemoryStream();

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffWriter.Write(document, stream));

        Assert.Contains("A key in", exception.Message);

        //Kills XliffWriter.cs:895 ($"the metadata of unit '{unit.Id}'" => $""): without the unit id
        //in "what", the message reads "A key in  is empty." for every unit alike, so asserting the
        //unit's own metadata context survives in the text tells the mutant apart from the original.
        Assert.Contains("the metadata of unit 'A'", exception.Message);
        Assert.AreEqual("document", exception.ParamName);
        Assert.AreEqual(0, stream.Length);
    }

    [TestMethod]
    public void RejectsAGroupIdThatIsNotAnXmlNameToken()
    {
        //Kills the mutant that drops NameTokenProblems from the group's own Problems(): unit and file
        //ids are checked elsewhere (RejectsAnIdThatIsNotAnXmlNameToken), but a group id is a separate
        //check the writer applies on its own.
        var group = new XliffGroup("g roup", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), "group id");
    }

    [TestMethod]
    public void RejectsASegmentIdThatIsNotAnXmlNameToken()
    {
        //Kills the mutant that drops NameTokenProblems from the segment's own Problems(): unit and
        //file ids are checked elsewhere, but a segment id is a separate check the writer applies on
        //its own.
        var unit = new XliffUnit(
            "A",
            [new XliffSegment("s 1", SegmentKind.Translatable, "Home", null, SegmentState.Initial, null)],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "segment id");
    }

    [TestMethod]
    public void RejectsAGroupScopeContainingWhitespace()
    {
        //Kills the mutant that drops ScopeProblems from the group's own Problems(): unit scopes are
        //checked elsewhere (RejectsAScopeContainingWhitespace), but a group's own scopes are a
        //separate check.
        var group = new XliffGroup("G", [new Scope("two words")], ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), "of group");
    }

    [TestMethod]
    public void RejectsAUnitGlossaryScopeContainingWhitespace()
    {
        //Kills the mutant that skips the token-shaped ScopeProblems branch for a unit's glossary
        //(requireTokenScopes: true): the Glossary module's vcl:scopes attribute is space-separated,
        //so a scope with an embedded space could not be told apart from two scopes.
        XliffUnit unit = NewUnit("A", "Home") with
        {
            Glossary = new Glossary([new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, [new Scope("two words")], null)])
        };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "the glossary of unit");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInATarget()
    {
        //The U+0001 control character below is spelled as the escape rather than the raw invisible
        //byte, so it shows up in a plain-text search of this file.
        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", "fi", [], [NewUnit("A", "Home", "bad\u0001target")])]), "the target of unit");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInANote()
    {
        XliffUnit unit = NewUnit("A", "Home", null, "bad\u0001note");

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a note of unit");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAGlossaryTerm()
    {
        XliffUnit unit = NewUnit("A", "Home") with
        {
            Glossary = new Glossary([new GlossaryEntry("bad\u0001term", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)])
        };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a term in");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAToneField()
    {
        var toneProfile = new ToneProfile(
            "1.0",
            "bad\u0001authority",
            ToneRegister.Formal,
            "voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile");
    }

    [TestMethod]
    public void ListsEveryProblemInOneMessage()
    {
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "One", "Yksi"), NewUnit("A", "bad\u0001char")])]);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => WriteToBytes(document));

        Assert.IsTrue(exception.Message.Contains("Duplicate unit id", StringComparison.Ordinal));
        Assert.IsTrue(exception.Message.Contains("declares no target language", StringComparison.Ordinal));
        Assert.IsTrue(exception.Message.Contains("cannot carry", StringComparison.Ordinal));

        //Kills the mutant that turns Serialize's r1 string.Join(" ", problems) into
        //string.Join("", problems) at XliffWriter.cs:128: with an empty separator the two problem
        //sentences below would run together as "...within a file.Unit 'A'..." with no space after the
        //period, so only a check that spans the join point catches it.
        Assert.Contains("unique within a file. Unit 'A' in file 'wallet' carries a target", exception.Message);
    }

    [TestMethod]
    public void ThrowsForNullArguments()
    {
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);
        using var stream = new MemoryStream();
        var pipe = new Pipe();

        Assert.ThrowsExactly<ArgumentNullException>(() => XliffWriter.Write(null!, stream));
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffWriter.Write(document, (Stream)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffWriter.Write(document, (PipeWriter)null!));

        //Kills the mutant that drops XliffWriter.cs:55's ArgumentNullException.ThrowIfNull(document)
        //in the synchronous Write(XliffDocument, PipeWriter) overload: the three calls above never
        //pass a null document to this particular overload, so without this line a null document
        //would reach Serialize instead of failing fast with the documented ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffWriter.Write(null!, pipe.Writer));
    }

    [TestMethod]
    public async Task WriteAsyncThrowsForNullArguments()
    {
        //Kills the mutants that drop XliffWriter.cs's async ArgumentNullException.ThrowIfNull
        //guards: line 74 (document) and line 75 (output) in WriteAsync(XliffDocument, PipeWriter,
        //CancellationToken), and line 111 (document) and line 112 (stream) in
        //WriteAsync(XliffDocument, Stream, CancellationToken). With any one guard removed, the null
        //argument would reach Serialize or the writer's own WriteAsync call and fault the task with
        //a NullReferenceException instead of the documented ArgumentNullException.
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);
        var pipe = new Pipe();
        using var stream = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => XliffWriter.WriteAsync(null!, pipe.Writer, TestContext.CancellationToken));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => XliffWriter.WriteAsync(document, (PipeWriter)null!, TestContext.CancellationToken));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => XliffWriter.WriteAsync(null!, stream, TestContext.CancellationToken));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => XliffWriter.WriteAsync(document, (Stream)null!, TestContext.CancellationToken));
    }

    /// <summary>
    /// A stream that delegates to an in-memory buffer while recording whether <see cref="Flush"/> or
    /// <see cref="FlushAsync(CancellationToken)"/> was called, so a test can tell a dropped Flush
    /// call from a kept one; a plain <see cref="MemoryStream"/> cannot, since its own Flush is a
    /// no-op. Declared once here and reused by WriteAsyncFlushesTheStreamAfterWriting.
    /// </summary>
    private sealed class FlushTrackingStream: MemoryStream
    {
        /// <summary>
        /// Whether the synchronous <see cref="Flush"/> was called.
        /// </summary>
        public bool FlushCalled { get; private set; }

        /// <summary>
        /// Whether the asynchronous <see cref="FlushAsync(CancellationToken)"/> was called.
        /// </summary>
        public bool FlushAsyncCalled { get; private set; }

        /// <inheritdoc/>
        public override void Flush()
        {
            FlushCalled = true;

            base.Flush();
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushAsyncCalled = true;

            return base.FlushAsync(cancellationToken);
        }
    }

    [TestMethod]
    public void FlushesTheStreamAfterWriting()
    {
        //Kills the mutant that drops XliffWriter.cs:96's stream.Flush(): the writer leaves the
        //stream open rather than disposing it (see Write(XliffDocument, Stream)'s own doc comment),
        //so Flush is the only guarantee that every written byte reaches whatever the stream wraps.
        //A plain MemoryStream can't tell a dropped Flush from a kept one, so this uses a stream that
        //records the call.
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);
        using var stream = new FlushTrackingStream();

        XliffWriter.Write(document, stream);

        Assert.IsTrue(stream.FlushCalled);
    }

    [TestMethod]
    public async Task WriteAsyncFlushesTheStreamAfterWriting()
    {
        //Kills the mutant that drops XliffWriter.cs:117's await stream.FlushAsync(...): as with the
        //synchronous overload (see FlushesTheStreamAfterWriting), the writer leaves the stream open,
        //so FlushAsync is the only guarantee that every written byte reaches whatever the stream
        //wraps.
        var document = new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [NewUnit("A", "Home")])]);
        using var stream = new FlushTrackingStream();

        await XliffWriter.WriteAsync(document, stream, TestContext.CancellationToken);

        Assert.IsTrue(stream.FlushAsyncCalled);
    }

    [TestMethod]
    public void WritesFileMetadataWhenOnlyAToneProfileIsPresentWithoutAGlossary()
    {
        //Kills the mutant that turns r1's file.ToneProfile is not null || file.Glossary is not null
        //into && at XliffWriter.cs:195: a file carrying only a tone profile and no file-wide glossary
        //must still get its <mda:metadata> wrapper; the && variant only opens it when both are
        //present, so it would silently drop the tone profile whenever a file has just one of the two.
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "brand voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.IsTrue(xml.Contains("category=\"vericula:tone\"", StringComparison.Ordinal));
        AssertToneProfilesEqual(toneProfile, roundTripped.Files[0].ToneProfile);
    }

    [TestMethod]
    public void WritesMultipleFilesAsSiblingsUnderTheRoot()
    {
        //Kills the mutant that drops WriteFile's own r1 WriteEndElement() at XliffWriter.cs:226:
        //without it the <file> element is never closed there, so the next file's WriteStartElement
        //nests inside it instead of becoming its sibling; a single-file document cannot tell this
        //apart, since the root's own WriteEndElement()/WriteEndDocument() close the still-open <file>
        //anyway, so this needs two files.
        XliffFile first = NewFile("f1", "en", null, [], [NewUnit("A", "One")]);
        XliffFile second = NewFile("f2", "en", null, [], [NewUnit("B", "Two")]);
        var document = new XliffDocument(XliffVersion.V20, [first, second]);

        string xml = Encoding.UTF8.GetString(WriteToBytes(document));

        Assert.Contains("  </file>\n  <file id=\"f2\">", xml);
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAFileWideGlossaryTerm()
    {
        //Kills the mutant that empties the r1 "the glossary of file '{file.Id}'" context argument
        //passed to the glossary Problems() overload at XliffWriter.cs:718:
        //RejectsAU0001CharacterInAGlossaryTerm covers the same character in a unit's own glossary, so
        //this check needs the message to name the file-wide glossary specifically to tell the two call
        //sites apart.
        var fileGlossary = new Glossary([new GlossaryEntry("bad\u0001term", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)]);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, null, fileGlossary, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the glossary of file 'wallet'");
    }

    [TestMethod]
    public void AllowsWhitespaceInAFileGlossaryScopeSinceItTravelsAsMetadataTextNotAToken()
    {
        //Kills the mutant at XliffWriter.cs:718 that flips requireTokenScopes from false to true for
        //a file-wide glossary: unlike a unit's glossary, whose Glossary module vcl:scopes attribute is
        //space-separated and so cannot hold a scope containing a space, a file-wide glossary's scopes
        //travel as Metadata module element content, where a scope with a space is a legal value that
        //must not be rejected.
        var fileGlossary = new Glossary([
            new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, [new Scope("two words")], null)
        ]);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, null, fileGlossary, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        byte[] bytes = WriteToBytes(document);
        XliffDocument roundTripped = Read(bytes);

        Assert.AreEqual("two words", roundTripped.Files[0].Glossary!.Entries[0].Scopes[0].Value);
    }

    [TestMethod]
    public void RejectsAValidationRuleTextWithACharacterXmlCannotCarry()
    {
        //Kills the mutant at XliffWriter.cs:733 that empties the "what" fragment XmlTextProblems
        //receives for a validation rule's text: without "a validation rule of file 'wallet'" in the
        //message, a document with several files could not tell which file's rule needs fixing.
        var rules = new ValidationRuleSet([new PresenceRule("bad\u0001text")]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "a validation rule of file 'wallet'");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAValidationRuleText()
    {
        //Kills the mutant that turns the `yield return problem;` at line 735 into a no-op: a bad
        //character in a validation rule's own text (distinct from an empty value, covered by
        //RejectsAnEmptyRuleText) must still be reported.
        var rules = new ValidationRuleSet([new PresenceRule("bad\u0001text")]);
        XliffFile file = NewFile("wallet", "en", null, [], [NewUnit("A", "Home")]) with { ValidationRules = rules };

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "a validation rule of file 'wallet'");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInTheToneProfileVersion()
    {
        //Kills the mutant at XliffWriter.cs:783 that empties the "what" fragment XmlTextProblems
        //receives for tone.Version: without "the tone profile version" in the message, a bad
        //character there could not be told apart from one in another tone profile field.
        var toneProfile = new ToneProfile(
            "bad\u0001version",
            "house-tone",
            ToneRegister.Formal,
            "voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile version");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAToneVersion()
    {
        //Kills the mutant that turns the `yield return problem;` at line 785 into a no-op: a bad
        //character in the tone profile's own version text must be reported, distinct from the
        //authority field RejectsAU0001CharacterInAToneField already covers.
        var toneProfile = new ToneProfile(
            "bad\u0001version",
            null,
            ToneRegister.Formal,
            null,
            TextOrientation.Horizontal,
            null,
            null,
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile version");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInTheToneProfileVoice()
    {
        //Kills the mutant at XliffWriter.cs:793 that empties the "what" fragment XmlTextProblems
        //receives for tone.Voice: without "the tone profile voice" in the message, a bad character
        //there could not be told apart from one in another tone profile field.
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "bad\u0001voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile voice");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAToneVoice()
    {
        //Kills the mutant that turns the `yield return problem;` at line 795 into a no-op: a bad
        //character in the tone profile's voice text must be reported on its own.
        var toneProfile = new ToneProfile(
            "1.0",
            null,
            ToneRegister.Formal,
            "bad\u0001voice",
            TextOrientation.Horizontal,
            null,
            null,
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile voice");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInTheToneProfileCalendar()
    {
        //Kills the mutant at XliffWriter.cs:798 that empties the "what" fragment XmlTextProblems
        //receives for tone.Calendar: without "the tone profile calendar" in the message, a bad
        //character there could not be told apart from one in another tone profile field.
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "voice",
            TextOrientation.Horizontal,
            "bad\u0001calendar",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile calendar");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAToneCalendar()
    {
        //Kills the mutant that turns the yield return problem; at line 800 into a no-op: a bad
        //character in the tone profile's calendar text must be reported on its own.
        var toneProfile = new ToneProfile(
            "1.0",
            null,
            ToneRegister.Formal,
            null,
            TextOrientation.Horizontal,
            "bad\u0001calendar",
            null,
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile calendar");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInTheToneProfileDateFormat()
    {
        //Kills the mutant at XliffWriter.cs:803 that empties the "what" fragment XmlTextProblems
        //receives for tone.DateFormat: without "the tone profile date format" in the message, a bad
        //character there could not be told apart from one in another tone profile field.
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "voice",
            TextOrientation.Horizontal,
            "gregorian",
            "bad\u0001dateformat",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile date format");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAToneDateFormat()
    {
        //Kills the mutant that turns the yield return problem; at line 805 into a no-op: a bad
        //character in the tone profile's date format text must be reported on its own.
        var toneProfile = new ToneProfile(
            "1.0",
            null,
            ToneRegister.Formal,
            null,
            TextOrientation.Horizontal,
            null,
            "bad\u0001dateformat",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile date format");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAToneProfileMetadataValue()
    {
        //Kills the mutant at XliffWriter.cs:808 that empties the "what" fragment passed to the tone
        //profile free-form metadata Problems(): without "the tone profile metadata" in the message,
        //a bad character there could not be told apart from one in a group or unit own metadata.
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty.Add("channel", "bad\u0001value"));
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the tone profile metadata");
    }

    [TestMethod]
    public void RejectsAnEmptyKeyInAToneProfilesMetadata()
    {
        //Kills the mutant at XliffWriter.cs:810 (yield return problem; dropped) that drops the tone
        //profile own metadata problems from Problems(ToneProfile): without the forward, an empty key
        //in tone.Metadata would never reach Problems(XliffFile) and the document would be written as
        //if valid.
        var toneProfile = new ToneProfile(
            "1.0",
            "house-tone",
            ToneRegister.Formal,
            "brand voice",
            TextOrientation.Horizontal,
            "gregorian",
            "yyyy-MM-dd",
            DigitStyle.HalfWidth,
            ImmutableDictionary<string, string>.Empty.Add("", "value"));
        var file = new XliffFile("wallet", new LanguageTag("en"), null, toneProfile, null, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "A key in the tone profile metadata");
    }

    [TestMethod]
    public void AllowsGroupNestingExactlyAt64Levels()
    {
        //Kills the mutant at XliffWriter.cs:828 that changes depth greater-than MaxGroupDepth to
        //greater-than-or-equal MaxGroupDepth: the reader own bound allows a chain exactly
        //64 levels deep, so this depth must still be writable, not off-by-one rejected.
        XliffGroup group = new("g63", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);
        for(int level = 0; level < 63; level++)
        {
            group = new XliffGroup($"g{level}", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, ImmutableArray<XliffUnit>.Empty, [group]);
        }

        byte[] bytes = WriteToBytes(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]));

        Assert.IsTrue(bytes.Length > 0);
    }

    [TestMethod]
    public void ReportsGroupNestingDepthOnlyOnceEvenWhenNestingContinuesPastTheLimit()
    {
        //Kills the mutant at XliffWriter.cs:832 that replaces yield break with a no-op statement: once
        //a group nests past the 64-level cap the recursion must stop there, so a chain that keeps
        //nesting two levels past the cap must still report the problem exactly once, not once per
        //level still beneath the group that first exceeded it.
        XliffGroup group = new("g65", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);
        for(int level = 0; level < 65; level++)
        {
            group = new XliffGroup($"g{level}", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, ImmutableArray<XliffUnit>.Empty, [group]);
        }

        using var stream = new MemoryStream();
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => XliffWriter.Write(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), stream));

        int occurrences = exception.Message.Split("nests more than 64 levels").Length - 1;
        Assert.AreEqual(1, occurrences);
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAGroupMetadataValue()
    {
        //Kills the mutant at XliffWriter.cs:845 that empties the "what" fragment passed to a group
        //own metadata Problems(): without "the metadata of group 'G'" in the message, a bad character
        //there could not be told apart from one in a unit or a tone profile metadata.
        var group = new XliffGroup("G", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty.Add("channel", "bad\u0001value"), [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), "the metadata of group 'G'");
    }

    [TestMethod]
    public void RejectsAnEmptyKeyInAGroupsMetadata()
    {
        //Kills the mutant at XliffWriter.cs:847 (yield return problem; dropped) that drops a group
        //own metadata problems from Problems(XliffGroup): without the forward, an empty key in
        //group.Metadata would never reach Problems(XliffFile) and the document would be written as if
        //valid.
        var group = new XliffGroup("G", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty.Add("", "value"), [NewUnit("A", "Home")], ImmutableArray<XliffGroup>.Empty);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), "A key in the metadata of group 'G'");
    }

    [TestMethod]
    public void RejectsAnInvalidUnitThatIsDirectlyInAGroup()
    {
        //Kills the mutant at XliffWriter.cs:862 (yield return problem; dropped) that drops the
        //problems of a unit sitting directly in group.Units (as opposed to one reached through a
        //nested group): without the forward, a malformed unit placed directly under a group would be
        //written unchecked.
        var group = new XliffGroup("G", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("Tab Home", "Home")], ImmutableArray<XliffGroup>.Empty);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [group], [])]), "not an XML name token");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInASegmentSubState()
    {
        //Kills the mutant at XliffWriter.cs:951 that empties the sub-state "what" fragment: without
        //the unit id in that fragment, the message reads generically for every unit alike, so
        //asserting the sub-state own unit context survives in the text tells the mutant apart from
        //the original.
        var unit = new XliffUnit(
            "A",
            [new XliffSegment(null, SegmentKind.Translatable, "Home", null, SegmentState.Translated, "bad\u0001substate")],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "the sub-state of a segment in unit 'A'");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInASegmentsSubState()
    {
        //Kills the mutant at XliffWriter.cs:953 (yield return problem; dropped) that drops the
        //sub-state text problems from Problems(XliffSegment): without the forward, a control character
        //in segment.SubState that XML cannot carry would reach the output unchecked.
        var unit = new XliffUnit(
            "A",
            [new XliffSegment(null, SegmentKind.Translatable, "Home", null, SegmentState.Translated, "bad\u0001sub")],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "the sub-state of a segment in unit");
    }

    [TestMethod]
    public void RejectsAnIgnorableSegmentCarryingOnlyASubState()
    {
        //Kills XliffWriter.cs:966 (segment.State different-from SegmentState.Initial OR segment.SubState
        //not null, changed to AND): with State left at its default Initial, only the sub-state half of
        //the OR is true, so the AND variant would accept and write this ignorable segment instead of
        //rejecting it.
        var unit = new XliffUnit(
            "A",
            [
                new XliffSegment("s1", SegmentKind.Translatable, "Home", null, SegmentState.Initial, null),
                new XliffSegment("i1", SegmentKind.Ignorable, " ", null, SegmentState.Initial, "tool:x")
            ],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "ignorable segment");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAGlossaryTranslation()
    {
        //Kills XliffWriter.cs:988 ($"a translation in {what}" => $""): without "a translation in
        //{what}", the message reads "The text of  contains a character XML cannot carry." with no
        //hint that a glossary translation was the offender.
        //Kills the mutant at XliffWriter.cs:990 (yield return problem; dropped) that drops a glossary
        //entry's translation text problems: without the forward, a control character in
        //entry.Translation that XML cannot carry would reach the output unchecked.
        XliffUnit unit = NewUnit("A", "Home") with
        {
            Glossary = new Glossary([new GlossaryEntry("wallet", "bad\u0001translation", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)])
        };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a translation in");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAGlossaryDefinition()
    {
        //Kills XliffWriter.cs:993 ($"a definition in {what}" => $""): without "a definition in
        //{what}", the message reads "The text of  contains a character XML cannot carry." with no
        //hint that a glossary definition was the offender.
        //Kills the mutant at XliffWriter.cs:995 (yield return problem; dropped) that drops a glossary
        //entry's definition text problems: without the forward, a control character in
        //entry.Definition that XML cannot carry would reach the output unchecked.
        XliffUnit unit = NewUnit("A", "Home") with
        {
            Glossary = new Glossary([new GlossaryEntry("wallet", "lompakko", "bad\u0001definition", GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)])
        };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a definition in");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAGlossaryRationale()
    {
        //Kills XliffWriter.cs:998 ($"a rationale in {what}" => $""): without "a rationale in {what}",
        //the message reads "The text of  contains a character XML cannot carry." with no hint that a
        //glossary rationale was the offender.
        //Kills the mutant at XliffWriter.cs:1000 (yield return problem; dropped) that drops a
        //glossary entry's rationale text problems: without the forward, a control character in
        //entry.Rationale that XML cannot carry would reach the output unchecked.
        XliffUnit unit = NewUnit("A", "Home") with
        {
            Glossary = new Glossary([new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, "bad\u0001rationale")])
        };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a rationale in");
    }

    [TestMethod]
    public void AcceptsWhitespaceInAFileGlossaryScopeBecauseItTravelsAsMetadataText()
    {
        //Kills XliffWriter.cs:1003 (the requireTokenScopes ternary forced to the true branch): a
        //file-level glossary carries its scopes through the Metadata module as element content
        //(requireTokenScopes: false), where whitespace is legal text; the mutant always applies the
        //Glossary module's stricter, space-separated-token check and would reject this scope instead
        //of writing it.
        var fileGlossary = new Glossary([new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, [new Scope("two words")], null)]);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, null, fileGlossary, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        byte[] bytes = WriteToBytes(new XliffDocument(XliffVersion.V20, [file]));

        Assert.IsTrue(bytes.Length > 0);
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAFileGlossaryScope()
    {
        //Kills XliffWriter.cs:1005 ($"a scope in {what}" => $""): the file-level glossary scopes
        //travel through the Metadata module text-only check (requireTokenScopes: false), so without
        //"a scope in {what}" the message reads generically with no hint that a glossary scope was the
        //offender.
        var fileGlossary = new Glossary([new GlossaryEntry("wallet", "lompakko", null, GlossaryEntryStatus.Preferred, [new Scope("bad\u0001scope")], null)]);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, null, fileGlossary, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "a scope in");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAUnitScope()
    {
        //Kills XliffWriter.cs:1021 ($"a scope of {what}" => $""): without "a scope of {what}", the
        //message reads generically with no hint that a unit's own scope was the offender.
        XliffUnit unit = NewUnit("A", "Home") with { Scopes = [new Scope("bad\u0001scope")] };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a scope of unit 'A'");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAScope()
    {
        //Kills the mutant at XliffWriter.cs:1023 (yield return problem; dropped) that drops a
        //scope's own XmlTextProblems forward inside ScopeProblems: the value below has no whitespace,
        //so only this check (not the empty-or-whitespace check) can catch the character XML cannot
        //carry.
        XliffUnit unit = NewUnit("A", "Home") with { Scopes = [new Scope("bad\u0001scope")] };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a scope of unit 'A' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAMetadataKey()
    {
        //Kills XliffWriter.cs:1046 ($"a key in {what}" => $""): without "a key in {what}", the
        //message reads generically with no hint that a metadata key was the offender.
        //Kills the mutant at XliffWriter.cs:1048 (yield return problem; dropped) that drops a
        //metadata key's own XmlTextProblems forward: the key below is non-whitespace, so only this
        //check (not the empty-key check) can catch the character XML cannot carry.
        XliffUnit unit = NewUnit("A", "Home") with { Metadata = ImmutableDictionary<string, string>.Empty.Add("bad\u0001key", "value") };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a key in the metadata of unit 'A' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAMetadataValue()
    {
        //Kills XliffWriter.cs:1051 ($"a value in {what}" => $""): without "a value in {what}", the
        //message reads generically with no hint that a metadata value was the offender.
        //Kills the mutant at XliffWriter.cs:1053 (yield return problem; dropped) that drops a
        //metadata value's own XmlTextProblems forward: without it, a control character in a metadata
        //value that XML cannot carry would reach the output unchecked.
        XliffUnit unit = NewUnit("A", "Home") with { Metadata = ImmutableDictionary<string, string>.Empty.Add("key", "bad\u0001value") };

        AssertRejected(new XliffDocument(XliffVersion.V20, [NewFile("wallet", "en", null, [], [unit])]), "a value in the metadata of unit 'A' contains a character XML cannot carry");
    }

    [TestMethod]
    public void AllowsAFileWithNoTargetLanguageAndNoTargetTextWhenAnotherFileDeclaresOne()
    {
        //Kills the mutant that flips HasAnyTarget's final return false; (line 677) to return
        //true;: f1 declares no target language and no segment of it carries a target, so
        //HasAnyTarget(f1) must stay false even though f2's trgLang makes the document have one;
        //the write must succeed instead of wrongly refusing f1 for target text it does not carry.
        XliffFile f1 = NewFile("f1", "en", null, [], [NewUnit("u1", "Home")]);
        XliffFile f2 = NewFile("f2", "en", "fi", [], [NewUnit("u2", "Wallet", "Lompakko")]);
        var document = new XliffDocument(XliffVersion.V20, [f1, f2]);

        string xml = Encoding.UTF8.GetString(WriteToBytes(document));

        Assert.IsTrue(xml.Contains("<file id=\"f1\">", StringComparison.Ordinal));
        Assert.IsTrue(xml.Contains("<file id=\"f2\">", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RejectsAU0001CharacterInAFileGlossaryTerm()
    {
        //Kills the mutant that turns the yield return problem; at line 720 into a no-op: that loop
        //reports problems in a file-wide glossary (requireTokenScopes: false), a different call site
        //from RejectsAU0001CharacterInAGlossaryTerm's unit-level glossary, so a bad character here must
        //still be reported on its own.
        var fileGlossary = new Glossary([new GlossaryEntry("bad\u0001term", "lompakko", null, GlossaryEntryStatus.Preferred, ImmutableArray<Scope>.Empty, null)]);
        var file = new XliffFile("wallet", new LanguageTag("en"), null, null, fileGlossary, null, ImmutableArray<XliffGroup>.Empty, [NewUnit("A", "Home")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "the glossary of file 'wallet'");
    }

    [TestMethod]
    public void RejectsADuplicateUnitIdSharedBetweenAFileAndAGroupItOwns()
    {
        //Kills the mutant that drops XliffWriter.cs:1161's yield return unit; inside Flatten(XliffFile):
        //blanking this yield stops a group's units from ever reaching the file-level duplicate-id scan,
        //so a unit id repeated between the file's own units and a unit inside its group would go
        //unreported; this test's unit "A" appears once at file level and once inside the group.
        var group = new XliffGroup("Shell", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Two")], ImmutableArray<XliffGroup>.Empty);
        XliffFile file = NewFile("wallet", "en", null, [group], [NewUnit("A", "One")]);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "Duplicate unit id 'A'");
    }

    [TestMethod]
    public void RejectsDuplicateUnitIdsWithinTheSameGroup()
    {
        //Kills the mutant that drops XliffWriter.cs:1173's yield return unit; inside Flatten(XliffGroup):
        //blanking this yield stops a group's own directly-owned units from reaching the duplicate-id
        //scan at all (the group has no nested groups to fall back on), so two units sharing an id
        //inside the same group would go unreported.
        var group = new XliffGroup("Shell", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "One"), NewUnit("A", "Two")], ImmutableArray<XliffGroup>.Empty);
        XliffFile file = NewFile("wallet", "en", null, [group], []);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "Duplicate unit id 'A'");
    }

    [TestMethod]
    public void RejectsADuplicateUnitIdSharedWithANestedGroup()
    {
        //Kills the mutant that drops XliffWriter.cs:1180's yield return unit; inside Flatten(XliffGroup):
        //blanking this yield stops a nested group's units from reaching its parent group's flattened
        //output, so a unit id repeated between a group and the group nested inside it would go
        //unreported; this test's unit "A" appears once directly in the outer group and once inside
        //the group nested within it.
        var inner = new XliffGroup("Cards", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "Two")], ImmutableArray<XliffGroup>.Empty);
        var outer = new XliffGroup("Shell", ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, [NewUnit("A", "One")], [inner]);
        XliffFile file = NewFile("wallet", "en", null, [outer], []);

        AssertRejected(new XliffDocument(XliffVersion.V20, [file]), "Duplicate unit id 'A'");
    }

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Asserts that writing <paramref name="document"/> throws an <see cref="ArgumentException"/> whose
    /// message contains <paramref name="expectedFragment"/>, names <c>document</c> as its parameter, and
    /// that nothing was written to the stream.
    /// </summary>
    /// <param name="document">The document expected to be refused.</param>
    /// <param name="expectedFragment">A substring the refusal's message must contain.</param>
    private static void AssertRejected(XliffDocument document, string expectedFragment)
    {
        using var stream = new MemoryStream();

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffWriter.Write(document, stream));

        Assert.IsTrue(exception.Message.Contains(expectedFragment, StringComparison.Ordinal), exception.Message);
        Assert.AreEqual("document", exception.ParamName);
        Assert.AreEqual(0, stream.Length);
    }

    /// <summary>
    /// Builds a single-segment translatable unit with the given source and target text and notes.
    /// </summary>
    /// <param name="id">The unit's id.</param>
    /// <param name="source">The segment's source text.</param>
    /// <param name="target">The segment's target text, or null for no target.</param>
    /// <param name="notes">The unit's notes.</param>
    /// <returns>The built unit.</returns>
    private static XliffUnit NewUnit(string id, string source, string? target = null, params string[] notes)
    {
        return XliffUnit.FromText(id, source, target) with { Notes = [.. notes] };
    }

    /// <summary>
    /// Builds a file with no tone profile, glossary or validation rules, carrying the given groups and units.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="sourceLanguage">The file's source language.</param>
    /// <param name="targetLanguage">The file's target language, or null for none.</param>
    /// <param name="groups">The file's groups.</param>
    /// <param name="units">The file's units.</param>
    /// <returns>The built file.</returns>
    private static XliffFile NewFile(
        string id,
        string sourceLanguage,
        string? targetLanguage,
        ImmutableArray<XliffGroup> groups,
        ImmutableArray<XliffUnit> units)
    {
        return new XliffFile(
            id,
            new LanguageTag(sourceLanguage),
            targetLanguage is null ? null : new LanguageTag(targetLanguage),
            null,
            null,
            null,
            groups,
            units);
    }

    /// <summary>
    /// Writes <paramref name="document"/> and returns the serialized bytes.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <returns>The written bytes.</returns>
    private static byte[] WriteToBytes(XliffDocument document)
    {
        using var stream = new MemoryStream();
        XliffWriter.Write(document, stream);

        return stream.ToArray();
    }

    /// <summary>
    /// Reads a document back from bytes previously written by the writer.
    /// </summary>
    /// <param name="xml">The XLIFF bytes to read.</param>
    /// <returns>The parsed document.</returns>
    private static XliffDocument Read(byte[] xml)
    {
        using var stream = new MemoryStream(xml);

        return XliffReader.Read(stream);
    }

    /// <summary>
    /// Drains <paramref name="reader"/> to a byte array, bounded by a 10-second timeout linked to
    /// <paramref name="cancellationToken"/> so a writer that never completes the pipe fails the
    /// calling test instead of hanging the run.
    /// </summary>
    /// <param name="reader">The pipe reader to drain.</param>
    /// <param name="cancellationToken">The test's own cancellation token.</param>
    /// <returns>Every byte read from <paramref name="reader"/>.</returns>
    private static async Task<byte[]> DrainAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using var drained = new MemoryStream();
        while(true)
        {
            ReadResult result = await reader.ReadAsync(bounded.Token);
            foreach(ReadOnlyMemory<byte> segment in result.Buffer)
            {
                drained.Write(segment.Span);
            }

            reader.AdvanceTo(result.Buffer.End);
            if(result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();

        return drained.ToArray();
    }

    /// <summary>
    /// Asserts that two documents carry the same version and, file by file, the same content.
    /// </summary>
    /// <param name="expected">The document expected.</param>
    /// <param name="actual">The document to compare against <paramref name="expected"/>.</param>
    private static void AssertDocumentsEqual(XliffDocument expected, XliffDocument actual)
    {
        Assert.AreEqual(expected.Version, actual.Version);
        Assert.AreEqual(expected.Files.Length, actual.Files.Length);
        for(int index = 0; index < expected.Files.Length; index++)
        {
            AssertFilesEqual(expected.Files[index], actual.Files[index]);
        }
    }

    /// <summary>
    /// Asserts that two files carry the same id, languages, tone profile, glossary, validation rules,
    /// groups and units.
    /// </summary>
    /// <param name="expected">The file expected.</param>
    /// <param name="actual">The file to compare against <paramref name="expected"/>.</param>
    private static void AssertFilesEqual(XliffFile expected, XliffFile actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.SourceLanguage.Value, actual.SourceLanguage.Value);
        Assert.AreEqual(expected.TargetLanguage?.Value, actual.TargetLanguage?.Value);
        AssertToneProfilesEqual(expected.ToneProfile, actual.ToneProfile);
        AssertGlossariesEqual(expected.Glossary, actual.Glossary);
        AssertRuleSetsEqual(expected.ValidationRules, actual.ValidationRules);

        Assert.AreEqual(expected.Groups.Length, actual.Groups.Length);
        for(int index = 0; index < expected.Groups.Length; index++)
        {
            AssertGroupsEqual(expected.Groups[index], actual.Groups[index]);
        }

        Assert.AreEqual(expected.Units.Length, actual.Units.Length);
        for(int index = 0; index < expected.Units.Length; index++)
        {
            AssertUnitsEqual(expected.Units[index], actual.Units[index]);
        }
    }

    /// <summary>
    /// Asserts that two groups carry the same id, scopes, metadata, units and nested groups, recursively.
    /// </summary>
    /// <param name="expected">The group expected.</param>
    /// <param name="actual">The group to compare against <paramref name="expected"/>.</param>
    private static void AssertGroupsEqual(XliffGroup expected, XliffGroup actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        AssertScopesEqual(expected.Scopes, actual.Scopes);
        AssertDictionariesEqual(expected.Metadata, actual.Metadata);

        Assert.AreEqual(expected.Units.Length, actual.Units.Length);
        for(int index = 0; index < expected.Units.Length; index++)
        {
            AssertUnitsEqual(expected.Units[index], actual.Units[index]);
        }

        Assert.AreEqual(expected.Groups.Length, actual.Groups.Length);
        for(int index = 0; index < expected.Groups.Length; index++)
        {
            AssertGroupsEqual(expected.Groups[index], actual.Groups[index]);
        }
    }

    /// <summary>
    /// Asserts that two units carry the same id, segments, notes, scopes, metadata and glossary.
    /// </summary>
    /// <param name="expected">The unit expected.</param>
    /// <param name="actual">The unit to compare against <paramref name="expected"/>.</param>
    private static void AssertUnitsEqual(XliffUnit expected, XliffUnit actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        CollectionAssert.AreEqual(expected.Segments.ToArray(), actual.Segments.ToArray());
        CollectionAssert.AreEqual(expected.Notes.ToArray(), actual.Notes.ToArray());
        AssertScopesEqual(expected.Scopes, actual.Scopes);
        AssertDictionariesEqual(expected.Metadata, actual.Metadata);
        AssertGlossariesEqual(expected.Glossary, actual.Glossary);
    }

    /// <summary>
    /// Asserts that two optional tone profiles are both null or both carry the same fields and metadata.
    /// </summary>
    /// <param name="expected">The tone profile expected, or null.</param>
    /// <param name="actual">The tone profile to compare against <paramref name="expected"/>, or null.</param>
    private static void AssertToneProfilesEqual(ToneProfile? expected, ToneProfile? actual)
    {
        if(expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);

            return;
        }

        Assert.AreEqual(expected with { Metadata = ImmutableDictionary<string, string>.Empty }, actual with { Metadata = ImmutableDictionary<string, string>.Empty });
        AssertDictionariesEqual(expected.Metadata, actual.Metadata);
    }

    /// <summary>
    /// Asserts that two optional glossaries are both null or carry the same entries in the same order,
    /// each entry's scopes compared separately.
    /// </summary>
    /// <param name="expected">The glossary expected, or null.</param>
    /// <param name="actual">The glossary to compare against <paramref name="expected"/>, or null.</param>
    private static void AssertGlossariesEqual(Glossary? expected, Glossary? actual)
    {
        if(expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);

            return;
        }

        Assert.AreEqual(expected.Entries.Length, actual.Entries.Length);
        for(int index = 0; index < expected.Entries.Length; index++)
        {
            GlossaryEntry expectedEntry = expected.Entries[index];
            GlossaryEntry actualEntry = actual.Entries[index];
            Assert.AreEqual(expectedEntry with { Scopes = ImmutableArray<Scope>.Empty }, actualEntry with { Scopes = ImmutableArray<Scope>.Empty });
            AssertScopesEqual(expectedEntry.Scopes, actualEntry.Scopes);
        }
    }

    /// <summary>
    /// Asserts that two optional validation rule sets are both null or carry the same rules in the same order.
    /// </summary>
    /// <param name="expected">The rule set expected, or null.</param>
    /// <param name="actual">The rule set to compare against <paramref name="expected"/>, or null.</param>
    private static void AssertRuleSetsEqual(ValidationRuleSet? expected, ValidationRuleSet? actual)
    {
        if(expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);

            return;
        }

        CollectionAssert.AreEqual(expected.Rules.ToArray(), actual.Rules.ToArray());
    }

    /// <summary>
    /// Asserts that two scope arrays carry the same scopes in the same order.
    /// </summary>
    /// <param name="expected">The scopes expected.</param>
    /// <param name="actual">The scopes to compare against <paramref name="expected"/>.</param>
    private static void AssertScopesEqual(ImmutableArray<Scope> expected, ImmutableArray<Scope> actual)
    {
        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
    }

    /// <summary>
    /// Asserts that two metadata dictionaries carry the same entries, regardless of order.
    /// </summary>
    /// <param name="expected">The dictionary expected.</param>
    /// <param name="actual">The dictionary to compare against <paramref name="expected"/>.</param>
    private static void AssertDictionariesEqual(ImmutableDictionary<string, string> expected, ImmutableDictionary<string, string> actual)
    {
        CollectionAssert.AreEquivalent(expected.ToArray(), actual.ToArray());
    }
}
