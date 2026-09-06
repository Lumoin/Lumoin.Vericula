using System.Collections.Immutable;

namespace Lumoin.Vericula.MessageFormat.Diagnostics;

/// <summary>
/// Thrown when <see cref="Parsing.MessageFormatReader.Parse(string)"/> is given a message that does
/// not parse to a valid model: a syntax error, or one or more data model errors.
/// </summary>
public sealed class MessageFormatException: Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageFormatException"/> class.
    /// </summary>
    public MessageFormatException()
    {
        Diagnostics = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageFormatException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the format error.</param>
    public MessageFormatException(string message): base(message)
    {
        Diagnostics = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageFormatException"/> class with a message
    /// and the underlying cause.
    /// </summary>
    /// <param name="message">The message that describes the format error.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public MessageFormatException(string message, Exception innerException): base(message, innerException)
    {
        Diagnostics = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageFormatException"/> class with a message
    /// and the diagnostics it summarizes.
    /// </summary>
    /// <param name="message">The message that describes the format error, summarizing every entry in <paramref name="diagnostics"/>.</param>
    /// <param name="diagnostics">The diagnostics that caused parsing to fail; a default (never-assigned) array is stored as empty, so <see cref="Diagnostics"/> never throws for a caller who reads it.</param>
    public MessageFormatException(string message, ImmutableArray<MessageFormatDiagnostic> diagnostics): base(message)
    {
        Diagnostics = diagnostics.IsDefault ? [] : diagnostics;
    }

    /// <summary>
    /// The diagnostics that caused parsing to fail; empty when this instance was created by one of the
    /// plain constructors rather than the one taking diagnostics.
    /// </summary>
    public ImmutableArray<MessageFormatDiagnostic> Diagnostics { get; }
}
