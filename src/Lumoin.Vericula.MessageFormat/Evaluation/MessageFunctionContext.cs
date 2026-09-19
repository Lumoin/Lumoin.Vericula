using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The context surrounding one <see cref="MessageFunction"/> call: the locales in effect, the calling
/// expression's direction, and its <c>u:id</c>, distinct from the wider <see cref="MessageFormattingContext"/>
/// the whole message is formatted against. See UTS #35 part 9 (MessageFormat), version 48.2, section
/// "Function Resolution" (formatting.md) and "u:id" (u-namespace.md).
/// </summary>
/// <remarks>
/// <see cref="Locales"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>context with { Locales = default }</c> restores the default array the normalization exists to
/// remove.
/// </remarks>
/// <param name="Locales">The locales in effect for this call, most preferred first; a default (never-assigned) array is stored as empty.</param>
/// <param name="Direction">The calling expression's direction.</param>
/// <param name="Id">The calling expression's <c>u:id</c>, or <see langword="null"/> when none was given.</param>
[DebuggerDisplay("MessageFunctionContext: {Direction}")]
public sealed record MessageFunctionContext(ImmutableArray<CultureInfo> Locales, MessageDirection Direction, string? Id)
{
    /// <summary>The locales in effect for this call, most preferred first; a default (never-assigned) array is normalized to empty. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    public ImmutableArray<CultureInfo> Locales { get; init; } = Locales.IsDefault ? [] : Locales;
}
