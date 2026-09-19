using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The outer <c>type</c> strings a formatted <see cref="MessagePart"/> reports, matching the vendored
/// conformance suite's <c>expParts[].type</c> values. Each is spelled once as a UTF-8 source literal
/// and carried alongside as an interned string, the pair-form contract
/// <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/> uses in the core library. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Formatting" (formatting.md).
/// </summary>
/// <remarks>
/// Nested <see cref="MessageFieldPart.Type"/> field types (for example a number's integer digits)
/// arrive with the number backend in a later step. The suite's schema also allows <c>datetime</c> and
/// <c>test</c> as expression-part types, but the vendored data carries no example of either yet, so
/// they are left out until a case exists to pin. The design names one of these members <c>String</c>;
/// CA1720 (identifiers should not contain type names) forbids that exact identifier, and no
/// suppression exists anywhere in this repository, so it is <see cref="StringType"/> instead, the
/// closest available name, applied consistently (including its <c>Utf8</c> and <c>Is</c> pair-form
/// siblings). The interned string value itself is unaffected: still exactly <c>"string"</c>.
/// </remarks>
public static class WellKnownMessagePartTypes
{
    /// <summary>The UTF-8 source literal of <see cref="Text"/>.</summary>
    public static ReadOnlySpan<byte> TextUtf8 => "text"u8;

    /// <summary>The type of a <see cref="MessageTextPart"/>.</summary>
    public static readonly string Text = Utf8Constants.ToInternedString(TextUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BidiIsolation"/>.</summary>
    public static ReadOnlySpan<byte> BidiIsolationUtf8 => "bidiIsolation"u8;

    /// <summary>The type of a <see cref="MessageBidiPart"/>.</summary>
    public static readonly string BidiIsolation = Utf8Constants.ToInternedString(BidiIsolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Fallback"/>.</summary>
    public static ReadOnlySpan<byte> FallbackUtf8 => "fallback"u8;

    /// <summary>The type of a <see cref="MessageFallbackPart"/>.</summary>
    public static readonly string Fallback = Utf8Constants.ToInternedString(FallbackUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Markup"/>.</summary>
    public static ReadOnlySpan<byte> MarkupUtf8 => "markup"u8;

    /// <summary>The type of a <see cref="MessageMarkupPart"/>.</summary>
    public static readonly string Markup = Utf8Constants.ToInternedString(MarkupUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StringType"/>.</summary>
    public static ReadOnlySpan<byte> StringTypeUtf8 => "string"u8;

    /// <summary>The type of a <see cref="MessageValuePart"/> produced by <see cref="WellKnownMessageFunctions.StringName"/>.</summary>
    public static readonly string StringType = Utf8Constants.ToInternedString(StringTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Number"/>.</summary>
    public static ReadOnlySpan<byte> NumberUtf8 => "number"u8;

    /// <summary>The type of a <see cref="MessageValuePart"/> produced by <see cref="WellKnownMessageFunctions.Number"/> or <see cref="WellKnownMessageFunctions.IntegerName"/>.</summary>
    public static readonly string Number = Utf8Constants.ToInternedString(NumberUtf8);

    /// <summary>Determines if a part type is <see cref="Text"/>.</summary>
    /// <param name="value">The part type to test.</param>
    /// <returns><see langword="true"/> if the type is the text part type; otherwise, <see langword="false"/>.</returns>
    public static bool IsText(string value) => string.Equals(value, Text, StringComparison.Ordinal);

    /// <summary>Determines if a part type is <see cref="BidiIsolation"/>.</summary>
    /// <param name="value">The part type to test.</param>
    /// <returns><see langword="true"/> if the type is the bidi-isolation part type; otherwise, <see langword="false"/>.</returns>
    public static bool IsBidiIsolation(string value) => string.Equals(value, BidiIsolation, StringComparison.Ordinal);

    /// <summary>Determines if a part type is <see cref="Fallback"/>.</summary>
    /// <param name="value">The part type to test.</param>
    /// <returns><see langword="true"/> if the type is the fallback part type; otherwise, <see langword="false"/>.</returns>
    public static bool IsFallback(string value) => string.Equals(value, Fallback, StringComparison.Ordinal);

    /// <summary>Determines if a part type is <see cref="Markup"/>.</summary>
    /// <param name="value">The part type to test.</param>
    /// <returns><see langword="true"/> if the type is the markup part type; otherwise, <see langword="false"/>.</returns>
    public static bool IsMarkup(string value) => string.Equals(value, Markup, StringComparison.Ordinal);

    /// <summary>Determines if a part type is <see cref="StringType"/>.</summary>
    /// <param name="value">The part type to test.</param>
    /// <returns><see langword="true"/> if the type is the string part type; otherwise, <see langword="false"/>.</returns>
    public static bool IsStringType(string value) => string.Equals(value, StringType, StringComparison.Ordinal);

    /// <summary>Determines if a part type is <see cref="Number"/>.</summary>
    /// <param name="value">The part type to test.</param>
    /// <returns><see langword="true"/> if the type is the number part type; otherwise, <see langword="false"/>.</returns>
    public static bool IsNumber(string value) => string.Equals(value, Number, StringComparison.Ordinal);
}
