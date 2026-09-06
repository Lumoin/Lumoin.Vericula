using System.CommandLine;

namespace Lumoin.Vericula.Cli.Console.Commands;

/// <summary>
/// The sync command: reconciles target documents against a changed source document.
/// </summary>
public static class SyncCommand
{
    /// <summary>
    /// Creates the command definition.
    /// </summary>
    /// <returns>The command.</returns>
    public static Command Create()
    {
        //Scaffold stub. The real options and handler arrive in subsequent work.
        return new Command("sync", "Reconciles target documents against a changed source document.");
    }
}
