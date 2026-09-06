using System.Diagnostics;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The raw text of one XLIFF additional file, captured as a value-equatable pipeline input.
/// </summary>
/// <param name="Path">The additional file's path.</param>
/// <param name="Text">The additional file's full text.</param>
[DebuggerDisplay("XliffSource: {Path}")]
internal sealed record XliffSource(string Path, string Text);
