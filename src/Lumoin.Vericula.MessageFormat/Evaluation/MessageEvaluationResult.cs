using System.Collections.Immutable;
using System.Diagnostics;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The outcome of a <c>Try</c> evaluation: the best-effort result <see cref="MessageEvaluator"/> could
/// still reach, together with every diagnostic collected while reaching it. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Fallback Resolution" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Diagnostics"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>result with { Diagnostics = default }</c> restores the default array the normalization exists to
/// remove.
/// </remarks>
/// <typeparam name="T">The type of a formatted result: <see cref="string"/> for <see cref="MessageEvaluator.TryFormat(DataModel.Message, MessageFormattingContext)"/>, or <see cref="ImmutableArray{T}"/> of <see cref="MessagePart"/> for <see cref="MessageEvaluator.TryFormatToParts(DataModel.Message, MessageFormattingContext)"/>.</typeparam>
/// <param name="Value">The formatted result; still produced even when <paramref name="Diagnostics"/> is non-empty, using fallback rendering for whatever could not be resolved.</param>
/// <param name="Diagnostics">Every diagnostic collected while evaluating, in the order they were found; empty when evaluation raised none.</param>
[DebuggerDisplay("MessageEvaluationResult: {Diagnostics.Length} diagnostics")]
public sealed record MessageEvaluationResult<T>(T Value, ImmutableArray<MessageFormatDiagnostic> Diagnostics)
{
    /// <summary>Every diagnostic collected while evaluating, in the order they were found; a default (never-assigned) array is normalized to empty. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    public ImmutableArray<MessageFormatDiagnostic> Diagnostics { get; init; } = Diagnostics.IsDefault ? [] : Diagnostics;
}
