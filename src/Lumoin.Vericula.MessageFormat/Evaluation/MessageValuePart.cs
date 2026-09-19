using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// A function's or operand's resolved value, formatted to parts. See UTS #35 part 9 (MessageFormat),
/// version 48.2, section "Formatting" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Parts"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>part with { Parts = default }</c> restores the default array the normalization exists to remove.
/// </remarks>
/// <param name="Type">The value's type, from <see cref="WellKnownMessagePartTypes"/>, or the producing function's own type name.</param>
/// <param name="Value">The value's own value, or <see langword="null"/> when <paramref name="Parts"/> alone carries the content.</param>
/// <param name="Parts">The value's nested fields, when it formats as more than one field; a default (never-assigned) array is stored as empty.</param>
/// <param name="Locale">The value's resolved locale, or <see langword="null"/> when the function does not report one.</param>
/// <param name="Direction">The value's direction.</param>
/// <param name="Id">The value's <c>u:id</c>, or <see langword="null"/> when none applies.</param>
/// <param name="Options">The value's options, resolved to their plain values.</param>
[DebuggerDisplay("MessageValuePart: {Type}")]
public sealed record MessageValuePart(string Type, object? Value, ImmutableArray<MessageFieldPart> Parts,
    string? Locale, MessageDirection Direction, string? Id, MessagePartOptions Options): MessagePart
{
    /// <summary>The value's nested fields, when it formats as more than one field; a default (never-assigned) array is normalized to empty. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    public ImmutableArray<MessageFieldPart> Parts { get; init; } = Parts.IsDefault ? [] : Parts;
}
