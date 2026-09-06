namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// Reports translation statistics on behalf of the console command and the MCP tool.
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// Reports statistics for the documents under the given path.
    /// </summary>
    /// <param name="inputPath">The file or directory to report on.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>An exit-code style result; zero means success.</returns>
    Task<int> CollectAsync(string inputPath, CancellationToken cancellationToken);
}
