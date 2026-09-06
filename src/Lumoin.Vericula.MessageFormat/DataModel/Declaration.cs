using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The base of a message declaration: binds a variable name to the value produced by an expression.
/// Concrete declarations are <see cref="InputDeclaration"/> and <see cref="LocalDeclaration"/>. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Message Model" (data model) and
/// "Declarations" (syntax).
/// </summary>
/// <param name="Name">The declared variable's name, without the leading <c>$</c> sigil.</param>
[DebuggerDisplay("Declaration: {Name}")]
public abstract record Declaration(string Name);
