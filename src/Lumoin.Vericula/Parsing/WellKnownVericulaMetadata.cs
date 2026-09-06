using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The well-known names Vericula itself defines inside XLIFF: the Metadata module group categories
/// and value types that carry the parts of the model the standard has no slot for, the sub-state
/// that expresses a Vericula segment state, and the attribute names in Vericula's own namespace.
/// </summary>
/// <remarks>
/// The categories ride on the Metadata module's <c>category</c> attribute and the value types on
/// its <c>type</c> attribute, both free text per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#metadata_module">XLIFF 2.1, Metadata Module</see>;
/// the sub-state follows the <c>prefix:value</c> form of
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#substate">XLIFF 2.1, subState</see>;
/// the attributes sit where XLIFF permits attributes from other namespaces. Each name is spelled
/// once as a UTF-8 source literal and carried alongside as an interned string.
/// </remarks>
public static class WellKnownVericulaMetadata
{
    /// <summary>The UTF-8 source literal of <see cref="ToneCategory"/>.</summary>
    public static ReadOnlySpan<byte> ToneCategoryUtf8 => "vericula:tone"u8;

    /// <summary>The metadata group category carrying a file's tone profile.</summary>
    public static readonly string ToneCategory = Utf8Constants.ToInternedString(ToneCategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ToneMetadataCategory"/>.</summary>
    public static ReadOnlySpan<byte> ToneMetadataCategoryUtf8 => "vericula:toneMetadata"u8;

    /// <summary>The metadata group category carrying a tone profile's free-form metadata, nested inside <see cref="ToneCategory"/>.</summary>
    public static readonly string ToneMetadataCategory = Utf8Constants.ToInternedString(ToneMetadataCategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="GlossaryCategory"/>.</summary>
    public static ReadOnlySpan<byte> GlossaryCategoryUtf8 => "vericula:glossary"u8;

    /// <summary>The metadata group category carrying a file-wide glossary.</summary>
    public static readonly string GlossaryCategory = Utf8Constants.ToInternedString(GlossaryCategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="GlossaryEntryCategory"/>.</summary>
    public static ReadOnlySpan<byte> GlossaryEntryCategoryUtf8 => "vericula:glossaryEntry"u8;

    /// <summary>The metadata group category carrying one file-wide glossary entry, nested inside <see cref="GlossaryCategory"/>.</summary>
    public static readonly string GlossaryEntryCategory = Utf8Constants.ToInternedString(GlossaryEntryCategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ScopesCategory"/>.</summary>
    public static ReadOnlySpan<byte> ScopesCategoryUtf8 => "vericula:scopes"u8;

    /// <summary>The metadata group category carrying a group's or unit's scopes.</summary>
    public static readonly string ScopesCategory = Utf8Constants.ToInternedString(ScopesCategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MetadataCategory"/>.</summary>
    public static ReadOnlySpan<byte> MetadataCategoryUtf8 => "vericula:metadata"u8;

    /// <summary>The metadata group category carrying a group's or unit's named metadata.</summary>
    public static readonly string MetadataCategory = Utf8Constants.ToInternedString(MetadataCategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="NeedsTranslationSubState"/>.</summary>
    public static ReadOnlySpan<byte> NeedsTranslationSubStateUtf8 => "vericula:needsTranslation"u8;

    /// <summary>The sub-state that expresses <see cref="Units.SegmentState.NeedsTranslation"/> on an initial segment.</summary>
    public static readonly string NeedsTranslationSubState = Utf8Constants.ToInternedString(NeedsTranslationSubStateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VersionType"/>.</summary>
    public static ReadOnlySpan<byte> VersionTypeUtf8 => "version"u8;

    /// <summary>The metadata value type of a tone profile's version.</summary>
    public static readonly string VersionType = Utf8Constants.ToInternedString(VersionTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="AuthorityType"/>.</summary>
    public static ReadOnlySpan<byte> AuthorityTypeUtf8 => "authority"u8;

    /// <summary>The metadata value type of a tone profile's authority.</summary>
    public static readonly string AuthorityType = Utf8Constants.ToInternedString(AuthorityTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RegisterType"/>.</summary>
    public static ReadOnlySpan<byte> RegisterTypeUtf8 => "register"u8;

    /// <summary>The metadata value type of a tone profile's register.</summary>
    public static readonly string RegisterType = Utf8Constants.ToInternedString(RegisterTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VoiceType"/>.</summary>
    public static ReadOnlySpan<byte> VoiceTypeUtf8 => "voice"u8;

    /// <summary>The metadata value type of a tone profile's voice.</summary>
    public static readonly string VoiceType = Utf8Constants.ToInternedString(VoiceTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="OrientationType"/>.</summary>
    public static ReadOnlySpan<byte> OrientationTypeUtf8 => "orientation"u8;

    /// <summary>The metadata value type of a tone profile's text orientation.</summary>
    public static readonly string OrientationType = Utf8Constants.ToInternedString(OrientationTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CalendarType"/>.</summary>
    public static ReadOnlySpan<byte> CalendarTypeUtf8 => "calendar"u8;

    /// <summary>The metadata value type of a tone profile's calendar.</summary>
    public static readonly string CalendarType = Utf8Constants.ToInternedString(CalendarTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DateFormatType"/>.</summary>
    public static ReadOnlySpan<byte> DateFormatTypeUtf8 => "dateFormat"u8;

    /// <summary>The metadata value type of a tone profile's date format.</summary>
    public static readonly string DateFormatType = Utf8Constants.ToInternedString(DateFormatTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DigitStyleType"/>.</summary>
    public static ReadOnlySpan<byte> DigitStyleTypeUtf8 => "digitStyle"u8;

    /// <summary>The metadata value type of a tone profile's digit style.</summary>
    public static readonly string DigitStyleType = Utf8Constants.ToInternedString(DigitStyleTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="TermType"/>.</summary>
    public static ReadOnlySpan<byte> TermTypeUtf8 => "term"u8;

    /// <summary>The metadata value type of a file-wide glossary entry's term.</summary>
    public static readonly string TermType = Utf8Constants.ToInternedString(TermTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="TranslationType"/>.</summary>
    public static ReadOnlySpan<byte> TranslationTypeUtf8 => "translation"u8;

    /// <summary>The metadata value type of a file-wide glossary entry's translation.</summary>
    public static readonly string TranslationType = Utf8Constants.ToInternedString(TranslationTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DefinitionType"/>.</summary>
    public static ReadOnlySpan<byte> DefinitionTypeUtf8 => "definition"u8;

    /// <summary>The metadata value type of a file-wide glossary entry's definition.</summary>
    public static readonly string DefinitionType = Utf8Constants.ToInternedString(DefinitionTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StatusType"/>.</summary>
    public static ReadOnlySpan<byte> StatusTypeUtf8 => "status"u8;

    /// <summary>The metadata value type of a file-wide glossary entry's status.</summary>
    public static readonly string StatusType = Utf8Constants.ToInternedString(StatusTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RationaleType"/>.</summary>
    public static ReadOnlySpan<byte> RationaleTypeUtf8 => "rationale"u8;

    /// <summary>The metadata value type of a file-wide glossary entry's rationale.</summary>
    public static readonly string RationaleType = Utf8Constants.ToInternedString(RationaleTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ScopeType"/>.</summary>
    public static ReadOnlySpan<byte> ScopeTypeUtf8 => "scope"u8;

    /// <summary>The metadata value type of one scope of a group, unit or file-wide glossary entry.</summary>
    public static readonly string ScopeType = Utf8Constants.ToInternedString(ScopeTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MaxLengthAttribute"/>.</summary>
    public static ReadOnlySpan<byte> MaxLengthAttributeUtf8 => "maxLength"u8;

    /// <summary>The custom validation rule attribute capping the target length in UTF-16 code units; a custom rule per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#rule">XLIFF 2.1, rule</see>.</summary>
    public static readonly string MaxLengthAttribute = Utf8Constants.ToInternedString(MaxLengthAttributeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RegexAttribute"/>.</summary>
    public static ReadOnlySpan<byte> RegexAttributeUtf8 => "regex"u8;

    /// <summary>The custom validation rule attribute requiring the target to match a regular expression; a custom rule per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#rule">XLIFF 2.1, rule</see>.</summary>
    public static readonly string RegexAttribute = Utf8Constants.ToInternedString(RegexAttributeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StatusAttribute"/>.</summary>
    public static ReadOnlySpan<byte> StatusAttributeUtf8 => "status"u8;

    /// <summary>The attribute on a glossary translation carrying its <see cref="Glossaries.GlossaryEntryStatus"/>.</summary>
    public static readonly string StatusAttribute = Utf8Constants.ToInternedString(StatusAttributeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RationaleAttribute"/>.</summary>
    public static ReadOnlySpan<byte> RationaleAttributeUtf8 => "rationale"u8;

    /// <summary>The attribute on a glossary entry carrying its rationale.</summary>
    public static readonly string RationaleAttribute = Utf8Constants.ToInternedString(RationaleAttributeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ScopesAttribute"/>.</summary>
    public static ReadOnlySpan<byte> ScopesAttributeUtf8 => "scopes"u8;

    /// <summary>The attribute on a glossary entry carrying its space-separated scopes.</summary>
    public static readonly string ScopesAttribute = Utf8Constants.ToInternedString(ScopesAttributeUtf8);

    /// <summary>Determines if a metadata group category is <see cref="ToneCategory"/>.</summary>
    /// <param name="category">The category, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the category names a tone profile; otherwise, <see langword="false"/>.</returns>
    public static bool IsToneCategory(string? category) => string.Equals(category, ToneCategory, StringComparison.Ordinal);

    /// <summary>Determines if a metadata group category is <see cref="ToneMetadataCategory"/>.</summary>
    /// <param name="category">The category, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the category names a tone profile's metadata; otherwise, <see langword="false"/>.</returns>
    public static bool IsToneMetadataCategory(string? category) => string.Equals(category, ToneMetadataCategory, StringComparison.Ordinal);

    /// <summary>Determines if a metadata group category is <see cref="GlossaryCategory"/>.</summary>
    /// <param name="category">The category, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the category names a file-wide glossary; otherwise, <see langword="false"/>.</returns>
    public static bool IsGlossaryCategory(string? category) => string.Equals(category, GlossaryCategory, StringComparison.Ordinal);

    /// <summary>Determines if a metadata group category is <see cref="GlossaryEntryCategory"/>.</summary>
    /// <param name="category">The category, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the category names a file-wide glossary entry; otherwise, <see langword="false"/>.</returns>
    public static bool IsGlossaryEntryCategory(string? category) => string.Equals(category, GlossaryEntryCategory, StringComparison.Ordinal);

    /// <summary>Determines if a metadata group category is <see cref="ScopesCategory"/>.</summary>
    /// <param name="category">The category, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the category names scopes; otherwise, <see langword="false"/>.</returns>
    public static bool IsScopesCategory(string? category) => string.Equals(category, ScopesCategory, StringComparison.Ordinal);

    /// <summary>Determines if a metadata group category is <see cref="MetadataCategory"/>.</summary>
    /// <param name="category">The category, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the category names named metadata; otherwise, <see langword="false"/>.</returns>
    public static bool IsMetadataCategory(string? category) => string.Equals(category, MetadataCategory, StringComparison.Ordinal);

    /// <summary>Determines if a sub-state is <see cref="NeedsTranslationSubState"/>.</summary>
    /// <param name="subState">The sub-state, or null when the attribute is absent.</param>
    /// <returns><see langword="true"/> if the sub-state expresses that the segment needs translation; otherwise, <see langword="false"/>.</returns>
    public static bool IsNeedsTranslationSubState(string? subState) => string.Equals(subState, NeedsTranslationSubState, StringComparison.Ordinal);
}
