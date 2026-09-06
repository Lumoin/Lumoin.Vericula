using System.Diagnostics;

namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// The default <see cref="ILinterService"/> implementation.
/// </summary>
[DebuggerDisplay("LinterService")]
public sealed class LinterService: ILinterService
{
    /// <inheritdoc/>
    public Task<int> LintAsync(string inputPath, CancellationToken cancellationToken)
    {
        //Scaffold stub. The real lint logic arrives in subsequent work.
        return Task.FromResult(0);
    }
}
