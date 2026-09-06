namespace Lumoin.Vericula.Documents;

/// <summary>
/// Where a document was read from, carried as a <see cref="Lumoin.Base.Tag"/> value rather than
/// stored in the document itself: the document's bytes describe translation content, not their
/// own provenance.
/// </summary>
/// <param name="Path">The path the document was read from.</param>
public sealed record SourceLocation(string Path);
