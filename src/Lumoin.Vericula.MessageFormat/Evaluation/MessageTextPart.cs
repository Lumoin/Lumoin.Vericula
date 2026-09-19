using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Literal pattern text, formatted verbatim. See UTS #35 part 9 (MessageFormat), version 48.2,
/// section "Formatting" (formatting.md).
/// </summary>
/// <param name="Value">The part's text.</param>
[DebuggerDisplay("MessageTextPart: {Value}")]
public sealed record MessageTextPart(string Value): MessagePart;
