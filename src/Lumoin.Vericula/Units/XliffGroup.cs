using System.Collections.Immutable;
using System.Diagnostics;
using Lumoin.Vericula.Scopes;

namespace Lumoin.Vericula.Units;

/// <summary>
/// A named grouping of units and nested groups, used to scope metadata over a subtree of the file.
/// </summary>
[DebuggerDisplay("XliffGroup: {Id}, units: {Units.Length}, groups: {Groups.Length}")]
public sealed record XliffGroup(
    string Id,
    ImmutableArray<Scope> Scopes,
    ImmutableDictionary<string, string> Metadata,
    ImmutableArray<XliffUnit> Units,
    ImmutableArray<XliffGroup> Groups);
