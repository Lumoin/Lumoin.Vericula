using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Diagnostics;

/// <summary>
/// One finding raised while parsing or evaluating a MessageFormat 2.0 message, identified by a stable
/// diagnostic identifier from <see cref="WellKnownMessageFormatDiagnostics"/> and located either
/// within the source text or, for a finding raised while evaluating a model that was built directly
/// rather than parsed, within <see cref="Source"/>.
/// </summary>
/// <param name="Id">The stable diagnostic identifier, from <see cref="WellKnownMessageFormatDiagnostics"/>.</param>
/// <param name="Message">A human-readable description of the finding.</param>
/// <param name="Offset">
/// The zero-based UTF-16 index into the source text where the problem was found; the source's length
/// when the problem was found at the end of input; -1 when the finding has no source text to point
/// into (see <see cref="Source"/>).
/// </param>
/// <param name="Line">
/// The one-based line <paramref name="Offset"/> falls on, counted in UTF-16 code units from the start
/// of the source, where a line feed, a carriage return, and a carriage return followed by a line feed
/// each end exactly one line; 0 when the finding has no source text to point into (see <see cref="Source"/>).
/// </param>
/// <param name="Position">
/// The one-based position of <paramref name="Offset"/> on <paramref name="Line"/>, counted in UTF-16
/// code units from the start of the line; 0 when the finding has no source text to point into (see
/// <see cref="Source"/>).
/// </param>
[DebuggerDisplay("MessageFormatDiagnostic: {Id} ({Line}:{Position})")]
public sealed record MessageFormatDiagnostic(string Id, string Message, int Offset, int Line, int Position)
{
    /// <summary>
    /// The evaluated expression's source form, for a finding raised against a model that was built
    /// directly rather than parsed from source text: <see langword="null"/> for a parse-time finding,
    /// which points into real source text through <see cref="Offset"/>, <see cref="Line"/> and
    /// <see cref="Position"/> instead. Carried alongside the placeholder <c>(-1, 0, 0)</c> location for
    /// a model-only evaluation finding; this record does not itself enforce that pairing; the evaluator
    /// landing in a later step is what raises diagnostics through this shape. See UTS #35 part 9
    /// (MessageFormat), version 48.2, section "Fallback Resolution" (formatting.md).
    /// </summary>
    public string? Source { get; init; }
}
