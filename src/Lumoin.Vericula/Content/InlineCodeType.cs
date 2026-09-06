namespace Lumoin.Vericula.Content;

/// <summary>
/// The reserved values of a code's <c>type</c> attribute, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>:
/// <c>fmt</c>, <c>ui</c>, <c>image</c>, <c>quote</c>, <c>link</c> and <c>other</c> on <c>ph</c>,
/// <c>pc</c>, <c>sc</c> and <c>ec</c>.
/// </summary>
public enum InlineCodeType
{
    /// <summary>The <c>type</c> attribute is absent.</summary>
    None = 0,

    /// <summary>Data formatting markup, XLIFF's <c>fmt</c>.</summary>
    Format = 1,

    /// <summary>User-interface markup, XLIFF's <c>ui</c>.</summary>
    UserInterface = 2,

    /// <summary>Quotation markup, XLIFF's <c>quote</c>.</summary>
    Quote = 3,

    /// <summary>A hyperlink, XLIFF's <c>link</c>.</summary>
    Link = 4,

    /// <summary>An image, XLIFF's <c>image</c>.</summary>
    Image = 5,

    /// <summary>Any other kind of original markup, XLIFF's <c>other</c>.</summary>
    Other = 6
}
