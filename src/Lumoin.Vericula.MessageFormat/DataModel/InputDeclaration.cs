using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A declaration that binds a variable to an external input value, optionally passing it through a
/// function. The spec requires <see cref="VariableExpression.Variable"/> of <paramref name="Value"/>
/// to name the same variable as <paramref name="Name"/>. See UTS #35 part 9 (MessageFormat), version
/// 48.2, section "Message Model" (data model) and "Declarations" (syntax).
/// </summary>
/// <param name="Name">The declared variable's name, without the leading <c>$</c> sigil.</param>
/// <param name="Value">The variable expression whose resolved value is bound to <paramref name="Name"/>.</param>
[DebuggerDisplay("InputDeclaration: {Name}")]
public sealed record InputDeclaration(string Name, VariableExpression Value): Declaration(Name);
