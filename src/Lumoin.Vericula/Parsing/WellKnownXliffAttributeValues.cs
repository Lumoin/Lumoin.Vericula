using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The well-known XLIFF 2.x attribute VALUES the reader and writer exchange, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>:
/// the yes/no flags, the whitespace declaration, the version numbers, the normalization forms and
/// the segment states.
/// </summary>
/// <remarks>
/// These are values, not attribute names; names live in <see cref="WellKnownXliffAttributes"/>.
/// Each value is spelled once as a UTF-8 source literal and carried alongside as an interned string.
/// </remarks>
public static class WellKnownXliffAttributeValues
{
    /// <summary>The UTF-8 source literal of <see cref="Yes"/>.</summary>
    public static ReadOnlySpan<byte> YesUtf8 => "yes"u8;

    /// <summary>The affirmative value of a yes/no attribute.</summary>
    public static readonly string Yes = Utf8Constants.ToInternedString(YesUtf8);

    /// <summary>The UTF-8 source literal of <see cref="No"/>.</summary>
    public static ReadOnlySpan<byte> NoUtf8 => "no"u8;

    /// <summary>The negative value of a yes/no attribute.</summary>
    public static readonly string No = Utf8Constants.ToInternedString(NoUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Preserve"/>.</summary>
    public static ReadOnlySpan<byte> PreserveUtf8 => "preserve"u8;

    /// <summary>The whitespace-preserving value of <c>xml:space</c> per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#xml_space">XLIFF 2.1, xml:space</see>.</summary>
    public static readonly string Preserve = Utf8Constants.ToInternedString(PreserveUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Version20"/>.</summary>
    public static ReadOnlySpan<byte> Version20Utf8 => "2.0"u8;

    /// <summary>The version attribute value of XLIFF 2.0.</summary>
    public static readonly string Version20 = Utf8Constants.ToInternedString(Version20Utf8);

    /// <summary>The UTF-8 source literal of <see cref="Version21"/>.</summary>
    public static ReadOnlySpan<byte> Version21Utf8 => "2.1"u8;

    /// <summary>The version attribute value of XLIFF 2.1 per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#version">XLIFF 2.1, version</see>.</summary>
    public static readonly string Version21 = Utf8Constants.ToInternedString(Version21Utf8);

    /// <summary>The UTF-8 source literal of <see cref="NormalizationNone"/>.</summary>
    public static ReadOnlySpan<byte> NormalizationNoneUtf8 => "none"u8;

    /// <summary>The normalization form applying no normalization per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_normalization">XLIFF 2.1, normalization</see>.</summary>
    public static readonly string NormalizationNone = Utf8Constants.ToInternedString(NormalizationNoneUtf8);

    /// <summary>The UTF-8 source literal of <see cref="NormalizationNfc"/>.</summary>
    public static ReadOnlySpan<byte> NormalizationNfcUtf8 => "nfc"u8;

    /// <summary>The canonical composition normalization form per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_normalization">XLIFF 2.1, normalization</see>.</summary>
    public static readonly string NormalizationNfc = Utf8Constants.ToInternedString(NormalizationNfcUtf8);

    /// <summary>The UTF-8 source literal of <see cref="NormalizationNfd"/>.</summary>
    public static ReadOnlySpan<byte> NormalizationNfdUtf8 => "nfd"u8;

    /// <summary>
    /// The canonical decomposition normalization form per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_normalization">XLIFF 2.1, normalization</see>.
    /// A legal spec value the reader still refuses, since the linter only knows how to apply
    /// <see cref="NormalizationNone"/> or <see cref="NormalizationNfc"/>.
    /// </summary>
    public static readonly string NormalizationNfd = Utf8Constants.ToInternedString(NormalizationNfdUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StateInitial"/>.</summary>
    public static ReadOnlySpan<byte> StateInitialUtf8 => "initial"u8;

    /// <summary>The state of a segment that has not entered translation, the default, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string StateInitial = Utf8Constants.ToInternedString(StateInitialUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StateTranslated"/>.</summary>
    public static ReadOnlySpan<byte> StateTranslatedUtf8 => "translated"u8;

    /// <summary>The state of a segment with an unreviewed translation per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string StateTranslated = Utf8Constants.ToInternedString(StateTranslatedUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StateReviewed"/>.</summary>
    public static ReadOnlySpan<byte> StateReviewedUtf8 => "reviewed"u8;

    /// <summary>The state of a segment whose translation has been reviewed per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string StateReviewed = Utf8Constants.ToInternedString(StateReviewedUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StateFinal"/>.</summary>
    public static ReadOnlySpan<byte> StateFinalUtf8 => "final"u8;

    /// <summary>The state of a segment whose translation is final per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string StateFinal = Utf8Constants.ToInternedString(StateFinalUtf8);

    /// <summary>Determines if a yes/no attribute value is <see cref="Yes"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is yes; otherwise, <see langword="false"/>.</returns>
    public static bool IsYes(string? value) => string.Equals(value, Yes, StringComparison.Ordinal);

    /// <summary>Determines if a yes/no attribute value is <see cref="No"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is no; otherwise, <see langword="false"/>.</returns>
    public static bool IsNo(string? value) => string.Equals(value, No, StringComparison.Ordinal);

    /// <summary>Determines if a version attribute value names XLIFF 2.0.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is <see cref="Version20"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsVersion20(string? value) => string.Equals(value, Version20, StringComparison.Ordinal);

    /// <summary>Determines if a version attribute value names XLIFF 2.1.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is <see cref="Version21"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsVersion21(string? value) => string.Equals(value, Version21, StringComparison.Ordinal);

    /// <summary>Determines if a normalization attribute value is <see cref="NormalizationNone"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is none; otherwise, <see langword="false"/>.</returns>
    public static bool IsNormalizationNone(string? value) => string.Equals(value, NormalizationNone, StringComparison.Ordinal);

    /// <summary>Determines if a normalization attribute value is <see cref="NormalizationNfc"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is nfc; otherwise, <see langword="false"/>.</returns>
    public static bool IsNormalizationNfc(string? value) => string.Equals(value, NormalizationNfc, StringComparison.Ordinal);

    /// <summary>Determines if a normalization attribute value is <see cref="NormalizationNfd"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is nfd; otherwise, <see langword="false"/>.</returns>
    public static bool IsNormalizationNfd(string? value) => string.Equals(value, NormalizationNfd, StringComparison.Ordinal);

    /// <summary>Determines if a normalization attribute value is one the linter applies: absent, <see cref="NormalizationNone"/> or <see cref="NormalizationNfc"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is a normalization form the linter honours; otherwise, <see langword="false"/>.</returns>
    public static bool IsSupportedNormalization(string? value) => value is null
        || IsNormalizationNone(value)
        || IsNormalizationNfc(value);

    /// <summary>Determines if a state attribute value is <see cref="StateInitial"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is initial; otherwise, <see langword="false"/>.</returns>
    public static bool IsStateInitial(string? value) => string.Equals(value, StateInitial, StringComparison.Ordinal);

    /// <summary>Determines if a state attribute value is <see cref="StateTranslated"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is translated; otherwise, <see langword="false"/>.</returns>
    public static bool IsStateTranslated(string? value) => string.Equals(value, StateTranslated, StringComparison.Ordinal);

    /// <summary>Determines if a state attribute value is <see cref="StateReviewed"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is reviewed; otherwise, <see langword="false"/>.</returns>
    public static bool IsStateReviewed(string? value) => string.Equals(value, StateReviewed, StringComparison.Ordinal);

    /// <summary>Determines if a state attribute value is <see cref="StateFinal"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is final; otherwise, <see langword="false"/>.</returns>
    public static bool IsStateFinal(string? value) => string.Equals(value, StateFinal, StringComparison.Ordinal);

    /// <summary>The UTF-8 source literal of <see cref="FirstNo"/>.</summary>
    public static ReadOnlySpan<byte> FirstNoUtf8 => "firstNo"u8;

    /// <summary>The <c>canReorder</c> value forbidding the code from being the first reordered item per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#canReorder">XLIFF 2.1, canReorder</see>.</summary>
    public static readonly string FirstNo = Utf8Constants.ToInternedString(FirstNoUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Fmt"/>.</summary>
    public static ReadOnlySpan<byte> FmtUtf8 => "fmt"u8;

    /// <summary>The <c>type</c> value for data formatting markup per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Fmt = Utf8Constants.ToInternedString(FmtUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ui"/>.</summary>
    public static ReadOnlySpan<byte> UiUtf8 => "ui"u8;

    /// <summary>The <c>type</c> value for user-interface markup per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Ui = Utf8Constants.ToInternedString(UiUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Quote"/>.</summary>
    public static ReadOnlySpan<byte> QuoteUtf8 => "quote"u8;

    /// <summary>The <c>type</c> value for quotation markup per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Quote = Utf8Constants.ToInternedString(QuoteUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Link"/>.</summary>
    public static ReadOnlySpan<byte> LinkUtf8 => "link"u8;

    /// <summary>The <c>type</c> value for a hyperlink per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Link = Utf8Constants.ToInternedString(LinkUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Image"/>.</summary>
    public static ReadOnlySpan<byte> ImageUtf8 => "image"u8;

    /// <summary>The <c>type</c> value for an image per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Image = Utf8Constants.ToInternedString(ImageUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Other"/>.</summary>
    public static ReadOnlySpan<byte> OtherUtf8 => "other"u8;

    /// <summary>The <c>type</c> value for original markup none of the other reserved values name per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Other = Utf8Constants.ToInternedString(OtherUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Generic"/>.</summary>
    public static ReadOnlySpan<byte> GenericUtf8 => "generic"u8;

    /// <summary>The default <c>type</c> value of an annotation per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1, mrk</see>.</summary>
    public static readonly string Generic = Utf8Constants.ToInternedString(GenericUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Term"/>.</summary>
    public static ReadOnlySpan<byte> TermUtf8 => "term"u8;

    /// <summary>The annotation <c>type</c> value marking a terminology entry per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1, mrk</see>.</summary>
    public static readonly string Term = Utf8Constants.ToInternedString(TermUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Comment"/>.</summary>
    public static ReadOnlySpan<byte> CommentUtf8 => "comment"u8;

    /// <summary>The annotation <c>type</c> value marking a reviewer comment per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1, mrk</see>.</summary>
    public static readonly string Comment = Utf8Constants.ToInternedString(CommentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ltr"/>.</summary>
    public static ReadOnlySpan<byte> LtrUtf8 => "ltr"u8;

    /// <summary>The left-to-right value of a <c>dir</c> attribute per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dir">XLIFF 2.1, dir</see>.</summary>
    public static readonly string Ltr = Utf8Constants.ToInternedString(LtrUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Rtl"/>.</summary>
    public static ReadOnlySpan<byte> RtlUtf8 => "rtl"u8;

    /// <summary>The right-to-left value of a <c>dir</c> attribute per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dir">XLIFF 2.1, dir</see>.</summary>
    public static readonly string Rtl = Utf8Constants.ToInternedString(RtlUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Auto"/>.</summary>
    public static ReadOnlySpan<byte> AutoUtf8 => "auto"u8;

    /// <summary>The Unicode-bidirectional-algorithm value of a <c>dir</c> attribute per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dir">XLIFF 2.1, dir</see>; also <c>data</c>'s own default (XLIFF 2.1 §4.3.1.12).</summary>
    public static readonly string Auto = Utf8Constants.ToInternedString(AutoUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeBold"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeBoldUtf8 => "xlf:b"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;b&gt;</c> element, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subType">XLIFF 2.1, subType</see>; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeBold = Utf8Constants.ToInternedString(SubTypeBoldUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeItalic"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeItalicUtf8 => "xlf:i"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;i&gt;</c> element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeItalic = Utf8Constants.ToInternedString(SubTypeItalicUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeUnderline"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeUnderlineUtf8 => "xlf:u"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;u&gt;</c> element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeUnderline = Utf8Constants.ToInternedString(SubTypeUnderlineUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeLineBreak"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeLineBreakUtf8 => "xlf:lb"u8;

    /// <summary>The reserved <c>subType</c> value synthesizing an HTML <c>&lt;br/&gt;</c> element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypeLineBreak = Utf8Constants.ToInternedString(SubTypeLineBreakUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubTypePageBreak"/>.</summary>
    public static ReadOnlySpan<byte> SubTypePageBreakUtf8 => "xlf:pb"u8;

    /// <summary>The reserved <c>subType</c> value for a page break, which has no HTML element; requires <c>type="fmt"</c>.</summary>
    public static readonly string SubTypePageBreak = Utf8Constants.ToInternedString(SubTypePageBreakUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubTypeVariable"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeVariableUtf8 => "xlf:var"u8;

    /// <summary>The reserved <c>subType</c> value for a user-interface variable, which has no HTML element; requires <c>type="ui"</c>.</summary>
    public static readonly string SubTypeVariable = Utf8Constants.ToInternedString(SubTypeVariableUtf8);

    /// <summary>Determines if a <c>canReorder</c> attribute value is <see cref="FirstNo"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is firstNo; otherwise, <see langword="false"/>.</returns>
    public static bool IsFirstNo(string? value) => string.Equals(value, FirstNo, StringComparison.Ordinal);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Fmt"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is fmt; otherwise, <see langword="false"/>.</returns>
    public static bool IsFmt(string? value) => string.Equals(value, Fmt, StringComparison.Ordinal);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Ui"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is ui; otherwise, <see langword="false"/>.</returns>
    public static bool IsUi(string? value) => string.Equals(value, Ui, StringComparison.Ordinal);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Quote"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is quote; otherwise, <see langword="false"/>.</returns>
    public static bool IsQuote(string? value) => string.Equals(value, Quote, StringComparison.Ordinal);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Link"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is link; otherwise, <see langword="false"/>.</returns>
    public static bool IsLink(string? value) => string.Equals(value, Link, StringComparison.Ordinal);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Image"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is image; otherwise, <see langword="false"/>.</returns>
    public static bool IsImage(string? value) => string.Equals(value, Image, StringComparison.Ordinal);

    /// <summary>Determines if a <c>type</c> attribute value is <see cref="Other"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is other; otherwise, <see langword="false"/>.</returns>
    public static bool IsOther(string? value) => string.Equals(value, Other, StringComparison.Ordinal);

    /// <summary>Determines if an annotation's <c>type</c> attribute value is <see cref="Generic"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is generic; otherwise, <see langword="false"/>.</returns>
    public static bool IsGeneric(string? value) => string.Equals(value, Generic, StringComparison.Ordinal);

    /// <summary>Determines if an annotation's <c>type</c> attribute value is <see cref="Term"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is term; otherwise, <see langword="false"/>.</returns>
    public static bool IsTerm(string? value) => string.Equals(value, Term, StringComparison.Ordinal);

    /// <summary>Determines if an annotation's <c>type</c> attribute value is <see cref="Comment"/>.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the value is comment; otherwise, <see langword="false"/>.</returns>
    public static bool IsComment(string? value) => string.Equals(value, Comment, StringComparison.Ordinal);

    /// <summary>Determines if a <c>dir</c> attribute value is <see cref="Ltr"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is ltr; otherwise, <see langword="false"/>.</returns>
    public static bool IsLtr(string? value) => string.Equals(value, Ltr, StringComparison.Ordinal);

    /// <summary>Determines if a <c>dir</c> attribute value is <see cref="Rtl"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is rtl; otherwise, <see langword="false"/>.</returns>
    public static bool IsRtl(string? value) => string.Equals(value, Rtl, StringComparison.Ordinal);

    /// <summary>Determines if a <c>dir</c> attribute value is <see cref="Auto"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is auto; otherwise, <see langword="false"/>.</returns>
    public static bool IsAuto(string? value) => string.Equals(value, Auto, StringComparison.Ordinal);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeBold"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:b; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeBold(string? value) => string.Equals(value, SubTypeBold, StringComparison.Ordinal);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeItalic"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:i; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeItalic(string? value) => string.Equals(value, SubTypeItalic, StringComparison.Ordinal);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeUnderline"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:u; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeUnderline(string? value) => string.Equals(value, SubTypeUnderline, StringComparison.Ordinal);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeLineBreak"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:lb; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeLineBreak(string? value) => string.Equals(value, SubTypeLineBreak, StringComparison.Ordinal);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypePageBreak"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:pb; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypePageBreak(string? value) => string.Equals(value, SubTypePageBreak, StringComparison.Ordinal);

    /// <summary>Determines if a <c>subType</c> attribute value is <see cref="SubTypeVariable"/>.</summary>
    /// <param name="value">The attribute value, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the value is xlf:var; otherwise, <see langword="false"/>.</returns>
    public static bool IsSubTypeVariable(string? value) => string.Equals(value, SubTypeVariable, StringComparison.Ordinal);
}
