using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Scopes;

namespace Lumoin.Vericula.Units;

/// <summary>
/// The smallest translatable unit: an ordered run of segments with their translation states, plus
/// notes, scopes, metadata and an optional unit-level glossary.
/// </summary>
/// <remarks>
/// <see cref="Source"/> folds every segment's source into one text, in document order, so consumers
/// that work on whole strings, such as the resx cook and the linter, need not know about segmentation.
/// <see cref="Target"/> folds the same way only when the unit's translation is complete: it is null
/// whenever any translatable segment has no target or is in <see cref="SegmentState.NeedsTranslation"/>
/// (a stale target left over from before the source changed is not a translation of the current source).
/// Once every translatable segment carries a current translation, an ignorable segment contributes its
/// own target when it has one and its source otherwise, which is how the whitespace a tool leaves
/// untranslated on an ignorable still survives the fold.
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
    /// <summary>
    /// The unit's source text: every segment's source, translatable and ignorable alike, concatenated
    /// in document order.
    /// </summary>
    public string Source
    {
        get
        {
            var source = new StringBuilder();
            foreach(XliffSegment segment in Segments)
            {
                source.Append(segment.Source);
            }

            return source.ToString();
        }
    }

    /// <summary>
    /// The unit's complete translation, or null when it is not yet complete. Null whenever any
    /// translatable segment has no target or is still <see cref="SegmentState.NeedsTranslation"/>;
    /// otherwise every segment's target folds into one text in document order, where an ignorable
    /// segment contributes its own target when it has one and its source otherwise.
    /// </summary>
    public string? Target
    {
        get
        {
            foreach(XliffSegment segment in Segments)
            {
                if(segment.Kind is SegmentKind.Translatable && (segment.Target is null || segment.State is SegmentState.NeedsTranslation))
                {
                    return null;
                }
            }

            var target = new StringBuilder();
            foreach(XliffSegment segment in Segments)
            {
                target.Append(segment.Kind is SegmentKind.Ignorable ? segment.Target ?? segment.Source : segment.Target);
            }

            return target.ToString();
        }
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
        var segment = new XliffSegment(null, SegmentKind.Translatable, source, target, SegmentState.Initial, null);

        return new XliffUnit(
            id,
            [segment],
            ImmutableArray<string>.Empty,
            ImmutableArray<Scope>.Empty,
            ImmutableDictionary<string, string>.Empty,
            null);
    }
}
