using System.Diagnostics;

namespace Lumoin.Vericula.Scopes;

/// <summary>
/// A scope identifier attached to units, groups, and glossary entries. The library treats
/// scope strings as opaque; their meaning belongs to the consuming application.
/// </summary>
[DebuggerDisplay("Scope: {Value}")]
public sealed record Scope(string Value);
