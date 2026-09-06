using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A linear sequence of text and placeholders that is formatted as a unit. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Pattern Model" (data model) and "Pattern" (syntax).
/// </summary>
/// <param name="Parts">The pattern's text, expression, and markup parts, in source order; may be empty.</param>
[DebuggerDisplay("Pattern: {Parts.Length} parts")]
public sealed record Pattern(ImmutableArray<PatternPart> Parts);
