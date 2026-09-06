using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The two values the suite's JSON schema allows for a case's or a file's <c>bidiIsolation</c>
/// property. Each is spelled once as a UTF-8 source literal and carried alongside as an interned
/// string, the same pair-form contract <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/>
/// uses in the core library.
/// </summary>
public static class WellKnownSuiteBidiIsolation
{
    /// <summary>The UTF-8 source literal of <see cref="Default"/>.</summary>
    public static ReadOnlySpan<byte> DefaultUtf8 => "default"u8;

    /// <summary>The value a case uses when the schema's default bidi-isolation strategy applies, whether set explicitly or left for <see cref="MessageFormatSuite"/>'s merge to fall back to.</summary>
    public static readonly string Default = Utf8Constants.ToInternedString(DefaultUtf8);

    /// <summary>The UTF-8 source literal of <see cref="None"/>.</summary>
    public static ReadOnlySpan<byte> NoneUtf8 => "none"u8;

    /// <summary>The value a case uses to opt out of bidi isolation entirely.</summary>
    public static readonly string None = Utf8Constants.ToInternedString(NoneUtf8);

    /// <summary>Determines if a bidi-isolation value is <see cref="Default"/>.</summary>
    /// <param name="value">The bidi-isolation value to test.</param>
    /// <returns><see langword="true"/> if the value is <see cref="Default"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsDefault(string value) => string.Equals(value, Default, StringComparison.Ordinal);

    /// <summary>Determines if a bidi-isolation value is <see cref="None"/>.</summary>
    /// <param name="value">The bidi-isolation value to test.</param>
    /// <returns><see langword="true"/> if the value is <see cref="None"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsNone(string value) => string.Equals(value, None, StringComparison.Ordinal);
}
