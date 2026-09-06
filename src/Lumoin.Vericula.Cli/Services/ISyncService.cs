namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// Reconciles target documents against changed sources on behalf of the console command and the MCP tool.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Reconciles the documents under the given path.
    /// </summary>
    /// <param name="inputPath">The file or directory to reconcile.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>An exit-code style result; zero means success.</returns>
    Task<int> SyncAsync(string inputPath, CancellationToken cancellationToken);
}
