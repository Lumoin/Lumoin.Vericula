using Lumoin.Vericula.Cli.Console;
using Lumoin.Vericula.Cli.Mcp;

namespace Lumoin.Vericula.Cli;

/// <summary>
/// The process entry point. Dispatches to the MCP server when invoked with --mcp,
/// otherwise to the console command line.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the tool.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var cancellationToken = CancellationToken.None;
        if(args is ["--mcp", ..])
        {
            return await McpEntryPoint.RunAsync(args, cancellationToken).ConfigureAwait(false);
        }

        return await ConsoleEntryPoint.RunAsync(args, cancellationToken).ConfigureAwait(false);
    }
}
