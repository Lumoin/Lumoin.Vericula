namespace Lumoin.Vericula.Tone;

/// <summary>
/// The speech register a translation should use, including the Japanese politeness registers.
/// </summary>
public enum ToneRegister
{
    /// <summary>
    /// Casual, familiar speech.
    /// </summary>
    Casual = 0,

    /// <summary>
    /// Neutral register without marked politeness.
    /// </summary>
    Neutral = 1,

    /// <summary>
    /// Formal register.
    /// </summary>
    Formal = 2,

    /// <summary>
    /// Japanese polite speech (teineigo).
    /// </summary>
    Teineigo = 3,

    /// <summary>
    /// Japanese respectful speech (sonkeigo).
    /// </summary>
    Sonkeigo = 4,

    /// <summary>
    /// Japanese humble speech (kenjougo).
    /// </summary>
    Kenjogo = 5
}
