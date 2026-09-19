using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// One nested field of a compound <see cref="MessageValuePart"/>, such as an integer digit group or a
/// currency symbol within a formatted number. See UTS #35 part 9 (MessageFormat), version 48.2,
/// section "Formatting" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Fields"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>part with { Fields = null }</c> stores a null <see cref="Fields"/> without throwing.
/// </remarks>
/// <param name="Type">The field's type, defined by the function that produced it; nested number field types arrive with the number backend.</param>
/// <param name="Value">The field's value, or <see langword="null"/> when the type alone is the whole field.</param>
/// <param name="Fields">Further nested fields, keyed by name; empty for a field with no nesting of its own.</param>
[DebuggerDisplay("MessageFieldPart: {Type}")]
public sealed record MessageFieldPart(string Type, object? Value, ImmutableDictionary<string, object?> Fields)
{
    /// <summary>Further nested fields, keyed by name; empty for a field with no nesting of its own. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentNullException"><see cref="Fields"/> is <see langword="null"/>.</exception>
    public ImmutableDictionary<string, object?> Fields { get; init; } = Fields ?? throw new ArgumentNullException(nameof(Fields));
}
