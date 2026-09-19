using System.Diagnostics;
using Lumoin.Vericula.MessageFormat.DataModel;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// A markup placeholder carried through to the formatted output. See UTS #35 part 9 (MessageFormat),
/// version 48.2, section "Formatting" (formatting.md).
/// </summary>
/// <param name="Kind">Whether the markup opens, stands alone, or closes an element.</param>
/// <param name="Name">The markup's full identifier, including its namespace when one is present.</param>
/// <param name="Id">The markup's <c>u:id</c>, or <see langword="null"/> when none was given; markup never accepts <c>u:dir</c>.</param>
/// <param name="Options">The markup's options, resolved to their plain values.</param>
[DebuggerDisplay("{Kind} {Name}")]
public sealed record MessageMarkupPart(MarkupKind Kind, string Name, string? Id, MessagePartOptions Options): MessagePart;
