namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Whether formatting wraps directional parts in Unicode bidirectional isolation controls. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Handling Bidirectional Text" (formatting.md).
/// </summary>
/// <remarks>
/// <see cref="Default"/> is the value a default-initialized <see cref="MessageBidiStrategy"/> holds,
/// and is mandatory for a message whose pattern is a string: it wraps directional parts in
/// LRI/RLI/FSI and PDI, even for a right-to-left placeholder within a right-to-left message.
/// <see cref="None"/> emits no isolating controls but still retains each part's direction metadata.
/// </remarks>
public enum MessageBidiStrategy
{
    /// <summary>
    /// Isolate directional parts with LRI, RLI or FSI and a matching PDI. The value of a
    /// default-initialized <see cref="MessageBidiStrategy"/>, and mandatory when formatting to a
    /// string.
    /// </summary>
    Default = 0,

    /// <summary>Emit no isolating controls; direction metadata is still retained on each part.</summary>
    None = 1
}
