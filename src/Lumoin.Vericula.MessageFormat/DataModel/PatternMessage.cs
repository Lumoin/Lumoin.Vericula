using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A message with no selectors: its declarations are resolved and then its single pattern is
/// formatted. In the syntax, this is either a <em>simple message</em> (no declarations, whose whole
/// source is one pattern) or a <em>complex message</em> whose body is a quoted pattern. See UTS #35
/// part 9 (MessageFormat), version 48.2, section "Message Model" (data model) and "The Message"
/// (syntax).
/// </summary>
/// <param name="Declarations">The input and local declarations in scope for <paramref name="Pattern"/>, in source order.</param>
/// <param name="Pattern">The pattern to format.</param>
[DebuggerDisplay("PatternMessage: {Declarations.Length} declarations")]
public sealed record PatternMessage(ImmutableArray<Declaration> Declarations, Pattern Pattern): Message;
