using System.Runtime.CompilerServices;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// Maps data model constructs built while parsing (declarations, variants, options, select messages
/// and selector variables) to the zero-based UTF-16 offset where their source text began, keyed by
/// reference identity rather than the constructs' own, structural record equality. The public data
/// model (<see cref="Message"/> and its parts) intentionally carries no position of its own, so a
/// diagnostic can still be pinned to a source offset without adding a span field that every consumer
/// of the model would otherwise have to carry and ignore: this internal-only companion structure is
/// built once by <see cref="MessageParser"/> as it parses and consulted once by
/// <see cref="MessageDataModelValidator"/> to locate the diagnostics it raises.
/// </summary>
internal sealed class MessageFormatOffsets
{
    /// <summary>
    /// The backing map, compared by reference (<see cref="ReferenceEqualityComparer"/>) rather than by
    /// the constructs' own <c>Equals</c>: two structurally equal but distinct constructs (for example
    /// two variants that both turn out to have the same keys, which is exactly the case
    /// <see cref="WellKnownMessageFormatDiagnostics.DuplicateVariant"/> exists to catch) must not
    /// collide in this table.
    /// </summary>
    private readonly Dictionary<object, int> _offsets = new(ReferenceEqualityComparer.Instance);

    /// <summary>Records the offset a construct's source text began at.</summary>
    /// <param name="construct">The data model object the offset belongs to, compared by reference.</param>
    /// <param name="offset">The zero-based UTF-16 offset into the source where <paramref name="construct"/>'s text began.</param>
    internal void Record(object construct, int offset) => _offsets[construct] = offset;

    /// <summary>Looks up the offset recorded for a construct.</summary>
    /// <param name="construct">The data model object to look up, compared by reference.</param>
    /// <returns>
    /// The offset <see cref="Record"/> recorded for <paramref name="construct"/>; zero if none was
    /// recorded (which the validator never triggers, since it only looks up constructs the parser
    /// itself just built and recorded).
    /// </returns>
    internal int OffsetOf(object construct) => _offsets.TryGetValue(construct, out int offset) ? offset : 0;
}
