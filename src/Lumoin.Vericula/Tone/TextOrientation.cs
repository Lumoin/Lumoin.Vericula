namespace Lumoin.Vericula.Tone;

/// <summary>
/// The writing direction the rendered text should use.
/// </summary>
public enum TextOrientation
{
    /// <summary>
    /// Horizontal text flow.
    /// </summary>
    Horizontal = 0,

    /// <summary>
    /// Vertical text flow.
    /// </summary>
    Vertical = 1,

    /// <summary>
    /// The renderer chooses based on locale conventions.
    /// </summary>
    Auto = 2
}
