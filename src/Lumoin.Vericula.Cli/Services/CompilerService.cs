using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Lumoin.Base;
using Lumoin.Vericula.Cooking;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Parsing;

namespace Lumoin.Vericula.Cli.Services;

/// <summary>
/// The default <see cref="ICompilerService"/> implementation. It is a thin adapter over the core
/// <see cref="XliffReader"/> and <see cref="ResxCooker"/>: read every input document, cook the set
/// once, and write each artifact. All localization logic lives in the core library; this type only
/// touches the file system and the console.
/// </summary>
[DebuggerDisplay("CompilerService")]
public sealed class CompilerService: ICompilerService
{
    /// <inheritdoc/>
    public async Task<int> CompileAsync(string inputPath, string outputPath, string? baseName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentNullException.ThrowIfNull(outputPath);

        string[] inputs = ResolveInputs(inputPath);
        if(inputs.Length == 0)
        {
            System.Console.Error.WriteLine($"No .xliff or .xlf files were found at '{inputPath}'.");

            return 1;
        }

        var documents = new List<XliffDocument>(inputs.Length);
        var failures = new List<string>();
        foreach(string path in inputs)
        {
            try
            {
                await using FileStream stream = File.OpenRead(path);
                XliffDocument document = await XliffReader.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
                documents.Add(document with { Tag = Tag.Create(new SourceLocation(Path.GetFullPath(path))) });
            }
            catch(Exception exception) when(exception is XliffFormatException or IOException or UnauthorizedAccessException)
            {
                //Collect every bad file so an author sees all problems in one pass instead of one per run.
                failures.Add($"  {path}: {exception.Message}");
            }
        }

        if(failures.Count > 0)
        {
            System.Console.Error.WriteLine($"Failed to read {failures.Count} file(s):");
            foreach(string failure in failures)
            {
                System.Console.Error.WriteLine(failure);
            }

            return 1;
        }

        string[] fileIds = documents
            .SelectMany(document => document.Files)
            .Select(file => file.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if(baseName is null && fileIds.Length > 1)
        {
            System.Console.Error.WriteLine(
                $"The inputs declare several file ids ({string.Join(", ", fileIds)}); pass --name to choose one resource base name.");

            return 1;
        }

        try
        {
            ValidateNames(documents, baseName, fileIds);
        }
        catch(ArgumentException exception)
        {
            System.Console.Error.WriteLine(exception.Message);

            return 1;
        }

        ImmutableArray<CookedResource> resources = ResxCooker.Cook(documents, new ResxCookOptions { BaseName = baseName });

        try
        {
            Directory.CreateDirectory(outputPath);
            RemoveStaleSatellites(outputPath, resources);
            foreach(CookedResource resource in resources)
            {
                string target = Path.Combine(outputPath, resource.FileName);
                await File.WriteAllTextAsync(target, resource.Content, cancellationToken).ConfigureAwait(false);
                System.Console.WriteLine($"Wrote {target}");
            }
        }
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
        {
            System.Console.Error.WriteLine($"Could not write to '{outputPath}': {exception.Message}");

            return 1;
        }

        System.Console.WriteLine($"Cooked {resources.Length} resource file(s) from {documents.Count} document(s).");

        return 0;
    }

    /// <summary>
    /// Lists the XLIFF inputs at <paramref name="inputPath"/>: the file itself when it names one, or
    /// every top-level (non-recursive) <c>*.xliff</c> and conventional <c>*.xlf</c> file in the
    /// directory it names, sorted so the (last-wins) folding of shared keys is reproducible regardless
    /// of file-system order.
    /// </summary>
    private static string[] ResolveInputs(string inputPath)
    {
        if(Directory.Exists(inputPath))
        {
            string[] files =
            [
                .. Directory.GetFiles(inputPath, "*.xliff", SearchOption.TopDirectoryOnly),
                .. Directory.GetFiles(inputPath, "*.xlf", SearchOption.TopDirectoryOnly)
            ];
            Array.Sort(files, StringComparer.Ordinal);

            return files;
        }

        if(File.Exists(inputPath))
        {
            return [inputPath];
        }

        return [];
    }

    /// <summary>
    /// Refuses a resource base name or target-language culture that is not safe to fold into a resx
    /// file name, before anything is cooked or written. A file id is only checked by the reader for
    /// being non-blank, so it could otherwise carry a path separator, <c>..</c>, <c>:</c> (the NTFS
    /// alternate-data-stream separator, valid in an XML NMTOKEN) or another character the file system
    /// rejects, steering a written path outside the output directory. A base name ending in a segment
    /// that is itself a culture name is refused too: MSBuild's AssignCulture would mistake the neutral
    /// resource for a satellite of that culture.
    /// </summary>
    /// <remarks>
    /// <see langword="internal"/>, not <see langword="private"/>, and directly exercised by tests: the
    /// reader validates every trgLang it accepts against BCP 47's shape (letters, digits and hyphens
    /// only), so a hostile value here can never actually arrive through <see cref="CompileAsync"/>'s
    /// own file-reading path once a document is built by <c>XliffReader</c>. The check on the target
    /// language stays as defense-in-depth against a document assembled some other way, and this seam
    /// is what lets it be proven directly rather than only through <see cref="CompileAsync"/>.
    /// </remarks>
    /// <param name="documents">The documents being cooked, whose files' target languages are checked.</param>
    /// <param name="baseName">The base name passed to the command, or null to fall back to the first file id.</param>
    /// <param name="fileIds">The distinct file ids across <paramref name="documents"/>.</param>
    /// <exception cref="ArgumentException">If a name is unsafe or a target language is not a recognized culture.</exception>
    internal static void ValidateNames(IReadOnlyCollection<XliffDocument> documents, string? baseName, string[] fileIds)
    {
        string? effectiveBaseName = baseName ?? (fileIds.Length > 0 ? fileIds[0] : null);
        if(effectiveBaseName is not null)
        {
            ValidateNameSegment(effectiveBaseName, "resource base name");

            int lastDot = effectiveBaseName.LastIndexOf('.');
            string trailing = lastDot >= 0 ? effectiveBaseName[(lastDot + 1)..] : string.Empty;
            if(trailing.Length > 0 && IsCulture(trailing))
            {
                throw new ArgumentException(
                    $"The resource base name '{effectiveBaseName}' ends with '.{trailing}', which MSBuild would treat as a culture suffix; the neutral resource would be mistaken for a satellite. Pass --name to choose a different base name.");
            }
        }

        var cultures = new HashSet<string>(StringComparer.Ordinal);
        foreach(XliffDocument document in documents)
        {
            foreach(XliffFile file in document.Files)
            {
                if(file.TargetLanguage?.Value is string culture)
                {
                    cultures.Add(culture);
                }
            }
        }

        foreach(string culture in cultures)
        {
            ValidateNameSegment(culture, "target language");

            if(!IsCulture(culture))
            {
                throw new ArgumentException($"The target language '{culture}' is not a recognized culture.");
            }
        }
    }

    /// <summary>
    /// Refuses a candidate file-name segment that carries a path separator, <c>..</c>, <c>:</c> or any
    /// <see cref="Path.GetInvalidFileNameChars"/> character.
    /// </summary>
    /// <param name="value">The segment to check.</param>
    /// <param name="what">What the segment is, for the exception message (for example "resource base name").</param>
    /// <exception cref="ArgumentException">If the segment is unsafe.</exception>
    private static void ValidateNameSegment(string value, string what)
    {
        if(value.Contains("..", StringComparison.Ordinal)
            || value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar)
            || value.Contains(':')
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"The {what} '{value}' contains a character that cannot appear in a file name.");
        }
    }

    /// <summary>
    /// Reports whether <paramref name="value"/> is the name of a predefined culture, using
    /// <see cref="CultureInfo.GetCultureInfo(string, bool)"/> with <c>predefinedOnly: true</c> so the check
    /// agrees between platforms: without that flag, ICU (Linux, macOS) synthesizes a culture for any
    /// well-formed name, while NLS (Windows) already throws for a name it does not know.
    /// </summary>
    /// <param name="value">The candidate culture name.</param>
    /// <returns>True when the value names a predefined culture; false otherwise.</returns>
    /// <remarks>
    /// <c>Lumoin.Vericula.Cooking.ResxCooker.IsCulture(string)</c> keeps its own copy of this check for
    /// the same reason; change both together.
    /// </remarks>
    private static bool IsCulture(string value)
    {
        try
        {
            CultureInfo.GetCultureInfo(value, predefinedOnly: true);

            return true;
        }
        catch(CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Removes satellites left over from a language that was deleted from the source-of-truth, so a
    /// pre-build cook over a tool-owned output directory does not ship stale translations. Only a file
    /// this cooker could itself have produced is ever removed: the name must be exactly
    /// <c>{base}.{culture}.resx</c> with a middle segment that parses as a culture, so a hand-maintained
    /// resx or an unrelated <c>*.Designer.resx</c> in the output directory is left untouched.
    /// </summary>
    /// <param name="outputPath">The directory to remove stale satellites from.</param>
    /// <param name="resources">The resources this cook produced, whose file names are kept.</param>
    private static void RemoveStaleSatellites(string outputPath, ImmutableArray<CookedResource> resources)
    {
        if(resources.Length == 0)
        {
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(resources[0].FileName);
        string prefix = $"{baseName}.";
        var produced = resources.Select(resource => resource.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(string existing in Directory.GetFiles(outputPath, $"{baseName}.*.resx"))
        {
            string name = Path.GetFileName(existing);
            if(produced.Contains(name) || !IsStaleSatellite(name, prefix))
            {
                continue;
            }

            File.Delete(existing);
        }
    }

    /// <summary>
    /// Reports whether <paramref name="name"/> is exactly <c>{prefix}{culture}.resx</c> for a culture
    /// <see cref="IsCulture"/> accepts, the only shape a produced satellite can have.
    /// </summary>
    /// <param name="name">The candidate file name.</param>
    /// <param name="prefix">The base name including its trailing dot.</param>
    /// <returns>True when the name is a satellite this cooker could have produced.</returns>
    private static bool IsStaleSatellite(string name, string prefix)
    {
        if(!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !name.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string culture = name[prefix.Length..^".resx".Length];

        return !culture.Contains('.', StringComparison.Ordinal) && IsCulture(culture);
    }
}
