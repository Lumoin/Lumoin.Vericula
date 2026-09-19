using System.Collections.Immutable;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Formats a <see cref="MessageResolvedValue"/> to one or more <see cref="MessagePart"/>s. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Resolved Values" (formatting.md).
/// </summary>
/// <returns>The formatted parts, or one or more <see cref="MessageFunctionError"/>s on failure.</returns>
public delegate MessageOperation<ImmutableArray<MessagePart>> FormatValueParts();
