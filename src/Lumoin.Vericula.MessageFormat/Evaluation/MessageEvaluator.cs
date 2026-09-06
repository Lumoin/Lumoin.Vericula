using System.Globalization;
using Lumoin.Vericula.MessageFormat.DataModel;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Formats messages against caller-supplied arguments and a culture.
/// </summary>
public static class MessageEvaluator
{
    /// <summary>
    /// Formats the given message with the given arguments in the given culture.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="arguments">The values for the variables the message references.</param>
    /// <param name="culture">The culture that drives locale-sensitive formatting.</param>
    /// <returns>The formatted text.</returns>
    public static string Format(Message message, IReadOnlyDictionary<string, object?> arguments, CultureInfo culture)
    {
        //Scaffold stub. The real evaluator arrives in subsequent work.
        throw new NotImplementedException();
    }
}
