using System.Collections.Immutable;
using System.Text;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

/// <summary>
/// Tests for <see cref="XliffWriter"/>'s inline content writing (step 3 of the inline content slice):
/// the mixed-content mechanism of 5.4, <c>cp</c> encoding, attribute default omission, the
/// <c>&lt;originalData&gt;</c> ordering, and every rule <c>Problems()</c> gained for inline content.
/// <see cref="XliffWriterTests"/> keeps the writer's structural tests; this file is only about what a
/// segment's inline content writes as.
/// </summary>
[TestClass]
public sealed class XliffWriterInlineContentTests
{
    /// <summary>Wraps a unit body (its children, typically <c>&lt;segment&gt;</c>/<c>&lt;originalData&gt;</c> elements) in a minimal XLIFF 2.1 document with a target language.</summary>
    private static string Wrap(string unitBody) =>
        $"""<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="fi"><file id="f"><unit id="u">{unitBody}</unit></file></xliff>""";

    /// <summary>Parses <paramref name="unitBody"/> (wrapped by <see cref="Wrap"/>) with the whole-document reader.</summary>
    private static XliffDocument ReadDocument(string unitBody) => XliffReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(Wrap(unitBody))));

    /// <summary>Writes <paramref name="document"/> and returns the serialized bytes.</summary>
    private static byte[] WriteToBytes(XliffDocument document)
    {
        using var stream = new MemoryStream();
        XliffWriter.Write(document, stream);

        return stream.ToArray();
    }

    /// <summary>Reads a document back from bytes previously written by the writer.</summary>
    private static XliffDocument Read(byte[] xml) => XliffReader.Read(new MemoryStream(xml));

    /// <summary>Asserts that two units carry the same id and the same segments (by <see cref="XliffSegment"/>'s own equality, which delegates to <see cref="InlineContent.Equals(InlineContent?)"/>).</summary>
    private static void AssertUnitsEqual(XliffUnit expected, XliffUnit actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        CollectionAssert.AreEqual(expected.Segments.ToArray(), actual.Segments.ToArray());
    }

    /// <summary>Builds a document with one file (source "en", target "fi") carrying <paramref name="unit"/>.</summary>
    private static XliffDocument DocumentOf(XliffUnit unit) =>
        new(XliffVersion.V21, [new XliffFile("f", new LanguageTag("en"), new LanguageTag("fi"), null, null, null, ImmutableArray<XliffGroup>.Empty, [unit])]);

    /// <summary>Builds a unit with one translatable, id-less segment holding <paramref name="sourceParts"/> as its source and no target.</summary>
    private static XliffUnit UnitOf(params InlinePart[] sourceParts)
    {
        var segment = new XliffSegment(null, SegmentKind.Translatable, InlineContent.Create(sourceParts), null, SegmentState.Initial, null);

        return new XliffUnit("u", [segment], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);
    }

    /// <summary>Asserts that writing a document carrying <paramref name="unit"/> throws an <see cref="ArgumentException"/> whose message contains <paramref name="expectedFragment"/>, and that nothing was written.</summary>
    private static void AssertUnitRejected(XliffUnit unit, string expectedFragment)
    {
        using var stream = new MemoryStream();

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffWriter.Write(DocumentOf(unit), stream));

        Assert.Contains(expectedFragment, exception.Message);
        Assert.AreEqual(0, stream.Length);
    }

    /// <summary>Builds a <see cref="PlaceholderPart"/> (a <c>ph</c>) with every field at its spec default unless overridden.</summary>
    private static PlaceholderPart Ph(
        string id,
        InlineCodeType type = InlineCodeType.None,
        string? subType = null,
        string equiv = "",
        string? disp = null,
        string? dataRef = null,
        OriginalData? originalData = null,
        bool canCopy = true,
        bool canDelete = true,
        ReorderHint canReorder = ReorderHint.Yes,
        string? copyOf = null,
        string? subFlows = null) =>
        new(
            Id: id,
            Type: type,
            SubType: subType,
            Equiv: equiv,
            Disp: disp,
            DataRef: dataRef,
            OriginalData: originalData,
            CanCopy: canCopy,
            CanDelete: canDelete,
            CanReorder: canReorder,
            CopyOf: copyOf,
            SubFlows: subFlows);

    /// <summary>Builds a <c>Split</c>-form <see cref="StartCodePart"/> (an <c>sc</c>) with every field at its spec default unless overridden.</summary>
    private static StartCodePart Sc(
        string id,
        InlineCodeType type = InlineCodeType.None,
        bool canOverlap = true,
        bool isolated = false,
        TextDirection direction = TextDirection.Inherited) =>
        new(
            Id: id,
            Type: type,
            SubType: null,
            Equiv: string.Empty,
            Disp: null,
            DataRef: null,
            OriginalData: null,
            CanCopy: true,
            CanDelete: true,
            CanReorder: ReorderHint.Yes,
            CopyOf: null,
            SubFlows: null,
            CanOverlap: canOverlap,
            Isolated: isolated,
            Direction: direction,
            Form: SpanForm.Split);

    /// <summary>Builds a non-isolated, <c>Split</c>-form <see cref="EndCodePart"/> (an <c>ec</c>) closing <paramref name="startRef"/>, every other field at its spec default.</summary>
    private static EndCodePart Ec(string startRef) =>
        new(
            StartRef: startRef,
            Id: null,
            Type: InlineCodeType.None,
            SubType: null,
            Equiv: string.Empty,
            Disp: null,
            DataRef: null,
            OriginalData: null,
            CanCopy: true,
            CanDelete: true,
            CanOverlap: true,
            CanReorder: ReorderHint.Yes,
            CopyOf: null,
            SubFlows: null,
            Isolated: false,
            Direction: TextDirection.Inherited,
            Form: SpanForm.Split);

    /// <summary>Builds an isolated, <c>Split</c>-form <see cref="EndCodePart"/> (an <c>ec</c>) with its own id and no <c>startRef</c>.</summary>
    private static EndCodePart IsolatedEc(string id) =>
        new(
            StartRef: null,
            Id: id,
            Type: InlineCodeType.None,
            SubType: null,
            Equiv: string.Empty,
            Disp: null,
            DataRef: null,
            OriginalData: null,
            CanCopy: true,
            CanDelete: true,
            CanOverlap: true,
            CanReorder: ReorderHint.Yes,
            CopyOf: null,
            SubFlows: null,
            Isolated: true,
            Direction: TextDirection.Inherited,
            Form: SpanForm.Split);

    /// <summary>Builds the <c>Paired</c>-form <see cref="StartCodePart"/> half of a <c>pc</c>; pair with <see cref="PcEnd"/> using the same id.</summary>
    private static StartCodePart PcStart(string id, InlineCodeType type = InlineCodeType.None, bool isolated = false) =>
        new(
            Id: id,
            Type: type,
            SubType: null,
            Equiv: string.Empty,
            Disp: null,
            DataRef: null,
            OriginalData: null,
            CanCopy: true,
            CanDelete: true,
            CanReorder: ReorderHint.Yes,
            CopyOf: null,
            SubFlows: null,
            CanOverlap: false,
            Isolated: isolated,
            Direction: TextDirection.Inherited,
            Form: SpanForm.Paired);

    /// <summary>Builds the <c>Paired</c>-form <see cref="EndCodePart"/> half of a <c>pc</c>; pair with <see cref="PcStart"/> using the same id (its <c>StartRef</c>).</summary>
    private static EndCodePart PcEnd(string id, InlineCodeType type = InlineCodeType.None) =>
        new(
            StartRef: id,
            Id: null,
            Type: type,
            SubType: null,
            Equiv: string.Empty,
            Disp: null,
            DataRef: null,
            OriginalData: null,
            CanCopy: true,
            CanDelete: true,
            CanOverlap: false,
            CanReorder: ReorderHint.Yes,
            CopyOf: null,
            SubFlows: null,
            Isolated: false,
            Direction: TextDirection.Inherited,
            Form: SpanForm.Paired);

    /// <summary>Builds a <c>Marker</c>-form <see cref="AnnotationStartPart"/> (a <c>mrk</c> opening); pair with <see cref="MrkEnd"/> using the same id.</summary>
    private static AnnotationStartPart Mrk(string id, string type = "generic", bool? translate = null, string? refValue = null, string? value = null) =>
        new(id, type, translate, refValue, value, AnnotationForm.Marker);

    /// <summary>Builds a <c>Marker</c>-form <see cref="AnnotationEndPart"/> (a <c>mrk</c> closing) closing <paramref name="id"/>.</summary>
    private static AnnotationEndPart MrkEnd(string id) => new(id, AnnotationForm.Marker);

    /// <summary>Builds a <c>Split</c>-form <see cref="AnnotationStartPart"/> (an <c>sm</c>), <c>type</c> <c>generic</c> and nothing else set; pair with <see cref="Em"/> using the same id.</summary>
    private static AnnotationStartPart Sm(string id) => new(id, "generic", null, null, null, AnnotationForm.Split);

    /// <summary>Builds a <c>Split</c>-form <see cref="AnnotationEndPart"/> (an <c>em</c>) closing <paramref name="startRef"/>.</summary>
    private static AnnotationEndPart Em(string startRef) => new(startRef, AnnotationForm.Split);

    //---- Round-trip corpus (5.4's last bullet) ----------------------------------------------------

    /// <summary>
    /// The corpus 5.4's last bullet asks for: a <c>dir="rtl"</c> data entry, a two-segment
    /// <c>sc</c>/<c>ec</c> pair, a nested <c>pc</c>, an <c>sm</c>/<c>em</c> split annotation, a
    /// <c>comment</c> annotation by <c>ref</c> and by <c>value</c>, a <c>term</c> annotation,
    /// <c>translate="no"</c>, <c>cp</c> in text and in data, and shared <c>dataRef</c> ids (both
    /// codes agreeing on the resolved data, and both an id and a data id legitimately reused between
    /// a segment's source and its own target).
    /// </summary>
    private const string Corpus = """
        <notes><note>Corpus unit.</note></notes>
        <originalData>
        <data id="d1" dir="rtl">RTL <cp hex="0007"/>bell</data>
        <data id="d2">shared</data>
        </originalData>
        <segment id="s1">
        <source>Click <pc id="b1" type="fmt" subType="xlf:b">here</pc> now.</source>
        <target>Klikkaa <pc id="b1" type="fmt" subType="xlf:b">tästä</pc> nyt.</target>
        </segment>
        <segment id="s2">
        <source>Start <cp hex="0007"/><sc id="sp1" type="ui"/> middle</source>
        <target>Alku keski</target>
        </segment>
        <segment id="s3">
        <source>continue <ec startRef="sp1"/> end, <pc id="outer"><pc id="inner">nested</pc> done</pc>.</source>
        <target>jatkuu loppu.</target>
        </segment>
        <segment id="s4">
        <source>See <mrk id="t1" type="term">wallet</mrk> (<mrk id="c1" type="comment" value="clarify">note</mrk>) and <mrk id="c2" type="comment" ref="https://example/1">ref-note</mrk>.<mrk id="tno1" translate="no"> (internal)</mrk></source>
        <target>Katso <mrk id="t1" type="term">lompakko</mrk> (<mrk id="c1" type="comment" value="clarify">huom</mrk>) ja <mrk id="c2" type="comment" ref="https://example/1">viite-huom</mrk>.<mrk id="tno1" translate="no"> (sisäinen)</mrk></target>
        </segment>
        <segment id="s5">
        <source>Begin <sm id="sm1"/> middle5</source>
        <target>Alku5 keski5</target>
        </segment>
        <segment id="s6">
        <source>middle6 <em startRef="sm1"/> end.</source>
        <target>keski6 loppu.</target>
        </segment>
        <segment id="s7">
        <source>Image <ph id="ph1" type="image" dataRef="d1"/> and <ph id="ph2" dataRef="d2"/>.</source>
        <target>Kuva <ph id="ph1" type="image" dataRef="d1"/>.</target>
        </segment>
        """;

    [TestMethod]
    public void RoundTripsTheCorpusDocumentWithModelEquality()
    {
        XliffDocument original = ReadDocument(Corpus);

        XliffDocument roundTripped = Read(WriteToBytes(original));

        AssertUnitsEqual(original.Files[0].Units[0], roundTripped.Files[0].Units[0]);
    }

    [TestMethod]
    public void TheCorpusOriginalDataAppearsOnceEachInFirstAppearanceOrder()
    {
        XliffDocument original = ReadDocument(Corpus);

        string xml = Encoding.UTF8.GetString(WriteToBytes(original));

        //d1 is used first (segment s7's source), d2 second (segment s7's source, after d1); both
        //appear only once in <originalData> even though d1 is referenced again by s7's target
        //(shared dataRef ids, XLIFF 2.1 §4.2.2.10).
        int d1 = xml.IndexOf("<data id=\"d1\"", StringComparison.Ordinal);
        int d2 = xml.IndexOf("<data id=\"d2\"", StringComparison.Ordinal);
        Assert.IsTrue(d1 >= 0 && d2 >= 0);
        Assert.IsTrue(d1 < d2);
        Assert.AreEqual(1, CountOccurrences(xml, "<data id=\"d1\""));
        Assert.AreEqual(1, CountOccurrences(xml, "<data id=\"d2\""));
        Assert.Contains("<data id=\"d1\" dir=\"rtl\">", xml);
        Assert.IsFalse(xml.Contains("<data id=\"d2\" dir", StringComparison.Ordinal));

        //Kills XliffWriter.InlineContent.cs (WriteUnit's placement of the WriteOriginalDataElement
        //call): moving it below the segment loop would still round-trip green (the reader accepts
        //originalData anywhere among the unit's children, 5.3) but would violate XLIFF 2.1 §4.2.2.5's
        //unit child-order production, which puts <originalData> after <notes> and before the segments.
        int notesIndex = xml.IndexOf("<notes>", StringComparison.Ordinal);
        int originalDataIndex = xml.IndexOf("<originalData>", StringComparison.Ordinal);
        int segmentIndex = xml.IndexOf("<segment", StringComparison.Ordinal);
        Assert.IsTrue(notesIndex >= 0 && originalDataIndex >= 0 && segmentIndex >= 0);
        Assert.IsTrue(notesIndex < originalDataIndex);
        Assert.IsTrue(originalDataIndex < segmentIndex);
    }

    [TestMethod]
    public void WithinOneSegmentTheSourcesDataRefAppearsBeforeTheTargetsOwn()
    {
        //Kills XliffWriter.InlineContent.cs:415-421 (CollectOriginalData): swapping the per-segment
        //call order (source, then target only if present) to target-before-source: both "a" and "b"
        //are fresh here, so a target-first walk would record "b" before "a", flipping the assertion
        //below.
        XliffUnit unit = ReadDocument(
            """
            <originalData><data id="a">A</data><data id="b">B</data></originalData>
            <segment><source>x <ph id="1" dataRef="a"/></source><target>y <ph id="1" dataRef="b"/></target></segment>
            """).Files[0].Units[0];

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        int a = xml.IndexOf("<data id=\"a\">", StringComparison.Ordinal);
        int b = xml.IndexOf("<data id=\"b\">", StringComparison.Ordinal);
        Assert.IsTrue(a >= 0 && b >= 0);
        Assert.IsTrue(a < b);
    }

    [TestMethod]
    public void TheCorpusSplitCodeStaysSplitAndThePairedCodeStaysPairedAfterARoundTrip()
    {
        XliffDocument original = ReadDocument(Corpus);

        XliffDocument roundTripped = Read(WriteToBytes(original));

        XliffUnit unit = roundTripped.Files[0].Units[0];
        var splitStart = (StartCodePart)unit.Segments[1].SourceContent.Parts.Single(static part => part is StartCodePart { Id: "sp1" });
        var pairedStart = (StartCodePart)unit.Segments[0].SourceContent.Parts.Single(static part => part is StartCodePart { Id: "b1" });
        Assert.AreEqual(SpanForm.Split, splitStart.Form);
        Assert.AreEqual(SpanForm.Paired, pairedStart.Form);
    }

    [TestMethod]
    public void TheStreamingReaderParsesTheWritersOutputTheSameWayAsTheWholeDocumentRead()
    {
        XliffDocument original = ReadDocument(Corpus);
        byte[] bytes = WriteToBytes(original);

        using var streamForWholeRead = new MemoryStream(bytes);
        XliffUnit wholeRead = XliffReader.Read(streamForWholeRead).Files[0].Units[0];

        using var streamForStreaming = new MemoryStream(bytes);
        XliffUnit streamed = XliffReader.ReadUnits(streamForStreaming).Single();

        AssertUnitsEqual(wholeRead, streamed);
    }

    /// <summary>Counts the non-overlapping occurrences of <paramref name="needle"/> in <paramref name="haystack"/>.</summary>
    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    //---- Mixed-content byte-exact cases -----------------------------------------------------------

    [TestMethod]
    public void WritesElementFirstSourceContentWithoutInjectingWhitespace()
    {
        XliffUnit unit = ReadDocument("""<segment><source><ph id="1"/> tail</source></segment>""").Files[0].Units[0];

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<source><ph id=\"1\" /> tail</source>", xml);
    }

    [TestMethod]
    public void WritesElementOnlySourceContentWithoutInjectingWhitespace()
    {
        XliffUnit unit = ReadDocument("""<segment><source><ph id="1"/></source></segment>""").Files[0].Units[0];

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<source><ph id=\"1\" /></source>", xml);
    }

    [TestMethod]
    public void WritesDataStartingWithACodePointWithoutInjectingWhitespace()
    {
        XliffUnit unit = ReadDocument(
            """
            <originalData><data id="d1"><cp hex="0007"/>go</data></originalData>
            <segment><source><ph id="1" dataRef="d1"/></source></segment>
            """).Files[0].Units[0];

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<data id=\"d1\"><cp hex=\"0007\" />go</data>", xml);
    }

    //---- cp encoding --------------------------------------------------------------------------------

    [TestMethod]
    public void EncodesALoneHighSurrogateAsACodePoint()
    {
        XliffUnit unit = UnitOf(new InlineTextPart("a\uD800b"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));
        XliffDocument roundTripped = Read(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<cp hex=\"D800\" />", xml);
        Assert.AreEqual("a\uD800b", roundTripped.Files[0].Units[0].Segments[0].SourceContent.Parts.OfType<InlineTextPart>().Single().Text);
    }

    [TestMethod]
    public void EncodesU0003AsACodePoint()
    {
        XliffUnit unit = UnitOf(new InlineTextPart("a\u0003b"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<cp hex=\"0003\" />", xml);
    }

    [TestMethod]
    public void WritesAValidSurrogatePairLiterallyInsteadOfEncodingEachHalf()
    {
        //U+1F600 GRINNING FACE as its UTF-16 surrogate pair: a high half immediately followed by a
        //low half is one valid XML character above U+FFFF and must not be split into two <cp>s.
        XliffUnit unit = UnitOf(new InlineTextPart("a😀b"));

        byte[] bytes = WriteToBytes(DocumentOf(unit));
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.IsFalse(xml.Contains("<cp", StringComparison.Ordinal));
        Assert.AreEqual("a😀b", roundTripped.Files[0].Units[0].Segments[0].SourceContent.Parts.OfType<InlineTextPart>().Single().Text);
    }

    [TestMethod]
    public void RoundTripsAnIsolatedStartCodeWithNoMatchingEndCode()
    {
        XliffUnit unit = UnitOf(new InlineTextPart("before "), Sc("a", isolated: true), new InlineTextPart(" after"));

        byte[] bytes = WriteToBytes(DocumentOf(unit));
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.Contains("<sc id=\"a\" isolated=\"yes\" />", xml);
        AssertUnitsEqual(unit, roundTripped.Files[0].Units[0]);
    }

    [TestMethod]
    public void RoundTripsAnIsolatedEndCodeWithItsOwnId()
    {
        XliffUnit unit = UnitOf(new InlineTextPart("before "), IsolatedEc("z"), new InlineTextPart(" after"));

        byte[] bytes = WriteToBytes(DocumentOf(unit));
        string xml = Encoding.UTF8.GetString(bytes);
        XliffDocument roundTripped = Read(bytes);

        Assert.Contains("<ec id=\"z\" isolated=\"yes\" />", xml);
        AssertUnitsEqual(unit, roundTripped.Files[0].Units[0]);
    }

    [TestMethod]
    public void OmitsDirOnANonIsolatedEndCodeEvenWhenDirectionIsSet()
    {
        //Kills XliffWriter.InlineContent.cs:245 (WriteEndCode): writing dir unconditionally instead of
        //only inside the Isolated branch. XLIFF 2.1 §4.2.3.5: dir MAY be used if and only if
        //isolated="yes"; on a Split, non-isolated ec it MUST NOT appear, even when the model carries a
        //non-Inherited Direction.
        XliffUnit unit = UnitOf(Sc("sp1"), new InlineTextPart("x"), Ec("sp1") with { Direction = TextDirection.RightToLeft });

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<ec startRef=\"sp1\" />", xml);
        Assert.IsFalse(xml.Contains(" dir=", StringComparison.Ordinal));
    }

    //---- Attribute default omission -----------------------------------------------------------------

    [TestMethod]
    public void OmitsEveryPlaceholderAttributeAtItsSpecDefault()
    {
        XliffUnit unit = UnitOf(Ph("1"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<source><ph id=\"1\" /></source>", xml);
    }

    [TestMethod]
    public void WritesEveryPlaceholderAttributeWhenItDiffersFromTheDefault()
    {
        XliffUnit unit = UnitOf(Ph(
            "1",
            type: InlineCodeType.Other,
            subType: "vcl:custom",
            equiv: "[x]",
            disp: "<x/>",
            dataRef: "d1",
            originalData: new OriginalData("<x/>"),
            canCopy: false,
            canDelete: false,
            canReorder: ReorderHint.No,
            copyOf: "0",
            subFlows: "u2"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("type=\"other\"", xml);
        Assert.Contains("subType=\"vcl:custom\"", xml);
        Assert.Contains("equiv=\"[x]\"", xml);
        Assert.Contains("disp=\"&lt;x/&gt;\"", xml);
        Assert.Contains("dataRef=\"d1\"", xml);
        Assert.Contains("canCopy=\"no\"", xml);
        Assert.Contains("canDelete=\"no\"", xml);
        Assert.Contains("canReorder=\"no\"", xml);
        Assert.Contains("copyOf=\"0\"", xml);
        Assert.Contains("subFlows=\"u2\"", xml);
    }

    [TestMethod]
    public void OmitsCanOverlapOnAnScWhenTrueAndWritesItWhenFalse()
    {
        XliffUnit trueUnit = UnitOf(Sc("1", canOverlap: true), Ec("1"));
        XliffUnit falseUnit = UnitOf(Sc("1", canOverlap: false), Ec("1"));

        string trueXml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(trueUnit)));
        string falseXml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(falseUnit)));

        Assert.IsFalse(trueXml.Contains("canOverlap", StringComparison.Ordinal));
        Assert.Contains("canOverlap=\"no\"", falseXml);
    }

    [TestMethod]
    public void OmitsCanOverlapOnAPcWhenFalseAndWritesItWhenTrue()
    {
        XliffUnit falseUnit = UnitOf(PcStart("1") with { CanOverlap = false }, new InlineTextPart("x"), PcEnd("1") with { CanOverlap = false });
        XliffUnit trueUnit = UnitOf(PcStart("1") with { CanOverlap = true }, new InlineTextPart("x"), PcEnd("1") with { CanOverlap = true });

        string falseXml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(falseUnit)));
        string trueXml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(trueUnit)));

        Assert.IsFalse(falseXml.Contains("canOverlap", StringComparison.Ordinal));
        Assert.Contains("canOverlap=\"yes\"", trueXml);
    }

    //---- Problems(): pairing and nesting ------------------------------------------------------------

    /// <summary>Proves an undefined start-code form is reported with its position before output.</summary>
    [TestMethod]
    public void RejectsAnUndefinedStartCodeFormBeforeWriting()
    {
        AssertInvalidPartRejected(Sc("x") with { Form = (SpanForm)99 },
            "The inline part of type 'StartCodePart' at position 1 in the source of unit 'u' has undefined Form value 99.");
    }

    /// <summary>Proves an undefined end-code form is reported with its position before output.</summary>
    [TestMethod]
    public void RejectsAnUndefinedEndCodeFormBeforeWriting()
    {
        AssertInvalidPartRejected(Ec("x") with { Form = (SpanForm)99 },
            "The inline part of type 'EndCodePart' at position 1 in the source of unit 'u' has undefined Form value 99.");
    }

    /// <summary>Proves an undefined annotation-start form is reported with its position before output.</summary>
    [TestMethod]
    public void RejectsAnUndefinedAnnotationStartFormBeforeWriting()
    {
        AssertInvalidPartRejected(Mrk("x") with { Form = (AnnotationForm)99 },
            "The inline part of type 'AnnotationStartPart' at position 1 in the source of unit 'u' has undefined Form value 99.");
    }

    /// <summary>Proves an undefined annotation-end form is reported with its position before output.</summary>
    [TestMethod]
    public void RejectsAnUndefinedAnnotationEndFormBeforeWriting()
    {
        AssertInvalidPartRejected(MrkEnd("x") with { Form = (AnnotationForm)99 },
            "The inline part of type 'AnnotationEndPart' at position 1 in the source of unit 'u' has undefined Form value 99.");
    }

    /// <summary>Represents a public extension of the model that has no XLIFF serialization.</summary>
    private sealed record UnsupportedInlinePart : InlinePart;

    /// <summary>Proves unsupported part kinds are named and refused before output.</summary>
    [TestMethod]
    public void RejectsAnUnsupportedInlinePartBeforeWriting()
    {
        AssertInvalidPartRejected(new UnsupportedInlinePart(),
            $"The inline part of type '{typeof(UnsupportedInlinePart).FullName}' at position 1 in the source of unit 'u' is of a kind the writer cannot serialize.");
    }

    /// <summary>Checks the reported problem and the no-output refusal for a part after text.</summary>
    private static void AssertInvalidPartRejected(InlinePart part, string expectedProblem)
    {
        XliffUnit unit = UnitOf(new InlineTextPart("before"), part);

        AssertUnitRejected(unit, expectedProblem);
    }

    /// <summary>Proves paired codes, markers and their combined nesting refuse the sixty-fifth level.</summary>
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void RejectsInlineNestingDeeperThan64LevelsBeforeWriting(bool codes, bool markers)
    {
        XliffUnit unit = UnitWithNestedSpans(65, codes, markers);
        const string expectedProblem = "The inline part at position 64 in the source of unit 'u' exceeds the maximum inline nesting depth of 64.";

        AssertUnitRejected(unit, expectedProblem);
    }

    /// <summary>Proves the maximum accepted depth remains writable for each kind and mixed nesting.</summary>
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void AllowsInlineNestingExactlyAt64Levels(bool codes, bool markers)
    {
        XliffUnit unit = UnitWithNestedSpans(64, codes, markers);

        AssertUnitsEqual(unit, Read(WriteToBytes(DocumentOf(unit))).Files[0].Units[0]);
    }

    /// <summary>Builds properly nested paired codes, markers, or alternating codes and markers.</summary>
    private static XliffUnit UnitWithNestedSpans(int depth, bool codes, bool markers)
    {
        var parts = new InlinePart[(depth * 2) + 1];
        for(int index = 0; index < depth; index++)
        {
            string id = $"span{index}";
            bool code = codes && (!markers || index % 2 == 0);
            parts[index] = code ? PcStart(id) : Mrk(id);
            parts[parts.Length - index - 1] = code ? PcEnd(id) : MrkEnd(id);
        }

        parts[depth] = new InlineTextPart("inside");

        return UnitOf(parts);
    }

    /// <summary>Proves a marker must close in the same content before serialization can search for its end.</summary>
    [TestMethod]
    public void RejectsAMarkerStartWithoutAMatchingEndInTheSameContent()
    {
        AssertUnitRejected(UnitOf(Mrk("x"), new InlineTextPart("inside")), "has no closing end in the same content");
    }

    [TestMethod]
    public void RejectsAPairedStartWithoutAMatchingEndInTheSameContent()
    {
        XliffUnit unit = UnitOf(PcStart("a"), new InlineTextPart("x"));

        AssertUnitRejected(unit, "has no closing end in the same content");
    }

    [TestMethod]
    public void RejectsAPairedSpanThatCrossesAnotherInsteadOfNesting()
    {
        XliffUnit unit = UnitOf(PcStart("a"), PcStart("b"), new InlineTextPart("x"), PcEnd("a"), PcEnd("b"));

        AssertUnitRejected(unit, "does not close the innermost open span");
    }

    [TestMethod]
    public void RejectsAMarkerAnnotationThatCrossesAPairedSpanInsteadOfNesting()
    {
        XliffUnit unit = UnitOf(PcStart("a"), Mrk("m"), new InlineTextPart("x"), PcEnd("a"), MrkEnd("m"));

        AssertUnitRejected(unit, "does not close the innermost open span");
    }

    [TestMethod]
    public void RejectsAPairedStartThatIsIsolated()
    {
        XliffUnit unit = UnitOf(PcStart("a", isolated: true), new InlineTextPart("x"), PcEnd("a"));

        AssertUnitRejected(unit, "a span from <pc> must never be isolated");
    }

    [TestMethod]
    public void RejectsAPairedSpanWhoseHalvesDisagreeOnAType()
    {
        XliffUnit unit = UnitOf(PcStart("a", type: InlineCodeType.Format), new InlineTextPart("x"), PcEnd("a", type: InlineCodeType.Other));

        AssertUnitRejected(unit, "disagree on a shared attribute");
    }

    [TestMethod]
    public void RejectsAnEndCodeWhoseStartRefNamesNoOpenStartCode()
    {
        XliffUnit unit = UnitOf(new InlineTextPart("x"), Ec("ghost"));

        AssertUnitRejected(unit, "names no open <sc>");
    }

    [TestMethod]
    public void RejectsAnEndCodeThatTriesToCloseAnIsolatedStartCode()
    {
        XliffUnit unit = UnitOf(Sc("a", isolated: true), new InlineTextPart("x"), Ec("a"));

        AssertUnitRejected(unit, "is isolated and must not be closed by an <ec>");
    }

    [TestMethod]
    public void RejectsAStartCodeLeftOpenAtTheUnitsEnd()
    {
        XliffUnit unit = UnitOf(Sc("a"), new InlineTextPart("x"));

        AssertUnitRejected(unit, "is never closed by a matching <ec>");
    }

    [TestMethod]
    public void RejectsAnEndMarkerWhoseStartRefNamesNoOpenStartMarker()
    {
        XliffUnit unit = UnitOf(new InlineTextPart("x"), Em("ghost"));

        AssertUnitRejected(unit, "names no open <sm>");
    }

    //---- Problems(): duplicate inline ids ------------------------------------------------------------

    [TestMethod]
    public void RejectsTheSameInlineIdUsedTwiceOnTheSourceSide()
    {
        XliffUnit unit = UnitOf(Ph("p1"), new InlineTextPart(" "), Ph("p1"));

        AssertUnitRejected(unit, "The inline id 'p1' is used more than once");
    }

    [TestMethod]
    public void RejectsAnInlineIdReusedAcrossSegmentsWithoutTheSiblingExemption()
    {
        var segment1 = new XliffSegment("s1", SegmentKind.Translatable, InlineContent.Create([Ph("x")]), null, SegmentState.Initial, null);
        var segment2 = new XliffSegment("s2", SegmentKind.Translatable, InlineContent.FromText("y"), InlineContent.Create([Ph("x")]), SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment1, segment2], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "The inline id 'x' is used more than once");
    }

    [TestMethod]
    public void RejectsASourceThatReusesAnIdAnEarlierSegmentsTargetIntroduced()
    {
        //Kills XliffWriter.InlineContent.cs:610 (DuplicateInlineIdProblems, source-side check):
        //dropping the source-side loop's cross-side check entirely (the "|| targetSeen.Contains(id)"
        //disjunct). The exemption in XLIFF 2.1 §4.3.1.21 only ever runs target-reusing-source, never
        //the other way, so a later segment's source repeating an id an earlier segment's target
        //introduced (as an added code) is always a problem, with no sibling relationship to excuse it.
        var segment1 = new XliffSegment("s1", SegmentKind.Translatable, InlineContent.FromText("a"), InlineContent.Create([Ph("x")]), SegmentState.Initial, null);
        var segment2 = new XliffSegment("s2", SegmentKind.Translatable, InlineContent.Create([Ph("x")]), null, SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment1, segment2], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "The inline id 'x' is used more than once");
    }

    [TestMethod]
    public void RejectsAnInlineIdThatCollidesWithASegmentId()
    {
        var segment = new XliffSegment("dup", SegmentKind.Translatable, InlineContent.Create([Ph("dup")]), null, SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "The inline id 'dup' is used more than once");
    }

    //---- Problems(): original data -------------------------------------------------------------------

    [TestMethod]
    public void RejectsOriginalDataCarriedWithoutADataRef()
    {
        XliffUnit unit = UnitOf(Ph("1", originalData: new OriginalData("x")));

        AssertUnitRejected(unit, "carries original data without a dataRef");
    }

    [TestMethod]
    public void RejectsConflictingOriginalDataSharingOneDataRef()
    {
        XliffUnit unit = UnitOf(Ph("1", dataRef: "d1", originalData: new OriginalData("one")), Ph("2", dataRef: "d1", originalData: new OriginalData("two")));

        AssertUnitRejected(unit, "more than one distinct original data value for the dataRef 'd1'");
    }

    [TestMethod]
    public void RejectsOriginalDataSharingOneDataRefWithTheSameTextButDifferentDirections()
    {
        //Kills XliffWriter.InlineContent.cs (OriginalDataConsistencyProblems): comparing only Text and
        //not Direction. OriginalData equality (and so the HashSet used to detect a conflict) covers
        //both, per the "two parts with one DataRef but different OriginalData (text or direction)"
        //half of orientation 5.4; RejectsConflictingOriginalDataSharingOneDataRef above already covers
        //the text half.
        XliffUnit unit = UnitOf(
            Ph("1", dataRef: "d1", originalData: new OriginalData("same", TextDirection.LeftToRight)),
            Ph("2", dataRef: "d1", originalData: new OriginalData("same", TextDirection.RightToLeft)));

        AssertUnitRejected(unit, "more than one distinct original data value for the dataRef 'd1'");
    }

    [TestMethod]
    public void RejectsADataRefNoPartResolvesAnywhereInTheUnit()
    {
        XliffUnit unit = UnitOf(Ph("1", dataRef: "d1"));

        AssertUnitRejected(unit, "no part in the unit carries the resolved original data for it");
    }

    //---- Problems(): name-token shape and XML-text validity per field --------------------------------

    [TestMethod]
    public void RejectsAnInlineIdThatIsNotAnXmlNameToken()
    {
        XliffUnit unit = UnitOf(Ph("bad id"));

        AssertUnitRejected(unit, "inline id in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsADataRefThatIsNotAnXmlNameToken()
    {
        XliffUnit unit = UnitOf(Ph("1", dataRef: "bad ref", originalData: new OriginalData("x")));

        AssertUnitRejected(unit, "dataRef in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsACopyOfThatIsNotAnXmlNameToken()
    {
        XliffUnit unit = UnitOf(Ph("1", copyOf: "bad copy"));

        AssertUnitRejected(unit, "copyOf in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAnEquivContainingACharacterXmlCannotCarry()
    {
        XliffUnit unit = UnitOf(Ph("1", equiv: "bad\u0007equiv"));

        AssertUnitRejected(unit, "an equiv in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsADispContainingACharacterXmlCannotCarry()
    {
        XliffUnit unit = UnitOf(Ph("1", disp: "bad\u0007disp"));

        AssertUnitRejected(unit, "a disp in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsASubTypeContainingACharacterXmlCannotCarry()
    {
        XliffUnit unit = UnitOf(Ph("1", type: InlineCodeType.Other, subType: "vcl:bad\u0007sub"));

        AssertUnitRejected(unit, "a subType in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsASubFlowsContainingACharacterXmlCannotCarry()
    {
        //Kills XliffWriter.InlineContent.cs (CodeTextFieldProblems): omitting the subFlows check.
        //Without it, a character XML cannot carry in SubFlows reaches XmlWriter directly and fails
        //with its own context-free message instead of a Problems() refusal naming the unit and side.
        XliffUnit unit = UnitOf(Ph("1", subFlows: "bad\u0007flow"));

        AssertUnitRejected(unit, "a subFlows in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAnAnnotationValueContainingACharacterXmlCannotCarry()
    {
        XliffUnit unit = UnitOf(Mrk("m", type: "comment", value: "bad\u0007value"), new InlineTextPart("x"), MrkEnd("m"));

        AssertUnitRejected(unit, "an annotation value in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAnAnnotationRefContainingACharacterXmlCannotCarry()
    {
        XliffUnit unit = UnitOf(Mrk("m", type: "comment", refValue: "bad\u0007ref"), new InlineTextPart("x"), MrkEnd("m"));

        AssertUnitRejected(unit, "an annotation ref in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAnAnnotationTypeContainingACharacterXmlCannotCarry()
    {
        XliffUnit unit = UnitOf(Mrk("m", type: "bad\u0007type"), new InlineTextPart("x"), MrkEnd("m"));

        AssertUnitRejected(unit, "an annotation type in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsANonIsolatedEndCodeThatCarriesAnId()
    {
        //XLIFF 2.1 §4.2.3.5: id is used if and only if isolated="yes". The model can express a
        //non-isolated EndCodePart with a non-null Id even though WriteEndCode would then silently drop
        //it (it only ever writes StartRef on that branch), so Problems() must refuse this instead of
        //quietly writing an <ec startRef="..."/> that loses the stray id, mirroring the reader's own
        //refusal of the same shape.
        XliffUnit unit = UnitOf(Sc("sp1"), new InlineTextPart("x"), Ec("sp1") with { Id = "stray" });

        AssertUnitRejected(unit, "carries an id but is not isolated");
    }

    [TestMethod]
    public void RejectsAnAnnotationTypeThatIsNeitherReservedNorShapedAsPrefixValue()
    {
        //XLIFF 2.1 §4.3.1.40, §4.7.3.1.4: the model can carry an AnnotationStartPart.Type the reader
        //would refuse to read back (for example one built by hand, or round-tripped from a source that
        //validates less strictly), so Problems() must refuse it here too, mirroring the reader's own
        //ParseAnnotationAttributes refusal.
        //Twin: XliffReaderInlineContentTests.cs RejectsAnAnnotationTypeThatIsNeitherReservedNorShapedAsPrefixValue.
        XliffUnit unit = UnitOf(Mrk("m", type: "bogus"), new InlineTextPart("x"), MrkEnd("m"));

        AssertUnitRejected(unit, "is 'bogus', which is neither generic, term, comment nor shaped prefix:value");
    }

    [TestMethod]
    public void AcceptsAnAnnotationTypeShapedAsPrefixValue()
    {
        //Companion acceptance test: a prefix:value type must not be wrongly refused by the new check.
        XliffUnit unit = UnitOf(Mrk("m", type: "acme:widget"), new InlineTextPart("x"), MrkEnd("m"));

        XliffDocument document = Read(WriteToBytes(DocumentOf(unit)));
        var start = (AnnotationStartPart)document.Files[0].Units.Single().Segments[0].SourceContent.Parts[0];
        Assert.AreEqual("acme:widget", start.Type);
    }

    [TestMethod]
    public void RejectsACommentAnnotationWithNeitherValueNorRef()
    {
        //XLIFF 2.1 §4.7.3.1.3: "if and only if the value attribute is not present, the ref attribute
        //MUST be present" - a comment annotation with neither must be refused, mirroring the reader.
        //Twin: XliffReaderInlineContentTests.cs RejectsACommentAnnotationWithNeitherValueNorRef.
        XliffUnit unit = UnitOf(Mrk("m", type: "comment"), new InlineTextPart("x"), MrkEnd("m"));

        AssertUnitRejected(unit, "has neither a value nor a ref attribute");
    }

    [TestMethod]
    public void RejectsACommentAnnotationWithBothValueAndRef()
    {
        //Twin: XliffReaderInlineContentTests.cs RejectsACommentAnnotationWithBothValueAndRef.
        XliffUnit unit = UnitOf(Mrk("m", type: "comment", refValue: "#n=n1", value: "v"), new InlineTextPart("x"), MrkEnd("m"));

        AssertUnitRejected(unit, "has both a value and a ref attribute");
    }

    //---- Named killers: step 6b Stryker census survivors --------------------------------------------
    //S-054 (line 83) and S-055 (line 111) are not tested here: proven EQUIVALENT below (a nested
    //Paired/Marker start's own WriteString(string.Empty) prime is unobservable, because the enclosing
    //<source>/<target>'s own prime, written unconditionally by WriteInlineContent before any content is
    //ever reached, suppresses the indenting writer's mixed-content indentation for its whole subtree
    //regardless of depth; removing a nested prime changes nothing as long as the ancestor's own prime
    //still runs, which it always does on every path into WriteContentParts).

    [TestMethod]
    public void WritesEveryStartCodeAttributeWhenItDiffersFromTheDefault()
    {
        //Kills XliffWriter.InlineContent.cs:214,215,217,218,219,220,221,223,224,225 (WriteStartCode)
        //and contributes to :515 (the shared WriteDirectionIfNotInherited statement): the Sc() helper
        //hardcodes every one of these at its default, so no writer test gave a Split <sc> a
        //non-default value for any of them before this.
        StartCodePart part = Sc("1", type: InlineCodeType.Other) with
        {
            SubType = "vcl:custom",
            Direction = TextDirection.RightToLeft,
            DataRef = "d1",
            Equiv = "[x]",
            Disp = "<x/>",
            CanCopy = false,
            CanDelete = false,
            CanReorder = ReorderHint.No,
            CopyOf = "0",
            SubFlows = "u2",
            OriginalData = new OriginalData("<x/>")
        };
        XliffUnit unit = UnitOf(part, new InlineTextPart("x"), Ec("1"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("subType=\"vcl:custom\"", xml);
        Assert.Contains("dir=\"rtl\"", xml);
        Assert.Contains("dataRef=\"d1\"", xml);
        Assert.Contains("equiv=\"[x]\"", xml);
        Assert.Contains("disp=\"&lt;x/&gt;\"", xml);
        Assert.Contains("canCopy=\"no\"", xml);
        Assert.Contains("canDelete=\"no\"", xml);
        Assert.Contains("canReorder=\"no\"", xml);
        Assert.Contains("copyOf=\"0\"", xml);
        Assert.Contains("subFlows=\"u2\"", xml);
    }

    [TestMethod]
    public void WritesDirOnAnIsolatedEndCodeWhenDirectionIsSet()
    {
        //Kills XliffWriter.InlineContent.cs:237 (WriteEndCode, Isolated branch): dropping
        //WriteDirectionIfNotInherited there would omit dir even though XLIFF 2.1 §4.2.3.5 allows it
        //precisely on an isolated end code; RoundTripsAnIsolatedEndCodeWithItsOwnId never sets
        //Direction away from its Inherited default.
        XliffUnit unit = UnitOf(new InlineTextPart("x"), IsolatedEc("z") with { Direction = TextDirection.RightToLeft });

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<ec id=\"z\" isolated=\"yes\" dir=\"rtl\" />", xml);
    }

    [TestMethod]
    public void WritesEveryEndCodeAttributeWhenItDiffersFromTheDefault()
    {
        //Kills XliffWriter.InlineContent.cs:244-254 (WriteEndCode's shared attribute writes): the
        //Ec() helper hardcodes every one of these at its default; OmitsCanOverlapOnAnScWhenTrueAndWritesItWhenFalse
        //only varies CanOverlap on the start code, never on the end code, so that gap needed its own
        //case here too.
        EndCodePart end = Ec("1") with
        {
            Type = InlineCodeType.Other,
            SubType = "vcl:custom",
            Equiv = "[/x]",
            Disp = "</x>",
            DataRef = "d1",
            OriginalData = new OriginalData("</x>"),
            CanCopy = false,
            CanDelete = false,
            CanOverlap = false,
            CanReorder = ReorderHint.No,
            CopyOf = "0",
            SubFlows = "u2"
        };
        XliffUnit unit = UnitOf(Sc("1"), new InlineTextPart("x"), end);

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("type=\"other\"", xml);
        Assert.Contains("subType=\"vcl:custom\"", xml);
        Assert.Contains("dataRef=\"d1\"", xml);
        Assert.Contains("equiv=\"[/x]\"", xml);
        Assert.Contains("disp=\"&lt;/x&gt;\"", xml);
        Assert.Contains("canCopy=\"no\"", xml);
        Assert.Contains("canDelete=\"no\"", xml);
        Assert.Contains("canOverlap=\"no\"", xml);
        Assert.Contains("canReorder=\"no\"", xml);
        Assert.Contains("copyOf=\"0\"", xml);
        Assert.Contains("subFlows=\"u2\"", xml);
    }

    [TestMethod]
    public void WritesEveryPairedCodeAttributeWhenItDiffersFromTheDefault()
    {
        //Kills XliffWriter.InlineContent.cs:272-285 (WritePairedCodeStart), including the
        //defaultValue:true args on canCopy/canDelete at 279/280, and contributes to :515 (the shared
        //WriteDirectionIfNotInherited statement): no writer test built a <pc> whose shared or
        //half-specific attributes differ from their spec defaults before this (the corpus's b1 span
        //only sets type/subType, already covering lines 270-271). The end half repeats the shared
        //attributes (CanCopy, CanDelete, CanReorder, CopyOf, Direction) because PairedHalvesProblems
        //refuses a mismatch between a pc's two halves; only the start's values are ever written for
        //those.
        StartCodePart start = PcStart("1") with
        {
            Direction = TextDirection.RightToLeft,
            DataRef = "ds",
            Equiv = "[b",
            Disp = "<b>",
            CanCopy = false,
            CanDelete = false,
            CanReorder = ReorderHint.No,
            CopyOf = "0",
            SubFlows = "u1",
            OriginalData = new OriginalData("<b>")
        };
        EndCodePart end = PcEnd("1") with
        {
            DataRef = "de",
            Equiv = "[/b",
            Disp = "</b>",
            SubFlows = "u2",
            OriginalData = new OriginalData("</b>"),
            CanCopy = false,
            CanDelete = false,
            CanReorder = ReorderHint.No,
            CopyOf = "0",
            Direction = TextDirection.RightToLeft
        };
        XliffUnit unit = UnitOf(start, new InlineTextPart("x"), end);

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("dir=\"rtl\"", xml);
        Assert.Contains("dataRefStart=\"ds\"", xml);
        Assert.Contains("dataRefEnd=\"de\"", xml);
        Assert.Contains("equivStart=\"[b\"", xml);
        Assert.Contains("equivEnd=\"[/b\"", xml);
        Assert.Contains("dispStart=\"&lt;b&gt;\"", xml);
        Assert.Contains("dispEnd=\"&lt;/b&gt;\"", xml);
        Assert.Contains("canCopy=\"no\"", xml);
        Assert.Contains("canDelete=\"no\"", xml);
        Assert.Contains("canReorder=\"no\"", xml);
        Assert.Contains("copyOf=\"0\"", xml);
        Assert.Contains("subFlowsStart=\"u1\"", xml);
        Assert.Contains("subFlowsEnd=\"u2\"", xml);
    }

    [TestMethod]
    public void WritesTranslateYesWhenTranslateIsTrue()
    {
        //Kills XliffWriter.InlineContent.cs:314 (WriteAnnotationAttributes): every existing translate
        //test only ever sets Translate=false (translate="no"), so a mutant that always writes "no"
        //coincidentally matches; nothing exercises Translate=true.
        XliffUnit unit = UnitOf(Mrk("m", translate: true), new InlineTextPart("x"), MrkEnd("m"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("translate=\"yes\"", xml);
    }

    [TestMethod]
    public void EncodesATrailingLoneHighSurrogateAsACodePointWithoutThrowing()
    {
        //Kills XliffWriter.InlineContent.cs:344 (WriteInlineText's validPair check, all three
        //mutants: && => ||, the < => <= length check, and index+1 => index-1 in that check): for a
        //lone high surrogate that is the string's LAST character, each mutant lets validPair either
        //evaluate true or read text[index+1] out of range instead of short-circuiting; every existing
        //surrogate test has a trailing character after the surrogate, so this boundary was untested.
        XliffUnit unit = UnitOf(new InlineTextPart("a\uD800"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<cp hex=\"D800\" />", xml);
    }

    [TestMethod]
    public void EncodesTwoSeparateCodePointsWithOrdinaryTextBetweenThem()
    {
        //Kills XliffWriter.InlineContent.cs:361 (WriteInlineText's pending-text flush): once start
        //has advanced past zero from an earlier <cp>, index-start => index+start overshoots the
        //substring length and throws instead of writing "b"; the existing single-cp tests only ever
        //flush once, with start still 0, where the two expressions coincide.
        XliffUnit unit = UnitOf(new InlineTextPart("abc"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<source>a<cp hex=\"0003\" />b<cp hex=\"0007\" />c</source>", xml);
    }

    [TestMethod]
    public void NamesTheSourceSideInASplitPairingViolationMessage()
    {
        //Kills XliffWriter.InlineContent.cs:785 (SplitPairingProblems): the always-"target" and the
        //"source"=>"" mutants both mislabel a source-side violation; every existing split-pairing
        //test only asserted a side-agnostic message fragment.
        XliffUnit unit = UnitOf(new InlineTextPart("x"), Ec("ghost"));

        AssertUnitRejected(unit, "An <ec> in the source of unit 'u' has startRef 'ghost'");
    }

    [TestMethod]
    public void NamesTheTargetSideInASplitPairingViolationMessage()
    {
        //Kills XliffWriter.InlineContent.cs:573 (the isTarget:true call collapsing to isTarget:false),
        //:575 (its propagation into InlineUnitProblems), :785 (the always-"source" and "target"=>""
        //mutants) and :788 (isTarget ? TargetContent : SourceContent collapsing to always
        //SourceContent): a clean source with only a broken target must still be refused, and the
        //message must name the target; no existing test builds a target-only split-pairing violation.
        var segment = new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("x"), InlineContent.Create([Ec("ghost")]), SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "An <ec> in the target of unit 'u' has startRef 'ghost'");
    }

    [TestMethod]
    public void RejectsATargetInlineIdThatCollidesWithTheSegmentId()
    {
        //Kills XliffWriter.InlineContent.cs:625 (DuplicateInlineIdProblems, target loop): a target
        //inline id equal to the segment's own id, used only once on the target and not reused from
        //source, is reported under the real ||-chain but silently dropped once the first two operands
        //become a conjunction (&&); RejectsAnInlineIdThatCollidesWithASegmentId only covers the
        //source-side collision (line 610).
        var segment = new XliffSegment("dup", SegmentKind.Translatable, InlineContent.FromText("x"), InlineContent.Create([Ph("dup")]), SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "The inline id 'dup' is used more than once");
    }

    [TestMethod]
    public void RejectsTheSameIsolatedEndCodeIdUsedTwice()
    {
        //Kills XliffWriter.InlineContent.cs:647 (InlineIdsOf): flipping the Isolated guard to false
        //stops an isolated end code's own id from ever being registered, so a repeated isolated
        //end-code id is never flagged as a duplicate; no existing test reuses one.
        XliffUnit unit = UnitOf(new InlineTextPart("a"), IsolatedEc("z"), new InlineTextPart("b"), IsolatedEc("z"));

        AssertUnitRejected(unit, "The inline id 'z' is used more than once");
    }

    [TestMethod]
    public void NamesTheSourceSideInAPairedNestingViolationMessage()
    {
        //Kills XliffWriter.InlineContent.cs:673 ($"the source of unit '{unit.Id}'" => $""):
        //RejectsAPairedSpanThatCrossesAnotherInsteadOfNesting only asserts the generic "does not
        //close the innermost open span" fragment, never the side-naming prefix.
        XliffUnit unit = UnitOf(PcStart("a"), PcStart("b"), new InlineTextPart("x"), PcEnd("a"), PcEnd("b"));

        AssertUnitRejected(unit, "A </pc> in the source of unit 'u' does not close the innermost open span");
    }

    [TestMethod]
    public void NamesTheTargetSideInAPairedNestingViolationMessage()
    {
        //Kills XliffWriter.InlineContent.cs:680 ($"the target of unit '{unit.Id}'" => $"") and :682
        //(the foreach body that propagates a target-side nesting problem): no existing test builds a
        //target-side Paired/Marker nesting violation.
        var segment = new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("x"), InlineContent.Create([PcStart("a"), PcStart("b"), new InlineTextPart("y"), PcEnd("a"), PcEnd("b")]), SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "A </pc> in the target of unit 'u' does not close the innermost open span");
    }

    [TestMethod]
    public void RejectsAStartCodeIdThatIsNotAnXmlNameToken()
    {
        //Kills XliffWriter.InlineContent.cs:965 ($"inline id in {what}" => $"") and :967 (the foreach
        //body that propagates it): RejectsAnInlineIdThatIsNotAnXmlNameToken only exercises a
        //Placeholder's id, never a StartCodePart's.
        XliffUnit unit = UnitOf(Sc("bad id"), new InlineTextPart("x"), Ec("bad id"));

        AssertUnitRejected(unit, "inline id in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAnIsolatedEndCodeIdThatIsNotAnXmlNameToken()
    {
        //Kills XliffWriter.InlineContent.cs:986 ($"inline id in {what}" => $"") and :990 (the shared
        //idProblems foreach body): no existing test gives an isolated end code an invalid Id.
        XliffUnit unit = UnitOf(new InlineTextPart("x"), IsolatedEc("bad id"));

        AssertUnitRejected(unit, "inline id in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAnEndCodeStartRefThatIsNotAnXmlNameToken()
    {
        //Kills XliffWriter.InlineContent.cs:987 ($"inline startRef in {what}" => $""): no existing
        //test gives a non-isolated end code an invalid StartRef.
        XliffUnit unit = UnitOf(new InlineTextPart("x"), Ec("bad ref"));

        AssertUnitRejected(unit, "inline startRef in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAStartCodeDataRefAndCopyOfThatAreNotXmlNameTokens()
    {
        //Kills XliffWriter.InlineContent.cs:972 (the CodeReferenceProblems foreach body in the
        //StartCodePart arm): only PlaceholderPart's dataRef/copyOf are tested for name-token shape
        //today.
        StartCodePart start = Sc("1") with { DataRef = "bad ref", CopyOf = "bad copy", OriginalData = new OriginalData("x") };
        XliffUnit unit = UnitOf(start, new InlineTextPart("x"), Ec("1"));

        AssertUnitRejected(unit, "dataRef in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAStartCodeEquivContainingACharacterXmlCannotCarry()
    {
        //Kills XliffWriter.InlineContent.cs:977 (the CodeTextFieldProblems foreach body in the
        //StartCodePart arm): only PlaceholderPart's text fields are tested for XML-text validity
        //today.
        StartCodePart start = Sc("1") with { Equiv = "badequiv" };
        XliffUnit unit = UnitOf(start, new InlineTextPart("x"), Ec("1"));

        AssertUnitRejected(unit, "an equiv in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAnEndCodeDataRefAndCopyOfThatAreNotXmlNameTokens()
    {
        //Kills XliffWriter.InlineContent.cs:1000 (the CodeReferenceProblems foreach body in the
        //EndCodePart arm): only PlaceholderPart's dataRef/copyOf are tested for name-token shape
        //today.
        EndCodePart end = Ec("1") with { DataRef = "bad ref", CopyOf = "bad copy", OriginalData = new OriginalData("x") };
        XliffUnit unit = UnitOf(Sc("1"), new InlineTextPart("x"), end);

        AssertUnitRejected(unit, "dataRef in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAnEndCodeEquivContainingACharacterXmlCannotCarry()
    {
        //Kills XliffWriter.InlineContent.cs:1005 (the CodeTextFieldProblems foreach body in the
        //EndCodePart arm): only PlaceholderPart's text fields are tested for XML-text validity today.
        EndCodePart end = Ec("1") with { Equiv = "badequiv" };
        XliffUnit unit = UnitOf(Sc("1"), new InlineTextPart("x"), end);

        AssertUnitRejected(unit, "an equiv in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void RejectsAnAnnotationIdThatIsNotAnXmlNameToken()
    {
        //Kills XliffWriter.InlineContent.cs:1013 ($"inline id in {what}" => $"") and :1015 (the
        //NameTokenProblems foreach body in the AnnotationStartPart arm): no existing test gives an
        //mrk/sm annotation an invalid id.
        XliffUnit unit = UnitOf(Mrk("bad id"), new InlineTextPart("x"), MrkEnd("bad id"));

        AssertUnitRejected(unit, "inline id in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsAnAnnotationEndStartRefThatIsNotAnXmlNameToken()
    {
        //Kills XliffWriter.InlineContent.cs:1056 ($"inline startRef in {what}" => $"") and :1058 (the
        //NameTokenProblems foreach body in the AnnotationEndPart arm): no existing test gives an em
        //an invalid StartRef.
        XliffUnit unit = UnitOf(new InlineTextPart("x"), Em("bad ref"));

        AssertUnitRejected(unit, "inline startRef in the source of unit 'u' is not an XML name token");
    }

    [TestMethod]
    public void RejectsACopyOfContainingACharacterXmlCannotCarry()
    {
        //Kills XliffWriter.InlineContent.cs:1105 (the CodeTextFieldProblems copyOf check) and :1107
        //(its XmlTextProblems foreach body): RejectsACopyOfThatIsNotAnXmlNameToken only exercises the
        //NameTokenProblems shape check (a different call, CodeReferenceProblems); nothing gives
        //copyOf a character XmlTextProblems rejects.
        XliffUnit unit = UnitOf(Ph("1", copyOf: "badcopy"));

        AssertUnitRejected(unit, "a copyOf in the source of unit 'u' contains a character XML cannot carry");
    }

    [TestMethod]
    public void WritesNestedMarkerAnnotationsWithCorrectNesting()
    {
        //Kills XliffWriter.InlineContent.cs:175 (FindMatchingMarkerEnd's depth++, both removed and
        //flipped to depth--): no writer test nests one Marker annotation inside another, so the depth
        //counter that distinguishes a nested mrk's own end from the outer mrk's end is never
        //exercised past 1; without it the search for the outer's end stops at the inner's own end
        //instead, and the leftover outer end is then reached as an unmatched part.
        XliffUnit unit = UnitOf(Mrk("outer"), new InlineTextPart("a"), Mrk("inner"), new InlineTextPart("b"), MrkEnd("inner"), new InlineTextPart("c"), MrkEnd("outer"));

        string xml = Encoding.UTF8.GetString(WriteToBytes(DocumentOf(unit)));

        Assert.Contains("<mrk id=\"outer\">a<mrk id=\"inner\">b</mrk>c</mrk>", xml);
    }

    [TestMethod]
    public void ThrowsForAnUndefinedReorderHintValue()
    {
        //Kills XliffWriter.InlineContent.cs:504 (WriteReorderIfNotDefault's default arm): ReorderHint
        //is a public field on public records and nothing in Problems() range-checks it, so a caller
        //can construct an out-of-range value that only the actual write phase catches.
        XliffUnit unit = UnitOf(Ph("1", canReorder: (ReorderHint)99));

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => XliffWriter.Write(DocumentOf(unit), new MemoryStream()));

        Assert.Contains("Unknown reorder hint.", exception.Message);
    }

    [TestMethod]
    public void ThrowsWhenOriginalDataDirectionIsInherited()
    {
        //Kills XliffWriter.InlineContent.cs:527 (DirectionAttributeValue's default arm):
        //OriginalData.Direction defaults to Auto, but nothing stops a caller constructing it as
        //TextDirection.Inherited directly, and WriteOriginalDataElement only guards against Auto
        //before calling DirectionAttributeValue.
        XliffUnit unit = UnitOf(Ph("1", dataRef: "d1", originalData: new OriginalData("x", TextDirection.Inherited)));

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => XliffWriter.Write(DocumentOf(unit), new MemoryStream()));

        Assert.Contains("A direction of Inherited, or an undefined value, has no dir attribute value.", exception.Message);
    }

    [TestMethod]
    public void ThrowsForAnUndefinedInlineCodeTypeValue()
    {
        //Kills XliffWriter.InlineContent.cs:543 (WriteCodeTypeIfPresent's default arm): same gap as
        //ThrowsForAnUndefinedReorderHintValue, for InlineCodeType instead of ReorderHint.
        XliffUnit unit = UnitOf(Ph("1", type: (InlineCodeType)99));

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => XliffWriter.Write(DocumentOf(unit), new MemoryStream()));

        Assert.Contains("Unknown inline code type.", exception.Message);
    }

    [TestMethod]
    public void RejectsAMarkerAnnotationEndThatCrossesAPairedSpanInsteadOfNesting()
    {
        //Kills XliffWriter.InlineContent.cs:732 (both the yield statement and its message text):
        //RejectsAMarkerAnnotationThatCrossesAPairedSpanInsteadOfNesting only crosses a </pc> out from
        //under an open mrk (the EndCodePart branch's message, line 714); no test crosses a </mrk> out
        //from under an open pc the other way, which is the only way to reach this branch.
        XliffUnit unit = UnitOf(Mrk("m"), PcStart("a"), new InlineTextPart("x"), MrkEnd("m"), PcEnd("a"));

        AssertUnitRejected(unit, "A </mrk> in the source of unit 'u' does not close the innermost open span");
    }

    [TestMethod]
    public void RejectsOriginalDataCarriedWithoutADataRefOnTheTargetSide()
    {
        //Kills XliffWriter.InlineContent.cs:873 (the target-side OriginalDataConsistencyProblems
        //propagation): RejectsOriginalDataCarriedWithoutADataRef only builds source content via
        //UnitOf; the target-side call is never exercised.
        var segment = new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("x"), InlineContent.Create([Ph("1", originalData: new OriginalData("y"))]), SegmentState.Initial, null);
        var unit = new XliffUnit("u", [segment], ImmutableArray<string>.Empty, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty, null);

        AssertUnitRejected(unit, "carries original data without a dataRef");
    }

    /// <summary>Proves a paired span after text recurses over exactly its inner content.</summary>
    [TestMethod]
    public void WritesTheExactBytesOfAPairedSpanAfterText()
    {
        //T-014, XliffWriter.InlineContent.cs:96 (census line 84): index + 1 to index - 1
        //re-enters this start until the depth invariant throws InvalidOperationException.
        //The byte assertion requires successful serialization of exactly the text and paired span.
        XliffUnit unit = UnitOf(new InlineTextPart("before"), PcStart("x"), new InlineTextPart("inside"), PcEnd("x"));

        CollectionAssert.AreEqual(InlineDocumentBytes("before<pc id=\"x\">inside</pc>"), WriteToBytes(DocumentOf(unit)));
    }

    /// <summary>Proves a marker after text recurses over exactly its inner content.</summary>
    [TestMethod]
    public void WritesTheExactBytesOfAMarkerAfterText()
    {
        //T-019, XliffWriter.InlineContent.cs:124 (census line 112): index + 1 to index - 1
        //re-enters this marker until the depth invariant throws InvalidOperationException.
        //The byte assertion requires successful serialization of exactly the text and marker.
        XliffUnit unit = UnitOf(new InlineTextPart("before"), Mrk("x"), new InlineTextPart("inside"), MrkEnd("x"));

        CollectionAssert.AreEqual(InlineDocumentBytes("before<mrk id=\"x\">inside</mrk>"), WriteToBytes(DocumentOf(unit)));
    }

    /// <summary>Proves an empty paired code retains explicit start and end tags.</summary>
    [TestMethod]
    public void WritesAnEmptyPairedCodeWithExplicitStartAndEndTags()
    {
        //S-054, XliffWriter.InlineContent.cs:95 (census line 83): removing WriteString(string.Empty)
        //lets the empty pc self-close; the exact bytes require <pc id="x"></pc>.
        XliffUnit unit = UnitOf(PcStart("x"), PcEnd("x"));

        CollectionAssert.AreEqual(InlineDocumentBytes("<pc id=\"x\"></pc>"), WriteToBytes(DocumentOf(unit)));
    }

    /// <summary>Proves an empty marker retains explicit start and end tags.</summary>
    [TestMethod]
    public void WritesAnEmptyMarkerWithExplicitStartAndEndTags()
    {
        //S-055, XliffWriter.InlineContent.cs:123 (census line 111): removing WriteString(string.Empty)
        //lets the empty mrk self-close; the exact bytes require <mrk id="x"></mrk>.
        XliffUnit unit = UnitOf(Mrk("x"), MrkEnd("x"));

        CollectionAssert.AreEqual(InlineDocumentBytes("<mrk id=\"x\"></mrk>"), WriteToBytes(DocumentOf(unit)));
    }

    /// <summary>Builds the exact expected UTF-8 document bytes for one inline source.</summary>
    private static byte[] InlineDocumentBytes(string source) => Encoding.UTF8.GetBytes($"""
        <?xml version="1.0" encoding="utf-8"?>
        <xliff version="2.1" srcLang="en" trgLang="fi" xml:space="preserve" xmlns="urn:oasis:names:tc:xliff:document:2.0">
          <file id="f">
            <unit id="u">
              <segment>
                <source>{source}</source>
              </segment>
            </unit>
          </file>
        </xliff>
        """);

    /// <summary>Proves each inline part advances and recursive spans finish within the bound.</summary>
    [TestMethod]
    public void WritesEveryPartKindWithoutHangingOnAMissingIndexAdvance()
    {
        //The five-second assertion bounds missing loop advances at census lines 57,65,73,94,102,122.
        //T-014/T-019, XliffWriter.InlineContent.cs:96/124 (census 84/112), change index + 1 to
        //index - 1 in recursion. The depth invariant now throws a named InvalidOperationException,
        //which faults this task; a timed task wait alone could not contain the former stack overflow.
        XliffUnit unit = UnitOf(
            new InlineTextPart("a"),
            Ph("1"),
            Sc("sp1"), new InlineTextPart("b"), Ec("sp1"),
            PcStart("pc1"), new InlineTextPart("c"), PcEnd("pc1"),
            Sm("sm1"), new InlineTextPart("d"), Em("sm1"),
            Mrk("mk1"), new InlineTextPart("e"), MrkEnd("mk1"));

        Task<byte[]> task = Task.Run(() => WriteToBytes(DocumentOf(unit)));

        Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(5)), "WriteInlineContent did not complete in time; a part kind's index was not advanced.");
    }

    [TestMethod]
    public void EncodesConsecutiveCodePointsWithoutHangingOnAMissingIndexAdvance()
    {
        //Kills XliffWriter.InlineContent.cs:367 (WriteInlineText, after encoding a <cp>):
        //index += 1 => index -= 1 sends index backward, re-encoding the same and preceding characters
        //forever instead of advancing past the encoded code point.
        XliffUnit unit = UnitOf(new InlineTextPart("abc"));

        Task<byte[]> task = Task.Run(() => WriteToBytes(DocumentOf(unit)));

        Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(5)), "WriteInlineText did not complete in time; index was not advanced past an encoded code point.");
    }
}
