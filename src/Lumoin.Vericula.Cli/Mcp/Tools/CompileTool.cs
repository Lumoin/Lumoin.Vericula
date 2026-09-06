namespace Lumoin.Vericula.Cli.Mcp.Tools;

/// <summary>
/// The MCP tool counterpart of the compile command.
/// </summary>
public static class CompileTool
{
    /// <summary>
    /// Compiles the documents under the given path.
    /// </summary>
    /// <param name="inputPath">The file or directory to compile.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>An exit-code style result; zero means success.</returns>
    public static Task<int> RunAsync(string inputPath, CancellationToken cancellationToken)
    {
        //Scaffold stub. The real MCP tool wiring arrives in subsequent work.
        return Task.FromResult(0);
    }
}
