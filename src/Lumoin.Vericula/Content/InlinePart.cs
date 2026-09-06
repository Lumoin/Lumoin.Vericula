namespace Lumoin.Vericula.Content;

/// <summary>
/// The base of the flat inline content sequence that makes up an <see cref="InlineContent"/>: a run
/// of text, a code, or an annotation marker. Concrete parts are sealed records:
/// <see cref="InlineTextPart"/>, <see cref="PlaceholderPart"/>, <see cref="StartCodePart"/>,
/// <see cref="EndCodePart"/>, <see cref="AnnotationStartPart"/> and <see cref="AnnotationEndPart"/>.
/// </summary>
/// <remarks>
/// The sequence is flat, not a tree, because XLIFF <c>sc</c>/<c>ec</c> pairs may overlap and cross
/// segment boundaries, and MessageFormat markup is flat too; a tree is derived later only where a
/// renderer needs one. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#inlineelements">XLIFF 2.1 §4.7 Inline Elements</see>.
/// </remarks>
public abstract record InlinePart;
