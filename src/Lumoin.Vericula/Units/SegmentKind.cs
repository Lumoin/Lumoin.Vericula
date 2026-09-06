namespace Lumoin.Vericula.Units;

/// <summary>
/// Whether a segment is translatable content or extracted content that is not translated.
/// </summary>
public enum SegmentKind
{
    /// <summary>
    /// Translatable content, carried by an XLIFF <c>segment</c> element.
    /// </summary>
    Translatable = 0,

    /// <summary>
    /// Extracted content that is not translatable, such as whitespace between sentences, carried by an
    /// XLIFF <c>ignorable</c> element.
    /// </summary>
    Ignorable = 1
}
