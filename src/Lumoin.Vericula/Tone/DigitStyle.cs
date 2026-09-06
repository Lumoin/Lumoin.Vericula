namespace Lumoin.Vericula.Tone;

/// <summary>
/// How digits should render in translated text.
/// </summary>
public enum DigitStyle
{
    /// <summary>
    /// Half-width (ASCII) digits.
    /// </summary>
    HalfWidth = 0,

    /// <summary>
    /// Full-width digits.
    /// </summary>
    FullWidth = 1,

    /// <summary>
    /// Whatever the locale prefers by default.
    /// </summary>
    LocaleDefault = 2
}
