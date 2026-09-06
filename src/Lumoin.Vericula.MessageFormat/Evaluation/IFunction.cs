using System.Collections.Immutable;
using System.Globalization;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// A formatting function callable from message expressions.
/// </summary>
public interface IFunction
{
    /// <summary>
    /// Applies the function to an input value.
    /// </summary>
    /// <param name="input">The operand value, or <see langword="null"/> for an operand-less call.</param>
    /// <param name="options">The options written at the call site.</param>
    /// <param name="culture">The culture that drives locale-sensitive behavior.</param>
    /// <returns>The function result.</returns>
    object Apply(object? input, ImmutableDictionary<string, object?> options, CultureInfo culture);
}
