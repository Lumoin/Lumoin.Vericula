using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Loads the vendored Unicode MessageFormat 2.0 conformance suite (see the notice embedded from
/// <c>MessageFormatSuite/LDML48.2/NOTICE.md</c>, exposed here as <see cref="Notice"/>) from this
/// assembly's embedded resources, and exposes its cases, each merged with its file's shared
/// defaults, as one flat sequence.
/// </summary>
public static class MessageFormatSuite
{
    /// <summary>The greatest number of already-escaped source characters a display name shows.</summary>
    private const int MaxDisplaySourceLength = 60;

    /// <summary>The suite's licence text, decoded from the embedded <see cref="WellKnownSuiteResources.License"/> resource.</summary>
    public static string License { get; } = ReadResourceText(WellKnownSuiteResources.License);

    /// <summary>This vendoring's own notice, decoded from the embedded <see cref="WellKnownSuiteResources.Notice"/> resource.</summary>
    public static string Notice { get; } = ReadResourceText(WellKnownSuiteResources.Notice);

    /// <summary>The suite's own read-me, decoded from the embedded <see cref="WellKnownSuiteResources.Readme"/> resource.</summary>
    public static string Readme { get; } = ReadResourceText(WellKnownSuiteResources.Readme);

    /// <summary>Every case in the vendored suite, each merged with its file's defaults, in file then source order.</summary>
    public static ImmutableArray<SuiteCase> AllCases { get; } = LoadAllCases();

    /// <summary>
    /// Builds a human-readable, bounded-length display name for <paramref name="testCase"/>: its
    /// file and one-based index, followed by its source text with C0 control characters made visible
    /// and cut to a bounded length, so a suite of hundreds of generated test names stays legible.
    /// </summary>
    /// <param name="testCase">The case to name.</param>
    /// <returns>A display name of the form <c>tests/syntax.json#12 hello \tworld</c>.</returns>
    public static string DisplayName(SuiteCase testCase)
    {
        string escapedSrc = EscapeControlCharacters(testCase.Src);
        string clippedSrc = escapedSrc.Length > MaxDisplaySourceLength ? escapedSrc[..MaxDisplaySourceLength] : escapedSrc;

        return $"{testCase.File}#{testCase.Index} {clippedSrc}";
    }

    /// <summary>
    /// Loads and merges every case from every <c>tests/**/*.json</c> resource embedded in this
    /// assembly.
    /// </summary>
    /// <returns>Every suite case, in file then source order.</returns>
    private static ImmutableArray<SuiteCase> LoadAllCases()
    {
        Assembly assembly = typeof(MessageFormatSuite).Assembly;
        ImmutableArray<SuiteCase>.Builder cases = ImmutableArray.CreateBuilder<SuiteCase>();

        foreach(string resourceName in TestFileResourceNames(assembly))
        {
            SuiteFileDocument document = JsonSerializer.Deserialize(
                ReadResourceText(resourceName), MessageFormatSuiteJsonContext.Default.SuiteFileDocument)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' decoded to a null test file.");

            for(int index = 0; index < document.Tests.Length; index++)
            {
                cases.Add(MergeCase(resourceName, index + 1, document.Tests[index], document.DefaultTestProperties));
            }
        }

        return cases.ToImmutable();
    }

    /// <summary>
    /// Finds every embedded resource holding a vendored test file: everything whose logical name
    /// starts with the suite's <c>tests/</c> prefix and ends in <c>.json</c>, sorted ordinally so the
    /// load order, and hence <see cref="SuiteCase.Index"/> assignment within a run, is stable.
    /// Internal rather than private so a smoke test that must see each file's raw, unmerged JSON
    /// shape (to check the schema's own property-presence rules rather than this loader's merged
    /// output) can enumerate the same files without duplicating the discovery logic.
    /// </summary>
    /// <param name="assembly">The assembly to search.</param>
    /// <returns>The matching resource logical names, sorted ordinally.</returns>
    internal static ImmutableArray<string> TestFileResourceNames(Assembly assembly)
    {
        return [.. assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(WellKnownSuiteResources.TestFilePrefix, StringComparison.Ordinal)
                && name.EndsWith(WellKnownSuiteResources.TestFileSuffix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Folds one case's own properties over its file's <c>defaultTestProperties</c>: a property the
    /// case sets wins, an absent one comes from the defaults, and
    /// <see cref="SuiteCase.BidiIsolation"/> falls back to <see cref="WellKnownSuiteBidiIsolation.Default"/> when neither sets it.
    /// </summary>
    /// <param name="file">The vendored file's resource logical name.</param>
    /// <param name="index">The case's one-based position within <paramref name="file"/>.</param>
    /// <param name="testCase">The case's own JSON shape.</param>
    /// <param name="defaults">The file's shared defaults; <see langword="null"/> when the file sets none.</param>
    /// <returns>The merged case.</returns>
    private static SuiteCase MergeCase(string file, int index, SuiteRawCase testCase, SuiteRawCase? defaults)
    {
        string locale = testCase.Locale ?? defaults?.Locale
            ?? throw new InvalidOperationException($"{file}#{index} has no locale, on the case or on the file's defaults.");
        string src = testCase.Src ?? defaults?.Src
            ?? throw new InvalidOperationException($"{file}#{index} has no src, on the case or on the file's defaults.");

        return new SuiteCase(
            file,
            index,
            testCase.Description,
            locale,
            src,
            testCase.BidiIsolation ?? defaults?.BidiIsolation ?? WellKnownSuiteBidiIsolation.Default,
            [.. testCase.Params ?? defaults?.Params ?? []],
            [.. testCase.Tags ?? defaults?.Tags ?? []],
            testCase.Exp ?? defaults?.Exp,
            [.. testCase.ExpParts ?? defaults?.ExpParts ?? []],
            [.. (testCase.ExpErrors ?? defaults?.ExpErrors ?? []).Select(error => error.Type)],
            testCase.Only ?? false);
    }

    /// <summary>
    /// Reads an embedded resource's bytes as UTF-8 text. Internal rather than private for the same
    /// reason as <see cref="TestFileResourceNames"/>.
    /// </summary>
    /// <param name="logicalName">The resource's logical name, as set by its <c>LogicalName</c> project item metadata.</param>
    /// <returns>The resource's decoded text.</returns>
    internal static string ReadResourceText(string logicalName)
    {
        using Stream stream = typeof(MessageFormatSuite).Assembly.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded resource '{logicalName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Rewrites every C0 control character (U+0000 to U+001F) in <paramref name="text"/> into a
    /// visible escape: the familiar two-character form for newline, tab and carriage return, and a
    /// <c>\u</c> escape for every other one, so a suite case's source text is always safe to print on
    /// one line in a test name or failure message.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>The escaped text; the same instance when it contains no control character.</returns>
    private static string EscapeControlCharacters(string text)
    {
        StringBuilder? builder = null;

        for(int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            string? escape = character switch
            {
                '\n' => "\\n",
                '\t' => "\\t",
                '\r' => "\\r",
                < ' ' => $"\\u{(int)character:x4}",
                _ => null
            };

            if(escape is not null)
            {
                builder ??= new StringBuilder(text.Length + 16).Append(text, 0, index);
                builder.Append(escape);

                continue;
            }

            builder?.Append(character);
        }

        return builder?.ToString() ?? text;
    }
}
