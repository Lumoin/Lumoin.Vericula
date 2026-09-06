using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A placeholder within a pattern that is an expression. See UTS #35 part 9 (MessageFormat), version
/// 48.2, section "Pattern Model" (data model) and "Placeholder" (syntax).
/// </summary>
/// <param name="Expression">The wrapped expression.</param>
[DebuggerDisplay("ExpressionPart: {Expression}")]
public sealed record ExpressionPart(Expression Expression): PatternPart;
