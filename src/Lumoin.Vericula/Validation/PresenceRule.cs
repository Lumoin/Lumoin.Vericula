using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// Requires the given text to appear in the target.
/// </summary>
[DebuggerDisplay("PresenceRule: {Text}")]
public sealed record PresenceRule(string Text): ValidationRule;
