using System.Collections.Immutable;
using System.Diagnostics;
using Lumoin.Vericula.Scopes;

namespace Lumoin.Vericula.Glossaries;

/// <summary>
/// One glossary term: its required translation, status, and the scopes it applies to.
/// </summary>
[DebuggerDisplay("GlossaryEntry: {Term} -> {Translation} ({Status})")]
public sealed record GlossaryEntry(
    string Term,
    string Translation,
    string? Definition,
    GlossaryEntryStatus Status,
    ImmutableArray<Scope> Scopes,
    string? Rationale);
