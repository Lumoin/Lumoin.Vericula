using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A literal operand value, quoted or unquoted, with escape sequences already processed. The data
/// model does not distinguish a quoted literal from an unquoted one. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Expression Model" (data model) and "Literals" (syntax).
/// </summary>
/// <param name="Value">The literal's cooked value: quotes removed, escapes processed, otherwise verbatim.</param>
[DebuggerDisplay("Literal: {Value}")]
public sealed record Literal(string Value): Operand;
