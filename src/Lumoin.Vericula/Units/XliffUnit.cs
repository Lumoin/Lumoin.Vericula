using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Scopes;

namespace Lumoin.Vericula.Units;

/// <summary>
/// The smallest translatable unit: an ordered run of segments with their translation states, plus
/// notes, scopes, metadata and an optional unit-level glossary.
/// </summary>
/// <remarks>
/// <see cref="RenderSource(InlineRendering)"/> folds every segment's source into one text, in
/// document order, so consumers that work on whole strings, such as the resx cook and the linter,
/// need not know about segmentation. <see cref="RenderTarget(InlineRendering)"/> folds the same way
/// only when the unit's translation is complete: a translatable segment is complete when it is not
/// <see cref="SegmentState.NeedsTranslation"/> and either carries target content or its source has no
/// translatable text at all (nothing to translate, because the source holds only markup or only text
/// inside <c>translate="no"</c> annotations); an ignorable segment is exempt and always counts.
/// Translatable text is found by <see cref="InlineTranslatability.Walk(InlineContent, ImmutableStack{bool})"/>,
/// carrying its stack of nested <c>translate</c> overrides across the unit's segments in document
/// order on the source side, so an <c>sm translate="no"</c> opened in one segment and closed in a
/// later one also exempts every segment in between, not only the segment that opens or closes it.
/// <see cref="Source"/> and <see cref="Target"/> are the <see cref="InlineRendering.Markup"/>
/// shorthands of these two methods.
/// </remarks>
/// <param name="Id">The unit's identifier, unique within its file.</param>
/// <param name="Segments">The unit's segments in document order; at least one.</param>
/// <param name="Notes">Free-text notes for translators.</param>
/// <param name="Scopes">The scopes the unit belongs to.</param>
/// <param name="Metadata">Named metadata attached to the unit.</param>
/// <param name="Glossary">Glossary entries that apply to this unit, or null when it carries none.</param>
[DebuggerDisplay("XliffUnit: {Id}, segments: {Segments.Length}")]
public sealed record XliffUnit(
    string Id,
    ImmutableArray<XliffSegment> Segments,
    ImmutableArray<string> Notes,
    ImmutableArray<Scope> Scopes,
    ImmutableDictionary<string, string> Metadata,
    Glossary? Glossary)
{
    /// <summary>The <see cref="InlineRendering.Markup"/> rendering of the unit's source; see <see cref="RenderSource(InlineRendering)"/>.</summary>
    public string Source => RenderSource(InlineRendering.Markup);

    /// <summary>The <see cref="InlineRendering.Markup"/> rendering of the unit's target, or null when the translation is not complete; see <see cref="RenderTarget(InlineRendering)"/>.</summary>
    public string? Target => RenderTarget(InlineRendering.Markup);

    /// <summary>
    /// Renders the unit's source: every segment's source, translatable and ignorable alike, folded in
    /// document order.
    /// </summary>
    /// <param name="rendering">Which rendering each segment's source is folded from.</param>
    /// <returns>The rendered source.</returns>
    public string RenderSource(InlineRendering rendering)
    {
        var source = new StringBuilder();
        foreach(XliffSegment segment in Segments)
        {
            source.Append(segment.SourceContent.Render(rendering));
        }

        return source.ToString();
    }

    /// <summary>
    /// Renders the unit's complete translation, or null when it is not yet complete. A translatable
    /// segment is complete when it is not <see cref="SegmentState.NeedsTranslation"/> and either
    /// carries target content or its source has no translatable text at all, where "translatable text"
    /// is found by walking every segment's source in document order with one
    /// <see cref="InlineTranslatability"/> stack carried across the whole unit (so a <c>translate="no"</c>
    /// span opened in one segment and closed in a later one also exempts the segments between them); an
    /// ignorable segment is always complete. When every segment is complete, each one's target folds in
    /// when it has one and its source otherwise, in document order, which is how the whitespace a tool
    /// leaves untranslated on an ignorable, and the markup-only text a complete segment has no target
    /// for, both survive the fold.
    /// </summary>
    /// <param name="rendering">Which rendering each segment's target or source is folded from.</param>
    /// <returns>The rendered target, or null when the translation is not complete.</returns>
    public string? RenderTarget(InlineRendering rendering)
    {
        ImmutableStack<bool> translateStack = ImmutableStack<bool>.Empty;
        foreach(XliffSegment segment in Segments)
        {
            string translatableText;
            (translatableText, translateStack) = InlineTranslatability.Walk(segment.SourceContent, translateStack);

            if(segment.Kind is SegmentKind.Translatable && !IsComplete(segment, translatableText))
            {
                return null;
            }
        }

        var target = new StringBuilder();
        foreach(XliffSegment segment in Segments)
        {
            target.Append(segment.TargetContent is null ? segment.SourceContent.Render(rendering) : segment.TargetContent.Render(rendering));
        }

        return target.ToString();
    }

    /// <summary>
    /// Creates a unit holding one translatable segment in the initial state, with no notes, scopes,
    /// metadata or glossary.
    /// </summary>
    /// <param name="id">The unit's identifier.</param>
    /// <param name="source">The source text.</param>
    /// <param name="target">The target text, or null when the unit is untranslated.</param>
    /// <returns>The unit.</returns>
    public static XliffUnit FromText(string id, string source, string? target = null)
    {
        var segment = new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText(source), target is null ? null : InlineContent.FromText(target), SegmentState.Initial, null);

        return new XliffUnit(
            id,
            [segment],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);
    }

    /// <summary>
    /// Whether a translatable segment is complete: not <see cref="SegmentState.NeedsTranslation"/>,
    /// and either it carries target content or its source has no translatable text at all.
    /// </summary>
    /// <param name="segment">The translatable segment to check.</param>
    /// <param name="translatableText">
    /// The segment's source text found translatable by <see cref="InlineTranslatability.Walk(InlineContent, ImmutableStack{bool})"/>,
    /// carried from the unit's earlier segments rather than <see cref="InlineContent.TranslatableText"/>'s
    /// own isolated, empty-stack walk.
    /// </param>
    /// <returns><see langword="true"/> if the segment is complete; otherwise, <see langword="false"/>.</returns>
    private static bool IsComplete(XliffSegment segment, string translatableText)
    {
        return segment.State is not SegmentState.NeedsTranslation
            && (segment.TargetContent is not null || translatableText.Length == 0);
    }
}
