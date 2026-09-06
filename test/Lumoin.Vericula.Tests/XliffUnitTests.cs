using System.Collections.Immutable;
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
            new XliffSegment(null, SegmentKind.Translatable, "First.", "Eka.", SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, " ", " ", SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, "Second.", "Toka.", SegmentState.Translated, null));

        Assert.AreEqual("First. Second.", unit.Source);
        Assert.AreEqual("Eka. Toka.", unit.Target);
    }

    [TestMethod]
    public void TargetIsNullWhenNoSegmentCarriesOne()
    {
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, "First.", null, SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Ignorable, " ", null, SegmentState.Initial, null));

        Assert.AreEqual("First. ", unit.Source);
        Assert.IsNull(unit.Target);
    }

    [TestMethod]
    public void TargetIsNullWhenATranslatableSegmentHasNoTarget()
    {
        //r1-58: a partial translation must not fold to a truncated string; the whole unit is
        //untranslated until every translatable segment carries a target.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, "First.", "Eka.", SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, " ", null, SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, "Second.", null, SegmentState.Initial, null));

        Assert.IsNull(unit.Target);
    }

    [TestMethod]
    public void TargetFoldsAnIgnorablesSourceWhenTheIgnorableCarriesNoTarget()
    {
        //r1-58: an ignorable that was left untranslated still contributes its source, so the
        //whitespace between sentences survives the fold instead of vanishing.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, "First.", "Eka.", SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, " ", null, SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, "Second.", "Toka.", SegmentState.Translated, null));

        Assert.AreEqual("Eka. Toka.", unit.Target);
    }

    [TestMethod]
    public void TargetUsesAnIgnorablesOwnTargetWhenItDiffersFromSource()
    {
        //XliffUnit.cs:77, segment.Target ?? segment.Source => segment.Source: an ignorable's own
        //target must survive the fold when it differs from its source, so this assertion fails if
        //the coalescing is short-circuited straight to the source.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, "First.", "Eka.", SegmentState.Translated, null),
            new XliffSegment(null, SegmentKind.Ignorable, " ", "_", SegmentState.Initial, null),
            new XliffSegment(null, SegmentKind.Translatable, "Second.", "Toka.", SegmentState.Translated, null));

        Assert.AreEqual("Eka._Toka.", unit.Target);
    }

    [TestMethod]
    public void TargetIsNullWhenATranslatableSegmentNeedsTranslation()
    {
        //r1-62: a segment flagged NeedsTranslation carries a stale target left over from before the
        //source changed; the cook must not treat it as a finished translation.
        XliffUnit unit = UnitOf(
            new XliffSegment(null, SegmentKind.Translatable, "First.", "Eka.", SegmentState.NeedsTranslation, null));

        Assert.IsNull(unit.Target);
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
