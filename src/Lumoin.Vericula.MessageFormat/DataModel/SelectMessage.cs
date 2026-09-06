using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A message with one or more selectors: each selector's resolved value ranks or excludes the
/// variants, and the pattern of the best-matching variant is formatted. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Message Model" (data model) and "Matcher" (syntax).
/// </summary>
/// <param name="Declarations">The input and local declarations in scope for <paramref name="Selectors"/> and every <paramref name="Variants"/> pattern, in source order.</param>
/// <param name="Selectors">The variables that drive variant selection, in source order; at least one.</param>
/// <param name="Variants">The message's variants, in source order; in a valid message, at least one, with at least one whose keys are all catch-all (a data model error otherwise, see <see cref="Diagnostics.WellKnownMessageFormatDiagnostics.MissingFallbackVariant"/>).</param>
[DebuggerDisplay("SelectMessage: {Selectors.Length} selectors, {Variants.Length} variants")]
public sealed record SelectMessage(ImmutableArray<Declaration> Declarations, ImmutableArray<Variable> Selectors, ImmutableArray<Variant> Variants): Message;
