namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The well-known XLIFF 2.x attribute VALUES this generator reads, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Parsing.WellKnownXliffAttributeValues</c>; the generator assembly cannot
/// reference the library, so it carries its own copy of the values it needs.
/// </remarks>
internal static class WellKnownXliffAttributeValues
{
    /// <summary>The UTF-8 source literal of <see cref="StateInitial"/>.</summary>
    public static ReadOnlySpan<byte> StateInitialUtf8 => "initial"u8;

    /// <summary>The default, not-yet-worked-on segment state per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string StateInitial = Utf8Constants.ToInternedString(StateInitialUtf8);

    /// <summary>Determines if a segment's <c>state</c> value is <see cref="StateInitial"/>, explicit or by the attribute's absence.</summary>
    /// <param name="value">The segment's <c>state</c> attribute value, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> if the state is initial; otherwise, <see langword="false"/>.</returns>
    public static bool IsStateInitial(string? value) => value is null || string.Equals(value, StateInitial, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Yes"/>.</summary>
    public static ReadOnlySpan<byte> YesUtf8 => "yes"u8;

    /// <summary>The affirmative value of a yes/no attribute.</summary>
    public static readonly string Yes = Utf8Constants.ToInternedString(YesUtf8);

    /// <summary>The UTF-8 source literal of <see cref="No"/>.</summary>
    public static ReadOnlySpan<byte> NoUtf8 => "no"u8;

    /// <summary>The negative value of a yes/no attribute.</summary>
    public static readonly string No = Utf8Constants.ToInternedString(NoUtf8);

    /// <summary>Determines if a yes/no attribute value is <see cref="Yes"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is yes; otherwise, <see langword="false"/>.</returns>
    public static bool IsYes(string? value) => string.Equals(value, Yes, StringComparison.Ordinal);

    /// <summary>Determines if a yes/no attribute value is <see cref="No"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is no; otherwise, <see langword="false"/>.</returns>
    public static bool IsNo(string? value) => string.Equals(value, No, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Fmt"/>.</summary>
    public static ReadOnlySpan<byte> FmtUtf8 => "fmt"u8;

    /// <summary>The <c>type</c> value for data formatting markup per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Fmt = Utf8Constants.ToInternedString(FmtUtf8);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Fmt"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is fmt; otherwise, <see langword="false"/>.</returns>
    public static bool IsFmt(string? value) => string.Equals(value, Fmt, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Ui"/>.</summary>
    public static ReadOnlySpan<byte> UiUtf8 => "ui"u8;

    /// <summary>The <c>type</c> value for user-interface markup per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Ui = Utf8Constants.ToInternedString(UiUtf8);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Ui"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is ui; otherwise, <see langword="false"/>.</returns>
    public static bool IsUi(string? value) => string.Equals(value, Ui, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Quote"/>.</summary>
    public static ReadOnlySpan<byte> QuoteUtf8 => "quote"u8;

    /// <summary>The <c>type</c> value for quotation markup per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Quote = Utf8Constants.ToInternedString(QuoteUtf8);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Quote"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is quote; otherwise, <see langword="false"/>.</returns>
    public static bool IsQuote(string? value) => string.Equals(value, Quote, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Link"/>.</summary>
    public static ReadOnlySpan<byte> LinkUtf8 => "link"u8;

    /// <summary>The <c>type</c> value for a hyperlink per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Link = Utf8Constants.ToInternedString(LinkUtf8);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Link"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is link; otherwise, <see langword="false"/>.</returns>
    public static bool IsLink(string? value) => string.Equals(value, Link, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Image"/>.</summary>
    public static ReadOnlySpan<byte> ImageUtf8 => "image"u8;

    /// <summary>The <c>type</c> value for an image per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Image = Utf8Constants.ToInternedString(ImageUtf8);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Image"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is image; otherwise, <see langword="false"/>.</returns>
    public static bool IsImage(string? value) => string.Equals(value, Image, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Other"/>.</summary>
    public static ReadOnlySpan<byte> OtherUtf8 => "other"u8;

    /// <summary>The <c>type</c> value for original markup none of the other reserved values name per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Other = Utf8Constants.ToInternedString(OtherUtf8);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Other"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is other; otherwise, <see langword="false"/>.</returns>
    public static bool IsOther(string? value) => string.Equals(value, Other, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="Comment"/>.</summary>
    public static ReadOnlySpan<byte> CommentUtf8 => "comment"u8;

    /// <summary>The annotation <c>type</c> value marking a reviewer comment per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1, mrk</see>.</summary>
    public static readonly string Comment = Utf8Constants.ToInternedString(CommentUtf8);

    /// <summary>Determines if an annotation's <c>type</c> attribute value is <see cref="Comment"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is comment; otherwise, <see langword="false"/>.</returns>
    public static bool IsComment(string? value) => string.Equals(value, Comment, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeBold"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeBoldUtf8 => "xlf:b"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;b&gt;</c> element, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subType">XLIFF 2.1, subType</see>; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeBold = Utf8Constants.ToInternedString(SubTypeBoldUtf8);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeBold"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:b; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeBold(string? value) => string.Equals(value, SubTypeBold, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeItalic"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeItalicUtf8 => "xlf:i"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;i&gt;</c> element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeItalic = Utf8Constants.ToInternedString(SubTypeItalicUtf8);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeItalic"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:i; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeItalic(string? value) => string.Equals(value, SubTypeItalic, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeUnderline"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeUnderlineUtf8 => "xlf:u"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;u&gt;</c> element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeUnderline = Utf8Constants.ToInternedString(SubTypeUnderlineUtf8);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeUnderline"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:u; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeUnderline(string? value) => string.Equals(value, SubTypeUnderline, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeLineBreak"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeLineBreakUtf8 => "xlf:lb"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;br/&gt;</c> element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeLineBreak = Utf8Constants.ToInternedString(SubTypeLineBreakUtf8);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeLineBreak"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:lb; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeLineBreak(string? value) => string.Equals(value, SubTypeLineBreak, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="SubTypePageBreak"/>.</summary>
    public static ReadOnlySpan<byte> SubTypePageBreakUtf8 => "xlf:pb"u8;

    /// <summary>The reserved <c>subType</c> value for a page break, which has no HTML element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypePageBreak = Utf8Constants.ToInternedString(SubTypePageBreakUtf8);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypePageBreak"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:pb; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypePageBreak(string? value) => string.Equals(value, SubTypePageBreak, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeVariable"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeVariableUtf8 => "xlf:var"u8;

    /// <summary>The reserved <c>subType</c> value for a user-interface variable, which has no HTML element; requires <c>type="ui"</c>.</summary>
    public static readonly string SubTypeVariable = Utf8Constants.ToInternedString(SubTypeVariableUtf8);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeVariable"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:var; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeVariable(string? value) => string.Equals(value, SubTypeVariable, StringComparison.Ordinal);
}
