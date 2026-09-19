using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Everything <see cref="MessageEvaluator"/> needs beyond the message itself: the locales to format
/// against, the caller-supplied input values, and the evaluation options. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Formatting Context" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Locales"/> and <see cref="Inputs"/> are validated in the primary constructor. A
/// <c>with</c> expression uses the synthesized copy constructor instead and does not run this
/// validation again: for example <c>context with { Inputs = null }</c> stores a null
/// <see cref="Inputs"/> without throwing, and <c>context with { Locales = default }</c> restores the
/// default array the normalization exists to remove.
/// </remarks>
/// <param name="Locales">The locales to format against, most preferred first; a default (never-assigned) array is stored as empty.</param>
/// <param name="Inputs">The values for the variables the message's input declarations reference, keyed by name.</param>
/// <param name="Options">The evaluation options.</param>
[DebuggerDisplay("MessageFormattingContext: {Locales.Length} locales")]
public sealed record MessageFormattingContext(ImmutableArray<CultureInfo> Locales, ImmutableDictionary<string, object?> Inputs, MessageEvaluationOptions Options)
{
    /// <summary>The locales to format against, most preferred first; a default (never-assigned) array is normalized to empty. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    public ImmutableArray<CultureInfo> Locales { get; init; } = Locales.IsDefault ? [] : Locales;

    /// <summary>The values for the variables the message's input declarations reference, keyed by name. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Inputs"/> is <see langword="null"/>.</exception>
    public ImmutableDictionary<string, object?> Inputs { get; init; } = Inputs ?? throw new ArgumentNullException(nameof(Inputs));
}
