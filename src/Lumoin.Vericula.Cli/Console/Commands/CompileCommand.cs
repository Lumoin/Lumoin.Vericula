using System.CommandLine;
using Lumoin.Vericula.Cli.Services;

namespace Lumoin.Vericula.Cli.Console.Commands;

/// <summary>
/// The compile command: cooks XLIFF documents into a .resx resource set.
/// </summary>
public static class CompileCommand
{
    /// <summary>
    /// Creates the command definition, binding it to the given compiler service.
    /// </summary>
    /// <param name="compiler">The service the command's handler delegates to.</param>
    /// <returns>The command.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="compiler"/> is null.</exception>
    public static Command Create(ICompilerService compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);

        var input = new Option<string>("--input", "-i")
        {
            Description = "An XLIFF file, or a directory whose top-level (non-recursive) *.xliff files are cooked.",
            Required = true
        };

        var output = new Option<string>("--output", "-o")
        {
            Description = "The directory the .resx files are written into.",
            DefaultValueFactory = _ => "."
        };

        var name = new Option<string?>("--name", "-n")
        {
            Description = "The resource base name; defaults to the first file's id."
        };

        var command = new Command("compile", "Cooks XLIFF documents into a .resx resource set.");
        command.Add(input);
        command.Add(output);
        command.Add(name);

        command.SetAction((parseResult, cancellationToken) => compiler.CompileAsync(
            parseResult.GetRequiredValue(input),
            parseResult.GetValue(output)!,
            parseResult.GetValue(name),
            cancellationToken));

        return command;
    }
}
