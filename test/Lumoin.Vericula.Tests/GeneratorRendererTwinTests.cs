using System.Linq;
using System.Text;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

/// <summary>
/// The core-side half of the mandatory step 5 (5.7) cross-check: the generator's small inline
/// renderer (<c>Lumoin.Vericula.SourceGenerators.XliffSourceGenerator</c>, in
/// <c>XliffSourceGenerator.InlineContent.cs</c>) is a necessary duplicate of
/// <see cref="InlineContent.Render(InlineRendering)"/> and <see cref="XliffUnit.RenderTarget(InlineRendering)"/>,
/// since the generator cannot reference this assembly. Each test here shares its sample XLIFF text and
/// its expected rendered string, verbatim, with one test in
/// <c>test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs</c> named in its own
/// "Twin:" comment, so the two renderers cannot drift silently: a change to one that is not mirrored in
/// the other breaks either this suite or that one.
/// </summary>
[TestClass]
public sealed class GeneratorRendererTwinTests
{
    /// <summary>Reads a complete XLIFF document's first unit through the production reader, the same way the generator reads the same text through its own small parser.</summary>
    /// <param name="xliff">A complete <c>&lt;xliff&gt;</c> document.</param>
    /// <returns>The document's first file's first unit.</returns>
    private static XliffUnit ReadFirstUnit(string xliff)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        return XliffReader.Read(stream).Files[0].Units[0];
    }

    /// <summary>Reads a complete XLIFF document's first unit by id through the production reader.</summary>
    /// <param name="xliff">A complete <c>&lt;xliff&gt;</c> document.</param>
    /// <param name="unitId">The id of the unit to return.</param>
    /// <returns>The named unit.</returns>
    private static XliffUnit ReadUnitById(string xliff, string unitId)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        return XliffReader.Read(stream).Files[0].Units.Single(unit => unit.Id == unitId);
    }

    [TestMethod]
    public void PreferenceOrderPicksOriginalDataOverASynthesizedTagDispAndEquiv()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs PreferenceOrderPicksOriginalDataOverASynthesizedTagDispAndEquiv
        //5.2: InlineContent.Render(Markup) prefers original data over a synthesized tag, disp and
        //equiv, even though this code also carries a resolvable type (link), a disp and an equiv.
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="Icon">
                  <originalData>
                    <data id="d1">&lt;b&gt;</data>
                  </originalData>
                  <segment>
                    <source>Hello <ph id="1" type="link" dataRef="d1" disp="LinkText" equiv="[link]"/> world</source>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("Hello <b> world", unit.RenderSource(InlineRendering.Markup));
    }

    [TestMethod]
    public void TextRendersVerbatimWhenTheContentHasNoCodes()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs TextRendersVerbatimWhenTheContentHasNoCodes
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="PlainAngles">
                  <segment>
                    <source>A &amp; B &lt; C</source>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("A & B < C", unit.RenderSource(InlineRendering.Markup));
    }

    [TestMethod]
    public void TextEscapesWhenTheContentHasAtLeastOneCode()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs TextEscapesWhenTheContentHasAtLeastOneCode
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="AnglesWithCode">
                  <segment>
                    <source>A &amp; B &lt; C <ph id="1" equiv="x"/></source>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("A &amp; B &lt; C x", unit.RenderSource(InlineRendering.Markup));
    }

    [TestMethod]
    public void ACodePointAboveFfffBecomesASurrogatePairAndTwoHalvesMergeTheSameWay()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs ACodePointAboveFfffBecomesASurrogatePairAndTwoHalvesMergeTheSameWay
        string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="EmojiOneCp">
                  <segment>
                    <source><cp hex="01F600"/></source>
                  </segment>
                </unit>
                <unit id="EmojiTwoCp">
                  <segment>
                    <source><cp hex="d83d"/><cp hex="DE00"/></source>
                  </segment>
                </unit>
              </file>
            </xliff>
            """;
        string expected = char.ConvertFromUtf32(0x1F600);

        Assert.AreEqual(expected, ReadUnitById(xliff, "EmojiOneCp").RenderSource(InlineRendering.Markup));
        Assert.AreEqual(expected, ReadUnitById(xliff, "EmojiTwoCp").RenderSource(InlineRendering.Markup));
    }

    [TestMethod]
    public void TwoCodesSharingADataRefRenderTheSameOriginalDataText()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs TwoCodesSharingADataRefRenderTheSameOriginalDataText
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="Shared">
                  <originalData>
                    <data id="d1">*</data>
                    <data id="unused">ZZZ</data>
                  </originalData>
                  <segment>
                    <source><ph id="p1" dataRef="d1"/> and <ph id="p2" dataRef="d1"/></source>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("* and *", unit.RenderSource(InlineRendering.Markup));
    }

    [TestMethod]
    public void NestedPairedCodesRenderTheirStartHalfChildrenThenEndHalf()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs NestedPairedCodesRenderTheirStartHalfChildrenThenEndHalf
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="Nested">
                  <segment>
                    <source>A <pc id="1" type="fmt" subType="xlf:b">bold <pc id="2" type="fmt" subType="xlf:i">and italic</pc> text</pc> end</source>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("A <b>bold <i>and italic</i> text</b> end", unit.RenderSource(InlineRendering.Markup));
    }

    [TestMethod]
    public void ATranslateNoSpanOpenedInOneSegmentAndClosedInALaterOneExemptsTheSegmentBetween()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs ATranslateNoSpanOpenedInOneSegmentAndClosedInALaterOneExemptsTheSegmentBetween
        //5.1: the translate stack carried across the unit's segments in document order means the
        //middle segment's target-less "ACME" counts as complete (nothing to translate), so
        //RenderTarget folds it in from its own source rather than returning null for the whole unit.
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="ThreeSegmentSpan">
                  <segment>
                    <source>Hello <sm id="s1" translate="no"/></source>
                    <target>Hei </target>
                  </segment>
                  <segment>
                    <source>ACME</source>
                  </segment>
                  <segment>
                    <source><em startRef="s1"/> world</source>
                    <target> maailma</target>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("Hei ACME maailma", unit.RenderTarget(InlineRendering.Markup));
    }

    [TestMethod]
    public void AnIgnorableWithAPresentButEmptyTargetFallsBackToItsSource()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs AnIgnorableWithAPresentButEmptyTargetFallsBackToItsSource
        //5.1: the reader turns a present-but-empty <target> into a null TargetContent, the same as an
        //absent one, so RenderTarget's fold uses the ignorable's own source (the single space) between
        //the two translated segments.
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="IgnorableEmptyTarget">
                  <segment>
                    <source>Hello</source>
                    <target>Hei</target>
                  </segment>
                  <ignorable>
                    <source> </source>
                    <target></target>
                  </ignorable>
                  <segment>
                    <source>World</source>
                    <target>maailma</target>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("Hei maailma", unit.RenderTarget(InlineRendering.Markup));
    }

    [TestMethod]
    public void AWholeSegmentInsideAMrkTranslateNoWithAnEmptyTargetElementFoldsItsSource()
    {
        //Twin: test/Lumoin.Vericula.SourceGenerators.Tests/XliffSourceGeneratorTests.cs AWholeSegmentInsideAMrkTranslateNoWithAnEmptyTargetElementFoldsItsSource
        //5.1: a present-but-empty <target> is a null TargetContent (XliffReader), so RenderTarget falls
        //back to the segment's source even though the segment's own translatable text is empty (fully
        //inside translate="no").
        XliffUnit unit = ReadFirstUnit("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="EmptyTargetNotATarget">
                  <segment>
                    <source><mrk id="m1" translate="no">Internal only</mrk></source>
                    <target></target>
                  </segment>
                </unit>
              </file>
            </xliff>
            """);

        Assert.AreEqual("Internal only", unit.RenderTarget(InlineRendering.Markup));
    }
}
