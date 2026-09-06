using System.CommandLine;

namespace Lumoin.Vericula.Cli.Console.Commands;

/// <summary>
/// The stats command: reports translation coverage and segment state counts.
/// </summary>
public static class StatsCommand
{
    /// <summary>
    /// Creates the command definition.
    /// </summary>
    /// <returns>The command.</returns>
    public static Command Create()
    {
        //Scaffold stub. The real options and handler arrive in subsequent work.
        return new Command("stats", "Reports translation coverage and segment state counts.");
    }
}
