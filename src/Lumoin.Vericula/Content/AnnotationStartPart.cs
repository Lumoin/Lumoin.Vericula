using System.Diagnostics;

namespace Lumoin.Vericula.Content;

/// <summary>
/// The start of a spanning annotation: XLIFF's <c>mrk</c>, or <c>sm</c> when the annotation is split
/// across a <c>sm</c>/<c>em</c> pair. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1 §4.7.3 mrk</see>
/// and <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#sm">§4.7.3.2 sm</see>.
/// </summary>
/// <param name="Id">The annotation's identifier, unique among the inline elements on its side of the segment.</param>
/// <param name="Type">
/// The annotation's raw <c>type</c> value: one of the reserved words <c>generic</c>, <c>term</c> or
/// <c>comment</c>, or another tool's own value; the spec default is <c>generic</c>.
/// </param>
/// <param name="Translate">
/// Whether the annotation's content is translatable, or null when the attribute is absent, meaning
/// the value is inherited from whatever encloses it (see <see cref="InlineTranslatability"/>).
/// </param>
/// <param name="Ref">A URI referencing further information about the annotation, or null when absent.</param>
/// <param name="Value">The annotation's own value text (its meaning depends on <see cref="Type"/>), or null when absent.</param>
/// <param name="Form">Whether this start came from, or serializes as, a <c>mrk</c> element or a separate <c>sm</c>/<c>em</c> pair.</param>
[DebuggerDisplay("AnnotationStartPart: {Id}")]
public sealed record AnnotationStartPart(string Id, string Type, bool? Translate, string? Ref, string? Value, AnnotationForm Form) : InlinePart;
