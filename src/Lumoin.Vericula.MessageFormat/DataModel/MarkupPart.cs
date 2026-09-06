using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A markup placeholder within a pattern, carried by kind and name and passed through to the
/// consumer. See UTS #35 part 9 (MessageFormat), version 48.2, section "Markup Model" (data model)
/// and "Markup" (syntax).
/// </summary>
/// <param name="Kind">Whether the markup opens, stands alone, or closes an element.</param>
/// <param name="Name">The markup's full identifier, including its namespace when one is present, without the sigils.</param>
/// <param name="Options">The markup's options, in source order; unique by name in a valid message (the parser reports a repeat as <see cref="Diagnostics.WellKnownMessageFormatDiagnostics.DuplicateOptionName"/>).</param>
/// <param name="Attributes">The markup's attributes, in source order.</param>
[DebuggerDisplay("{Kind} {Name}")]
public sealed record MarkupPart(MarkupKind Kind, string Name, ImmutableArray<Option> Options, ImmutableArray<MessageAttribute> Attributes): PatternPart;
