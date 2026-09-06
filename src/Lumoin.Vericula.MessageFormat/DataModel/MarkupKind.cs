namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The three shapes of markup a <see cref="MarkupPart"/> can take. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Markup Model" (data model) and "Markup" (syntax).
/// </summary>
/// <remarks>
/// <see cref="None"/> exists only to give a default-initialized <see cref="MarkupKind"/> a value; the
/// parser never produces it, so the three source shapes carry values 1 to 3, not 0.
/// </remarks>
public enum MarkupKind
{
    /// <summary>
    /// The value of a default-initialized <see cref="MarkupKind"/>. The parser never produces this
    /// value; a consumer that encounters it holds a <see cref="MarkupPart"/> that was not built from
    /// parsed source and should treat it as invalid.
    /// </summary>
    None = 0,

    /// <summary>An opening markup element, written <c>{#name}</c>.</summary>
    Open = 1,

    /// <summary>A self-closing markup element, written <c>{#name/}</c>.</summary>
    Standalone = 2,

    /// <summary>A closing markup element, written <c>{/name}</c>.</summary>
    Close = 3
}
