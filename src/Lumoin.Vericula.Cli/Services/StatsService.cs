using System.Diagnostics;

namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// The default <see cref="IStatsService"/> implementation.
/// </summary>
[DebuggerDisplay("StatsService")]
public sealed class StatsService: IStatsService
{
    /// <inheritdoc/>
    public Task<int> CollectAsync(string inputPath, CancellationToken cancellationToken)
    {
        //Scaffold stub. The real statistics logic arrives in subsequent work.
        return Task.FromResult(0);
    }
}
