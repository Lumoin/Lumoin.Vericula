using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// One Unicode bidirectional isolation control character (LRI, RLI, FSI or PDI) that
/// <see cref="MessageBidiStrategy.Default"/> inserts around a directional part. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Handling Bidirectional Text" (formatting.md).
/// </summary>
/// <param name="Value">The single isolation control character.</param>
[DebuggerDisplay("MessageBidiPart: {Value}")]
public sealed record MessageBidiPart(string Value): MessagePart;
