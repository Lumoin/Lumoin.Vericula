using System.Collections.Immutable;
using Lumoin.Vericula.Content;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class InlineContentTests
{
    [TestMethod]
    public void CreateMergesAdjacentTextPartsIntoOne()
    {
        InlineContent content = InlineContent.Create([new InlineTextPart("Hello, "), new InlineTextPart("world!")]);

        InlinePart part = content.Parts.Single();
        Assert.IsInstanceOfType<InlineTextPart>(part);
        Assert.AreEqual("Hello, world!", ((InlineTextPart)part).Text);
    }

    [TestMethod]
    public void CreateKeepsATextPartBetweenTwoCodesSeparate()
    {
        InlineContent content = InlineContent.Create([Placeholder("ph1"), new InlineTextPart(" and "), Placeholder("ph2")]);

        Assert.HasCount(3, content.Parts);
        Assert.IsInstanceOfType<InlineTextPart>(content.Parts[1]);
        Assert.AreEqual(" and ", ((InlineTextPart)content.Parts[1]).Text);
    }

    [TestMethod]
    public void CreateKeepsWhitespaceOnlyTextBetweenCodes()
    {
        //Whitespace-only text between codes is still text and must be kept, never dropped (design 5.1).
        InlineContent content = InlineContent.Create([Placeholder("ph1"), new InlineTextPart("   "), Placeholder("ph2")]);

        Assert.HasCount(3, content.Parts);
        Assert.AreEqual("   ", ((InlineTextPart)content.Parts[1]).Text);
    }

    [TestMethod]
    public void CreateOfAnEmptySequenceReturnsEmpty()
    {
        Assert.AreEqual(InlineContent.Empty, InlineContent.Create([]));
        Assert.IsTrue(InlineContent.Create(Array.Empty<InlinePart>()).IsEmpty);
    }

    [TestMethod]
    public void CreateTreatsADefaultImmutableArrayTheSameAsAnEmptySequence()
    {
        //A default(ImmutableArray<InlinePart>) throws if enumerated directly (its backing array is
        //null); Create must special-case IsDefault rather than foreach over it.
        ImmutableArray<InlinePart> uninitialized = default;

        InlineContent content = InlineContent.Create(uninitialized);

        Assert.AreEqual(InlineContent.Empty, content);
    }

    [TestMethod]
    public void CreateThrowsForANullSequence()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => InlineContent.Create(null!));
    }

    [TestMethod]
    public void FromTextOfAnEmptyStringReturnsEmpty()
    {
        Assert.AreEqual(InlineContent.Empty, InlineContent.FromText(string.Empty));
    }

    [TestMethod]
    public void FromTextHoldsItsTextVerbatimWithoutParsingItForMarkup()
    {
        InlineContent content = InlineContent.FromText("<b>not markup</b>");

        InlinePart part = content.Parts.Single();
        Assert.IsInstanceOfType<InlineTextPart>(part);
        Assert.AreEqual("<b>not markup</b>", ((InlineTextPart)part).Text);
    }

    [TestMethod]
    public void FromTextThrowsForANullString()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => InlineContent.FromText(null!));
    }

    [TestMethod]
    public void EmptyHasNoParts()
    {
        Assert.IsTrue(InlineContent.Empty.IsEmpty);
        Assert.HasCount(0, InlineContent.Empty.Parts);
    }

    [TestMethod]
    public void IsEmptyIsFalseForNonEmptyContent()
    {
        Assert.IsFalse(InlineContent.FromText("x").IsEmpty);
    }

    [TestMethod]
    public void HasCodesIsFalseForTextOnlyContent()
    {
        Assert.IsFalse(InlineContent.FromText("plain").HasCodes);
    }

    [TestMethod]
    public void HasCodesIsTrueWhenAPlaceholderIsPresent()
    {
        Assert.IsTrue(InlineContent.Create([Placeholder("ph1")]).HasCodes);
    }

    [TestMethod]
    public void HasCodesIsTrueWhenAStartOrEndCodeIsPresent()
    {
        Assert.IsTrue(InlineContent.Create([StartCode("c1"), EndCode("c1")]).HasCodes);
    }

    [TestMethod]
    public void HasCodesIsFalseWhenOnlyAnnotationsArePresent()
    {
        Assert.IsFalse(InlineContent.Create([AnnotationStart("m1"), new InlineTextPart("x"), AnnotationEnd("m1")]).HasCodes);
    }

    [TestMethod]
    public void TwoContentsBuiltSeparatelyFromTheSameTextAreEqual()
    {
        //Named killer: InlineContent.cs, Equals(InlineContent?) reverted to the compiler-synthesized
        //record equality (dropping the SequenceEqual override). ImmutableArray<T>'s own equality
        //compares the backing array's reference, so two arrays built by two separate Create/FromText
        //calls would then compare unequal even though every element is equal by value, and this
        //assertion would fail.
        InlineContent first = InlineContent.FromText("Home");
        InlineContent second = InlineContent.FromText("Home");

        Assert.AreEqual(first, second);
        Assert.IsTrue(first == second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void ContentsWithDifferentPartsAreNotEqual()
    {
        Assert.AreNotEqual(InlineContent.FromText("Home"), InlineContent.FromText("Away"));
    }

    [TestMethod]
    public void ContentsOfDifferentLengthAreNotEqual()
    {
        InlineContent shorter = InlineContent.Create([new InlineTextPart("a")]);
        InlineContent longer = InlineContent.Create([new InlineTextPart("a"), Placeholder("ph1")]);

        Assert.AreNotEqual(shorter, longer);
    }

    [TestMethod]
    public void EqualsReturnsFalseWhenComparedToNull()
    {
        Assert.IsFalse(InlineContent.FromText("x").Equals(null));
    }

    [TestMethod]
    public void RenderMarkupOfTextOnlyContentIsVerbatimAndUnescaped()
    {
        //Named killer: InlineContent.cs RenderMarkup, `escapeText = HasCodes` changed to `true`
        //unconditionally: a code-free content must render its ampersands and angle brackets verbatim,
        //since nothing downstream will ever tokenize it as HTML.
        InlineContent content = InlineContent.FromText("Tom & Jerry <3");

        Assert.AreEqual("Tom & Jerry <3", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupEscapesTextOnlyWhenTheContentHasCodes()
    {
        InlineContent content = InlineContent.Create([new InlineTextPart("Tom & Jerry <3"), Placeholder("ph1")]);

        Assert.AreEqual("Tom &amp; Jerry &lt;3<br/>", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupPrefersOriginalDataOverASynthesizedTag()
    {
        //OriginalData wins even when the sub-type would otherwise resolve a tag of its own.
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.None, "xlf:b", string.Empty, null, "d1", new OriginalData("[bold]"), true, true, ReorderHint.Yes, null, null);
        InlineContent content = InlineContent.Create([placeholder]);

        Assert.AreEqual("[bold]", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupOfAnEmptyOriginalDataRendersEmpty()
    {
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.None, null, string.Empty, "shown", "d1", new OriginalData(string.Empty), true, true, ReorderHint.Yes, null, null);
        InlineContent content = InlineContent.Create([placeholder]);

        Assert.AreEqual(string.Empty, content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupSynthesizesASelfClosingTagForAPlaceholder()
    {
        InlineContent content = InlineContent.Create([Placeholder("ph1")]);

        Assert.AreEqual("<br/>", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupSynthesizesOpenAndCloseTagsForAStartAndEndCode()
    {
        InlineContent content = InlineContent.Create([StartCode("c1"), new InlineTextPart("bold"), EndCode("c1")]);

        Assert.AreEqual("<b>bold</b>", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupPrefersASynthesizedTagOverDisp()
    {
        //Named killer: InlineContent.cs RenderCodeText, the WellKnownInlineTokens.TryResolve check
        //moved to run after the `disp is not null` check: a code with both a resolvable sub-type and
        //a disp must still render its synthesized tag, not the disp text.
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.Format, "xlf:lb", string.Empty, "a line break", null, null, true, true, ReorderHint.Yes, null, null);
        InlineContent content = InlineContent.Create([placeholder]);

        Assert.AreEqual("<br/>", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupFallsBackToDispWhenNoTagResolves()
    {
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.Other, null, "fallback", "shown to translator", null, null, true, true, ReorderHint.Yes, null, null);
        InlineContent content = InlineContent.Create([placeholder]);

        Assert.AreEqual("shown to translator", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupFallsBackToEscapedEquivWhenNothingElseResolves()
    {
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.Other, null, "A & B", null, null, null, true, true, ReorderHint.Yes, null, null);
        InlineContent content = InlineContent.Create([placeholder]);

        Assert.AreEqual("A &amp; B", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderMarkupOfAnnotationsContributesNothing()
    {
        InlineContent content = InlineContent.Create([AnnotationStart("m1"), new InlineTextPart("hi"), AnnotationEnd("m1"), Placeholder("ph1")]);

        Assert.AreEqual("hi<br/>", content.Render(InlineRendering.Markup));
    }

    [TestMethod]
    public void RenderPlainRendersTextVerbatimAndNeverEscapes()
    {
        InlineContent content = InlineContent.Create([new InlineTextPart("Tom & Jerry <3"), Placeholder("ph1")]);

        Assert.AreEqual("Tom & Jerry <3", content.Render(InlineRendering.Plain));
    }

    [TestMethod]
    public void RenderPlainUsesEquivForEveryCodeKindIgnoringOriginalDataAndDisp()
    {
        var placeholder = new PlaceholderPart("ph1", InlineCodeType.None, null, "[img]", "shown", "d1", new OriginalData("ignored"), true, true, ReorderHint.Yes, null, null);
        InlineContent content = InlineContent.Create([placeholder, StartCode("c1"), EndCode("c1")]);

        Assert.AreEqual("[img][start][end]", content.Render(InlineRendering.Plain));
    }

    [TestMethod]
    public void RenderPlainOfAnnotationsContributesNothing()
    {
        InlineContent content = InlineContent.Create([AnnotationStart("m1"), new InlineTextPart("hi"), AnnotationEnd("m1")]);

        Assert.AreEqual("hi", content.Render(InlineRendering.Plain));
    }

    [TestMethod]
    public void TranslatableTextIsEverythingWhenNoAnnotationOverridesIt()
    {
        InlineContent content = InlineContent.FromText("Cancel");

        Assert.AreEqual("Cancel", content.TranslatableText);
    }

    [TestMethod]
    public void TranslatableTextExcludesTextInsideATranslateNoAnnotation()
    {
        InlineContent content = InlineContent.Create(
        [
            new InlineTextPart("Keep "),
            AnnotationStart("m1", translate: false),
            new InlineTextPart("Acme"),
            AnnotationEnd("m1"),
            new InlineTextPart(" this")
        ]);

        Assert.AreEqual("Keep  this", content.TranslatableText);
    }

    [TestMethod]
    public void TranslatableTextReenablesATranslateYesAnnotationNestedInsideATranslateNoOne()
    {
        //Decision 3: a nested translate="yes" inside translate="no" re-enables its own span.
        InlineContent content = InlineContent.Create(
        [
            AnnotationStart("outer", translate: false),
            new InlineTextPart("brand "),
            AnnotationStart("inner", translate: true),
            new InlineTextPart("Cancel"),
            AnnotationEnd("inner"),
            new InlineTextPart(" brand"),
            AnnotationEnd("outer")
        ]);

        Assert.AreEqual("Cancel", content.TranslatableText);
    }

    [TestMethod]
    public void TranslatableTextTreatsAnAbsentTranslateAttributeAsInheritingTheEnclosingValue()
    {
        InlineContent content = InlineContent.Create(
        [
            AnnotationStart("outer", translate: false),
            AnnotationStart("inner", translate: null),
            new InlineTextPart("hidden"),
            AnnotationEnd("inner"),
            AnnotationEnd("outer")
        ]);

        Assert.AreEqual(string.Empty, content.TranslatableText);
    }

    [TestMethod]
    public void TranslatableTextOfASplitAnnotationExcludesItsSpan()
    {
        InlineContent content = InlineContent.Create(
        [
            new InlineTextPart("Keep "),
            new AnnotationStartPart("m1", "generic", false, null, null, AnnotationForm.Split),
            new InlineTextPart("drop"),
            new AnnotationEndPart("m1", AnnotationForm.Split),
            new InlineTextPart(" keep")
        ]);

        Assert.AreEqual("Keep  keep", content.TranslatableText);
    }

    [TestMethod]
    public void TranslatableTextIgnoresAnUnmatchedAnnotationEnd()
    {
        //Defensive: an AnnotationEndPart with nothing open must not throw from a text-only walk.
        InlineContent content = InlineContent.Create([new InlineTextPart("a"), AnnotationEnd("dangling"), new InlineTextPart("b")]);

        Assert.AreEqual("ab", content.TranslatableText);
    }

    /// <summary>Builds a placeholder whose sub-type resolves to the HTML line-break element.</summary>
    /// <param name="id">The placeholder's id.</param>
    /// <returns>The built placeholder.</returns>
    private static PlaceholderPart Placeholder(string id) =>
        new(id, InlineCodeType.Format, "xlf:lb", string.Empty, null, null, null, true, true, ReorderHint.Yes, null, null);

    /// <summary>Builds a start code whose sub-type resolves to the HTML bold element.</summary>
    /// <param name="id">The code's id.</param>
    /// <returns>The built start code.</returns>
    private static StartCodePart StartCode(string id) =>
        new(id, InlineCodeType.Format, "xlf:b", "[start]", null, null, null, true, true, ReorderHint.Yes, null, null, false, false, TextDirection.Inherited, SpanForm.Paired);

    /// <summary>Builds an end code closing <paramref name="startRef"/>, whose sub-type resolves to the HTML bold element.</summary>
    /// <param name="startRef">The id of the start code this end closes.</param>
    /// <returns>The built end code.</returns>
    private static EndCodePart EndCode(string startRef) =>
        new(startRef, null, InlineCodeType.Format, "xlf:b", "[end]", null, null, null, true, true, false, ReorderHint.Yes, null, null, false, TextDirection.Inherited, SpanForm.Paired);

    /// <summary>Builds an annotation start with the given translate value.</summary>
    /// <param name="id">The annotation's id.</param>
    /// <param name="translate">The annotation's <c>translate</c> value, or null for inherited.</param>
    /// <returns>The built annotation start.</returns>
    private static AnnotationStartPart AnnotationStart(string id, bool? translate = null) =>
        new(id, "generic", translate, null, null, AnnotationForm.Marker);

    /// <summary>Builds an annotation end closing <paramref name="startRef"/>.</summary>
    /// <param name="startRef">The id of the start this end closes.</param>
    /// <returns>The built annotation end.</returns>
    private static AnnotationEndPart AnnotationEnd(string startRef) => new(startRef, AnnotationForm.Marker);
}
