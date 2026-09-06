namespace Lumoin.Vericula.Content;

/// <summary>
/// The values of a code's <c>canReorder</c> attribute, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#canReorder">XLIFF 2.1, canReorder</see>.
/// </summary>
public enum ReorderHint
{
    /// <summary>The code may be reordered relative to the other content, the default.</summary>
    Yes = 0,

    /// <summary>
    /// The code may not be the first reordered item, but reordering among the remaining content is
    /// otherwise unconstrained.
    /// </summary>
    FirstNo = 1,

    /// <summary>The code may not be reordered at all.</summary>
    No = 2
}
