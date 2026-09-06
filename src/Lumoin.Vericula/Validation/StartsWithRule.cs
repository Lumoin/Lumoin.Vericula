using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// Requires that a unit's target text starts with the given text.
/// </summary>
/// <param name="Text">The text the target must start with.</param>
[DebuggerDisplay("StartsWithRule: {Text}")]
public sealed record StartsWithRule(string Text): ValidationRule;
