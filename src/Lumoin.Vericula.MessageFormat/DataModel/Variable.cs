using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A reference to a named variable supplied by the caller or bound by a declaration. See UTS #35
/// part 9 (MessageFormat), version 48.2, section "Expression Model" (data model) and "Names and
/// Identifiers" (syntax).
/// </summary>
/// <param name="Name">The referenced variable's name, without the leading <c>$</c> sigil.</param>
[DebuggerDisplay("Variable: {Name}")]
public sealed record Variable(string Name): Operand;
