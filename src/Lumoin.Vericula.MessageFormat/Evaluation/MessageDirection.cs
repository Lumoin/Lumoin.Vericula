namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The direction of a resolved value or of the message itself: left-to-right, right-to-left, or
/// unknown. See UTS #35 part 9 (MessageFormat), version 48.2, section "Handling Bidirectional Text"
/// (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> is the value a default-initialized <see cref="MessageDirection"/> holds, and
/// is also the direction a fresh function result carries when nothing overrides it: a fresh
/// <c>:string</c> result stays <see cref="Unknown"/> unless <c>u:dir</c> forces a direction.
/// </remarks>
public enum MessageDirection
{
    /// <summary>
    /// No direction is known or has been forced. The value of a default-initialized
    /// <see cref="MessageDirection"/>, and the direction a fresh function result carries before any
    /// override is applied.
    /// </summary>
    Unknown = 0,

    /// <summary>Left-to-right.</summary>
    Ltr = 1,

    /// <summary>Right-to-left.</summary>
    Rtl = 2
}
