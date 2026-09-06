using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A named argument passed to a function or to markup, written as <c>name=value</c> at the call
/// site. See UTS #35 part 9 (MessageFormat), version 48.2, section "Expression Model" (data model)
/// and "Options" (syntax).
/// </summary>
/// <param name="Name">The option's full identifier, including its namespace when one is present.</param>
/// <param name="Value">The option's value: a literal or a variable reference.</param>
[DebuggerDisplay("Option: {Name}")]
public sealed record Option(string Name, Operand Value);
