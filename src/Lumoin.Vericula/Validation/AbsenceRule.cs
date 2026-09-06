using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// Requires the given text to not appear in the target.
/// </summary>
[DebuggerDisplay("AbsenceRule: {Text}")]
public sealed record AbsenceRule(string Text): ValidationRule;
