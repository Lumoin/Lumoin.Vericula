using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// Parses MessageFormat 2.0 source text into the message data model.
/// </summary>
public static class MessageFormatReader
{
    /// <summary>
    /// Parses the given MessageFormat 2.0 source text, collecting diagnostics rather than throwing on
    /// invalid input.
    /// </summary>
    /// <param name="source">The message source text.</param>
    /// <returns>The model reached (if any) and every diagnostic collected while reaching it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static MessageParseResult TryParse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return MessageParser.Parse(source);
    }

    /// <summary>
    /// Parses the given MessageFormat 2.0 source text, throwing when it is not a valid message.
    /// </summary>
    /// <param name="source">The message source text.</param>
    /// <returns>The parsed message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="MessageFormatException">
    /// <paramref name="source"/> did not parse to a valid message: a syntax error, or one or more data
    /// model errors. The exception's message lists every diagnostic and its <see cref="MessageFormatException.Diagnostics"/>
    /// carries them.
    /// </exception>
    public static Message Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        MessageParseResult result = TryParse(source);

        if(result is { IsValid: true, Message: Message parsedMessage })
        {
            return parsedMessage;
        }

        string formatted = string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Id} at line {diagnostic.Line}, position {diagnostic.Position}: {diagnostic.Message}"));

        throw new MessageFormatException(formatted, result.Diagnostics);
    }
}
