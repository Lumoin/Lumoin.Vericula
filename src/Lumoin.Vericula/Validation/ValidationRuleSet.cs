using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.Validation;

/// <summary>
/// The validation rules attached to a file, evaluated against each unit's target text.
/// </summary>
[DebuggerDisplay("ValidationRuleSet: {Rules.Length} rules")]
public sealed record ValidationRuleSet(ImmutableArray<ValidationRule> Rules);
