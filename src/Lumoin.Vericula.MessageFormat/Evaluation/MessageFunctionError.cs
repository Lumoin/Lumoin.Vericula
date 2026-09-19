using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// One error a <see cref="MessageFunction"/>, or a delegate it hands back on its resolved value,
/// raised while handling a call. See UTS #35 part 9 (MessageFormat), version 48.2, section "Message
/// Function Errors" (errors.md).
/// </summary>
/// <param name="Kind">The kind of error.</param>
/// <param name="Detail">A human-readable description of the error.</param>
[DebuggerDisplay("MessageFunctionError: {Kind}")]
public sealed record MessageFunctionError(MessageFunctionErrorKind Kind, string Detail);
