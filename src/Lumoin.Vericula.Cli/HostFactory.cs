using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lumoin.Vericula.Cli.Services;

namespace Lumoin.Vericula.Cli;

/// <summary>
/// Builds the host that backs both the console commands and the MCP tools,
/// so each surface resolves the same services.
/// </summary>
public static class HostFactory
{
    /// <summary>
    /// Creates the host with all tool services registered. The command line is parsed by
    /// System.CommandLine, not by the host, so its arguments are deliberately not handed to the
    /// host's configuration (their subcommand/single-dash syntax is not valid configuration input).
    /// </summary>
    /// <returns>The built host. The caller owns its lifetime.</returns>
    public static IHost Create()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ILinterService, LinterService>();
        builder.Services.AddSingleton<ICompilerService, CompilerService>();
        builder.Services.AddSingleton<IStatsService, StatsService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();

        return builder.Build();
    }
}
