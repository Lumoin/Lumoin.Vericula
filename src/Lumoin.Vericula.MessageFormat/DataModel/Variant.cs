using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// One variant of a select message: the keys it matches, one per selector, and the pattern it
/// formats when chosen. See UTS #35 part 9 (MessageFormat), version 48.2, section "Message Model"
/// (data model) and "Variant" (syntax).
/// </summary>
/// <param name="Keys">The variant's keys, in the same order as the message's selectors.</param>
/// <param name="Pattern">The pattern to format when this variant is chosen.</param>
[DebuggerDisplay("Variant: {Keys.Length} keys")]
public sealed record Variant(ImmutableArray<VariantKey> Keys, Pattern Pattern);
