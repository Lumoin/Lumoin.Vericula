using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A reference to a formatting function by name, with the options written at the call site. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Expression Model" (data model) and
/// "Function" (syntax).
/// </summary>
/// <param name="Name">The function's full identifier, including its namespace (<c>ns:name</c>), without the leading <c>:</c> sigil.</param>
/// <param name="Options">The function's options, in source order; unique by name in a valid message (the parser reports a repeat as <see cref="Diagnostics.WellKnownMessageFormatDiagnostics.DuplicateOptionName"/>).</param>
[DebuggerDisplay("FunctionRef: {Name}")]
public sealed record FunctionRef(string Name, ImmutableArray<Option> Options);
