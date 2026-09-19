using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The caller's choices for one evaluation: whether to isolate directional parts, the message's own
/// direction, the fallback string for an unresolvable value, and the functions available. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Formatting Context" and "Handling
/// Bidirectional Text" (formatting.md).
/// </summary>
/// <param name="Bidi">Whether formatting isolates directional parts.</param>
/// <param name="Direction">The message's own direction.</param>
/// <param name="Fallback">The text substituted for a variable with no resolvable value, or <see langword="null"/> to use the variable's own source form.</param>
/// <param name="Registry">The functions available to the message.</param>
[DebuggerDisplay("MessageEvaluationOptions: {Bidi}, {Direction}")]
public sealed record MessageEvaluationOptions(MessageBidiStrategy Bidi, MessageDirection Direction, string? Fallback, MessageFunctionRegistry Registry);
