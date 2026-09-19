using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Construction contract tests for <see cref="MessageOperation{T}"/>: a failed operation must carry at
/// least one error, a default (never-assigned) <see cref="MessageOperation{T}.Errors"/> array is
/// normalized to empty, and a succeeded operation may still carry one or more nonfatal errors.
/// </summary>
[TestClass]
public sealed class MessageOperationTests
{
    /// <summary>A stand-in error, reused across the cases below.</summary>
    private static readonly MessageFunctionError SampleError = new(MessageFunctionErrorKind.BadOperand, "bad operand");

    /// <summary>A failed operation with an empty <see cref="MessageOperation{T}.Errors"/> array is refused, naming the parameter.</summary>
    [TestMethod]
    public void AFailedOperationWithNoErrorsIsRefused()
    {
        //Named killer: MessageOperation.cs's NormalizedErrors, the "!succeeded && normalized.IsEmpty"
        //guard replaced with a constant false: a failed, error-less operation would then construct
        //successfully instead of throwing.
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => new MessageOperation<string>("value", false, ImmutableArray<MessageFunctionError>.Empty));

        Assert.AreEqual("Errors", exception.ParamName);
    }

    /// <summary>A failed operation with a default (never-assigned) <see cref="MessageOperation{T}.Errors"/> array is refused: the default normalizes to empty first, which is still too few for a failure.</summary>
    [TestMethod]
    public void AFailedOperationWithADefaultErrorsArrayIsRefused()
    {
        //Named killer: MessageOperation.cs's NormalizedErrors, the "errors.IsDefault ? [] : errors"
        //normalization removed (leaving the default array as-is): the IsEmpty check below would then
        //throw NullReferenceException enumerating the default array instead of this ArgumentException,
        //or, if IsEmpty tolerates a default array, the failure guard would still need the normalized
        //value to see it as empty.
        Assert.ThrowsExactly<ArgumentException>(
            () => new MessageOperation<string>("value", false, default));
    }

    /// <summary>A failed operation with at least one error constructs successfully.</summary>
    [TestMethod]
    public void AFailedOperationWithAnErrorConstructsSuccessfully()
    {
        var operation = new MessageOperation<string>(null, false, [SampleError]);

        Assert.IsFalse(operation.Succeeded);
        Assert.HasCount(1, operation.Errors);
        Assert.AreEqual(SampleError, operation.Errors[0]);
    }

    /// <summary>A succeeded operation with a default (never-assigned) <see cref="MessageOperation{T}.Errors"/> array normalizes it to empty rather than throwing.</summary>
    [TestMethod]
    public void ASucceededOperationWithADefaultErrorsArrayNormalizesToEmpty()
    {
        var operation = new MessageOperation<string>("value", true, default);

        Assert.IsTrue(operation.Errors.IsEmpty);
    }

    /// <summary>A succeeded operation may still carry one or more nonfatal errors.</summary>
    [TestMethod]
    public void ASucceededOperationMayCarryNonfatalErrors()
    {
        var operation = new MessageOperation<string>("value", true, [SampleError]);

        Assert.IsTrue(operation.Succeeded);
        Assert.HasCount(1, operation.Errors);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can restore a default <see cref="MessageOperation{T}.Errors"/> array where the constructor would have normalized it to empty.</summary>
    [TestMethod]
    public void WithBypassesTheErrorsNormalization()
    {
        //Pins the documented with-bypass (MessageOperation.cs's <remarks>): the primary constructor
        //normalizes a default array to empty, but "with" uses the synthesized copy constructor and the
        //plain init accessor, which runs no normalization. This restores exactly the default array the
        //normalization exists to remove; reading IsEmpty below would otherwise throw
        //NullReferenceException far from this assignment. Current, intended behavior, not a mutant to kill.
        var operation = new MessageOperation<string>("value", true, []);

        MessageOperation<string> withDefaultErrors = operation with { Errors = default };

        Assert.IsTrue(withDefaultErrors.Errors.IsDefault);
    }
}
