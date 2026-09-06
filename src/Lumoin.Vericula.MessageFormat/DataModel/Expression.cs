using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The base of the closed expression hierarchy: a <see cref="LiteralExpression"/>, a
/// <see cref="VariableExpression"/>, or a <see cref="FunctionExpression"/>, each optionally carrying a
/// function and zero or more attributes. Concrete expressions are sealed records. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Pattern Model" (data model) and "Expressions" (syntax).
/// </summary>
/// <param name="Function">The function applied to the expression's operand, or <see langword="null"/> for a bare operand.</param>
/// <param name="Attributes">The expression's attributes, in source order.</param>
[DebuggerDisplay("Expression: {Function}")]
public abstract record Expression(FunctionRef? Function, ImmutableArray<MessageAttribute> Attributes);
