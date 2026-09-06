using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The catalogue of error type strings the suite's <c>expErrors</c> assertions use: the thirteen
/// values the suite's own JSON schema enumerates for <c>expErrors[].type</c>. Each is spelled once as
/// a UTF-8 source literal and carried alongside as an interned string, the same pair-form contract
/// <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/> uses in the core library.
/// </summary>
/// <remarks>
/// The first seven names (<see cref="SyntaxError"/> through <see cref="DuplicateVariant"/>) are the
/// suite's static errors: a syntactic and data-model parse alone detects every one of them, with no
/// variable resolution or function invocation, and this slice's parser reports exactly these seven,
/// under the matching <see cref="WellKnownMessageFormatDiagnostics"/> ids <see cref="ToDiagnosticId"/>
/// maps them to. The remaining six are runtime errors raised while formatting, for a later slice's
/// evaluator to report.
/// </remarks>
public static class WellKnownSuiteErrorTypes
{
    /// <summary>The UTF-8 source literal of <see cref="SyntaxError"/>.</summary>
    public static ReadOnlySpan<byte> SyntaxErrorUtf8 => "syntax-error"u8;

    /// <summary>The type reported for a message that fails to parse under the MessageFormat 2.0 grammar. Static.</summary>
    public static readonly string SyntaxError = Utf8Constants.ToInternedString(SyntaxErrorUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VariantKeyMismatch"/>.</summary>
    public static ReadOnlySpan<byte> VariantKeyMismatchUtf8 => "variant-key-mismatch"u8;

    /// <summary>The type reported for a variant whose key count differs from the matcher's selector count. Static.</summary>
    public static readonly string VariantKeyMismatch = Utf8Constants.ToInternedString(VariantKeyMismatchUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MissingFallbackVariant"/>.</summary>
    public static ReadOnlySpan<byte> MissingFallbackVariantUtf8 => "missing-fallback-variant"u8;

    /// <summary>The type reported when a matcher has no variant whose keys are all catch-all. Static.</summary>
    public static readonly string MissingFallbackVariant = Utf8Constants.ToInternedString(MissingFallbackVariantUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MissingSelectorAnnotation"/>.</summary>
    public static ReadOnlySpan<byte> MissingSelectorAnnotationUtf8 => "missing-selector-annotation"u8;

    /// <summary>The type reported when a selector variable does not reach a function-bearing declaration. Static.</summary>
    public static readonly string MissingSelectorAnnotation = Utf8Constants.ToInternedString(MissingSelectorAnnotationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DuplicateDeclaration"/>.</summary>
    public static ReadOnlySpan<byte> DuplicateDeclarationUtf8 => "duplicate-declaration"u8;

    /// <summary>The type reported when a declaration rebinds a name that is already bound. Static.</summary>
    public static readonly string DuplicateDeclaration = Utf8Constants.ToInternedString(DuplicateDeclarationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DuplicateOptionName"/>.</summary>
    public static ReadOnlySpan<byte> DuplicateOptionNameUtf8 => "duplicate-option-name"u8;

    /// <summary>The type reported when one function's or one markup's options repeat an identifier. Static.</summary>
    public static readonly string DuplicateOptionName = Utf8Constants.ToInternedString(DuplicateOptionNameUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DuplicateVariant"/>.</summary>
    public static ReadOnlySpan<byte> DuplicateVariantUtf8 => "duplicate-variant"u8;

    /// <summary>The type reported when two variants of a matcher have NFC-equal key lists. Static.</summary>
    public static readonly string DuplicateVariant = Utf8Constants.ToInternedString(DuplicateVariantUtf8);

    /// <summary>The UTF-8 source literal of <see cref="UnresolvedVariable"/>.</summary>
    public static ReadOnlySpan<byte> UnresolvedVariableUtf8 => "unresolved-variable"u8;

    /// <summary>The type reported when formatting refers to a variable with no resolvable value. Runtime.</summary>
    public static readonly string UnresolvedVariable = Utf8Constants.ToInternedString(UnresolvedVariableUtf8);

    /// <summary>The UTF-8 source literal of <see cref="UnknownFunction"/>.</summary>
    public static ReadOnlySpan<byte> UnknownFunctionUtf8 => "unknown-function"u8;

    /// <summary>The type reported when an expression names a function the formatter has no implementation for. Runtime.</summary>
    public static readonly string UnknownFunction = Utf8Constants.ToInternedString(UnknownFunctionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadSelector"/>.</summary>
    public static ReadOnlySpan<byte> BadSelectorUtf8 => "bad-selector"u8;

    /// <summary>The type reported when a selector's resolved value cannot be used to select a variant. Runtime.</summary>
    public static readonly string BadSelector = Utf8Constants.ToInternedString(BadSelectorUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadOperand"/>.</summary>
    public static ReadOnlySpan<byte> BadOperandUtf8 => "bad-operand"u8;

    /// <summary>The type reported when an operand's resolved value is unsuitable for its expression. Runtime.</summary>
    public static readonly string BadOperand = Utf8Constants.ToInternedString(BadOperandUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadOption"/>.</summary>
    public static ReadOnlySpan<byte> BadOptionUtf8 => "bad-option"u8;

    /// <summary>The type reported when an option's resolved value is unsuitable for the function it configures. Runtime.</summary>
    public static readonly string BadOption = Utf8Constants.ToInternedString(BadOptionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BadVariantKey"/>.</summary>
    public static ReadOnlySpan<byte> BadVariantKeyUtf8 => "bad-variant-key"u8;

    /// <summary>The type reported when a variant key's literal is unsuitable for the selector it is matched against. Runtime.</summary>
    public static readonly string BadVariantKey = Utf8Constants.ToInternedString(BadVariantKeyUtf8);

    /// <summary>Determines if an error type string is <see cref="SyntaxError"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the syntax-error type; otherwise, <see langword="false"/>.</returns>
    public static bool IsSyntaxError(string type) => string.Equals(type, SyntaxError, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="VariantKeyMismatch"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the variant-key-mismatch type; otherwise, <see langword="false"/>.</returns>
    public static bool IsVariantKeyMismatch(string type) => string.Equals(type, VariantKeyMismatch, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="MissingFallbackVariant"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the missing-fallback-variant type; otherwise, <see langword="false"/>.</returns>
    public static bool IsMissingFallbackVariant(string type) => string.Equals(type, MissingFallbackVariant, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="MissingSelectorAnnotation"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the missing-selector-annotation type; otherwise, <see langword="false"/>.</returns>
    public static bool IsMissingSelectorAnnotation(string type) => string.Equals(type, MissingSelectorAnnotation, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="DuplicateDeclaration"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the duplicate-declaration type; otherwise, <see langword="false"/>.</returns>
    public static bool IsDuplicateDeclaration(string type) => string.Equals(type, DuplicateDeclaration, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="DuplicateOptionName"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the duplicate-option-name type; otherwise, <see langword="false"/>.</returns>
    public static bool IsDuplicateOptionName(string type) => string.Equals(type, DuplicateOptionName, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="DuplicateVariant"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the duplicate-variant type; otherwise, <see langword="false"/>.</returns>
    public static bool IsDuplicateVariant(string type) => string.Equals(type, DuplicateVariant, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="UnresolvedVariable"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the unresolved-variable type; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnresolvedVariable(string type) => string.Equals(type, UnresolvedVariable, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="UnknownFunction"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the unknown-function type; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnknownFunction(string type) => string.Equals(type, UnknownFunction, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="BadSelector"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the bad-selector type; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadSelector(string type) => string.Equals(type, BadSelector, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="BadOperand"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the bad-operand type; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadOperand(string type) => string.Equals(type, BadOperand, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="BadOption"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the bad-option type; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadOption(string type) => string.Equals(type, BadOption, StringComparison.Ordinal);

    /// <summary>Determines if an error type string is <see cref="BadVariantKey"/>.</summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if the type is the bad-variant-key type; otherwise, <see langword="false"/>.</returns>
    public static bool IsBadVariantKey(string type) => string.Equals(type, BadVariantKey, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether an error type string is one of the suite's seven static errors: the ones a
    /// syntactic and data-model parse alone can detect, with no variable resolution or function
    /// invocation. Each of the seven has a matching <see cref="WellKnownMessageFormatDiagnostics"/> id,
    /// found through <see cref="ToDiagnosticId"/>.
    /// </summary>
    /// <param name="type">The error type string to test.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> is one of the seven static error types; otherwise, <see langword="false"/>.</returns>
    public static bool IsStatic(string type) => IsSyntaxError(type) || IsVariantKeyMismatch(type) || IsMissingFallbackVariant(type)
        || IsMissingSelectorAnnotation(type) || IsDuplicateDeclaration(type) || IsDuplicateOptionName(type) || IsDuplicateVariant(type);

    /// <summary>
    /// Maps one of the suite's seven static error types to the <see cref="WellKnownMessageFormatDiagnostics"/>
    /// id this slice's parser reports for it.
    /// </summary>
    /// <param name="type">One of the suite's thirteen <c>expErrors[].type</c> strings.</param>
    /// <returns>The matching <c>VFX2xx</c> id.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="type"/> is one of the suite's six runtime error types (<see cref="UnresolvedVariable"/>,
    /// <see cref="UnknownFunction"/>, <see cref="BadSelector"/>, <see cref="BadOperand"/>, <see cref="BadOption"/>
    /// or <see cref="BadVariantKey"/>), or an unrecognized string: none of these has a <c>VFX2xx</c> id yet.
    /// </exception>
    public static string ToDiagnosticId(string type) => type switch
    {
        _ when IsSyntaxError(type) => WellKnownMessageFormatDiagnostics.SyntaxError,
        _ when IsVariantKeyMismatch(type) => WellKnownMessageFormatDiagnostics.VariantKeyMismatch,
        _ when IsMissingFallbackVariant(type) => WellKnownMessageFormatDiagnostics.MissingFallbackVariant,
        _ when IsMissingSelectorAnnotation(type) => WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation,
        _ when IsDuplicateDeclaration(type) => WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
        _ when IsDuplicateOptionName(type) => WellKnownMessageFormatDiagnostics.DuplicateOptionName,
        _ when IsDuplicateVariant(type) => WellKnownMessageFormatDiagnostics.DuplicateVariant,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "The suite's runtime error types have no VFX2xx id yet.")
    };
}
