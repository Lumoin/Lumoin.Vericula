using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// The three case-sensitive keywords that open a MessageFormat 2.0 declaration or matcher. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Keywords".
/// </summary>
/// <remarks>
/// Each keyword is spelled once as a UTF-8 source literal and carried alongside as an interned
/// string, the same pair-form contract <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/>
/// uses in the core library. The grammar requires an exact, case-sensitive match: <c>.Input</c> or
/// <c>.INPUT</c> is not the keyword.
/// </remarks>
public static class WellKnownMessageFormatKeywords
{
    /// <summary>The UTF-8 source literal of <see cref="Input"/>.</summary>
    public static ReadOnlySpan<byte> InputUtf8 => ".input"u8;

    /// <summary>The keyword that opens an input declaration.</summary>
    public static readonly string Input = Utf8Constants.ToInternedString(InputUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Local"/>.</summary>
    public static ReadOnlySpan<byte> LocalUtf8 => ".local"u8;

    /// <summary>The keyword that opens a local declaration.</summary>
    public static readonly string Local = Utf8Constants.ToInternedString(LocalUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Match"/>.</summary>
    public static ReadOnlySpan<byte> MatchUtf8 => ".match"u8;

    /// <summary>The keyword that opens a matcher.</summary>
    public static readonly string Match = Utf8Constants.ToInternedString(MatchUtf8);

    /// <summary>Determines if a keyword is <see cref="Input"/>.</summary>
    /// <param name="value">The keyword text to test.</param>
    /// <returns><see langword="true"/> if the text is the input keyword; otherwise, <see langword="false"/>.</returns>
    public static bool IsInput(string value) => string.Equals(value, Input, StringComparison.Ordinal);

    /// <summary>Determines if a keyword is <see cref="Local"/>.</summary>
    /// <param name="value">The keyword text to test.</param>
    /// <returns><see langword="true"/> if the text is the local keyword; otherwise, <see langword="false"/>.</returns>
    public static bool IsLocal(string value) => string.Equals(value, Local, StringComparison.Ordinal);

    /// <summary>Determines if a keyword is <see cref="Match"/>.</summary>
    /// <param name="value">The keyword text to test.</param>
    /// <returns><see langword="true"/> if the text is the match keyword; otherwise, <see langword="false"/>.</returns>
    public static bool IsMatch(string value) => string.Equals(value, Match, StringComparison.Ordinal);
}
