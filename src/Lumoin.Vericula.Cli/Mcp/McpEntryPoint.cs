namespace Lumoin.Vericula.Cli.Mcp;

/// <summary>
/// The MCP server surface of the tool, exposing the same operations as the console commands.
/// </summary>
public static class McpEntryPoint
{
    /// <summary>
    /// Runs the MCP server over standard input and output.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The process exit code.</returns>
    public static Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        //Scaffold stub. The real MCP server wiring arrives in subsequent work.
        return Task.FromResult(0);
    }
}
