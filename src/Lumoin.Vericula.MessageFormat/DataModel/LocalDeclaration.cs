using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A declaration that binds a variable to the resolved value of an expression. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Message Model" (data model) and "Declarations" (syntax).
/// </summary>
/// <param name="Name">The declared variable's name, without the leading <c>$</c> sigil.</param>
/// <param name="Value">The expression whose resolved value is bound to <paramref name="Name"/>.</param>
[DebuggerDisplay("LocalDeclaration: {Name}")]
public sealed record LocalDeclaration(string Name, Expression Value): Declaration(Name);
