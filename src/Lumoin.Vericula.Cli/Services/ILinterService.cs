namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// Lints XLIFF documents on behalf of the console command and the MCP tool.
/// </summary>
public interface ILinterService
{
    /// <summary>
    /// Lints the documents under the given path.
    /// </summary>
    /// <param name="inputPath">The file or directory to lint.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>An exit-code style result; zero means clean.</returns>
    Task<int> LintAsync(string inputPath, CancellationToken cancellationToken);
}
