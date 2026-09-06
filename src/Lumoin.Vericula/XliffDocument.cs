using System.Collections.Immutable;
using System.Diagnostics;
using Lumoin.Base;
using Lumoin.Vericula.Documents;

namespace Lumoin.Vericula;

/// <summary>
/// The root of a parsed XLIFF document: the declared specification version and the files it carries.
/// </summary>
[DebuggerDisplay("XliffDocument: {Version}, files: {Files.Length}")]
public sealed record XliffDocument(XliffVersion Version, ImmutableArray<XliffFile> Files)
{
    /// <summary>
    /// Out-of-band metadata about this document's origin, provenance and context — for example the
    /// <see cref="SourceLocation"/> it was read from. Never serialized into the XLIFF bytes.
    /// </summary>
    public Tag Tag { get; init; } = Tag.Empty;
}
