using System.Diagnostics;

namespace Lumoin.Vericula.Content;

/// <summary>
/// The end of a spanning annotation: XLIFF's <c>mrk</c> closing tag, or <c>em</c> when the annotation
/// is split across a <c>sm</c>/<c>em</c> pair. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#em">XLIFF 2.1 §4.7.3.2 em</see>.
/// </summary>
/// <param name="StartRef">The identifier of the start this end closes.</param>
/// <param name="Form">Whether this end came from, or serializes as, a <c>mrk</c> element or a separate <c>sm</c>/<c>em</c> pair.</param>
[DebuggerDisplay("AnnotationEndPart: {StartRef}")]
public sealed record AnnotationEndPart(string StartRef, AnnotationForm Form) : InlinePart;
