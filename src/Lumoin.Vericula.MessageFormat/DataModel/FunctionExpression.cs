using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// An expression that is a bare function call without an operand. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Pattern Model" (data model) and "Expressions" (syntax).
/// </summary>
/// <param name="Function">The function call.</param>
/// <param name="Attributes">The expression's attributes, in source order.</param>
[DebuggerDisplay("FunctionExpression: {Function}")]
public sealed record FunctionExpression(FunctionRef Function, ImmutableArray<MessageAttribute> Attributes): Expression(Function, Attributes);
