using System.Diagnostics;

namespace Lumoin.Vericula.Linting;

/// <summary>
/// One lint finding, identified by a stable diagnostic identifier in the VFX range.
/// </summary>
/// <param name="Id">The stable diagnostic identifier, from <see cref="Diagnostics.WellKnownDiagnostics"/>.</param>
/// <param name="Severity">How serious the finding is.</param>
/// <param name="Message">A human-readable description of the finding, naming the segment when <paramref name="SegmentId"/> is not null.</param>
/// <param name="Location">Where the finding points, or null when it applies to the document as a whole.</param>
/// <param name="SegmentId">
/// The id of the segment the finding was raised against, per XLIFF 2.1 §5.8.4.2 validation's
/// requirement that rules be applied "to all &lt;target&gt; elements within the scope" of the
/// enclosing element; null when the finding is not tied to one segment, or the segment has no id.
/// </param>
[DebuggerDisplay("LintDiagnostic: {Id} ({Severity})")]
public sealed record LintDiagnostic(string Id, LintSeverity Severity, string Message, LintLocation? Location, string? SegmentId = null);
