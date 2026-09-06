using System.Diagnostics;

namespace Lumoin.Vericula.Linting;

/// <summary>
/// Where a lint diagnostic points: the file, the unit within it, and the line number.
/// </summary>
[DebuggerDisplay("LintLocation: {FilePath}:{LineNumber} ({UnitId})")]
public sealed record LintLocation(string FilePath, string? UnitId, int LineNumber);
