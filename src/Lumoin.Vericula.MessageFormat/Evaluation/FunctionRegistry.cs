using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The extension point for formatting functions: locale-specific number, date, plural,
/// list, and select functions plug in here by name.
/// </summary>
[DebuggerDisplay("FunctionRegistry: {Functions.Count} functions")]
public sealed class FunctionRegistry
{
    private Dictionary<string, IFunction> Functions { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a function under the given name, replacing any previous registration.
    /// </summary>
    /// <param name="name">The function name as written in message source.</param>
    /// <param name="function">The function implementation.</param>
    public void Add(string name, IFunction function)
    {
        Functions[name] = function;
    }

    /// <summary>
    /// Resolves a function by name.
    /// </summary>
    /// <param name="name">The function name as written in message source.</param>
    /// <returns>The registered function, or <see langword="null"/> when none is registered.</returns>
    public IFunction? Resolve(string name)
    {
        return Functions.TryGetValue(name, out IFunction? function) ? function : null;
    }
}
