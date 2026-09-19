namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The kinds of error a <see cref="MessageFunction"/> can report through <see cref="MessageFunctionError"/>.
/// See UTS #35 part 9 (MessageFormat), version 48.2, section "Message Function Errors" (errors.md).
/// </summary>
/// <remarks>
/// Three further resolution errors exist (unresolved variable, unknown function, bad selector), but
/// they fire before or outside a function handler's own call and so are reported directly as
/// <see cref="Diagnostics.WellKnownMessageFormatDiagnostics"/> ids rather than through this enum.
/// </remarks>
public enum MessageFunctionErrorKind
{
    /// <summary>An operand's resolved value was unsuitable for the function it was passed to.</summary>
    BadOperand = 0,

    /// <summary>An option's resolved value was unsuitable for the function it configures.</summary>
    BadOption = 1,

    /// <summary>A variant key's literal was unsuitable for the selector it was matched against.</summary>
    BadVariantKey = 2,

    /// <summary>The function does not support the operation it was asked to perform.</summary>
    UnsupportedOperation = 3
}
