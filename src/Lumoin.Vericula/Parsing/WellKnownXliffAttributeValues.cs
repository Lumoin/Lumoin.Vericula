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
}
