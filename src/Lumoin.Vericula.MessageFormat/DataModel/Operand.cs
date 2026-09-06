namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The base of an expression's operand: either a <see cref="Literal"/> value or a <see cref="Variable"/>
/// reference. Concrete operands are sealed records. See UTS #35 part 9 (MessageFormat), version 48.2,
/// section "Expression Model" (data model) and "Operand" (syntax).
/// </summary>
public abstract record Operand;
