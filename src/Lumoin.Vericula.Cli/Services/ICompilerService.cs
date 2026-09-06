namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// Cooks XLIFF documents into build artifacts on behalf of the console command and the MCP tool.
/// </summary>
public interface ICompilerService
{
    /// <summary>
    /// Reads the XLIFF documents at <paramref name="inputPath"/> (a single file or a directory of
    /// <c>*.xliff</c> files) and writes the cooked .resx set into <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="inputPath">The file or directory to cook.</param>
    /// <param name="outputPath">The directory the .resx files are written into; it is created if absent.</param>
    /// <param name="baseName">The resource base name, or null to take it from the first file's id.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>An exit-code style result; zero means success.</returns>
    Task<int> CompileAsync(string inputPath, string outputPath, string? baseName, CancellationToken cancellationToken);
}
