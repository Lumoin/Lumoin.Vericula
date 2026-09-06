using System.Diagnostics;

namespace Lumoin.Vericula.Units;

/// <summary>
/// One aligned source and target run within a unit, with its translation lifecycle state.
/// </summary>
/// <remarks>
/// A unit is an ordered list of segments. Translatable segments carry the text a translator works on;
/// ignorable segments carry extracted content that sits between them, such as inter-sentence
/// whitespace, and are never translated. <see cref="State"/> is meaningful for translatable segments
/// only; an ignorable segment always reports <see cref="SegmentState.Initial"/>.
/// </remarks>
/// <param name="Id">The segment's identifier, unique within its file, or null when it has none.</param>
/// <param name="Kind">Whether the segment is translatable or ignorable.</param>
/// <param name="Source">The source text of the segment.</param>
/// <param name="Target">The target text of the segment, or null when it has not been translated.</param>
/// <param name="State">The translation lifecycle state of the segment.</param>
/// <param name="SubState">
/// A tool-specific refinement of <see cref="State"/> in the XLIFF form <c>prefix:value</c>, or null.
/// <see cref="SegmentState.NeedsTranslation"/> is itself expressed through this channel when serialized,
/// so a segment in that state carries no separate sub-state.
/// </param>
[DebuggerDisplay("XliffSegment: {Kind}, {State}")]
public sealed record XliffSegment(
    string? Id,
    SegmentKind Kind,
    string Source,
    string? Target,
    SegmentState State,
    string? SubState);
