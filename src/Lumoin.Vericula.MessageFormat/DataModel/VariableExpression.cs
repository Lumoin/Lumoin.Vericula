using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// An expression whose operand is a variable reference, optionally passed through a function. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Pattern Model" (data model) and
/// "Expressions" (syntax).
/// </summary>
/// <param name="Variable">The variable operand.</param>
/// <param name="Function">The function applied to <paramref name="Variable"/>, or <see langword="null"/> for a bare operand.</param>
/// <param name="Attributes">The expression's attributes, in source order.</param>
[DebuggerDisplay("VariableExpression: {Variable}")]
public sealed record VariableExpression(Variable Variable, FunctionRef? Function, ImmutableArray<MessageAttribute> Attributes): Expression(Function, Attributes);
