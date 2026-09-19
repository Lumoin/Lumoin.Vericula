namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// A formatting function callable from a message expression: given an optional resolved operand, the
/// options written at the call site, and the surrounding function context, produces a resolved value.
/// See UTS #35 part 9 (MessageFormat), version 48.2, section "Function Handler" (formatting.md).
/// </summary>
/// <param name="operand">The resolved operand, or <see langword="null"/> for an operand-less call.</param>
/// <param name="options">The function's resolved call options.</param>
/// <param name="context">The locales, expression direction and id surrounding this call.</param>
/// <returns>The resolved value, or one or more <see cref="MessageFunctionError"/>s on failure.</returns>
public delegate MessageOperation<MessageResolvedValue> MessageFunction(MessageResolvedValue? operand,
    MessageResolvedOptions options, MessageFunctionContext context);
