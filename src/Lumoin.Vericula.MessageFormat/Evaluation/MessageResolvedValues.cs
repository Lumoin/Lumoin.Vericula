namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Projects the plain value apart from a <see cref="MessageResolvedValue"/>'s handler state. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Resolved Values" (formatting.md).
/// </summary>
public static class MessageResolvedValues
{
    /// <summary>
    /// Returns <paramref name="value"/>'s plain <see cref="MessageResolvedValue.Value"/>, for generic
    /// operand conversion and option resolution. Does not format the value or expose any handler state
    /// it retains.
    /// </summary>
    /// <param name="value">The resolved value to unwrap.</param>
    /// <returns>The plain value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static object? Unwrap(MessageResolvedValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Value;
    }
}
