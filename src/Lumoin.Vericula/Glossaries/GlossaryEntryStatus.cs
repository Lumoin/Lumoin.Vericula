namespace Lumoin.Vericula.Glossaries;

/// <summary>
/// Whether a glossary translation is the preferred choice, an allowed alternative, or forbidden.
/// </summary>
public enum GlossaryEntryStatus
{
    /// <summary>
    /// The translation translators should use.
    /// </summary>
    Preferred = 0,

    /// <summary>
    /// An acceptable alternative translation.
    /// </summary>
    Allowed = 1,

    /// <summary>
    /// A translation that must not be used.
    /// </summary>
    Forbidden = 2
}
