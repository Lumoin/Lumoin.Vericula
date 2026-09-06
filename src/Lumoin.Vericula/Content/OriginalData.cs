using System.Diagnostics;

namespace Lumoin.Vericula.Content;

/// <summary>
/// A resolved <c>&lt;data&gt;</c> entry, carried by the code parts that reference it so a segment
/// renders standalone without a separate lookup. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#data">XLIFF 2.1 §4.2.2.11 data</see>.
/// </summary>
/// <param name="Text">The data's text, decoded (<c>cp</c> children merged into it as characters).</param>
/// <param name="Direction">
/// The data's text direction. Defaults to <see cref="TextDirection.Auto"/>: XLIFF 2.1 §4.3.1.12
/// dir gives <c>data</c> its own default of <c>auto</c> rather than inheriting one, unlike every
/// other element the attribute applies to.
/// </param>
[DebuggerDisplay("OriginalData: {Text}")]
public sealed record OriginalData(string Text, TextDirection Direction = TextDirection.Auto);
