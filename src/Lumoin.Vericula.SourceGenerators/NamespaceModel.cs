using System.Diagnostics;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The dotted C# namespace the generated accessor class is emitted into, together with any warnings
/// produced while sanitizing it from the project's root namespace or assembly name.
/// </summary>
/// <param name="Value">The sanitized, dotted namespace, always a valid sequence of C# identifiers.</param>
/// <param name="Warnings">Any non-fatal warnings raised while sanitizing a segment or falling back to a placeholder.</param>
[DebuggerDisplay("NamespaceModel: {Value}")]
internal sealed record NamespaceModel(string Value, EquatableArray<string> Warnings);
