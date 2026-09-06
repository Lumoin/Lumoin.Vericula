using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Diagnostics;

/// <summary>
/// One finding raised while parsing a MessageFormat 2.0 message, identified by a stable diagnostic
/// identifier from <see cref="WellKnownMessageFormatDiagnostics"/> and located within the source text.
/// </summary>
/// <param name="Id">The stable diagnostic identifier, from <see cref="WellKnownMessageFormatDiagnostics"/>.</param>
/// <param name="Message">A human-readable description of the finding.</param>
/// <param name="Offset">
/// The zero-based UTF-16 index into the source text where the problem was found; the source's length
/// when the problem was found at the end of input.
/// </param>
/// <param name="Line">
/// The one-based line <paramref name="Offset"/> falls on, counted in UTF-16 code units from the start
/// of the source, where a line feed, a carriage return, and a carriage return followed by a line feed
/// each end exactly one line.
/// </param>
/// <param name="Position">
/// The one-based position of <paramref name="Offset"/> on <paramref name="Line"/>, counted in UTF-16
/// code units from the start of the line.
/// </param>
[DebuggerDisplay("MessageFormatDiagnostic: {Id} ({Line}:{Position})")]
public sealed record MessageFormatDiagnostic(string Id, string Message, int Offset, int Line, int Position);
