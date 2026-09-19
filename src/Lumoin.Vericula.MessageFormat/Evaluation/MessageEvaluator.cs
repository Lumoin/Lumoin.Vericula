using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Formats a parsed or directly-built <see cref="Message"/> against a <see cref="MessageFormattingContext"/>.
/// See UTS #35 part 9 (MessageFormat), version 48.2, section "Formatting" (formatting.md).
/// </summary>
public static class MessageEvaluator
{
    /// <summary>
    /// Formats the given message, throwing when it cannot be formatted without a finding.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="ctx">The locales, inputs and evaluation options to format against.</param>
    /// <returns>The formatted text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="ctx"/> is <see langword="null"/>.</exception>
    public static string Format(Message message, MessageFormattingContext ctx)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(ctx);

        //Scaffold stub. The real evaluator arrives in subsequent work.
        throw new NotImplementedException();
    }

    /// <summary>
    /// Formats the given message, collecting diagnostics rather than throwing when it cannot be
    /// formatted without a finding.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="ctx">The locales, inputs and evaluation options to format against.</param>
    /// <returns>The best-effort formatted text and every diagnostic collected while reaching it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="ctx"/> is <see langword="null"/>.</exception>
    public static MessageEvaluationResult<string> TryFormat(Message message, MessageFormattingContext ctx)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(ctx);

        //Scaffold stub. The real evaluator arrives in subsequent work.
        throw new NotImplementedException();
    }

    /// <summary>
    /// Formats the given message to parts, throwing when it cannot be formatted without a finding.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="ctx">The locales, inputs and evaluation options to format against.</param>
    /// <returns>The formatted parts.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="ctx"/> is <see langword="null"/>.</exception>
    public static ImmutableArray<MessagePart> FormatToParts(Message message, MessageFormattingContext ctx)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(ctx);

        //Scaffold stub. The real evaluator arrives in subsequent work.
        throw new NotImplementedException();
    }

    /// <summary>
    /// Formats the given message to parts, collecting diagnostics rather than throwing when it cannot
    /// be formatted without a finding.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="ctx">The locales, inputs and evaluation options to format against.</param>
    /// <returns>The best-effort formatted parts and every diagnostic collected while reaching them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="ctx"/> is <see langword="null"/>.</exception>
    public static MessageEvaluationResult<ImmutableArray<MessagePart>> TryFormatToParts(Message message, MessageFormattingContext ctx)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(ctx);

        //Scaffold stub. The real evaluator arrives in subsequent work.
        throw new NotImplementedException();
    }
}
