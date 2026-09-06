using System.Collections.Immutable;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class XliffUnitTests
{
    [TestMethod]
    public void FromTextCreatesOneInitialTranslatableSegment()
    {
        XliffUnit unit = XliffUnit.FromText("A", "Home", "Koti");

        XliffSegment segment = unit.Segments.Single();
        Assert.IsNull(segment.Id);
        Assert.AreEqual(SegmentKind.Translatable, segment.Kind);
        Assert.AreEqual("Home", segment.Source);
        Assert.AreEqual("Koti", segment.Target);
        Assert.AreEqual(SegmentState.Initial, segment.State);
        Assert.IsNull(segment.SubState);
        Assert.IsTrue(unit.Notes.IsEmpty);
        Assert.IsTrue(unit.Scopes.IsEmpty);
        Assert.IsTrue(unit.Metadata.IsEmpty);
        Assert.IsNull(unit.Glossary);
    }

    [TestMethod]
    public void SourceFoldsEverySegmentInOrderIncludingIgnorables()
    {
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("First."), InlineContent.FromText("Eka."), SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, InlineContent.FromText(" "), InlineContent.FromText(" "), SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Second."), InlineContent.FromText("Toka."), SegmentState.Translated, null));

        Assert.AreEqual("First. Second.", unit.Source);
        Assert.AreEqual("Eka. Toka.", unit.Target);
    }

    [TestMethod]
    public void TargetIsNullWhenNoSegmentCarriesOne()
    {
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("First."), null, SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Ignorable, InlineContent.FromText(" "), null, SegmentState.Initial, null));

        Assert.AreEqual("First. ", unit.Source);
        Assert.IsNull(unit.Target);
    }

    [TestMethod]
    public void TargetIsNullWhenATranslatableSegmentHasNoTarget()
    {
        //r1-58: a partial translation must not fold to a truncated string; the whole unit is
        //untranslated until every translatable segment carries a target.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("First."), InlineContent.FromText("Eka."), SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, InlineContent.FromText(" "), null, SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Second."), null, SegmentState.Initial, null));

        Assert.IsNull(unit.Target);
    }

    [TestMethod]
    public void TargetFoldsAnIgnorablesSourceWhenTheIgnorableCarriesNoTarget()
    {
        //r1-58: an ignorable that was left untranslated still contributes its source, so the
        //whitespace between sentences survives the fold instead of vanishing.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("First."), InlineContent.FromText("Eka."), SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, InlineContent.FromText(" "), null, SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Second."), InlineContent.FromText("Toka."), SegmentState.Translated, null));

        Assert.AreEqual("Eka. Toka.", unit.Target);
    }

    [TestMethod]
    public void TargetUsesAnIgnorablesOwnTargetWhenItDiffersFromSource()
    {
        //XliffUnit.cs:77, segment.Target ?? segment.Source => segment.Source: an ignorable's own
        //target must survive the fold when it differs from its source, so this assertion fails if
        //the coalescing is short-circuited straight to the source.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("First."), InlineContent.FromText("Eka."), SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, InlineContent.FromText(" "), InlineContent.FromText("_"), SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Second."), InlineContent.FromText("Toka."), SegmentState.Translated, null));

        Assert.AreEqual("Eka._Toka.", unit.Target);
    }

    [TestMethod]
    public void TargetIsNullWhenATranslatableSegmentNeedsTranslation()
    {
        //r1-62: a segment flagged NeedsTranslation carries a stale target left over from before the
        //source changed; the cook must not treat it as a finished translation.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("First."), InlineContent.FromText("Eka."), SegmentState.NeedsTranslation, null));

        Assert.IsNull(unit.Target);
    }

    [TestMethod]
    public void ATranslatableSegmentWithNoTranslatableSourceTextIsCompleteWithoutATarget()
    {
        //Decision 3: a segment whose source is markup-only (or wholly translate="no") has nothing to
        //translate, so it does not block the unit's completeness even though it carries no target of
        //its own; its own source folds in for that segment instead.
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.Format, "xlf:lb", string.Empty, null, null, null, true, true, ReorderHint.Yes, null, null);
        InlineContent markupOnlySource = InlineContent.Create([placeholder]);
        XliffUnit unit = UnitOf(new XliffSegment(null, SegmentKind.Translatable, markupOnlySource, null, SegmentState.Initial, null));

        Assert.AreEqual(string.Empty, markupOnlySource.TranslatableText);
        Assert.AreEqual("<br/>", unit.Target);
    }

    [TestMethod]
    public void ATranslatableSegmentWithTranslatableSourceTextIsNotCompleteWithoutATarget()
    {
        XliffUnit unit = UnitOf(new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Cancel"), null, SegmentState.Initial, null));

        Assert.IsNull(unit.Target);
    }

    [TestMethod]
    public void RenderSourceFoldsEverySegmentUnderTheRequestedRendering()
    {
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.None, null, "[x]", null, null, null, true, true, ReorderHint.Yes, null, null);
        XliffUnit unit = UnitOf(new XliffSegment(null, SegmentKind.Translatable, InlineContent.Create([new InlineTextPart("Tom & Jerry "), placeholder]), InlineContent.FromText("ok"), SegmentState.Translated, null));

        Assert.AreEqual("Tom &amp; Jerry [x]", unit.RenderSource(InlineRendering.Markup));
        Assert.AreEqual("Tom & Jerry [x]", unit.RenderSource(InlineRendering.Plain));
    }

    [TestMethod]
    public void SourceAndTargetAreTheMarkupShorthandsOfRenderSourceAndRenderTarget()
    {
        XliffUnit unit = XliffUnit.FromText("A", "Home", "Koti");

        Assert.AreEqual(unit.RenderSource(InlineRendering.Markup), unit.Source);
        Assert.AreEqual(unit.RenderTarget(InlineRendering.Markup), unit.Target);
    }

    /// <summary>
    /// Builds a unit with id "A" carrying the given segments and no notes, scopes, metadata or glossary.
    /// </summary>
    /// <param name="segments">The unit's segments.</param>
    /// <returns>The built unit.</returns>
    private static XliffUnit UnitOf(params XliffSegment[] segments)
    {
        return new XliffUnit(
            "A",
            ImmutableArray.Create(segments),
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);
    }
}
