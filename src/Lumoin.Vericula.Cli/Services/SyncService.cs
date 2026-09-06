using System.Diagnostics;

namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// The default <see cref="ISyncService"/> implementation.
/// </summary>
[DebuggerDisplay("SyncService")]
public sealed class SyncService: ISyncService
{
    /// <inheritdoc/>
    public Task<int> SyncAsync(string inputPath, CancellationToken cancellationToken)
    {
        //Scaffold stub. The real reconciliation logic arrives in subsequent work.
        return Task.FromResult(0);
    }
}
