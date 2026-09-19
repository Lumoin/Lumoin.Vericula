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
/// free number. VFX200 to VFX206 are found statically, by a syntactic and data-model parse alone,
/// with no variable resolution or function invocation. VFX207 to VFX213 arise during resolution and
/// function calls: a later step's evaluator raises them against a model that may have been built
/// directly rather than parsed, so <see cref="MessageFormatDiagnostic.Source"/> stands in for a
/// source location the model does not carry.
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

    /// <summary>The UTF-8 source literal of <see cref="UnresolvedVariable"/>.</summary>
    public static ReadOnlySpan<byte> UnresolvedVariableUtf8 => "VFX207"u8;

    /// <summary>
    /// The id reported when formatting refers to a variable with no resolvable value. See
    /// UTS #35 part 9 (MessageFormat), version 48.2, section "Resolution Errors" (errors.md).
    /// </summary>
    public static readonly string UnresolvedVariable = Utf8Constants.ToInternedString(UnresolvedVariableUtf8);

    /// <summary>The UTF-8 source literal of <see cref="UnknownFunction"/>.</summary>
    public static ReadOnlySpan<byte> UnknownFunctionUtf8 => "VFX208"u8;

    /// <summary>
    /// The id reported when an expression names a function the registry has no implementation for. See
    /// UTS #35 part 9 (MessageFormat), version 48.2, section "Resolution Errors" (errors.md).
    /// </summary>
    public static readonly string UnknownFunction = Utf8Constants.ToInternedString(UnknownFunctionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadSelector"/>.</summary>
    public static ReadOnlySpan<byte> BadSelectorUtf8 => "VFX209"u8;

    /// <summary>
    /// The id reported when a selector's resolved value cannot be used to select a variant. See
    /// UTS #35 part 9 (MessageFormat), version 48.2, section "Resolution Errors" (errors.md).
    /// </summary>
    public static readonly string BadSelector = Utf8Constants.ToInternedString(BadSelectorUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadOperand"/>.</summary>
    public static ReadOnlySpan<byte> BadOperandUtf8 => "VFX210"u8;

    /// <summary>
    /// The id reported when an operand's resolved value is unsuitable for the function it was passed
    /// to. See UTS #35 part 9 (MessageFormat), version 48.2, section "Message Function Errors"
    /// (errors.md).
    /// </summary>
    public static readonly string BadOperand = Utf8Constants.ToInternedString(BadOperandUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadOption"/>.</summary>
    public static ReadOnlySpan<byte> BadOptionUtf8 => "VFX211"u8;

    /// <summary>
    /// The id reported when an option's resolved value is unsuitable for the function it configures.
    /// See UTS #35 part 9 (MessageFormat), version 48.2, section "Message Function Errors" (errors.md).
    /// </summary>
    public static readonly string BadOption = Utf8Constants.ToInternedString(BadOptionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadVariantKey"/>.</summary>
    public static ReadOnlySpan<byte> BadVariantKeyUtf8 => "VFX212"u8;

    /// <summary>
    /// The id reported when a variant key's literal is unsuitable for the selector it is matched
    /// against. See UTS #35 part 9 (MessageFormat), version 48.2, section "Message Function Errors"
    /// (errors.md).
    /// </summary>
    public static readonly string BadVariantKey = Utf8Constants.ToInternedString(BadVariantKeyUtf8);

    /// <summary>The UTF-8 source literal of <see cref="UnsupportedOperation"/>.</summary>
    public static ReadOnlySpan<byte> UnsupportedOperationUtf8 => "VFX213"u8;

    /// <summary>
    /// The id reported when a function does not support the operation it was asked to perform. See
    /// UTS #35 part 9 (MessageFormat), version 48.2, section "Message Function Errors" (errors.md).
    /// </summary>
    public static readonly string UnsupportedOperation = Utf8Constants.ToInternedString(UnsupportedOperationUtf8);

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

    /// <summary>Determines if a diagnostic id is <see cref="UnresolvedVariable"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the unresolved-variable id; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnresolvedVariable(string id) => string.Equals(id, UnresolvedVariable, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="UnknownFunction"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the unknown-function id; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnknownFunction(string id) => string.Equals(id, UnknownFunction, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="BadSelector"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the bad-selector id; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadSelector(string id) => string.Equals(id, BadSelector, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="BadOperand"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the bad-operand id; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadOperand(string id) => string.Equals(id, BadOperand, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="BadOption"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the bad-option id; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadOption(string id) => string.Equals(id, BadOption, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="BadVariantKey"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the bad-variant-key id; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadVariantKey(string id) => string.Equals(id, BadVariantKey, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="UnsupportedOperation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the unsupported-operation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnsupportedOperation(string id) => string.Equals(id, UnsupportedOperation, StringComparison.Ordinal);
}
