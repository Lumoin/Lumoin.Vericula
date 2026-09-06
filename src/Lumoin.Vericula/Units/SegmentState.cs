namespace Lumoin.Vericula.Units;

/// <summary>
/// The translation lifecycle state of a segment.
/// </summary>
public enum SegmentState
{
    /// <summary>
    /// The segment has not entered translation.
    /// </summary>
    Initial = 0,

    /// <summary>
    /// The segment has a translation that has not been reviewed.
    /// </summary>
    Translated = 1,

    /// <summary>
    /// The translation has been reviewed.
    /// </summary>
    Reviewed = 2,

    /// <summary>
    /// The translation is final.
    /// </summary>
    Final = 3,

    /// <summary>
    /// The segment needs a new translation, for example after a source change.
    /// </summary>
    NeedsTranslation = 4
}
