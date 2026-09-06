using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Diagnostics;

/// <summary>
/// The catalogue of stable diagnostic ids the <see cref="Parsing.MessageFormatReader"/> reports
/// against a MessageFormat 2.0 message. Each id is spelled once as a UTF-8 source literal and carried
/// alongside as an interned string, the pair-form contract <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/>
/// uses in the core library.
/// </summary>
/// <remarks>
/// VFX200 to VFX299 is the MessageFormat project's own range: VFX1xx belongs to the linter and VFX3xx
/// to the source generator. Ids are never renumbered or reused, and a new condition takes the next
/// free number; the resolution errors and message function errors UTS #35 part 9 also defines
/// (unresolved variable, unknown function, bad selector, bad operand, bad option, bad variant key) are
/// raised by a later slice's evaluator and take the next free numbers then.
/// </remarks>
public static class WellKnownMessageFormatDiagnostics
{
    /// <summary>The UTF-8 source literal of <see cref="SyntaxError"/>.</summary>
    public static ReadOnlySpan<byte> SyntaxErrorUtf8 => "VFX200"u8;

    /// <summary>
    /// The id reported when a message's source text does not conform to the MessageFormat 2.0
    /// grammar. See UTS #35 part 9 (MessageFormat), version 48.2, section "Syntax Errors".
    /// </summary>
    public static readonly string SyntaxError = Utf8Constants.ToInternedString(SyntaxErrorUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VariantKeyMismatch"/>.</summary>
    public static ReadOnlySpan<byte> VariantKeyMismatchUtf8 => "VFX201"u8;

    /// <summary>
    /// The id reported when the number of keys on a variant does not equal the number of selectors.
    /// See UTS #35 part 9 (MessageFormat), version 48.2, section "Variant Key Mismatch".
    /// </summary>
    public static readonly string VariantKeyMismatch = Utf8Constants.ToInternedString(VariantKeyMismatchUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MissingFallbackVariant"/>.</summary>
    public static ReadOnlySpan<byte> MissingFallbackVariantUtf8 => "VFX202"u8;

    /// <summary>
    /// The id reported when a matcher has no variant with only catch-all keys. See UTS #35 part 9
    /// (MessageFormat), version 48.2, section "Missing Fallback Variant".
    /// </summary>
    public static readonly string MissingFallbackVariant = Utf8Constants.ToInternedString(MissingFallbackVariantUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MissingSelectorAnnotation"/>.</summary>
    public static ReadOnlySpan<byte> MissingSelectorAnnotationUtf8 => "VFX203"u8;

    /// <summary>
    /// The id reported when a selector does not directly or indirectly reference a declaration with a
    /// function. See UTS #35 part 9 (MessageFormat), version 48.2, section "Missing Selector Annotation".
    /// </summary>
    public static readonly string MissingSelectorAnnotation = Utf8Constants.ToInternedString(MissingSelectorAnnotationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DuplicateDeclaration"/>.</summary>
    public static ReadOnlySpan<byte> DuplicateDeclarationUtf8 => "VFX204"u8;

    /// <summary>
    /// The id reported when a variable is declared more than once. See UTS #35 part 9
    /// (MessageFormat), version 48.2, section "Duplicate Declaration".
    /// </summary>
    public static readonly string DuplicateDeclaration = Utf8Constants.ToInternedString(DuplicateDeclarationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DuplicateOptionName"/>.</summary>
    public static ReadOnlySpan<byte> DuplicateOptionNameUtf8 => "VFX205"u8;

    /// <summary>
    /// The id reported when the same identifier appears on the left-hand side of more than one option
    /// in the same expression. See UTS #35 part 9 (MessageFormat), version 48.2, section "Duplicate
    /// Option Name".
    /// </summary>
    public static readonly string DuplicateOptionName = Utf8Constants.ToInternedString(DuplicateOptionNameUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DuplicateVariant"/>.</summary>
    public static ReadOnlySpan<byte> DuplicateVariantUtf8 => "VFX206"u8;

    /// <summary>
    /// The id reported when the same list of keys is used for more than one variant. See UTS #35
    /// part 9 (MessageFormat), version 48.2, section "Duplicate Variant".
    /// </summary>
    public static readonly string DuplicateVariant = Utf8Constants.ToInternedString(DuplicateVariantUtf8);

    /// <summary>Determines if a diagnostic id is <see cref="SyntaxError"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the syntax-error id; otherwise, <see langword="false"/>.</returns>
    public static bool IsSyntaxError(string id) => string.Equals(id, SyntaxError, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="VariantKeyMismatch"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the variant-key-mismatch id; otherwise, <see langword="false"/>.</returns>
    public static bool IsVariantKeyMismatch(string id) => string.Equals(id, VariantKeyMismatch, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="MissingFallbackVariant"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the missing-fallback-variant id; otherwise, <see langword="false"/>.</returns>
    public static bool IsMissingFallbackVariant(string id) => string.Equals(id, MissingFallbackVariant, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="MissingSelectorAnnotation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the missing-selector-annotation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsMissingSelectorAnnotation(string id) => string.Equals(id, MissingSelectorAnnotation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="DuplicateDeclaration"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the duplicate-declaration id; otherwise, <see langword="false"/>.</returns>
    public static bool IsDuplicateDeclaration(string id) => string.Equals(id, DuplicateDeclaration, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="DuplicateOptionName"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the duplicate-option-name id; otherwise, <see langword="false"/>.</returns>
    public static bool IsDuplicateOptionName(string id) => string.Equals(id, DuplicateOptionName, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="DuplicateVariant"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the duplicate-variant id; otherwise, <see langword="false"/>.</returns>
    public static bool IsDuplicateVariant(string id) => string.Equals(id, DuplicateVariant, StringComparison.Ordinal);
}
