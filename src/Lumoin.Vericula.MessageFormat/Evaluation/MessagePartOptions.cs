using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The options carried out to a formatted <see cref="MessagePart"/>, resolved to their plain values.
/// See UTS #35 part 9 (MessageFormat), version 48.2, section "Formatting" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Values"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>options with { Values = null }</c> stores a null <see cref="Values"/> without throwing.
/// </remarks>
/// <param name="Values">The options, keyed by their normalized name.</param>
[DebuggerDisplay("MessagePartOptions: {Values.Count} values")]
public sealed record MessagePartOptions(ImmutableDictionary<string, object?> Values)
{
    /// <summary>The options, keyed by their normalized name. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Values"/> is <see langword="null"/>.</exception>
    public ImmutableDictionary<string, object?> Values { get; init; } = Values ?? throw new ArgumentNullException(nameof(Values));
}
