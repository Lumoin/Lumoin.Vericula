using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// Requires that a unit's target text ends with the given text.
/// </summary>
/// <param name="Text">The text the target must end with.</param>
[DebuggerDisplay("EndsWithRule: {Text}")]
public sealed record EndsWithRule(string Text): ValidationRule;
