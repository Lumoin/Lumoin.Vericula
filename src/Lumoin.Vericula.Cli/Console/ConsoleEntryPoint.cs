using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lumoin.Vericula.Cli.Console.Commands;
using Lumoin.Vericula.Cli.Services;

namespace Lumoin.Vericula.Cli.Console;

/// <summary>
/// The console command line surface of the tool. It resolves the shared services from the host and
/// dispatches the parsed command line.
/// </summary>
public static class ConsoleEntryPoint
{
    /// <summary>
    /// Runs the console command line.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        using IHost host = HostFactory.Create();
        ICompilerService compiler = host.Services.GetRequiredService<ICompilerService>();

        var root = new RootCommand("Vericula cooks XLIFF localization into platform resources.");
        root.Add(CompileCommand.Create(compiler));

        ParseResult parseResult = root.Parse(args);

        return await parseResult.InvokeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
