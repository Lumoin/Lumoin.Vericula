using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The <c>{source}</c> fallback rendering of an expression whose resolution or function call failed.
/// See UTS #35 part 9 (MessageFormat), version 48.2, section "Fallback Resolution" (formatting.md).
/// </summary>
/// <param name="Source">The failed expression's source form, rendered wrapped in braces.</param>
[DebuggerDisplay("MessageFallbackPart: {Source}")]
public sealed record MessageFallbackPart(string Source): MessagePart;
