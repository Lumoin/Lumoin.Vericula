using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Lumoin.Base;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Cooking;

/// <summary>
/// Cooks XLIFF documents into a standard .NET resource (.resx) set: one neutral
/// <c>{BaseName}.resx</c> carrying the source-language texts and one
/// <c>{BaseName}.{culture}.resx</c> satellite per target culture carrying its translations.
/// Resource names are the XLIFF unit ids and values are the folded segment text, so the output
/// drops straight into the framework's satellite-assembly and <c>IStringLocalizer</c> pipeline.
/// </summary>
public static class ResxCooker
{
    /// <summary>
    /// Cooks the given documents into a resx set. Sources fold into the neutral resource; each
    /// file that declares a target language contributes its translations to that culture's
    /// satellite. When several documents share unit ids the last one read wins, which lets a
    /// directory of per-language files (the usual layout) fold into one coherent set.
    /// </summary>
    /// <param name="documents">The source-of-truth documents to cook.</param>
    /// <param name="options">Options that shape the run, or null for defaults.</param>
    /// <returns>The neutral resource followed by one satellite per target culture, ordered by culture.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="documents"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// If two <c>&lt;file&gt;</c> elements of the same document both contribute a unit with the same
    /// id but different source text, or both translate the same unit id into the same culture with
    /// different target text (the same id, source or target, recurring across different documents,
    /// the per-language layout, is unaffected and still resolves last-write-wins); or if the effective base
    /// name's trailing dotted segment is itself a culture name, which would make MSBuild's
    /// AssignCulture mistake the neutral resource for a satellite.
    /// </exception>
    public static ImmutableArray<CookedResource> Cook(IReadOnlyCollection<XliffDocument> documents, ResxCookOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(documents);

        string? baseName = options?.BaseName;
        var neutral = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var cultures = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Tag neutralTag = Tag.Empty;
        var cultureTags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);

        foreach(XliffDocument document in documents)
        {
            //Contributing file id and source per unit id, reset for each document: a collision
            //between two files of the *same* document is a real authoring mistake and is refused
            //below, while the same id recurring across different documents is the documented
            //per-language layout and stays last-write-wins.
            var contributors = new Dictionary<string, (string FileId, string Source)>(StringComparer.Ordinal);

            //The same guard, scoped to (culture, unit id): two <file> elements of one document that
            //both translate the same unit into the same culture but disagree on the target text are
            //a real authoring mistake, refused below for the same reason as the source collision
            //above. The per-language layout (one culture per document) never revisits a key here, so
            //it is unaffected and still resolves last-write-wins across documents.
            var targetContributors = new Dictionary<(string Culture, string UnitId), (string FileId, string Target)>();

            foreach(XliffFile file in document.Files)
            {
                baseName ??= file.Id;
                string? culture = file.TargetLanguage?.Value;
                foreach(XliffUnit unit in EnumerateUnits(file))
                {
                    if(contributors.TryGetValue(unit.Id, out (string FileId, string Source) contributor)
                        && !string.Equals(contributor.FileId, file.Id, StringComparison.Ordinal)
                        && !string.Equals(contributor.Source, unit.Source, StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            $"Unit '{unit.Id}' is contributed by both file '{contributor.FileId}' and file '{file.Id}' with different source text.",
                            nameof(documents));
                    }

                    contributors[unit.Id] = (file.Id, unit.Source);

                    neutral[unit.Id] = unit.Source;

                    //Tracks the same last-write-wins order as the values themselves, so the resource
                    //carries the tag of whichever document most recently contributed to it.
                    neutralTag = document.Tag;

                    //An empty target is treated as untranslated: writing a blank satellite value would
                    //shadow the neutral source at runtime instead of falling back to it.
                    if(culture is not null && !string.IsNullOrEmpty(unit.Target))
                    {
                        (string Culture, string UnitId) targetKey = (culture, unit.Id);
                        if(targetContributors.TryGetValue(targetKey, out (string FileId, string Target) targetContributor)
                            && !string.Equals(targetContributor.FileId, file.Id, StringComparison.Ordinal)
                            && !string.Equals(targetContributor.Target, unit.Target, StringComparison.Ordinal))
                        {
                            throw new ArgumentException(
                                $"Unit '{unit.Id}' for culture '{culture}' is contributed by both file '{targetContributor.FileId}' and file '{file.Id}' with different target text.",
                                nameof(documents));
                        }

                        targetContributors[targetKey] = (file.Id, unit.Target);

                        TableFor(cultures, culture)[unit.Id] = unit.Target;
                        cultureTags[culture] = document.Tag;
                    }
                }
            }
        }

        if(baseName is null)
        {
            return ImmutableArray<CookedResource>.Empty;
        }

        //The .NET satellite convention reserves the {base}.{culture}.resx shape (ResxCookOptions.BaseName
        //promises {BaseName}.resx is the neutral resource): a base name whose own trailing dotted
        //segment is a culture name is ambiguous, since MSBuild's AssignCulture would parse it as a
        //satellite of that culture and no neutral resource would embed in the main assembly.
        int lastDot = baseName.LastIndexOf('.');
        string trailingSegment = lastDot >= 0 ? baseName[(lastDot + 1)..] : string.Empty;
        if(trailingSegment.Length > 0 && IsCulture(trailingSegment))
        {
            throw new ArgumentException(
                $"The base name '{baseName}' ends with '.{trailingSegment}', which MSBuild would treat as a culture suffix; the neutral resource would be mistaken for a satellite. Choose a different {nameof(ResxCookOptions)}.{nameof(ResxCookOptions.BaseName)}.");
        }

        var resources = ImmutableArray.CreateBuilder<CookedResource>(cultures.Count + 1);
        resources.Add(new CookedResource($"{baseName}.resx", BuildResx(neutral)) { Tag = neutralTag });
        foreach(KeyValuePair<string, SortedDictionary<string, string>> culture in cultures)
        {
            resources.Add(new CookedResource($"{baseName}.{culture.Key}.resx", BuildResx(culture.Value)) { Tag = cultureTags[culture.Key] });
        }

        return resources.ToImmutable();
    }

    /// <summary>
    /// Enumerates a file's own units, then every unit under its groups, depth-first, so a document with
    /// nested groups contributes every unit exactly once.
    /// </summary>
    /// <param name="file">The file to enumerate units from.</param>
    /// <returns>Every unit under the file, in document order.</returns>
    private static IEnumerable<XliffUnit> EnumerateUnits(XliffFile file)
    {
        foreach(XliffUnit unit in file.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup group in file.Groups)
        {
            foreach(XliffUnit unit in EnumerateUnits(group))
            {
                yield return unit;
            }
        }
    }

    /// <summary>
    /// Recursively enumerates a group's own units, then every unit of its nested groups, depth-first.
    /// </summary>
    /// <param name="group">The group to enumerate units from.</param>
    /// <returns>Every unit under the group, in document order.</returns>
    private static IEnumerable<XliffUnit> EnumerateUnits(XliffGroup group)
    {
        foreach(XliffUnit unit in group.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup nested in group.Groups)
        {
            foreach(XliffUnit unit in EnumerateUnits(nested))
            {
                yield return unit;
            }
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
    /// <c>Lumoin.Vericula.Cli.Services.CompilerService.IsCulture(string)</c> keeps its own copy of this
    /// check for the same reason; change both together.
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
    /// Gets the culture's translation table, creating and registering an empty one when the culture has
    /// not contributed a unit yet.
    /// </summary>
    /// <param name="cultures">The per-culture tables built so far, keyed by culture name.</param>
    /// <param name="culture">The culture to get the table for.</param>
    /// <returns>The culture's translation table.</returns>
    private static SortedDictionary<string, string> TableFor(
        SortedDictionary<string, SortedDictionary<string, string>> cultures,
        string culture)
    {
        if(!cultures.TryGetValue(culture, out SortedDictionary<string, string>? table))
        {
            table = new SortedDictionary<string, string>(StringComparer.Ordinal);
            cultures.Add(culture, table);
        }

        return table;
    }

    /// <summary>
    /// Builds one resx document's XML from a culture's or the neutral resource's entries. The document
    /// is schema-less: the resheaders alone are enough for the resource pipeline to read it, and the
    /// optional xsd schema is omitted to keep the artifact small. <see cref="XElement"/> escapes all
    /// data, so no entry needs its own escaping here.
    /// </summary>
    /// <param name="entries">The resource name to value entries to write, in sorted order.</param>
    /// <returns>The serialized resx document text.</returns>
    private static string BuildResx(SortedDictionary<string, string> entries)
    {
        var root = new XElement(
            WellKnownResxElements.Root,
            Resheader(WellKnownResxHeaderValues.ResMimeTypeHeader, WellKnownResxHeaderValues.ResMimeType),
            Resheader(WellKnownResxHeaderValues.VersionHeader, WellKnownResxHeaderValues.Version),
            Resheader(WellKnownResxHeaderValues.ReaderHeader, WellKnownResxHeaderValues.ReaderTypeName),
            Resheader(WellKnownResxHeaderValues.WriterHeader, WellKnownResxHeaderValues.WriterTypeName));

        foreach(KeyValuePair<string, string> entry in entries)
        {
            root.Add(new XElement(
                WellKnownResxElements.Data,
                new XAttribute(WellKnownResxAttributes.Name, entry.Key),
                new XAttribute(XNamespace.Xml + "space", "preserve"),
                new XElement(WellKnownResxElements.Value, entry.Value)));
        }

        return Serialize(new XDocument(root));
    }

    /// <summary>
    /// Builds one <c>resheader</c> element carrying a single-valued setting the resource pipeline reads
    /// before any data entry, such as the reader or writer type.
    /// </summary>
    /// <param name="name">The resheader's name.</param>
    /// <param name="value">The resheader's value.</param>
    /// <returns>The resheader element.</returns>
    private static XElement Resheader(string name, string value)
    {
        return new XElement(WellKnownResxElements.ResHeader, new XAttribute(WellKnownResxAttributes.Name, name), new XElement(WellKnownResxElements.Value, value));
    }

    /// <summary>
    /// Serializes a resx document to UTF-8 text without a byte order mark, with carriage returns
    /// entitized so they round-trip through <c>ResXResourceReader</c> byte-for-byte and line endings
    /// fixed to LF so the cooked bytes are identical across host platforms.
    /// </summary>
    /// <param name="document">The resx document to serialize.</param>
    /// <returns>The serialized document text.</returns>
    private static string Serialize(XDocument document)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,

            //Fix the indentation newline so the cooked bytes are identical across host platforms
            //(the default is Environment.NewLine, which differs between Windows and Linux/macOS),
            //mirroring XliffWriter.CreateWriterSettings.
            NewLineChars = "\n",

            //Entitize carriage returns rather than passing them through raw (NewLineHandling.None) or
            //rewriting every bare \n to \r\n (the Replace default): every conformant XML reader,
            //including ResXResourceReader, normalizes a literal CR or CRLF in text content to LF on
            //input per XML 1.0 section 2.11, so a raw CR is silently lost on the next read. Only the
            //character reference &#xD; survives that normalization, and Entitize leaves a bare LF
            //untouched, so multi-line translations round-trip byte-for-byte.
            NewLineHandling = NewLineHandling.Entitize,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var stream = new MemoryStream();
        using(XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetString(stream.ToArray());
    }
}
