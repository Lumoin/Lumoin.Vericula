using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The resolved value of an operand, a literal, or a function's result: the plain value together with
/// everything a later function call or matcher needs to keep treating it correctly. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Resolved Values" (formatting.md).
/// </summary>
/// <remarks>
/// Custom function authors build this type directly, so every constraint below is enforced in the
/// primary constructor. A <c>with</c> expression uses the synthesized copy constructor instead and does
/// not run this validation again: for example <c>resolved with { Options = null }</c> stores a null
/// <see cref="Options"/> without throwing.
/// </remarks>
/// <param name="Value">The plain value, before any function has run on it, or a function's plain result; see <see cref="MessageResolvedValues.Unwrap(MessageResolvedValue)"/>.</param>
/// <param name="Options">The per-function options this value carries forward from the call that produced it.</param>
/// <param name="IsLiteralOption">Whether this value came from a literal written directly as an option value, rather than from a variable.</param>
/// <param name="IsFallback">Whether this value is a fallback substituted after a resolution or function-handler failure.</param>
/// <param name="Source">The value's source form, used to render <c>{source}</c> when formatting falls back.</param>
/// <param name="Direction">The value's direction.</param>
/// <param name="Isolate">Whether formatting must wrap this value in bidirectional isolation controls under <see cref="MessageBidiStrategy.Default"/>.</param>
/// <param name="Format">Formats this value to a string.</param>
/// <param name="FormatParts">Formats this value to one or more <see cref="MessagePart"/>s.</param>
/// <param name="Match">Tests this value against a variant key, or <see langword="null"/> when this value cannot be a selector's resolved value.</param>
/// <param name="BetterThan">Ranks two matching variant keys against each other, or <see langword="null"/> when this value cannot be a selector's resolved value.</param>
[DebuggerDisplay("MessageResolvedValue: {Value}")]
public sealed record MessageResolvedValue(object? Value, MessageResolvedOptions Options, bool IsLiteralOption,
    bool IsFallback, string Source, MessageDirection Direction, bool Isolate, FormatValue Format,
    FormatValueParts FormatParts, MatchValue? Match, PreferKey? BetterThan)
{
    /// <summary>The per-function options this value carries forward from the call that produced it. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Options"/> is <see langword="null"/>.</exception>
    public MessageResolvedOptions Options { get; init; } = Options ?? throw new ArgumentNullException(nameof(Options));

    /// <summary>The value's source form, used to render <c>{source}</c> when formatting falls back. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Source"/> is <see langword="null"/>.</exception>
    public string Source { get; init; } = Source ?? throw new ArgumentNullException(nameof(Source));

    /// <summary>Formats this value to a string. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Format"/> is <see langword="null"/>.</exception>
    public FormatValue Format { get; init; } = Format ?? throw new ArgumentNullException(nameof(Format));

    /// <summary>Formats this value to one or more <see cref="MessagePart"/>s. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="FormatParts"/> is <see langword="null"/>.</exception>
    public FormatValueParts FormatParts { get; init; } = FormatParts ?? throw new ArgumentNullException(nameof(FormatParts));

    /// <summary>Tests this value against a variant key, or <see langword="null"/> when this value cannot be a selector's resolved value. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentException"><see cref="Match"/> and <see cref="BetterThan"/> are not both null or both present.</exception>
    public MatchValue? Match { get; init; } = (Match is null) == (BetterThan is null)
        ? Match
        : throw new ArgumentException("Match and BetterThan must both be present or both be null.", Match is null ? nameof(Match) : nameof(BetterThan));
}
