using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The names of the functions a default <see cref="MessageFunctionRegistry"/> resolves, spelled
/// without the leading <c>:</c> sigil, the same convention <see cref="DataModel.FunctionRef.Name"/>
/// uses. Each is spelled once as a UTF-8 source literal and carried alongside as an interned string,
/// the pair-form contract <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/> uses in the
/// core library. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions"
/// (functions/README.md).
/// </summary>
/// <remarks>
/// <see cref="StringName"/>, <see cref="Number"/>, <see cref="IntegerName"/>, <see cref="Offset"/>,
/// <see cref="Currency"/> and <see cref="Percent"/> are Stable. <see cref="Unit"/>,
/// <see cref="Datetime"/>, <see cref="Date"/> and <see cref="Time"/> are Draft: they have no handler yet; the step
/// that adds the default registry validates their call and reports
/// <see cref="MessageFunctionErrorKind.UnsupportedOperation"/> until they get real handlers.
/// The design names two of these members <c>String</c> and <c>Integer</c>; CA1720 (identifiers should
/// not contain type names) forbids both exact identifiers, and no suppression exists anywhere in this
/// repository, so they are <see cref="StringName"/> and <see cref="IntegerName"/> instead, the closest
/// available names, applied consistently (including their <c>Utf8</c> and <c>Is</c> pair-form
/// siblings) everywhere the design's bare "String" or "Integer" would otherwise have been an identifier.
/// The interned string values themselves are unaffected: still exactly <c>"string"</c> and <c>"integer"</c>.
/// </remarks>
public static class WellKnownMessageFunctions
{
    /// <summary>The UTF-8 source literal of <see cref="StringName"/>.</summary>
    public static ReadOnlySpan<byte> StringNameUtf8 => "string"u8;

    /// <summary>The name of the Stable string-selection and pass-through function. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions" (functions/README.md).</summary>
    public static readonly string StringName = Utf8Constants.ToInternedString(StringNameUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Number"/>.</summary>
    public static ReadOnlySpan<byte> NumberUtf8 => "number"u8;

    /// <summary>The name of the Stable number-formatting and plural-selection function. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions" (functions/README.md).</summary>
    public static readonly string Number = Utf8Constants.ToInternedString(NumberUtf8);

    /// <summary>The UTF-8 source literal of <see cref="IntegerName"/>.</summary>
    public static ReadOnlySpan<byte> IntegerNameUtf8 => "integer"u8;

    /// <summary>The name of the Stable integer-formatting and ordinal-selection function. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions" (functions/README.md).</summary>
    public static readonly string IntegerName = Utf8Constants.ToInternedString(IntegerNameUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Offset"/>.</summary>
    public static ReadOnlySpan<byte> OffsetUtf8 => "offset"u8;

    /// <summary>The name of the Stable function that adjusts a numeric operand's resolved value by a fixed amount. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions" (functions/README.md).</summary>
    public static readonly string Offset = Utf8Constants.ToInternedString(OffsetUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Currency"/>.</summary>
    public static ReadOnlySpan<byte> CurrencyUtf8 => "currency"u8;

    /// <summary>The name of the Stable currency-amount-formatting function. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions" (functions/README.md).</summary>
    public static readonly string Currency = Utf8Constants.ToInternedString(CurrencyUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Percent"/>.</summary>
    public static ReadOnlySpan<byte> PercentUtf8 => "percent"u8;

    /// <summary>The name of the Stable percentage-formatting function. See UTS #35 part 9 (MessageFormat), version 48.2, section "Default Functions" (functions/README.md).</summary>
    public static readonly string Percent = Utf8Constants.ToInternedString(PercentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Unit"/>.</summary>
    public static ReadOnlySpan<byte> UnitUtf8 => "unit"u8;

    /// <summary>The name of the Draft unit-formatting function. See UTS #35 part 9 (MessageFormat), version 48.2, section ":unit" (functions/number.md).</summary>
    public static readonly string Unit = Utf8Constants.ToInternedString(UnitUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Datetime"/>.</summary>
    public static ReadOnlySpan<byte> DatetimeUtf8 => "datetime"u8;

    /// <summary>The name of the Draft combined date-and-time-formatting function. See UTS #35 part 9 (MessageFormat), version 48.2 (functions/datetime.md).</summary>
    public static readonly string Datetime = Utf8Constants.ToInternedString(DatetimeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Date"/>.</summary>
    public static ReadOnlySpan<byte> DateUtf8 => "date"u8;

    /// <summary>The name of the Draft date-only-formatting function. See UTS #35 part 9 (MessageFormat), version 48.2 (functions/datetime.md).</summary>
    public static readonly string Date = Utf8Constants.ToInternedString(DateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Time"/>.</summary>
    public static ReadOnlySpan<byte> TimeUtf8 => "time"u8;

    /// <summary>The name of the Draft time-only-formatting function. See UTS #35 part 9 (MessageFormat), version 48.2 (functions/datetime.md).</summary>
    public static readonly string Time = Utf8Constants.ToInternedString(TimeUtf8);

    /// <summary>Determines if a function name is <see cref="StringName"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the string function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsStringName(string value) => string.Equals(value, StringName, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Number"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the number function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsNumber(string value) => string.Equals(value, Number, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="IntegerName"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the integer function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsIntegerName(string value) => string.Equals(value, IntegerName, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Offset"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the offset function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsOffset(string value) => string.Equals(value, Offset, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Currency"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the currency function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsCurrency(string value) => string.Equals(value, Currency, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Percent"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the percent function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsPercent(string value) => string.Equals(value, Percent, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Unit"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the unit function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnit(string value) => string.Equals(value, Unit, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Datetime"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the datetime function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsDatetime(string value) => string.Equals(value, Datetime, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Date"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the date function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsDate(string value) => string.Equals(value, Date, StringComparison.Ordinal);

    /// <summary>Determines if a function name is <see cref="Time"/>.</summary>
    /// <param name="value">The function name to test.</param>
    /// <returns><see langword="true"/> if the name is the time function's name; otherwise, <see langword="false"/>.</returns>
    public static bool IsTime(string value) => string.Equals(value, Time, StringComparison.Ordinal);
}
