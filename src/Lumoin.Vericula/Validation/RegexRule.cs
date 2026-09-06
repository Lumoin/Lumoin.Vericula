using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// Requires the target to match the given regular expression pattern.
/// </summary>
[DebuggerDisplay("RegexRule: {Pattern}")]
public sealed record RegexRule(string Pattern): ValidationRule;
