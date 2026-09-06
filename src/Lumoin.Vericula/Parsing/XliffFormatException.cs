namespace Lumoin.Vericula.Parsing;

/// <summary>
/// Thrown when a document presented to <see cref="XliffReader"/> is not well-formed XLIFF:
/// malformed XML, a missing root, or a missing required attribute such as srcLang or a unit id.
/// </summary>
public sealed class XliffFormatException: Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XliffFormatException"/> class.
    /// </summary>
    public XliffFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XliffFormatException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the format error.</param>
    public XliffFormatException(string message): base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XliffFormatException"/> class with a message
    /// and the underlying cause.
    /// </summary>
    /// <param name="message">The message that describes the format error.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public XliffFormatException(string message, Exception innerException): base(message, innerException)
    {
    }

    /// <summary>
    /// The one-based line the offending content was found at, when the reader was able to determine
    /// one; null otherwise.
    /// </summary>
    public int? Line { get; set; }

    /// <summary>
    /// The one-based character position on <see cref="Line"/> the offending content was found at, when
    /// the reader was able to determine one; null otherwise.
    /// </summary>
    public int? Position { get; set; }
}
