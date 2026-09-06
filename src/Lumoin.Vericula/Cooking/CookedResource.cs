using System.Diagnostics;
using Lumoin.Base;

namespace Lumoin.Vericula.Cooking;

/// <summary>
/// One artifact produced by a cooker: a suggested file name and its UTF-8 text content.
/// A cooker turns the source-of-truth XLIFF model into the form a particular platform consumes;
/// the caller decides where the named content is written.
/// </summary>
[DebuggerDisplay("CookedResource: {FileName}")]
public sealed record CookedResource(string FileName, string Content)
{
    /// <summary>
    /// Out-of-band metadata about this resource's origin and provenance — for example the
    /// <see cref="Lumoin.Vericula.Documents.SourceLocation"/> of the document it was cooked from.
    /// Never serialized into the resx bytes.
    /// </summary>
    public Tag Tag { get; init; } = Tag.Empty;
}
