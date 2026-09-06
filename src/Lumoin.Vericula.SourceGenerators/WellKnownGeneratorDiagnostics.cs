namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The diagnostic ids this source generator reports.
/// </summary>
/// <remarks>
/// VFX3xx is the generator's own range; VFX1xx is reserved for the project's linter.
/// </remarks>
internal static class WellKnownGeneratorDiagnostics
{
    /// <summary>The UTF-8 source literal of <see cref="ParseFailure"/>.</summary>
    public static ReadOnlySpan<byte> ParseFailureUtf8 => "VFX300"u8;

    /// <summary>The id reported when an XLIFF additional file cannot be parsed at all.</summary>
    public static readonly string ParseFailure = Utf8Constants.ToInternedString(ParseFailureUtf8);

    /// <summary>The UTF-8 source literal of <see cref="InvalidUnitId"/>.</summary>
    public static ReadOnlySpan<byte> InvalidUnitIdUtf8 => "VFX301"u8;

    /// <summary>The id reported when a unit id cannot be given its own generated accessor.</summary>
    public static readonly string InvalidUnitId = Utf8Constants.ToInternedString(InvalidUnitIdUtf8);

    /// <summary>The UTF-8 source literal of <see cref="InvalidNamespaceSegment"/>.</summary>
    public static ReadOnlySpan<byte> InvalidNamespaceSegmentUtf8 => "VFX302"u8;

    /// <summary>The id reported when a namespace segment derived from the project needed sanitizing or a fallback.</summary>
    public static readonly string InvalidNamespaceSegment = Utf8Constants.ToInternedString(InvalidNamespaceSegmentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="LanguageTagFileNameMismatch"/>.</summary>
    public static ReadOnlySpan<byte> LanguageTagFileNameMismatchUtf8 => "VFX303"u8;

    /// <summary>The id reported when an additional file's name suggests a different target language than its declared trgLang.</summary>
    public static readonly string LanguageTagFileNameMismatch = Utf8Constants.ToInternedString(LanguageTagFileNameMismatchUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SourceLanguageMismatch"/>.</summary>
    public static ReadOnlySpan<byte> SourceLanguageMismatchUtf8 => "VFX304"u8;

    /// <summary>The id reported when a document's srcLang differs from the srcLang of the first document in path order.</summary>
    public static readonly string SourceLanguageMismatch = Utf8Constants.ToInternedString(SourceLanguageMismatchUtf8);

    /// <summary>Determines if a diagnostic id is <see cref="ParseFailure"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the parse-failure id; otherwise, <see langword="false"/>.</returns>
    public static bool IsParseFailure(string id) => string.Equals(id, ParseFailure, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="InvalidUnitId"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the invalid-unit-id id; otherwise, <see langword="false"/>.</returns>
    public static bool IsInvalidUnitId(string id) => string.Equals(id, InvalidUnitId, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="InvalidNamespaceSegment"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the invalid-namespace-segment id; otherwise, <see langword="false"/>.</returns>
    public static bool IsInvalidNamespaceSegment(string id) => string.Equals(id, InvalidNamespaceSegment, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="LanguageTagFileNameMismatch"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the language-tag-mismatch id; otherwise, <see langword="false"/>.</returns>
    public static bool IsLanguageTagFileNameMismatch(string id) => string.Equals(id, LanguageTagFileNameMismatch, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="SourceLanguageMismatch"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the source-language-mismatch id; otherwise, <see langword="false"/>.</returns>
    public static bool IsSourceLanguageMismatch(string id) => string.Equals(id, SourceLanguageMismatch, StringComparison.Ordinal);
}
