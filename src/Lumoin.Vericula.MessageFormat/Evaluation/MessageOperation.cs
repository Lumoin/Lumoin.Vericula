using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The result of one step of the evaluator's function pipeline: a <see cref="MessageFunction"/> call,
/// or a call through one of the delegates a <see cref="MessageResolvedValue"/> carries
/// (<see cref="Evaluation.FormatValue"/>, <see cref="Evaluation.FormatValueParts"/>,
/// <see cref="Evaluation.MatchValue"/>, <see cref="Evaluation.PreferKey"/>). Either a successful
/// <typeparamref name="T"/>, a failed step, or a succeeded step that still carries one or more
/// nonfatal errors. See UTS #35 part 9 (MessageFormat), version 48.2, section "Function Handler"
/// (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Errors"/> is validated in the primary constructor. A <c>with</c> expression uses the
/// synthesized copy constructor instead and does not run this validation again: for example
/// <c>operation with { Succeeded = false }</c> can store a failed operation with zero errors, and
/// <c>operation with { Errors = default }</c> restores the default array the normalization exists to
/// remove.
/// </remarks>
/// <typeparam name="T">The type of a successful result.</typeparam>
/// <param name="Value">The result value; meaningful when <paramref name="Succeeded"/> is <see langword="true"/>, default otherwise.</param>
/// <param name="Succeeded">Whether the step succeeded.</param>
/// <param name="Errors">
/// The errors the step raised, in order; a default (never-assigned) array is stored as empty. A
/// failed step (<paramref name="Succeeded"/> is <see langword="false"/>) must carry at least one
/// error; a succeeded step may still carry one or more nonfatal errors, for example an invalid option
/// that was ignored rather than failing the call.
/// </param>
[DebuggerDisplay("MessageOperation: {Succeeded}")]
public sealed record MessageOperation<T>(T? Value, bool Succeeded, ImmutableArray<MessageFunctionError> Errors)
{
    /// <summary>The errors the step raised, in order; a default (never-assigned) array is normalized to empty. Validated at construction; a <c>with</c> expression is not revalidated.</summary>
    /// <exception cref="ArgumentException">The operation did not succeed, and <see cref="Errors"/> is empty.</exception>
    public ImmutableArray<MessageFunctionError> Errors { get; init; } = NormalizedErrors(Succeeded, Errors, nameof(Errors));

    /// <summary>
    /// Normalizes a default (never-assigned) <paramref name="errors"/> array to empty, then refuses an
    /// empty result for a failed operation.
    /// </summary>
    /// <param name="succeeded">Whether the operation succeeded.</param>
    /// <param name="errors">The errors as given to the constructor.</param>
    /// <param name="paramName">The constructor parameter name to report on failure.</param>
    /// <returns><paramref name="errors"/>, or empty when it was a default array.</returns>
    /// <exception cref="ArgumentException"><paramref name="succeeded"/> is <see langword="false"/> and <paramref name="errors"/> is empty.</exception>
    private static ImmutableArray<MessageFunctionError> NormalizedErrors(bool succeeded, ImmutableArray<MessageFunctionError> errors, string paramName)
    {
        ImmutableArray<MessageFunctionError> normalized = errors.IsDefault ? [] : errors;

        if(!succeeded && normalized.IsEmpty)
        {
            throw new ArgumentException("A failed operation must carry at least one error.", paramName);
        }

        return normalized;
    }
}
