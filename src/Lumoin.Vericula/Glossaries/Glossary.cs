using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.Glossaries;

/// <summary>
/// The terminology glossary attached to a file: the terms translators must, may, or must not use.
/// </summary>
[DebuggerDisplay("Glossary: {Entries.Length} entries")]
public sealed record Glossary(ImmutableArray<GlossaryEntry> Entries);
