using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// Caps the target length so translations fit the space budgeted in the user interface.
/// </summary>
[DebuggerDisplay("LengthBudgetRule: max {MaximumLength}")]
public sealed record LengthBudgetRule(int MaximumLength): ValidationRule;
