using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The functions available to a message during evaluation, keyed by their normalized name. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Function Resolution" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Functions"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>registry with { Functions = null }</c> stores a null <see cref="Functions"/> without throwing.
/// </remarks>
/// <param name="Functions">The registered functions, keyed by their normalized, namespace-qualified name, without the leading <c>:</c> sigil.</param>
[DebuggerDisplay("MessageFunctionRegistry: {Functions.Count} functions")]
public sealed record MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction> Functions)
{
    /// <summary>The registered functions, keyed by their normalized, namespace-qualified name, without the leading <c>:</c> sigil. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Functions"/> is <see langword="null"/>.</exception>
    public ImmutableDictionary<string, MessageFunction> Functions { get; init; } = Functions ?? throw new ArgumentNullException(nameof(Functions));
}
