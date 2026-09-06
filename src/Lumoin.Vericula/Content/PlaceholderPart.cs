using System.Diagnostics;

namespace Lumoin.Vericula.Content;

/// <summary>
/// A standalone code, XLIFF's <c>ph</c>: a placeholder that stands for original content the target
/// carries as-is, such as an image or a variable, with no wrapped content of its own. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ph">XLIFF 2.1 §4.7.2.1 ph</see>.
/// </summary>
/// <param name="Id">The code's identifier, unique among the inline elements on its side of the segment.</param>
/// <param name="Type">The code's reserved kind, or <see cref="InlineCodeType.None"/> when the <c>type</c> attribute is absent.</param>
/// <param name="SubType">The code's full <c>prefix:value</c> sub-type, or null when absent.</param>
/// <param name="Equiv">The code's plain-text stand-in, used by <see cref="InlineRendering.Plain"/> and as the last-resort <see cref="InlineRendering.Markup"/> rendering; the spec default is empty.</param>
/// <param name="Disp">Text meant for display to a human translator, or null when absent.</param>
/// <param name="DataRef">The identifier of the <c>&lt;data&gt;</c> entry this code refers to, or null when it refers to none.</param>
/// <param name="OriginalData">The resolved original data this code's <see cref="DataRef"/> names, or null when the code carries no data.</param>
/// <param name="CanCopy">Whether the code may be copied; the spec default is true.</param>
/// <param name="CanDelete">Whether the code may be deleted; the spec default is true.</param>
/// <param name="CanReorder">How freely the code may be reordered relative to the other content; the spec default is <see cref="ReorderHint.Yes"/>.</param>
/// <param name="CopyOf">The identifier of the code this one was copied from, or null when it was not copied.</param>
/// <param name="SubFlows">The code's raw, space-separated <c>subFlows</c> attribute value, unparsed, or null when absent.</param>
[DebuggerDisplay("PlaceholderPart: {Id}")]
public sealed record PlaceholderPart(
    string Id,
    InlineCodeType Type,
    string? SubType,
    string Equiv,
    string? Disp,
    string? DataRef,
    OriginalData? OriginalData,
    bool CanCopy,
    bool CanDelete,
    ReorderHint CanReorder,
    string? CopyOf,
    string? SubFlows) : InlinePart;
