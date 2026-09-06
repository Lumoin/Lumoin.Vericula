namespace Lumoin.Vericula.Content;

/// <summary>
/// The values of a <c>dir</c> attribute, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dir">XLIFF 2.1, dir</see>.
/// </summary>
public enum TextDirection
{
    /// <summary>
    /// The attribute is absent; the direction is inherited from the enclosing content. This is the
    /// default on a code's own <c>dir</c>; on <see cref="OriginalData"/>, the spec's default is
    /// <see cref="Auto"/> instead (XLIFF 2.1 §4.3.1.12 dir: <c>data</c> is not inherited).
    /// </summary>
    Inherited = 0,

    /// <summary>Left-to-right text.</summary>
    LeftToRight = 1,

    /// <summary>Right-to-left text.</summary>
    RightToLeft = 2,

    /// <summary>The direction is determined by the Unicode bidirectional algorithm.</summary>
    Auto = 3
}
