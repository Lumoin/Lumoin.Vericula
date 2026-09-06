using System.Collections.Immutable;
using System.Diagnostics;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// The outcome of <see cref="MessageFormatReader.TryParse(string)"/>: the parsed model, when parsing
/// reached one, and every diagnostic collected along the way.
/// </summary>
/// <param name="Message">
/// The parsed message, or <see langword="null"/> exactly when a syntax error stopped parsing before a
/// model could be built. A data model error leaves this populated: the model is still built, and the
/// error is one of the entries in <paramref name="Diagnostics"/>.
/// </param>
/// <param name="Diagnostics">Every diagnostic collected while parsing, in the order they were found; empty when the source is a valid message.</param>
[DebuggerDisplay("MessageParseResult: {IsValid}")]
public sealed record MessageParseResult(Message? Message, ImmutableArray<MessageFormatDiagnostic> Diagnostics)
{
    /// <summary>
    /// Whether parsing produced a usable message: a model was built and no diagnostic was raised
    /// against it. <see langword="false"/> when <see cref="Message"/> is <see langword="null"/> (a
    /// syntax error) or when <see cref="Diagnostics"/> is non-empty (one or more data model errors).
    /// Uses <see cref="ImmutableArray{T}.IsDefaultOrEmpty"/> rather than <c>IsEmpty</c> so a result
    /// built with the record's default <c>default(ImmutableArray&lt;MessageFormatDiagnostic&gt;)</c>
    /// (never produced by <see cref="MessageFormatReader"/> itself, but not prevented by the type
    /// either) reads as valid instead of throwing.
    /// </summary>
    public bool IsValid => Message is not null && Diagnostics.IsDefaultOrEmpty;
}
