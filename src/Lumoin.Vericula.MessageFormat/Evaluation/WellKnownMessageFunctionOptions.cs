using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The <c>u:</c> namespace option names every function accepts, and the values <see cref="UDir"/>
/// accepts. Each is spelled once as a UTF-8 source literal and carried alongside as an interned
/// string, the pair-form contract <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/> uses
/// in the core library. See UTS #35 part 9 (MessageFormat), version 48.2 (u-namespace.md).
/// </summary>
/// <remarks>
/// Numeric option names (for example <c>:number</c>'s <c>signDisplay</c> or <c>useGrouping</c>) arrive
/// with the number backend in a later step.
/// </remarks>
public static class WellKnownMessageFunctionOptions
{
    /// <summary>The UTF-8 source literal of <see cref="UDir"/>.</summary>
    public static ReadOnlySpan<byte> UDirUtf8 => "u:dir"u8;

    /// <summary>The option that overrides an expression's resolved direction before dispatch. See UTS #35 part 9 (MessageFormat), version 48.2, section "u:dir" (u-namespace.md).</summary>
    public static readonly string UDir = Utf8Constants.ToInternedString(UDirUtf8);

    /// <summary>The UTF-8 source literal of <see cref="UId"/>.</summary>
    public static ReadOnlySpan<byte> UIdUtf8 => "u:id"u8;

    /// <summary>The option that sets an expression's or markup's id, carried out to its formatted part. See UTS #35 part 9 (MessageFormat), version 48.2, section "u:id" (u-namespace.md).</summary>
    public static readonly string UId = Utf8Constants.ToInternedString(UIdUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ULocale"/>.</summary>
    public static ReadOnlySpan<byte> ULocaleUtf8 => "u:locale"u8;

    /// <summary>
    /// The option that overrides the locale a function resolves against. See UTS #35 part 9
    /// (MessageFormat), version 48.2 (u-namespace.md). The section name is left out here: unlike
    /// <see cref="UDir"/> and <see cref="UId"/>, no read source quoted <c>u:locale</c>'s exact section
    /// title, so citing one would not be verified.
    /// </summary>
    public static readonly string ULocale = Utf8Constants.ToInternedString(ULocaleUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ltr"/>.</summary>
    public static ReadOnlySpan<byte> LtrUtf8 => "ltr"u8;

    /// <summary>The <see cref="UDir"/> value that forces left-to-right, isolated with LRI/PDI under <see cref="MessageBidiStrategy.Default"/>. See UTS #35 part 9 (MessageFormat), version 48.2, section "u:dir" (u-namespace.md).</summary>
    public static readonly string Ltr = Utf8Constants.ToInternedString(LtrUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Rtl"/>.</summary>
    public static ReadOnlySpan<byte> RtlUtf8 => "rtl"u8;

    /// <summary>The <see cref="UDir"/> value that forces right-to-left, isolated with RLI/PDI under <see cref="MessageBidiStrategy.Default"/>. See UTS #35 part 9 (MessageFormat), version 48.2, section "u:dir" (u-namespace.md).</summary>
    public static readonly string Rtl = Utf8Constants.ToInternedString(RtlUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Auto"/>.</summary>
    public static ReadOnlySpan<byte> AutoUtf8 => "auto"u8;

    /// <summary>The <see cref="UDir"/> value that resolves direction from content, isolated with FSI/PDI under <see cref="MessageBidiStrategy.Default"/>. See UTS #35 part 9 (MessageFormat), version 48.2, section "u:dir" (u-namespace.md).</summary>
    public static readonly string Auto = Utf8Constants.ToInternedString(AutoUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Inherit"/>.</summary>
    public static ReadOnlySpan<byte> InheritUtf8 => "inherit"u8;

    /// <summary>The <see cref="UDir"/> value that takes the operand's or message's own direction and forces no new isolation. See UTS #35 part 9 (MessageFormat), version 48.2, section "u:dir" (u-namespace.md).</summary>
    public static readonly string Inherit = Utf8Constants.ToInternedString(InheritUtf8);

    /// <summary>Determines if an option name is <see cref="UDir"/>.</summary>
    /// <param name="value">The option name to test.</param>
    /// <returns><see langword="true"/> if the name is the <c>u:dir</c> option; otherwise, <see langword="false"/>.</returns>
    public static bool IsUDir(string value) => string.Equals(value, UDir, StringComparison.Ordinal);

    /// <summary>Determines if an option name is <see cref="UId"/>.</summary>
    /// <param name="value">The option name to test.</param>
    /// <returns><see langword="true"/> if the name is the <c>u:id</c> option; otherwise, <see langword="false"/>.</returns>
    public static bool IsUId(string value) => string.Equals(value, UId, StringComparison.Ordinal);

    /// <summary>Determines if an option name is <see cref="ULocale"/>.</summary>
    /// <param name="value">The option name to test.</param>
    /// <returns><see langword="true"/> if the name is the <c>u:locale</c> option; otherwise, <see langword="false"/>.</returns>
    public static bool IsULocale(string value) => string.Equals(value, ULocale, StringComparison.Ordinal);

    /// <summary>Determines if a <see cref="UDir"/> value is <see cref="Ltr"/>.</summary>
    /// <param name="value">The option value to test.</param>
    /// <returns><see langword="true"/> if the value is <c>ltr</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsLtr(string value) => string.Equals(value, Ltr, StringComparison.Ordinal);

    /// <summary>Determines if a <see cref="UDir"/> value is <see cref="Rtl"/>.</summary>
    /// <param name="value">The option value to test.</param>
    /// <returns><see langword="true"/> if the value is <c>rtl</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsRtl(string value) => string.Equals(value, Rtl, StringComparison.Ordinal);

    /// <summary>Determines if a <see cref="UDir"/> value is <see cref="Auto"/>.</summary>
    /// <param name="value">The option value to test.</param>
    /// <returns><see langword="true"/> if the value is <c>auto</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsAuto(string value) => string.Equals(value, Auto, StringComparison.Ordinal);

    /// <summary>Determines if a <see cref="UDir"/> value is <see cref="Inherit"/>.</summary>
    /// <param name="value">The option value to test.</param>
    /// <returns><see langword="true"/> if the value is <c>inherit</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsInherit(string value) => string.Equals(value, Inherit, StringComparison.Ordinal);
}
