namespace Lumoin.Vericula.Cli.Mcp.Tools;

/// <summary>
/// The MCP tool counterpart of the lint command.
/// </summary>
public static class LintTool
{
    /// <summary>
    /// Lints the documents under the given path.
    /// </summary>
    /// <param name="inputPath">The file or directory to lint.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>An exit-code style result; zero means clean.</returns>
    public static Task<int> RunAsync(string inputPath, CancellationToken cancellationToken)
    {
        //Scaffold stub. The real MCP tool wiring arrives in subsequent work.
        return Task.FromResult(0);
    }
}
