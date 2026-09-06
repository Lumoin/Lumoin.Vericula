namespace Lumoin.Vericula.Linting;

/// <summary>
/// The severity of a lint diagnostic.
/// </summary>
public enum LintSeverity
{
    /// <summary>
    /// Informational; no action required.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Suspicious but not necessarily wrong.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A violation that must be fixed.
    /// </summary>
    Error = 2
}
