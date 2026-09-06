using System.Diagnostics;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// One translation unit as the generator pipeline sees it.
/// </summary>
/// <remarks>
/// This deliberately carries no source position: a <c>&lt;unit&gt;</c> element's line number shifts
/// with any edit above it in the file, even one with no bearing on this unit's own content, and doing
/// so would report every such edit as a change to every unit that follows, defeating the incremental
/// cache. Per-unit diagnostics report <see cref="Microsoft.CodeAnalysis.Location.None"/> instead;
/// <see cref="XliffDocumentModel"/> carries the document-level position document-level failures use.
/// </remarks>
/// <param name="Id">The unit's id.</param>
/// <param name="SourceText">The unit's folded source text.</param>
/// <param name="TargetText">The unit's folded target text, or <see langword="null"/> when the unit has no complete translation.</param>
[DebuggerDisplay("UnitModel: {Id}")]
internal sealed record UnitModel(string Id, string SourceText, string? TargetText);
