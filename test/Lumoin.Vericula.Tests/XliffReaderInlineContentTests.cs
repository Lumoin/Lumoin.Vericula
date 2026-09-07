using System.Text;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

/// <summary>
/// Tests for <see cref="XliffReader"/>'s inline content parsing (step 2 of the inline content slice):
/// every element and attribute default of 5.3, the <c>cp</c> code point rules of 5.3.1, and every
/// structural refusal of 5.3.2. <see cref="XliffReaderTests"/> keeps the reader's structural tests;
/// this file is only about what a segment's <c>&lt;source&gt;</c>/<c>&lt;target&gt;</c> content parses
/// into.
/// </summary>
[TestClass]
public sealed class XliffReaderInlineContentTests
{
    /// <summary>Wraps one unit body (its children, typically <c>&lt;segment&gt;</c>/<c>&lt;originalData&gt;</c> elements) in a minimal document.</summary>
    private static string Wrap(string unitBody) =>
        $"""<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi"><file id="f"><unit id="u">{unitBody}</unit></file></xliff>""";

    /// <summary>Wraps a single segment's source (and optional target) markup in a minimal document and returns the parsed unit.</summary>
    private static XliffUnit ReadUnit(string unitBody)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Wrap(unitBody)));

        return XliffReader.Read(stream).Files[0].Units.Single();
    }

    /// <summary>Wraps a unit body and expects the read to throw.</summary>
    private static XliffFormatException ReadUnitExpectingFailure(string unitBody)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Wrap(unitBody)));

        return Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.Read(stream));
    }

    [TestMethod]
    public void ReadsAPlaceholderWithItsAttributeDefaults()
    {
        XliffUnit unit = ReadUnit("""<segment><source>Hi <ph id="1"/> there</source></segment>""");

        var placeholder = (PlaceholderPart)unit.Segments[0].SourceContent.Parts[1];
        Assert.AreEqual("1", placeholder.Id);
        Assert.AreEqual(InlineCodeType.None, placeholder.Type);
        Assert.IsNull(placeholder.SubType);
        Assert.AreEqual(string.Empty, placeholder.Equiv);
        Assert.IsNull(placeholder.Disp);
        Assert.IsNull(placeholder.DataRef);
        Assert.IsNull(placeholder.OriginalData);
        Assert.IsTrue(placeholder.CanCopy);
        Assert.IsTrue(placeholder.CanDelete);
        Assert.AreEqual(ReorderHint.Yes, placeholder.CanReorder);
        Assert.IsNull(placeholder.CopyOf);
        Assert.IsNull(placeholder.SubFlows);
    }

    [TestMethod]
    public void ReadsAPlaceholderWithEveryAttributeSet()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="d1">%s</data></originalData>
            <segment><source><ph id="1" type="fmt" subType="xlf:b" equiv="[b]" disp="&lt;b&gt;" dataRef="d1" canCopy="no" canDelete="no" canReorder="firstNo" copyOf="0" subFlows="u2"/></source></segment>
            """);

        var placeholder = (PlaceholderPart)unit.Segments[0].SourceContent.Parts[0];
        Assert.AreEqual(InlineCodeType.Format, placeholder.Type);
        Assert.AreEqual("xlf:b", placeholder.SubType);
        Assert.AreEqual("[b]", placeholder.Equiv);
        Assert.AreEqual("<b>", placeholder.Disp);
        Assert.AreEqual("d1", placeholder.DataRef);
        Assert.AreEqual("%s", placeholder.OriginalData?.Text);
        Assert.IsFalse(placeholder.CanCopy);
        Assert.IsFalse(placeholder.CanDelete);
        Assert.AreEqual(ReorderHint.FirstNo, placeholder.CanReorder);
        Assert.AreEqual("0", placeholder.CopyOf);
        Assert.AreEqual("u2", placeholder.SubFlows);
    }

    [TestMethod]
    public void RejectsAPlaceholderWithoutAnId()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph/></source></segment>""");

        Assert.Contains("A <ph> element does not declare the required id attribute", exception.Message, StringComparison.Ordinal);
        //Line info must survive a semantic (not just well-formedness) refusal, matching the reader's
        //existing contract for a missing id elsewhere (MissingIdExceptionCarriesTheElementsLineAndPosition).
        Assert.IsNotNull(exception.Line);
        Assert.IsNotNull(exception.Position);
    }

    [TestMethod]
    public void RejectsAPlaceholderWithAnUnsupportedAttribute()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph id="1" bogus="x"/></source></segment>""");

        Assert.Contains("carries the unsupported attribute 'bogus'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void DropsANamespaceQualifiedAttributeOnAPlaceholderInsteadOfRefusingIt()
    {
        XliffUnit unit = ReadUnit(
            """<segment xmlns:its="http://www.w3.org/2005/11/its"><source><ph id="1" its:translate="no"/></source></segment>""");

        var placeholder = (PlaceholderPart)unit.Segments[0].SourceContent.Parts[0];
        Assert.AreEqual("1", placeholder.Id);
    }

    [TestMethod]
    public void ReadsAPairedCodeIntoAStartPartItsChildAndAnEndPart()
    {
        XliffUnit unit = ReadUnit("""<segment><source>Click <pc id="1">here</pc> now</source></segment>""");

        InlineContent content = unit.Segments[0].SourceContent;
        Assert.HasCount(5, content.Parts);
        var start = (StartCodePart)content.Parts[1];
        var end = (EndCodePart)content.Parts[3];
        Assert.AreEqual("1", start.Id);
        Assert.AreEqual(SpanForm.Paired, start.Form);
        Assert.IsFalse(start.Isolated);
        Assert.AreEqual("here", ((InlineTextPart)content.Parts[2]).Text);
        Assert.AreEqual("1", end.StartRef);
        Assert.IsNull(end.Id);
        Assert.AreEqual(SpanForm.Paired, end.Form);
    }

    [TestMethod]
    public void MapsPairedCodeAttributesToTheirStartAndEndHalvesPerTable2()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="ds">&lt;B&gt;</data><data id="de">&lt;/B&gt;</data></originalData>
            <segment><source><pc id="1" type="fmt" subType="xlf:b" canCopy="no" canDelete="no" canReorder="firstNo" copyOf="0" canOverlap="yes" dir="rtl"
                dispStart="[b" dispEnd="b]" equivStart="&lt;b&gt;" equivEnd="&lt;/b&gt;" subFlowsStart="u1" subFlowsEnd="u2" dataRefStart="ds" dataRefEnd="de">x</pc></source></segment>
            """);

        var start = (StartCodePart)unit.Segments[0].SourceContent.Parts[0];
        var end = (EndCodePart)unit.Segments[0].SourceContent.Parts[2];

        //Shared attributes: both halves carry the same value.
        Assert.AreEqual("1", start.Id);
        Assert.AreEqual("1", end.StartRef);
        Assert.AreEqual(InlineCodeType.Format, start.Type);
        Assert.AreEqual(InlineCodeType.Format, end.Type);
        Assert.AreEqual("xlf:b", start.SubType);
        Assert.AreEqual("xlf:b", end.SubType);
        Assert.IsFalse(start.CanCopy);
        Assert.IsFalse(end.CanCopy);
        Assert.IsFalse(start.CanDelete);
        Assert.IsFalse(end.CanDelete);
        Assert.AreEqual(ReorderHint.FirstNo, start.CanReorder);
        Assert.AreEqual(ReorderHint.FirstNo, end.CanReorder);
        Assert.AreEqual("0", start.CopyOf);
        Assert.AreEqual("0", end.CopyOf);
        Assert.IsTrue(start.CanOverlap);
        Assert.IsTrue(end.CanOverlap);
        Assert.AreEqual(TextDirection.RightToLeft, start.Direction);
        Assert.AreEqual(TextDirection.RightToLeft, end.Direction);

        //Half-specific attributes: Start from the "Start" suffix, End from the "End" suffix.
        Assert.AreEqual("[b", start.Disp);
        Assert.AreEqual("b]", end.Disp);
        Assert.AreEqual("<b>", start.Equiv);
        Assert.AreEqual("</b>", end.Equiv);
        Assert.AreEqual("u1", start.SubFlows);
        Assert.AreEqual("u2", end.SubFlows);
        Assert.AreEqual("<B>", start.OriginalData?.Text);
        Assert.AreEqual("</B>", end.OriginalData?.Text);
    }

    [TestMethod]
    public void APairedCodeIsNeverIsolatedBecausePcHasNoIsolatedAttributeOfItsOwn()
    {
        XliffUnit unit = ReadUnit("""<segment><source><pc id="1">x</pc></source></segment>""");

        var start = (StartCodePart)unit.Segments[0].SourceContent.Parts[0];
        var end = (EndCodePart)unit.Segments[0].SourceContent.Parts[2];
        Assert.IsFalse(start.Isolated);
        Assert.IsFalse(end.Isolated);
    }

    [TestMethod]
    public void ReadsNestedPairedCodes()
    {
        XliffUnit unit = ReadUnit("""<segment><source><pc id="1">a <pc id="2">b</pc> c</pc></source></segment>""");

        InlineContent content = unit.Segments[0].SourceContent;
        //outer-start, "a ", inner-start, "b", inner-end, " c", outer-end
        Assert.HasCount(7, content.Parts);
        Assert.AreEqual("1", ((StartCodePart)content.Parts[0]).Id);
        Assert.AreEqual("2", ((StartCodePart)content.Parts[2]).Id);
        Assert.AreEqual("2", ((EndCodePart)content.Parts[4]).StartRef);
        Assert.AreEqual("1", ((EndCodePart)content.Parts[6]).StartRef);
    }

    [TestMethod]
    public void StartCodeDefaultsCanOverlapToTrueAndPairedCodeDefaultsItToFalse()
    {
        XliffUnit scUnit = ReadUnit("""<segment><source><sc id="1"/>x<ec startRef="1"/></source></segment>""");
        XliffUnit pcUnit = ReadUnit("""<segment><source><pc id="1">x</pc></source></segment>""");

        Assert.IsTrue(((StartCodePart)scUnit.Segments[0].SourceContent.Parts[0]).CanOverlap);
        Assert.IsFalse(((StartCodePart)pcUnit.Segments[0].SourceContent.Parts[0]).CanOverlap);
    }

    [TestMethod]
    public void ReadsATwoSegmentStartAndEndCodePair()
    {
        //A spanning code's sc and ec may live in different segments (XLIFF 2.1 §4.7.2.1); the reader's
        //per-side state threads the open start across the segment boundary.
        XliffUnit unit = ReadUnit("""
            <segment><source><sc id="1" type="fmt" subType="xlf:b"/>First sentence.</source></segment>
            <segment><source>Second sentence.<ec startRef="1" type="fmt" subType="xlf:b"/></source></segment>
            """);

        var start = (StartCodePart)unit.Segments[0].SourceContent.Parts[0];
        var end = (EndCodePart)unit.Segments[1].SourceContent.Parts[1];
        Assert.AreEqual(SpanForm.Split, start.Form);
        Assert.AreEqual("1", start.Id);
        Assert.AreEqual("1", end.StartRef);
        Assert.AreEqual(SpanForm.Split, end.Form);
    }

    [TestMethod]
    public void AnIsolatedStartCodeNeedsNoMatchingEndCodeInTheUnit()
    {
        XliffUnit unit = ReadUnit("""<segment><source><sc id="1" isolated="yes"/>Warning:</source></segment>""");

        var start = (StartCodePart)unit.Segments[0].SourceContent.Parts[0];
        Assert.IsTrue(start.Isolated);
    }

    [TestMethod]
    public void AnIsolatedEndCodeCarriesItsOwnIdAndNoStartRef()
    {
        XliffUnit unit = ReadUnit("""<segment><source>File not found.<ec id="2" isolated="yes"/></source></segment>""");

        var end = (EndCodePart)unit.Segments[0].SourceContent.Parts[1];
        Assert.IsTrue(end.Isolated);
        Assert.AreEqual("2", end.Id);
        Assert.IsNull(end.StartRef);
    }

    [TestMethod]
    public void ReadsAnEndCodeWithItsAttributeDefaults()
    {
        //Named killer: XliffReader.InlineContent.cs (AppendEndCode), the standalone <ec>'s canOverlap
        //default `true` changed to `false` (unlike ph/pc, no earlier test asserted this default for ec).
        XliffUnit unit = ReadUnit("""<segment><source><sc id="1"/>x<ec startRef="1"/></source></segment>""");

        var end = (EndCodePart)unit.Segments[0].SourceContent.Parts[2];
        Assert.AreEqual("1", end.StartRef);
        Assert.IsNull(end.Id);
        Assert.AreEqual(InlineCodeType.None, end.Type);
        Assert.IsNull(end.SubType);
        Assert.AreEqual(string.Empty, end.Equiv);
        Assert.IsNull(end.Disp);
        Assert.IsNull(end.DataRef);
        Assert.IsNull(end.OriginalData);
        Assert.IsTrue(end.CanCopy);
        Assert.IsTrue(end.CanDelete);
        Assert.IsTrue(end.CanOverlap);
        Assert.AreEqual(ReorderHint.Yes, end.CanReorder);
        Assert.IsNull(end.CopyOf);
        Assert.IsNull(end.SubFlows);
        Assert.IsFalse(end.Isolated);
        Assert.AreEqual(TextDirection.Inherited, end.Direction);
        Assert.AreEqual(SpanForm.Split, end.Form);
    }

    [TestMethod]
    public void ReadsAnEndCodeWithEveryAttributeSet()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="d1">&lt;/b&gt;</data></originalData>
            <segment><source><sc id="1"/>x<ec startRef="1" type="fmt" subType="xlf:b" equiv="[/b]" disp="&lt;/b&gt;" dataRef="d1" canCopy="no" canDelete="no" canOverlap="no" canReorder="firstNo" copyOf="0" subFlows="u2" dir="rtl"/></source></segment>
            """);

        var end = (EndCodePart)unit.Segments[0].SourceContent.Parts[2];
        Assert.AreEqual(InlineCodeType.Format, end.Type);
        Assert.AreEqual("xlf:b", end.SubType);
        Assert.AreEqual("[/b]", end.Equiv);
        Assert.AreEqual("</b>", end.Disp);
        Assert.AreEqual("d1", end.DataRef);
        Assert.AreEqual("</b>", end.OriginalData?.Text);
        Assert.IsFalse(end.CanCopy);
        Assert.IsFalse(end.CanDelete);
        Assert.IsFalse(end.CanOverlap);
        Assert.AreEqual(ReorderHint.FirstNo, end.CanReorder);
        Assert.AreEqual("0", end.CopyOf);
        Assert.AreEqual("u2", end.SubFlows);
        Assert.AreEqual(TextDirection.RightToLeft, end.Direction);
    }

    [TestMethod]
    public void RejectsAnIsolatedEndCodeWithoutAnId()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ec isolated="yes"/></source></segment>""");

        Assert.Contains("An isolated <ec> element in unit 'u' does not declare the required id attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnIsolatedEndCodeThatAlsoCarriesAStartRef()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ec id="2" isolated="yes" startRef="1"/></source></segment>""");

        Assert.Contains("must not declare a startRef attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnIsolatedEndCodeWithAnInvalidIdAndCarriesLineAndPosition()
    {
        //Named killer: XliffReader.InlineContent.cs (AppendEndCode's isolated branch), the
        //try/catch that wraps RequireNameToken with WithLocation removed would still throw the same
        //XliffFormatException but without Line/Position set, since RequireNameToken itself never
        //touches the element's line info; only the wrapping catches that.
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ec id="not a token" isolated="yes"/></source></segment>""");

        Assert.Contains("is not an XML name token", exception.Message, StringComparison.Ordinal);
        Assert.IsNotNull(exception.Line);
        Assert.IsNotNull(exception.Position);
    }

    [TestMethod]
    public void RejectsANonIsolatedEndCodeWithoutAStartRef()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ec/></source></segment>""");

        Assert.Contains("A <ec> element in unit 'u' does not declare the required startRef attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnEndCodeWhoseStartRefNamesNoOpenStartCode()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ec startRef="missing"/></source></segment>""");

        Assert.Contains("names no open <sc> on this side", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnEndCodeThatTriesToCloseAnIsolatedStartCode()
    {
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source><sc id="1" isolated="yes"/>x<ec startRef="1"/></source></segment>""");

        Assert.Contains("is isolated and must not be closed by an <ec>", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAStartCodeStillOpenAtTheEndOfTheUnit()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source>x<sc id="1"/></source></segment>""");

        Assert.Contains("that is never closed by a matching <ec>", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAStartCodeStillOpenOnTheTargetSideAtTheEndOfTheUnit()
    {
        //Named killer: XliffReader.cs:859, the target-side call to RequireEveryStartCodeClosedOrIsolated
        //dropped entirely: the source side is fine here, so only a check that actually runs over the
        //target's own state catches an sc left open there.
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source>x</source><target>y<sc id="1"/></target></segment>""");

        Assert.Contains("that is never closed by a matching <ec>", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AStartCodeOnTheTargetSideIsNotClosedByAnEndCodeOnTheSourceSide()
    {
        //sc/ec pairing is per side: a source-side sc must not be closeable by a target-side ec.
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source><sc id="1"/>x<ec startRef="1"/></source><target>y<ec startRef="1"/></target></segment>""");

        Assert.Contains("names no open <sc> on this side", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsAMarkerAnnotationWithTranslateFalse()
    {
        //Named killer: XliffReader.InlineContent.cs:519 (AppendMarker), the recursive
        //`state = AppendNodes(element.Nodes(), builder, state, context);` call over the mrk's children
        //replaced with a no-op would drop the "ACME" text run the mrk wraps, shifting every later index
        //down by one; Parts[3] would then be the trailing " as is" text instead of the
        //AnnotationEndPart, so the cast below throws InvalidCastException instead of the assertions
        //passing.
        XliffUnit unit = ReadUnit("""<segment><source>Keep <mrk id="m1" translate="no">ACME</mrk> as is</source></segment>""");

        InlineContent content = unit.Segments[0].SourceContent;
        var start = (AnnotationStartPart)content.Parts[1];
        var end = (AnnotationEndPart)content.Parts[3];
        Assert.AreEqual("m1", start.Id);
        Assert.AreEqual("generic", start.Type);
        Assert.AreEqual(false, start.Translate);
        Assert.AreEqual(AnnotationForm.Marker, start.Form);
        Assert.AreEqual("ACME", ((InlineTextPart)content.Parts[2]).Text);
        Assert.AreEqual("m1", end.StartRef);
        Assert.AreEqual(AnnotationForm.Marker, end.Form);
    }

    [TestMethod]
    public void ReadsATermAnnotation()
    {
        XliffUnit unit = ReadUnit(
            """<segment><source>my <mrk id="m1" type="term" ref="http://example.com/t">doppelganger</mrk></source></segment>""");

        var start = (AnnotationStartPart)unit.Segments[0].SourceContent.Parts[1];
        Assert.AreEqual("term", start.Type);
        Assert.AreEqual("http://example.com/t", start.Ref);
        Assert.IsNull(start.Value);
    }

    [TestMethod]
    public void ReadsACommentAnnotationByValue()
    {
        XliffUnit unit = ReadUnit(
            """<segment><source>The <mrk id="m1" type="comment" value="Printer or Stacker">setting</mrk> is enabled.</source></segment>""");

        var start = (AnnotationStartPart)unit.Segments[0].SourceContent.Parts[1];
        Assert.AreEqual("comment", start.Type);
        Assert.AreEqual("Printer or Stacker", start.Value);
        Assert.IsNull(start.Ref);
    }

    [TestMethod]
    public void ReadsACommentAnnotationByRef()
    {
        XliffUnit unit = ReadUnit(
            """<segment><source>You use your own <mrk id="m1" type="comment" ref="#n=n1">namespace</mrk>.</source></segment>""");

        var start = (AnnotationStartPart)unit.Segments[0].SourceContent.Parts[1];
        Assert.AreEqual("#n=n1", start.Ref);
        Assert.IsNull(start.Value);
    }

    [TestMethod]
    public void RejectsACommentAnnotationWithNeitherValueNorRef()
    {
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source><mrk id="m1" type="comment">x</mrk></source></segment>""");

        Assert.Contains("has neither a value nor a ref attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsACommentAnnotationWithBothValueAndRef()
    {
        //Named killer: XliffReader.InlineContent.cs (ParseAnnotationAttributes), dropping this
        //"both value and ref" guard would silently accept a comment annotation carrying both.
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source><mrk id="m1" type="comment" value="v" ref="#n=n1">x</mrk></source></segment>""");

        Assert.Contains("has both a value and a ref attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsASplitAnnotationAcrossTwoSegments()
    {
        XliffUnit unit = ReadUnit("""
            <segment><source>Sentence A. </source></segment>
            <segment><source><sm id="m1" type="comment" value="Comment for B and C"/>Sentence B. </source></segment>
            <segment><source>Sentence C.<em startRef="m1"/></source></segment>
            """);

        var start = (AnnotationStartPart)unit.Segments[1].SourceContent.Parts[0];
        var end = (AnnotationEndPart)unit.Segments[2].SourceContent.Parts[1];
        Assert.AreEqual(AnnotationForm.Split, start.Form);
        Assert.AreEqual("Comment for B and C", start.Value);
        Assert.AreEqual("m1", end.StartRef);
        Assert.AreEqual(AnnotationForm.Split, end.Form);
    }

    [TestMethod]
    public void AnOpenSplitAnnotationAtTheUnitsEndIsTolerated()
    {
        //Unlike an unclosed sc, an sm with no em by the unit's end is not refused (5.3.2).
        XliffUnit unit = ReadUnit("""<segment><source><sm id="m1"/>text</source></segment>""");

        var start = (AnnotationStartPart)unit.Segments[0].SourceContent.Parts[0];
        Assert.AreEqual("m1", start.Id);
    }

    [TestMethod]
    public void RejectsAnEndMarkerWhoseStartRefNamesNoOpenStartMarker()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><em startRef="missing"/></source></segment>""");

        Assert.Contains("names no open <sm> on this side", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnEndMarkerWithoutAStartRef()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><em/></source></segment>""");

        Assert.Contains("does not declare the required startRef attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void DecodesATwoDigitCodePoint()
    {
        //XLIFF 2.1 §4.2.3.1 binds writers, not readers: <cp hex="A0"/> decodes to U+00A0 even though a
        //writer would never emit cp for a character valid in XML.
        XliffUnit unit = ReadUnit("""<segment><source>x<cp hex="A0"/>y</source></segment>""");

        Assert.AreEqual("x y", ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void DecodesAFourDigitCodePoint()
    {
        XliffUnit unit = ReadUnit("""<segment><source>Ctrl+C=<cp hex="0003"/></source></segment>""");

        Assert.AreEqual("Ctrl+C=", ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void DecodesASixDigitLowercaseCodePoint()
    {
        XliffUnit unit = ReadUnit("""<segment><source><cp hex="01f600"/></source></segment>""");

        Assert.AreEqual(char.ConvertFromUtf32(0x1F600), ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void RejectsANonHexadecimalCodePoint()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><cp hex="zz"/></source></segment>""");

        Assert.Contains("is not a valid hexadecimal number", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsACodePointHexPaddedWithWhiteSpace()
    {
        //Named killer: XliffReader.InlineContent.cs (ParseCodePointHex), removing the per-character
        //char.IsAsciiHexDigit loop would leave only int.TryParse(hex, NumberStyles.HexNumber, ...), and
        //HexNumber's own AllowLeadingWhite/AllowTrailingWhite flags would then let a value padded with
        //white space through: " A0 " would decode to U+00A0 and " A"/"A " to U+000A instead of refusing.
        Assert.Contains("is not a valid hexadecimal number", ReadUnitExpectingFailure("""<segment><source><cp hex=" A0 "/></source></segment>""").Message, StringComparison.Ordinal);
        Assert.Contains("is not a valid hexadecimal number", ReadUnitExpectingFailure("""<segment><source><cp hex=" A"/></source></segment>""").Message, StringComparison.Ordinal);
        Assert.Contains("is not a valid hexadecimal number", ReadUnitExpectingFailure("""<segment><source><cp hex="A "/></source></segment>""").Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnOddLengthCodePoint()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><cp hex="A"/></source></segment>""");

        Assert.Contains("must be 2, 4 or 6 hexadecimal digits", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsASevenDigitCodePoint()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><cp hex="0110000"/></source></segment>""");

        Assert.Contains("must be 2, 4 or 6 hexadecimal digits", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AcceptsTheMaximumCodePointU10FFFF()
    {
        //Named killer: XliffReader.InlineContent.cs:294 `if(value > MaxCodePoint)` mutated to
        //`if(value >= MaxCodePoint)` would wrongly refuse the boundary value itself;
        //RejectsACodePointAboveTheMaximum alone (hex="110000") cannot catch that mutant because it is
        //already above the boundary either way.
        XliffUnit unit = ReadUnit("""<segment><source><cp hex="10FFFF"/></source></segment>""");

        Assert.AreEqual(char.ConvertFromUtf32(0x10FFFF), ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void RejectsACodePointAboveTheMaximum()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><cp hex="110000"/></source></segment>""");

        Assert.Contains("exceeds the maximum U+10FFFF", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ACodePointInsideDataDecodesTheSameWay()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="d1">x<cp hex="0009"/>y</data></originalData>
            <segment><source><ph id="1" dataRef="d1"/></source></segment>
            """);

        var placeholder = (PlaceholderPart)unit.Segments[0].SourceContent.Parts.Single();
        Assert.AreEqual("x\ty", placeholder.OriginalData?.Text);
    }

    [TestMethod]
    public void RejectsACodePointWithAnUnsupportedAttributeInsideData()
    {
        //Named killer: XliffReader.InlineContent.cs (ReadDataText), dropping the RefuseUnknownAttributes
        //call on the cp arm would let a <cp> inside <data> carry an attribute a <cp> inside content
        //could never get away with.
        XliffFormatException exception = ReadUnitExpectingFailure("""
            <originalData><data id="d1"><cp hex="0009" bogus="x"/></data></originalData>
            <segment><source>x</source></segment>
            """);

        Assert.Contains("carries the unsupported attribute 'bogus'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsACodeTypeOutsideTheSixReservedValues()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph id="1" type="bogus"/></source></segment>""");

        Assert.Contains("restricts a code's type to fmt, ui, quote, link, image or other", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsASubTypeWithoutAType()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph id="1" subType="custom:x"/></source></segment>""");

        Assert.Contains("without a type", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsTheReservedLineBreakSubTypeWithTheWrongType()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph id="1" type="ui" subType="xlf:lb"/></source></segment>""");

        Assert.Contains("requires type=\"fmt\"", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsTheReservedVariableSubTypeWithTheWrongType()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph id="1" type="fmt" subType="xlf:var"/></source></segment>""");

        Assert.Contains("requires type=\"ui\"", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AcceptsAFreeformSubTypeUnderAnyType()
    {
        XliffUnit unit = ReadUnit("""<segment><source><ph id="1" type="other" subType="myTool:widget"/></source></segment>""");

        Assert.AreEqual("myTool:widget", ((PlaceholderPart)unit.Segments[0].SourceContent.Parts.Single()).SubType);
    }

    [TestMethod]
    public void ReadsOriginalDataWithAnExplicitDirection()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="d1" dir="rtl">שלום</data></originalData>
            <segment><source><ph id="1" dataRef="d1"/></source></segment>
            """);

        OriginalData? data = ((PlaceholderPart)unit.Segments[0].SourceContent.Parts.Single()).OriginalData;
        Assert.AreEqual("שלום", data?.Text);
        Assert.AreEqual(TextDirection.RightToLeft, data?.Direction);
    }

    [TestMethod]
    public void DataDirectionDefaultsToAutoNotInherited()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="d1">x</data></originalData>
            <segment><source><ph id="1" dataRef="d1"/></source></segment>
            """);

        Assert.AreEqual(TextDirection.Auto, ((PlaceholderPart)unit.Segments[0].SourceContent.Parts.Single()).OriginalData?.Direction);
    }

    [TestMethod]
    public void TwoCodesSharingOneDataRefCarryEqualOriginalData()
    {
        XliffUnit unit = ReadUnit("""
            <originalData><data id="d1">\b </data></originalData>
            <segment><source><sc id="1" dataRef="d1"/>bold<ec startRef="1" dataRef="d1"/></source></segment>
            """);

        InlineContent content = unit.Segments[0].SourceContent;
        OriginalData? start = ((StartCodePart)content.Parts[0]).OriginalData;
        OriginalData? end = ((EndCodePart)content.Parts[2]).OriginalData;
        Assert.AreEqual(start, end);
    }

    [TestMethod]
    public void AnUnreferencedOriginalDataEntryIsSilentlyDropped()
    {
        //No code references "unused"; the unit still parses, and the referenced entry still resolves.
        XliffUnit unit = ReadUnit("""
            <originalData><data id="used">%s</data><data id="unused">%d</data></originalData>
            <segment><source><ph id="1" dataRef="used"/></source></segment>
            """);

        Assert.AreEqual("%s", ((PlaceholderPart)unit.Segments[0].SourceContent.Parts.Single()).OriginalData?.Text);
    }

    [TestMethod]
    public void RejectsADataRefThatNamesNoDataEntry()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment><source><ph id="1" dataRef="missing"/></source></segment>""");

        Assert.Contains("references the <data> id 'missing', which the unit does not define", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsADuplicateDataId()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""
            <originalData><data id="d1">a</data><data id="d1">b</data></originalData>
            <segment><source>x</source></segment>
            """);

        Assert.Contains("Duplicate <data> id 'd1'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsASecondOriginalDataElementInTheUnit()
    {
        //Named killer: XliffReader.InlineContent.cs (ParseOriginalData), dropping the
        //`if(originalDataElement is not null)` guard would silently keep only the first
        //<originalData> element's entries instead of refusing the second one.
        XliffFormatException exception = ReadUnitExpectingFailure("""
            <originalData><data id="d1">a</data></originalData>
            <originalData><data id="d2">b</data></originalData>
            <segment><source>x</source></segment>
            """);

        Assert.Contains("has more than one <originalData> element", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAForeignNamespaceElementInsideData()
    {
        //Named killer: XliffReader.InlineContent.cs (ReadDataText), dropping the trailing
        //`case(XElement element): throw ...` arm would silently ignore any element that is not a core
        //<cp>, letting "a<ext:tag>hidden</ext:tag>b" read as "ab" instead of being refused.
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<originalData xmlns:ext="urn:example:ext"><data id="d1">a<ext:tag/>b</data></originalData><segment><source>x</source></segment>""");

        Assert.Contains("appears inside <data> in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ATargetElementMayReuseItsSiblingSourceElementsId()
    {
        XliffUnit unit = ReadUnit("""<segment><source>Hi <ph id="1"/></source><target>Hei <ph id="1"/></target></segment>""");

        Assert.AreEqual("1", ((PlaceholderPart)unit.Segments[0].SourceContent.Parts[1]).Id);
        Assert.AreEqual("1", ((PlaceholderPart)unit.Segments[0].TargetContent!.Parts[1]).Id);
    }

    [TestMethod]
    public void TargetSideRejectsTheSameIdUsedTwiceEvenWhenItsSiblingSourceUsesIt()
    {
        //Named killer: XliffReader.InlineContent.cs (RegisterInlineId), applying the sibling-source
        //exemption to the same-side check (instead of only the cross-side one) would let the second
        //<ph id="1"/> in the target below pass, because its sibling source also uses "1": the exemption
        //covers one reuse of the source's id, not a second use on the target's own side.
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source><ph id="1"/></source><target><ph id="1"/><ph id="1"/></target></segment>""");

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ATargetElementMayIntroduceABrandNewIdAsAnAddedCode()
    {
        //XLIFF 2.1 §4.7.2.4: a target-side code whose id matches no source code is an added code.
        XliffUnit unit = ReadUnit("""<segment><source>Hi</source><target>Hei <ph id="added"/></target></segment>""");

        Assert.AreEqual("added", ((PlaceholderPart)unit.Segments[0].TargetContent!.Parts[1]).Id);
    }

    [TestMethod]
    public void SourceSideRejectsTheSameIdUsedTwiceInOneUnit()
    {
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source><ph id="1"/></source></segment><segment><source><ph id="1"/></source></segment>""");

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void TargetSideRejectsReusingAnIdFromADifferentSegmentsSource()
    {
        //segment 2's target tries to reuse segment 1's source id, which is not its own sibling.
        //Named killer: XliffReader.cs:921 (now unchanged in position by this fix), dropping
        //`.Except(sourceState.Ids)` from siblingSourceIds's computation would make it the cumulative set
        //of every source id ever seen on this side (here {"1"}, from segment 1) rather than just this
        //segment's own newly-introduced ids (empty, since segment 2's source is plain text "x"), so
        //segment 2's target above would wrongly look exempt to reuse segment 1's id and this assertion
        //would never see an exception.
        XliffFormatException exception = ReadUnitExpectingFailure("""
            <segment><source><ph id="1"/></source></segment>
            <segment><source>x</source><target><ph id="1"/></target></segment>
            """);

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void TargetSideRejectsTheSameIdUsedTwiceEvenAsAnAddedCode()
    {
        XliffFormatException exception = ReadUnitExpectingFailure("""
            <segment><source>x</source><target><ph id="added"/></target></segment>
            <segment><source>y</source><target><ph id="added"/></target></segment>
            """);

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void SourceSideRejectsAnIdAnEarlierSegmentsTargetIntroduced()
    {
        //Named killer: XliffReader.cs (ParseSegment), building the source side's InlineParseContext
        //with ImmutableHashSet<string>.Empty as OtherSideIds instead of the incoming targetState.Ids
        //would leave segment 2's source unaware that segment 1's target already introduced id "2" as an
        //added code, so the <ph id="2"/> below would wrongly be accepted instead of refused.
        XliffFormatException exception = ReadUnitExpectingFailure("""
            <segment><source>x</source><target><ph id="2"/></target></segment>
            <segment><source><ph id="2"/></source></segment>
            """);

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AnInlineIdMayCollideWithNoSegmentOrIgnorableId()
    {
        //§4.3.1.21: segment/ignorable ids share the same scope as inline ids.
        XliffFormatException exception = ReadUnitExpectingFailure("""<segment id="s1"><source><ph id="s1"/></source></segment>""");

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AnInlineIdMayNotCollideWithAnIgnorablesId()
    {
        //Named killer: XliffReader.InlineContent.cs:153 CollectSegmentScopeIds's
        //`IsSegment(...) || IsIgnorable(...)` mutated to drop the IsIgnorable half would stop seeding
        //an ignorable's id into the unit's shared id scope, silently letting an inline element reuse it.
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<ignorable id="i1"><source> </source></ignorable><segment><source><ph id="i1"/></source></segment>""");

        Assert.Contains("is used more than once in unit 'u'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAForeignNamespaceElementInsideContent()
    {
        XliffFormatException exception = ReadUnitExpectingFailure(
            """<segment><source xmlns:ext="urn:example:ext">Hi <ext:tag/></source></segment>""");

        Assert.Contains("from another namespace appears inside inline content", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsCDataAsPlainText()
    {
        XliffUnit unit = ReadUnit("""<segment><source>a<![CDATA[<b>not markup</b>]]>c</source></segment>""");

        Assert.AreEqual("a<b>not markup</b>c", ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void IgnoresAnXmlCommentInsideContent()
    {
        XliffUnit unit = ReadUnit("""<segment><source>a<!-- a note to self -->b</source></segment>""");

        Assert.AreEqual("ab", ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void AnEmptyTargetElementBecomesANullTargetContent()
    {
        XliffUnit unit = ReadUnit("""<segment><source>x</source><target/></segment>""");

        Assert.IsNull(unit.Segments[0].TargetContent);
    }

    [TestMethod]
    public void AnEmptySourceElementBecomesInlineContentEmpty()
    {
        XliffUnit unit = ReadUnit("""<segment><source></source></segment>""");

        Assert.AreEqual(InlineContent.Empty, unit.Segments[0].SourceContent);
    }

    [TestMethod]
    public void TheStreamingPathParsesInlineContentTheSameWayAsTheWholeDocumentRead()
    {
        const string body = """
            <originalData><data id="d1">\b </data></originalData>
            <segment><source>Text in <sc id="1" dataRef="d1"/>bold<ec startRef="1"/> now.</source></segment>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Wrap(body)));

        XliffUnit unit = XliffReader.ReadUnits(stream).Single();

        InlineContent content = unit.Segments[0].SourceContent;
        var start = (StartCodePart)content.Parts[1];
        Assert.AreEqual("1", start.Id);
        Assert.AreEqual("\\b ", start.OriginalData?.Text);
        Assert.AreEqual("bold", ((InlineTextPart)content.Parts[2]).Text);
        Assert.AreEqual("1", ((EndCodePart)content.Parts[3]).StartRef);
    }
}
