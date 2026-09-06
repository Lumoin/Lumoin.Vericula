using System.Diagnostics;

namespace Lumoin.Vericula.Content;

/// <summary>
/// The end of a spanning code: XLIFF's <c>ec</c>, or the closing half of a well-formed <c>pc</c>. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ec">XLIFF 2.1 §4.7.2.1 ec</see>
/// and <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#pc">§4.7.2.1 pc</see>.
/// </summary>
/// <param name="StartRef">The identifier of the start this end closes, or null only when <see cref="Isolated"/> is true.</param>
/// <param name="Id">This end's own identifier, carried only when <see cref="Isolated"/> is true (an isolated end has no start to share one with).</param>
/// <param name="Type">The code's reserved kind, or <see cref="InlineCodeType.None"/> when the <c>type</c> attribute is absent.</param>
/// <param name="SubType">The code's full <c>prefix:value</c> sub-type, or null when absent.</param>
/// <param name="Equiv">The code's plain-text stand-in, used by <see cref="InlineRendering.Plain"/> and as the last-resort <see cref="InlineRendering.Markup"/> rendering; the spec default is empty.</param>
/// <param name="Disp">Text meant for display to a human translator, or null when absent.</param>
/// <param name="DataRef">The identifier of the <c>&lt;data&gt;</c> entry this code refers to, or null when it refers to none.</param>
/// <param name="OriginalData">The resolved original data this code's <see cref="DataRef"/> names, or null when the code carries no data.</param>
/// <param name="CanCopy">Whether the code may be copied; the spec default is true.</param>
/// <param name="CanDelete">Whether the code may be deleted; the spec default is true.</param>
/// <param name="CanOverlap">Whether this span may overlap another; the spec default is false when <see cref="Form"/> is <see cref="SpanForm.Paired"/> and true when it is <see cref="SpanForm.Split"/>.</param>
/// <param name="CanReorder">How freely the code may be reordered relative to the other content; the spec default is <see cref="ReorderHint.Yes"/>.</param>
/// <param name="CopyOf">The identifier of the code this one was copied from, or null when it was not copied.</param>
/// <param name="SubFlows">The code's raw, space-separated <c>subFlows</c> attribute value, unparsed, or null when absent.</param>
/// <param name="Isolated">Whether this end has no matching start in the unit.</param>
/// <param name="Direction">The span's text direction; meaningful only when <see cref="Isolated"/> is true, since otherwise the direction belongs to the matching start.</param>
/// <param name="Form">Whether this end came from, or serializes as, a <c>pc</c> element or a separate <c>sc</c>/<c>ec</c> pair.</param>
[DebuggerDisplay("EndCodePart: {StartRef}")]
public sealed record EndCodePart(
    string? StartRef,
    string? Id,
    InlineCodeType Type,
    string? SubType,
    string Equiv,
    string? Disp,
    string? DataRef,
    OriginalData? OriginalData,
    bool CanCopy,
    bool CanDelete,
    bool CanOverlap,
    ReorderHint CanReorder,
    string? CopyOf,
    string? SubFlows,
    bool Isolated,
    TextDirection Direction,
    SpanForm Form) : InlinePart;
