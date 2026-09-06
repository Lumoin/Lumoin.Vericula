using System.CommandLine;

namespace Lumoin.Vericula.Cli.Console.Commands;

/// <summary>
/// The lint command: checks XLIFF documents against their tone profiles, glossaries, and rules.
/// </summary>
public static class LintCommand
{
    /// <summary>
    /// Creates the command definition.
    /// </summary>
    /// <returns>The command.</returns>
    public static Command Create()
    {
        //Scaffold stub. The real options and handler arrive in subsequent work.
        return new Command("lint", "Checks XLIFF documents against their tone profiles, glossaries, and validation rules.");
    }
}
