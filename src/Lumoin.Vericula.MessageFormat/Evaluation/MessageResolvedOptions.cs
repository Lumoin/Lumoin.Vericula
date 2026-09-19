using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The resolved call options passed to a <see cref="MessageFunction"/>: every option written at the
/// call site, each already resolved to a <see cref="MessageResolvedValue"/>. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Function Handler" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Values"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>options with { Values = null }</c> stores a null <see cref="Values"/> without throwing.
/// </remarks>
/// <param name="Values">The resolved options, keyed by their normalized option name.</param>
[DebuggerDisplay("MessageResolvedOptions: {Values.Count} values")]
public sealed record MessageResolvedOptions(ImmutableDictionary<string, MessageResolvedValue> Values)
{
    /// <summary>The resolved options, keyed by their normalized option name. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Values"/> is <see langword="null"/>.</exception>
    public ImmutableDictionary<string, MessageResolvedValue> Values { get; init; } = Values ?? throw new ArgumentNullException(nameof(Values));
}
