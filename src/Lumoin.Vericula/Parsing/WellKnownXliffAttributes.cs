using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The well-known XLIFF 2.x attribute NAMES the reader and writer exchange, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>:
/// the core attributes and the Metadata and Validation module attributes.
/// </summary>
/// <remarks>
/// These are attribute names, not their values; values live in <see cref="WellKnownXliffAttributeValues"/>.
/// Each name is spelled once as a UTF-8 source literal and carried alongside as an interned string.
/// </remarks>
public static class WellKnownXliffAttributes
{
    /// <summary>The UTF-8 source literal of <see cref="Version"/>.</summary>
    public static ReadOnlySpan<byte> VersionUtf8 => "version"u8;

    /// <summary>The XLIFF version on the root per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#version">XLIFF 2.1, version</see>.</summary>
    public static readonly string Version = Utf8Constants.ToInternedString(VersionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SourceLanguage"/>.</summary>
    public static ReadOnlySpan<byte> SourceLanguageUtf8 => "srcLang"u8;

    /// <summary>The source language on the root per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#srcLang">XLIFF 2.1, srcLang</see>.</summary>
    public static readonly string SourceLanguage = Utf8Constants.ToInternedString(SourceLanguageUtf8);

    /// <summary>The UTF-8 source literal of <see cref="TargetLanguage"/>.</summary>
    public static ReadOnlySpan<byte> TargetLanguageUtf8 => "trgLang"u8;

    /// <summary>The target language on the root per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#trgLang">XLIFF 2.1, trgLang</see>; required when any target is present.</summary>
    public static readonly string TargetLanguage = Utf8Constants.ToInternedString(TargetLanguageUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Id"/>.</summary>
    public static ReadOnlySpan<byte> IdUtf8 => "id"u8;

    /// <summary>The identifier of a file, group, unit or segment, an XML name token, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#id">XLIFF 2.1, id</see>.</summary>
    public static readonly string Id = Utf8Constants.ToInternedString(IdUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Space"/>.</summary>
    public static ReadOnlySpan<byte> SpaceUtf8 => "space"u8;

    /// <summary>The whitespace handling declaration in the XML namespace per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#xml_space">XLIFF 2.1, xml:space</see>.</summary>
    public static readonly string Space = Utf8Constants.ToInternedString(SpaceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="State"/>.</summary>
    public static ReadOnlySpan<byte> StateUtf8 => "state"u8;

    /// <summary>A segment's translation state per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string State = Utf8Constants.ToInternedString(StateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubState"/>.</summary>
    public static ReadOnlySpan<byte> SubStateUtf8 => "subState"u8;

    /// <summary>A segment's tool-specific state refinement, <c>prefix:value</c>, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#substate">XLIFF 2.1, subState</see>.</summary>
    public static readonly string SubState = Utf8Constants.ToInternedString(SubStateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Category"/>.</summary>
    public static ReadOnlySpan<byte> CategoryUtf8 => "category"u8;

    /// <summary>A metadata group's category per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#meta_category">XLIFF 2.1, category</see>.</summary>
    public static readonly string Category = Utf8Constants.ToInternedString(CategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Type"/>.</summary>
    public static ReadOnlySpan<byte> TypeUtf8 => "type"u8;

    /// <summary>A metadata value's type per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#meta_type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Type = Utf8Constants.ToInternedString(TypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="IsPresent"/>.</summary>
    public static ReadOnlySpan<byte> IsPresentUtf8 => "isPresent"u8;

    /// <summary>The rule attribute requiring its text in the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_isPresent">XLIFF 2.1, isPresent</see>.</summary>
    public static readonly string IsPresent = Utf8Constants.ToInternedString(IsPresentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="IsNotPresent"/>.</summary>
    public static ReadOnlySpan<byte> IsNotPresentUtf8 => "isNotPresent"u8;

    /// <summary>The rule attribute forbidding its text in the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_isNotPresent">XLIFF 2.1, isNotPresent</see>.</summary>
    public static readonly string IsNotPresent = Utf8Constants.ToInternedString(IsNotPresentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartsWith"/>.</summary>
    public static ReadOnlySpan<byte> StartsWithUtf8 => "startsWith"u8;

    /// <summary>The rule attribute pinning the start of the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_startsWith">XLIFF 2.1, startsWith</see>.</summary>
    public static readonly string StartsWith = Utf8Constants.ToInternedString(StartsWithUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndsWith"/>.</summary>
    public static ReadOnlySpan<byte> EndsWithUtf8 => "endsWith"u8;

    /// <summary>The rule attribute pinning the end of the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_endsWith">XLIFF 2.1, endsWith</see>.</summary>
    public static readonly string EndsWith = Utf8Constants.ToInternedString(EndsWithUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Occurs"/>.</summary>
    public static ReadOnlySpan<byte> OccursUtf8 => "occurs"u8;

    /// <summary>The rule attribute constraining occurrences per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_occurs">XLIFF 2.1, occurs</see>; the reader refuses it.</summary>
    public static readonly string Occurs = Utf8Constants.ToInternedString(OccursUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ExistsInSource"/>.</summary>
    public static ReadOnlySpan<byte> ExistsInSourceUtf8 => "existsInSource"u8;

    /// <summary>The rule attribute conditioning a rule on the source per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_existsInSource">XLIFF 2.1, existsInSource</see>; the reader refuses it when set.</summary>
    public static readonly string ExistsInSource = Utf8Constants.ToInternedString(ExistsInSourceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CaseSensitive"/>.</summary>
    public static ReadOnlySpan<byte> CaseSensitiveUtf8 => "caseSensitive"u8;

    /// <summary>The rule attribute selecting case sensitivity per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_caseSensitive">XLIFF 2.1, caseSensitive</see>; the reader refuses the insensitive setting.</summary>
    public static readonly string CaseSensitive = Utf8Constants.ToInternedString(CaseSensitiveUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Normalization"/>.</summary>
    public static ReadOnlySpan<byte> NormalizationUtf8 => "normalization"u8;

    /// <summary>The rule attribute selecting Unicode normalization per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_normalization">XLIFF 2.1, normalization</see>.</summary>
    public static readonly string Normalization = Utf8Constants.ToInternedString(NormalizationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Disabled"/>.</summary>
    public static ReadOnlySpan<byte> DisabledUtf8 => "disabled"u8;

    /// <summary>The rule attribute switching a rule off per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_disabled">XLIFF 2.1, disabled</see>.</summary>
    public static readonly string Disabled = Utf8Constants.ToInternedString(DisabledUtf8);
}
