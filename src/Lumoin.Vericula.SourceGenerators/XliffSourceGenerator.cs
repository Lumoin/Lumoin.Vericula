using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// Generates one strongly typed accessor class from the XLIFF documents supplied as
/// additional files. The class's source language is the <c>srcLang</c> of the first document in
/// path order; every document that shares it contributes its source-language texts and, when it
/// declares a target language, its target-language texts, while a document declaring a different
/// source language is reported through <see cref="XliffDiagnostics.SourceLanguageMismatch"/> and
/// excluded, rather than silently folded in with no accessor of its own. The generated class
/// resolves against the current UI culture and falls back to the source language. Every pipeline
/// stage exchanges value-equatable records so the incremental cache stays hot across keystrokes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class XliffSourceGenerator: IIncrementalGenerator
{
    /// <summary>The name stamped into the generated class's <c>GeneratedCodeAttribute</c>.</summary>
    private const string GeneratorName = "Lumoin.Vericula.SourceGenerators";

    /// <summary>The version stamped into the generated class's <c>GeneratedCodeAttribute</c>.</summary>
    private const string GeneratorVersion = "1.0.0";

    /// <summary>The MSBuild analyzer config key that carries the consuming project's <c>RootNamespace</c> property.</summary>
    private const string RootNamespaceOption = "build_property.RootNamespace";

    /// <summary>
    /// Matches a well-formed BCP 47 language tag: a 2-to-8-letter primary subtag followed by any
    /// number of 1-to-8 alphanumeric subtags, each separated by a hyphen. Used to validate
    /// <c>srcLang</c> and <c>trgLang</c> per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#srcLang">XLIFF 2.1, srcLang</see>.
    /// </summary>
    private static readonly Regex LanguageTagPattern = new(
        @"^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$", RegexOptions.Compiled);

    /// <summary>
    /// Matches a language tag embedded in a file name immediately before the final extension, such as
    /// the <c>fr</c> in <c>strings.fr.xliff</c>, so the emitted file name can be compared against the
    /// document's declared <c>trgLang</c> and a mismatch reported as a warning.
    /// </summary>
    private static readonly Regex FileNameLanguageHintPattern = new(
        @"\.([A-Za-z]{2,8}(?:-[A-Za-z0-9]{1,8})*)\.[^.]+$", RegexOptions.Compiled);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Select XLIFF additional files.
        var xliffFiles = context.AdditionalTextsProvider
            .Where(static additional => additional.Path.EndsWith(
                ".xliff", StringComparison.OrdinalIgnoreCase))
            .WithTrackingName(WellKnownGeneratorSteps.XliffAdditionalFiles);

        //Read each file into a value-equatable source record.
        var xliffSources = xliffFiles.Select(static (additional, cancellationToken) =>
            new XliffSource(
                Path: additional.Path,
                Text: additional.GetText(cancellationToken)?.ToString() ?? string.Empty))
            .WithTrackingName(WellKnownGeneratorSteps.XliffSource);

        //Parse each source into an equatable document model.
        var documents = xliffSources.Select(static (source, cancellationToken) =>
            ParseDocument(source, cancellationToken))
            .WithTrackingName(WellKnownGeneratorSteps.ParsedDocument);

        //Pull the assembly name once and reuse.
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName ?? "App")
            .WithTrackingName(WellKnownGeneratorSteps.AssemblyName);

        //The consuming project's RootNamespace MSBuild property, when the host set one.
        var rootNamespaceOption = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => provider.GlobalOptions.TryGetValue(RootNamespaceOption, out string? value) ? value : null)
            .WithTrackingName(WellKnownGeneratorSteps.RootNamespace);

        //The namespace the generated class is emitted into: RootNamespace when the host declares
        //one, otherwise a sanitized assembly name.
        var namespaceModel = rootNamespaceOption.Combine(assemblyName)
            .Select(static (pair, _) => BuildNamespace(pair.Left, pair.Right))
            .WithTrackingName(WellKnownGeneratorSteps.Namespace);

        //All documents fold into one accessor class, so emission needs the full set.
        var model = documents.Collect().Combine(namespaceModel)
            .WithTrackingName(WellKnownGeneratorSteps.CollectedModel);

        //Emit the accessor class and any parse diagnostics.
        context.RegisterSourceOutput(model,
            static (productionContext, pair) => Emit(productionContext, pair.Left, pair.Right));
    }

    /// <summary>
    /// Parses one XLIFF additional file's text into an equatable document model: its declared
    /// languages, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#srcLang">XLIFF 2.1, srcLang</see>
    /// and <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#trgLang">XLIFF 2.1, trgLang</see>,
    /// and its units, folding each unit's segments and ignorables into source and target text. A
    /// unit's target text is kept only when every segment carries a non-empty target that is not
    /// marked as still needing translation; otherwise the unit's <see cref="UnitModel.TargetText"/> is
    /// <see langword="null"/> so the generated accessor falls back to the source language instead of
    /// exposing a partial translation. Any structural problem returns a document with
    /// <see cref="XliffDocumentModel.Error"/> set rather than throwing, so one malformed file does not
    /// abort the whole pipeline run.
    /// </summary>
    /// <param name="source">The additional file's path and raw text.</param>
    /// <param name="cancellationToken">Token observed between units so a long parse can be abandoned promptly.</param>
    private static XliffDocumentModel ParseDocument(
        XliffSource source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var stringReader = new StringReader(source.Text);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            var document = XDocument.Load(xmlReader, LoadOptions.SetLineInfo);
            var root = document.Root;
            XNamespace core = WellKnownXliffNamespaces.Core;
            if(root is null || root.Name != core + WellKnownXliffElements.Xliff)
            {
                return Failed(source.Path, "The document root is not an <xliff> element in the XLIFF 2.x core namespace.", root);
            }

            string? sourceLanguage = root.Attribute(WellKnownXliffAttributes.SourceLanguage)?.Value;
            if(string.IsNullOrWhiteSpace(sourceLanguage))
            {
                return Failed(source.Path, "The <xliff> element does not declare srcLang.", root);
            }

            if(!LanguageTagPattern.IsMatch(sourceLanguage))
            {
                return Failed(source.Path, $"srcLang '{sourceLanguage}' is not a valid BCP 47 language tag.", root);
            }

            string? targetLanguage = root.Attribute(WellKnownXliffAttributes.TargetLanguage)?.Value;
            targetLanguage = string.IsNullOrWhiteSpace(targetLanguage) ? null : targetLanguage;
            if(targetLanguage is not null && !LanguageTagPattern.IsMatch(targetLanguage))
            {
                return Failed(source.Path, $"trgLang '{targetLanguage}' is not a valid BCP 47 language tag.", root);
            }

            var units = ImmutableArray.CreateBuilder<UnitModel>();
            bool anyTargetSeen = false;
            foreach(XElement unit in root.Descendants(core + WellKnownXliffElements.Unit))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? id = unit.Attribute(WellKnownXliffAttributes.Id)?.Value;
                if(string.IsNullOrWhiteSpace(id))
                {
                    return Failed(source.Path, "A <unit> element does not declare an id.", unit);
                }

                var sourceBuilder = new StringBuilder();
                var targetBuilder = new StringBuilder();
                bool sawSegment = false;
                bool translationComplete = true;
                foreach(XElement child in unit.Elements())
                {
                    if(!WellKnownXliffNamespaces.IsCore(child.Name.NamespaceName))
                    {
                        continue;
                    }

                    bool isSegment = WellKnownXliffElements.IsSegment(child.Name.LocalName);
                    bool isIgnorable = !isSegment && WellKnownXliffElements.IsIgnorable(child.Name.LocalName);
                    if(!isSegment && !isIgnorable)
                    {
                        continue;
                    }

                    if(!TryReadContent(child.Element(core + WellKnownXliffElements.Source), id!, out string sourceText, out string? failure))
                    {
                        return Failed(source.Path, failure!, unit);
                    }

                    sourceBuilder.Append(sourceText);

                    XElement? targetElement = child.Element(core + WellKnownXliffElements.Target);
                    if(targetElement is not null)
                    {
                        anyTargetSeen = true;
                    }

                    if(!TryReadContent(targetElement, id!, out string targetText, out failure))
                    {
                        return Failed(source.Path, failure!, unit);
                    }

                    if(isSegment)
                    {
                        sawSegment = true;
                        bool nonEmptyTarget = targetElement is not null && targetText.Length > 0;
                        bool needsTranslation = WellKnownXliffAttributeValues.IsStateInitial(child.Attribute(WellKnownXliffAttributes.State)?.Value)
                            && WellKnownVericulaMetadata.IsNeedsTranslationSubState(child.Attribute(WellKnownXliffAttributes.SubState)?.Value);
                        if(!nonEmptyTarget || needsTranslation)
                        {
                            translationComplete = false;
                        }

                        targetBuilder.Append(targetText);
                    }
                    else
                    {
                        targetBuilder.Append(targetElement is null ? sourceText : targetText);
                    }
                }

                bool isTranslated = sawSegment && translationComplete;
                units.Add(new UnitModel(id!, sourceBuilder.ToString(), isTranslated ? targetBuilder.ToString() : null));
            }

            if(targetLanguage is null && anyTargetSeen)
            {
                return Failed(source.Path, "The document declares <target> elements but the <xliff> element does not declare trgLang.", root);
            }

            var warnings = ImmutableArray.CreateBuilder<string>();
            string fileName = GetFileName(source.Path);
            Match hint = FileNameLanguageHintPattern.Match(fileName);
            if(hint.Success && targetLanguage is not null && !string.Equals(hint.Groups[1].Value, targetLanguage, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"The file name '{fileName}' suggests target language '{hint.Groups[1].Value}', but the document declares trgLang '{targetLanguage}'; trgLang is used.");
            }

            //Line and column stay 0 on a successful parse: DocumentLocation is only ever consulted
            //for a document whose Error is set, and a position here would make this record's
            //equality sensitive to edits elsewhere in the file that shift the root element's own
            //line, which would needlessly invalidate the incremental cache for every such edit.
            return new XliffDocumentModel(
                source.Path,
                sourceLanguage!,
                targetLanguage,
                units.ToImmutable(),
                Error: null,
                Line: 0,
                Column: 0,
                warnings.ToImmutable());
        }
        catch(XmlException exception)
        {
            return Failed(source.Path, exception.Message, locatedAt: null);
        }
    }

    /// <summary>
    /// Folds a <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element's text content, refusing when it
    /// contains inline markup <see cref="WellKnownXliffElements.IsUnsupportedInlineMarkup"/> flags: such
    /// markup carries data (an id, a data reference, a literal code point) that
    /// <see cref="XElement.Value"/> would silently discard, and the generator would rather fail the
    /// document than emit an accessor with lossy text.
    /// </summary>
    /// <param name="element">The <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element to read, or <see langword="null"/> when absent.</param>
    /// <param name="unitId">The enclosing unit's id, used in the failure message.</param>
    /// <param name="text">The folded text, or empty when <paramref name="element"/> is <see langword="null"/> or reading failed.</param>
    /// <param name="failure">The failure message naming the unsupported markup, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> if the content was read without loss; otherwise, <see langword="false"/>.</returns>
    private static bool TryReadContent(XElement? element, string unitId, out string text, out string? failure)
    {
        if(element is null)
        {
            text = string.Empty;
            failure = null;

            return true;
        }

        foreach(XElement descendant in element.DescendantsAndSelf())
        {
            if(descendant != element
                && WellKnownXliffNamespaces.IsCore(descendant.Name.NamespaceName)
                && WellKnownXliffElements.IsUnsupportedInlineMarkup(descendant.Name.LocalName))
            {
                text = string.Empty;
                failure = $"Inline markup <{descendant.Name.LocalName}> in unit '{unitId}' is not supported by the generator; the document was not turned into accessors.";

                return false;
            }
        }

        text = element.Value;
        failure = null;

        return true;
    }

    /// <summary>
    /// Turns the collected documents into the single generated <c>Translations</c> class: reports the
    /// namespace's own warnings, then reports a parse failure or file-name warning for each affected
    /// document, then reports and excludes any document whose <c>srcLang</c> differs from the first
    /// document's in path order, builds one translation table per distinct language across the
    /// remaining documents, resolves each unit id to a generated accessor name, and finally emits the
    /// accessor source. Nothing is added to the compilation when every document failed to parse or
    /// none shares the accepted source language.
    /// </summary>
    /// <param name="context">The source production context diagnostics are reported to and generated source is added through.</param>
    /// <param name="documents">Every additional file's parsed document model, valid or failed.</param>
    /// <param name="namespaceModel">The namespace the generated class is emitted into, and any warnings raised deriving it.</param>
    private static void Emit(
        SourceProductionContext context,
        ImmutableArray<XliffDocumentModel> documents,
        NamespaceModel namespaceModel)
    {
        foreach(string warning in namespaceModel.Warnings)
        {
            context.ReportDiagnostic(Diagnostic.Create(XliffDiagnostics.InvalidNamespaceSegment, Location.None, warning));
        }

        if(documents.Length == 0)
        {
            return;
        }

        //Order by path so emission is deterministic regardless of provider ordering.
        var ordered = documents.Sort(static (left, right) => string.CompareOrdinal(left.Path, right.Path));
        var valid = new List<XliffDocumentModel>(ordered.Length);
        foreach(XliffDocumentModel document in ordered)
        {
            if(document.Error is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    XliffDiagnostics.ParseFailure, DocumentLocation(document), document.Path, document.Error));
                continue;
            }

            foreach(string warning in document.Warnings)
            {
                context.ReportDiagnostic(Diagnostic.Create(XliffDiagnostics.LanguageTagFileNameMismatch, DocumentRootLocation(document), warning));
            }

            valid.Add(document);
        }

        if(valid.Count == 0)
        {
            return;
        }

        //Every document declares its own srcLang (XLIFF 2.1 §4.2.2.1), and nothing requires a
        //project's files to share one; but the generated class has one accessor surface, so it takes
        //the source language of the first document in path order and reports, rather than silently
        //drops, any document that declares a different one.
        string sourceLanguageKey = NormalizeLanguageTag(valid[0].SourceLanguage);
        var accepted = new List<XliffDocumentModel>(valid.Count);
        foreach(XliffDocumentModel document in valid)
        {
            if(!string.Equals(NormalizeLanguageTag(document.SourceLanguage), sourceLanguageKey, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    XliffDiagnostics.SourceLanguageMismatch, DocumentRootLocation(document), document.Path, document.SourceLanguage, valid[0].Path, valid[0].SourceLanguage));

                continue;
            }

            accepted.Add(document);
        }

        if(accepted.Count == 0)
        {
            return;
        }

        var reservedNames = new HashSet<string>(StringComparer.Ordinal) { "Resolve", "Languages", "Translations" };
        var languages = new SortedDictionary<string, LanguageTable>(StringComparer.Ordinal);

        //Register every language table up front, from the document languages alone, so the field
        //Resolve falls back to always exists (regardless of whether any unit id survives) and so
        //every reserved member name is known before unit ids are checked against it.
        foreach(XliffDocumentModel document in accepted)
        {
            EnsureLanguageTable(languages, document.SourceLanguage, reservedNames);
            if(document.TargetLanguage is not null)
            {
                EnsureLanguageTable(languages, document.TargetLanguage, reservedNames);
            }
        }

        string sourceField = languages[sourceLanguageKey].FieldName;

        var accessorNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach(XliffDocumentModel document in accepted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            LanguageTable sourceTable = languages[NormalizeLanguageTag(document.SourceLanguage)];
            LanguageTable? targetTable = document.TargetLanguage is null ? null : languages[NormalizeLanguageTag(document.TargetLanguage)];
            foreach(UnitModel unit in document.Units)
            {
                if(!accessorNames.TryGetValue(unit.Id, out string? accessorName))
                {
                    if(!TryResolveAccessorName(unit.Id, reservedNames, out accessorName, out string? skipReason))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            XliffDiagnostics.InvalidUnitId, Location.None, unit.Id, document.Path, skipReason));

                        continue;
                    }

                    accessorNames.Add(unit.Id, accessorName);
                }

                AddText(context, sourceTable, unit.Id, unit.SourceText, document.Path);
                if(targetTable is not null && unit.TargetText is not null)
                {
                    AddText(context, targetTable, unit.Id, unit.TargetText, document.Path);
                }
            }
        }

        string code = BuildAccessorSource(namespaceModel.Value, sourceField, languages, accessorNames);
        context.AddSource("XliffTranslations.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    /// <summary>
    /// Renders the generated <c>Translations</c> class as C# source text: one dictionary field per
    /// language, the language lookup keyed by BCP 47 tag, the culture-fallback <c>Resolve</c> method,
    /// and one string-returning accessor property per resolvable unit id in the source language.
    /// </summary>
    /// <param name="namespaceName">The namespace the class is emitted into.</param>
    /// <param name="sourceField">The field name of the source-language table, used as <c>Resolve</c>'s final fallback.</param>
    /// <param name="languages">Every language table, keyed by normalized BCP 47 tag.</param>
    /// <param name="accessorNames">Every resolvable unit id mapped to its generated accessor property name.</param>
    /// <returns>The complete generated source file text.</returns>
    private static string BuildAccessorSource(
        string namespaceName,
        string sourceField,
        SortedDictionary<string, LanguageTable> languages,
        Dictionary<string, string> accessorNames)
    {
        //C# 7.3, which a netstandard2.0-targeting consumer with no explicit LangVersion defaults to,
        //has no file-scoped namespace declaration (that needs C# 10); the class is built into its
        //own block here and indented one level into a braced namespace instead.
        var builder = new StringBuilder();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Strongly typed access to the translations carried by the project's XLIFF documents.");
        builder.AppendLine("/// <see cref=\"Resolve\"/> tries the exact current UI culture, then each of its parent");
        builder.AppendLine("/// cultures in turn, then the culture's neutral primary subtag, then the source");
        builder.AppendLine("/// language, and finally returns the key itself.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{GeneratorName}\", \"{GeneratorVersion}\")]");
        builder.AppendLine("public static class Translations");
        builder.AppendLine("{");

        //One table per language, named after its full BCP 47 tag so that, for example, pt-BR and
        //pt-PT or zh-Hans and zh-Hant each keep their own table instead of overwriting one another.
        foreach(KeyValuePair<string, LanguageTable> language in languages)
        {
            builder.AppendLine($"    private static readonly global::System.Collections.Generic.Dictionary<string, string> {language.Value.FieldName} = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal)");
            builder.AppendLine("    {");
            foreach(KeyValuePair<string, string> entry in language.Value.Entries)
            {
                string literal = SymbolDisplay.FormatLiteral(entry.Value, quote: true);
                string keyLiteral = SymbolDisplay.FormatLiteral(entry.Key, quote: true);
                builder.AppendLine($"        [{keyLiteral}] = {literal},");
            }

            builder.AppendLine("    };");
            builder.AppendLine();
        }

        //The language map keyed by the full, lowercased BCP 47 tag.
        builder.AppendLine("    private static readonly global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.Dictionary<string, string>> Languages = new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.Dictionary<string, string>>(global::System.StringComparer.OrdinalIgnoreCase)");
        builder.AppendLine("    {");
        foreach(KeyValuePair<string, LanguageTable> language in languages)
        {
            string keyLiteral = SymbolDisplay.FormatLiteral(language.Key, quote: true);
            builder.AppendLine($"        [{keyLiteral}] = {language.Value.FieldName},");
        }

        builder.AppendLine("    };");
        builder.AppendLine();

        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Resolves the translation for the given unit id: the current UI culture, then its");
        builder.AppendLine("    /// parent cultures, then the neutral primary-subtag table, then the source language,");
        builder.AppendLine("    /// and finally the id itself.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static string Resolve(string key)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.Globalization.CultureInfo culture = global::System.Globalization.CultureInfo.CurrentUICulture;");
        builder.AppendLine("        while(culture != null && !global::System.String.IsNullOrEmpty(culture.Name))");
        builder.AppendLine("        {");
        builder.AppendLine("            global::System.Collections.Generic.Dictionary<string, string> table;");
        builder.AppendLine("            string value;");
        builder.AppendLine("            if(Languages.TryGetValue(culture.Name, out table) && table.TryGetValue(key, out value))");
        builder.AppendLine("            {");
        builder.AppendLine("                return value;");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            culture = culture.Parent;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        string neutral = global::System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;");
        builder.AppendLine("        global::System.Collections.Generic.Dictionary<string, string> neutralTable;");
        builder.AppendLine("        string neutralValue;");
        builder.AppendLine("        if(Languages.TryGetValue(neutral, out neutralTable) && neutralTable.TryGetValue(key, out neutralValue))");
        builder.AppendLine("        {");
        builder.AppendLine("            return neutralValue;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        string fallback;");
        builder.AppendLine($"        return {sourceField}.TryGetValue(key, out fallback) ? fallback : key;");
        builder.AppendLine("    }");

        //One accessor per unit in the source language, so missing translations
        //still resolve and missing sources are caught at compile time at call sites.
        foreach(KeyValuePair<string, string> entry in languages[NormalizeLanguageTagFromField(sourceField, languages)].Entries)
        {
            string accessorName = accessorNames[entry.Key];
            string keyLiteral = SymbolDisplay.FormatLiteral(entry.Key, quote: true);
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// Gets the translation of \"{EscapeXmlDocumentation(entry.Value)}\".");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public static string {accessorName} => Resolve({keyLiteral});");
        }

        builder.AppendLine("}");

        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/>");
        source.AppendLine($"//Generated by {GeneratorName} from the project's .xliff additional files.");
        source.AppendLine();
        source.AppendLine($"namespace {namespaceName}.Localization");
        source.AppendLine("{");
        source.Append(IndentLines(builder.ToString(), "    "));
        source.AppendLine("}");

        return source.ToString();
    }

    /// <summary>
    /// Prefixes every non-empty line of <paramref name="text"/> with <paramref name="indent"/>,
    /// leaving blank lines untouched, so a block built at its own top-level indentation can be
    /// nested one level deeper without re-authoring every literal it was written with.
    /// </summary>
    /// <param name="text">The text to indent, with either line-ending style.</param>
    /// <param name="indent">The indentation to prefix each non-empty line with.</param>
    /// <returns>The indented text, with line endings normalized to <c>\n</c>.</returns>
    private static string IndentLines(string text, string indent)
    {
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for(int index = 0; index < lines.Length; index++)
        {
            if(lines[index].Length > 0)
            {
                lines[index] = indent + lines[index];
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Recovers a language table's normalized BCP 47 tag from its generated field name, the reverse of
    /// <see cref="LanguageFieldName"/>, so the source-language accessor loop can find its own table's
    /// entries by the field name it was already given.
    /// </summary>
    /// <param name="fieldName">The generated field name to look up.</param>
    /// <param name="languages">Every language table, keyed by normalized BCP 47 tag.</param>
    /// <returns>The normalized tag whose table has this field name, or empty when none matches.</returns>
    private static string NormalizeLanguageTagFromField(string fieldName, SortedDictionary<string, LanguageTable> languages)
    {
        foreach(KeyValuePair<string, LanguageTable> language in languages)
        {
            if(string.Equals(language.Value.FieldName, fieldName, StringComparison.Ordinal))
            {
                return language.Key;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Turns a unit id into the name of the C# property that will expose it, refusing an id that is
    /// not a valid identifier or that collides with a member the generated class already declares,
    /// since either would fail to compile or silently shadow an existing accessor.
    /// </summary>
    /// <param name="id">The unit id to turn into an accessor name.</param>
    /// <param name="reservedNames">Every name already claimed by a generated member or language field.</param>
    /// <param name="accessorName">The accessor name, escaped with <c>@</c> when the id is a C# keyword; empty on failure.</param>
    /// <param name="skipReason">The reason the id was skipped, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> if the id could be turned into an accessor name; otherwise, <see langword="false"/>.</returns>
    private static bool TryResolveAccessorName(string id, HashSet<string> reservedNames, out string accessorName, out string? skipReason)
    {
        if(!SyntaxFacts.IsValidIdentifier(id))
        {
            accessorName = string.Empty;
            skipReason = "is not a valid C# identifier, so no accessor was generated for it";

            return false;
        }

        if(reservedNames.Contains(id))
        {
            accessorName = string.Empty;
            skipReason = "collides with a member the generated class already declares, so no accessor was generated for it";

            return false;
        }

        accessorName = SyntaxFacts.GetKeywordKind(id) == SyntaxKind.None ? id : "@" + id;
        skipReason = null;

        return true;
    }

    /// <summary>
    /// Adds one unit id's text to a language table, reporting <see cref="XliffDiagnostics.InvalidUnitId"/>
    /// instead of overwriting when the id already carries different text from an earlier document: the
    /// first occurrence wins and every later conflicting one is skipped, so the generated accessor's
    /// text stays deterministic regardless of provider ordering.
    /// </summary>
    /// <param name="context">The source production context conflict diagnostics are reported to.</param>
    /// <param name="table">The language table to add the entry to.</param>
    /// <param name="id">The unit id.</param>
    /// <param name="text">The folded text for this language.</param>
    /// <param name="documentPath">The path of the document this occurrence came from.</param>
    private static void AddText(SourceProductionContext context, LanguageTable table, string id, string text, string documentPath)
    {
        if(table.Entries.TryGetValue(id, out string? existingText))
        {
            if(!string.Equals(existingText, text, StringComparison.Ordinal))
            {
                string firstPath = table.SourcePaths.TryGetValue(id, out string? path) ? path : documentPath;
                context.ReportDiagnostic(Diagnostic.Create(
                    XliffDiagnostics.InvalidUnitId,
                    Location.None,
                    id,
                    documentPath,
                    $"was already generated from '{firstPath}' with different text; this occurrence was skipped"));
            }

            return;
        }

        table.Entries[id] = text;
        table.SourcePaths[id] = documentPath;
    }

    /// <summary>
    /// Registers a language table for the given tag if one does not already exist, so every document's
    /// declared source and target languages have a table before any unit is folded into one, regardless
    /// of which document is processed first.
    /// </summary>
    /// <param name="languages">Every language table registered so far, keyed by normalized BCP 47 tag.</param>
    /// <param name="originalTag">The language tag as declared in the document, before normalization.</param>
    /// <param name="reservedNames">Every name already claimed by a generated member or language field; the new field name is added to it.</param>
    private static void EnsureLanguageTable(SortedDictionary<string, LanguageTable> languages, string originalTag, HashSet<string> reservedNames)
    {
        string key = NormalizeLanguageTag(originalTag);
        if(languages.ContainsKey(key))
        {
            return;
        }

        string fieldName = LanguageFieldName(originalTag);
        languages.Add(key, new LanguageTable(fieldName));
        reservedNames.Add(fieldName);
    }

    /// <summary>
    /// Builds a failed <see cref="XliffDocumentModel"/> anchored at the given element's line and
    /// column, so <see cref="XliffDiagnostics.ParseFailure"/> can point the developer at the offending
    /// part of the document instead of only naming the file.
    /// </summary>
    /// <param name="path">The additional file's path.</param>
    /// <param name="error">The parse failure message.</param>
    /// <param name="locatedAt">The element the failure is anchored to, or <see langword="null"/> when none is available.</param>
    /// <returns>A document model with <see cref="XliffDocumentModel.Error"/> set and no units.</returns>
    private static XliffDocumentModel Failed(string path, string error, XElement? locatedAt)
    {
        var (line, column) = LineInfoOf(locatedAt);

        return new XliffDocumentModel(
            path,
            SourceLanguage: string.Empty,
            TargetLanguage: null,
            Units: ImmutableArray<UnitModel>.Empty,
            Error: error,
            line,
            column,
            Warnings: ImmutableArray<string>.Empty);
    }

    /// <summary>
    /// Reads an element's one-based line and column from its <see cref="IXmlLineInfo"/>, when the
    /// document was loaded with <see cref="LoadOptions.SetLineInfo"/> and the information is available.
    /// </summary>
    /// <param name="element">The element to read position information from, or <see langword="null"/>.</param>
    /// <returns>The element's line and column, or <c>(0, 0)</c> when no position information is available.</returns>
    private static (int Line, int Column) LineInfoOf(XElement? element)
    {
        if(element is IXmlLineInfo info && info.HasLineInfo())
        {
            return (info.LineNumber, info.LinePosition);
        }

        return (0, 0);
    }

    /// <summary>
    /// Turns a document's stored path, line and column into the <see cref="Location"/> a diagnostic is
    /// reported at.
    /// </summary>
    /// <param name="document">The document to build a location for.</param>
    /// <returns>The document's location, or <see cref="Location.None"/> when it carries no line.</returns>
    private static Location DocumentLocation(XliffDocumentModel document)
    {
        return LocationOf(document.Path, document.Line, document.Column);
    }

    /// <summary>
    /// Turns a document's stored path, line and column into the <see cref="Location"/> a diagnostic is
    /// reported at, the same way <see cref="DocumentLocation"/> does for <see cref="XliffDiagnostics.ParseFailure"/>,
    /// but falling back to the document's root (line 1, column 1) instead of <see cref="Location.None"/>
    /// when the document carries no better position: a document that parsed successfully always has
    /// <see cref="XliffDocumentModel.Line"/> 0 (see <see cref="ParseDocument"/>'s own remark on why), so
    /// a warning about it - unlike a parse failure - would otherwise never anchor to the file at all.
    /// </summary>
    /// <param name="document">The document to build a location for.</param>
    /// <returns>The document's location, anchored at its root when no better position is known.</returns>
    private static Location DocumentRootLocation(XliffDocumentModel document)
    {
        return LocationOf(document.Path, document.Line > 0 ? document.Line : 1, document.Column > 0 ? document.Column : 1);
    }

    /// <summary>
    /// Builds a zero-width <see cref="Location"/> at the given one-based line and column, or
    /// <see cref="Location.None"/> when no line is available, so a diagnostic still reports even when
    /// the document offers no better anchor than the file itself.
    /// </summary>
    /// <param name="path">The file path the location is reported against.</param>
    /// <param name="line">The one-based line, or 0 when unavailable.</param>
    /// <param name="column">The one-based column, or 0 when unavailable.</param>
    /// <returns>The resolved location.</returns>
    private static Location LocationOf(string path, int line, int column)
    {
        if(line <= 0)
        {
            return Location.None;
        }

        var position = new LinePosition(line - 1, Math.Max(0, column - 1));

        return Location.Create(path, new TextSpan(0, 0), new LinePositionSpan(position, position));
    }

    /// <summary>
    /// Extracts the final path segment from a possibly-rooted, possibly-relative file path, tolerating
    /// either directory separator so the file-name language hint check works regardless of host OS.
    /// </summary>
    /// <param name="path">The path to extract the file name from.</param>
    /// <returns>The final path segment.</returns>
    private static string GetFileName(string path)
    {
        int separator = path.LastIndexOfAny(['/', '\\']);

        return separator < 0 ? path : path.Substring(separator + 1);
    }

    /// <summary>
    /// Lowercases a BCP 47 language tag so it can key a language table case-insensitively without the
    /// table itself needing a case-insensitive comparer.
    /// </summary>
    /// <param name="languageTag">The language tag to normalize.</param>
    /// <returns>The lowercased tag.</returns>
    private static string NormalizeLanguageTag(string languageTag)
    {
        return languageTag.ToLowerInvariant();
    }

    /// <summary>
    /// Builds the generated field name for a language table: <c>Language</c>, the capitalized primary
    /// subtag, and every remaining subtag joined by <c>_</c>. The separator matters: without it,
    /// "en-ab" and "en-a-b" would both concatenate to "Enab" and collide on the same field name,
    /// producing a duplicate field the generated class fails to compile with (CS0102). Every subtag
    /// <see cref="LanguageTagPattern"/> accepts is ASCII letters and digits only, so the result is
    /// always a valid identifier; <see cref="SyntaxFacts.IsValidIdentifier"/> confirms it rather than
    /// assuming it.
    /// </summary>
    private static string LanguageFieldName(string originalTag)
    {
        string[] parts = originalTag.Split('-');
        var builder = new StringBuilder("Language");
        builder.Append(Capitalize(parts[0].ToLowerInvariant()));
        for(int index = 1; index < parts.Length; index++)
        {
            builder.Append('_').Append(parts[index]);
        }

        string fieldName = builder.ToString();

        return SyntaxFacts.IsValidIdentifier(fieldName) ? fieldName : throw new InvalidOperationException($"The language field name '{fieldName}' built from tag '{originalTag}' is not a valid C# identifier.");
    }

    /// <summary>
    /// Uppercases a string's first character, leaving the rest unchanged, so a language field name
    /// reads as <c>LanguageEn</c> rather than <c>Languageen</c>.
    /// </summary>
    /// <param name="text">The text to capitalize.</param>
    /// <returns>The text with its first character uppercased, or the text itself when empty.</returns>
    private static string Capitalize(string text)
    {
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    /// <summary>
    /// Builds the generated class's namespace from the consuming project's <c>RootNamespace</c>, or its
    /// assembly name when no root namespace was set, sanitizing every dotted segment into a valid C#
    /// identifier so the emitted <c>namespace</c> declaration always compiles.
    /// </summary>
    /// <param name="rootNamespace">The project's <c>RootNamespace</c> MSBuild property, or <see langword="null"/> when unset.</param>
    /// <param name="assemblyName">The project's assembly name, used when <paramref name="rootNamespace"/> is unset.</param>
    /// <returns>The sanitized namespace and any warnings raised sanitizing it.</returns>
    private static NamespaceModel BuildNamespace(string? rootNamespace, string assemblyName)
    {
        string candidate = string.IsNullOrWhiteSpace(rootNamespace) ? assemblyName : rootNamespace!;
        string[] parts = candidate.Split('.');
        var builder = new StringBuilder();
        var warnings = ImmutableArray.CreateBuilder<string>();
        for(int index = 0; index < parts.Length; index++)
        {
            string sanitized = SanitizeNamespaceSegment(parts[index], index, warnings);
            if(index > 0)
            {
                builder.Append('.');
            }

            builder.Append(sanitized);
        }

        if(builder.Length == 0)
        {
            builder.Append("App");
        }

        return new NamespaceModel(builder.ToString(), warnings.ToImmutable());
    }

    /// <summary>
    /// Sanitizes one dotted segment of a candidate namespace into a valid C# identifier: an empty
    /// segment or one that cannot become an identifier falls back to <c>Segment{index}</c>, invalid
    /// characters become <c>_</c>, a leading digit gets an underscore prefix, and a C# keyword is
    /// verbatim-escaped with <c>@</c>. Every fallback or change is recorded as a warning so the
    /// generator's diagnostics explain why the emitted namespace differs from the project's own.
    /// </summary>
    /// <param name="segment">The candidate segment, as split from the root namespace or assembly name.</param>
    /// <param name="index">The segment's zero-based position, used to name a fallback segment.</param>
    /// <param name="warnings">The builder any sanitizing or fallback warning is appended to.</param>
    /// <returns>A valid C# identifier for this segment.</returns>
    private static string SanitizeNamespaceSegment(string segment, int index, ImmutableArray<string>.Builder warnings)
    {
        if(string.IsNullOrEmpty(segment))
        {
            warnings.Add($"The project's namespace segment at position {index} is empty; 'Segment{index}' was used instead.");

            return $"Segment{index}";
        }

        var builder = new StringBuilder(segment.Length);
        foreach(char character in segment)
        {
            builder.Append(builder.Length == 0
                ? (SyntaxFacts.IsIdentifierStartCharacter(character) ? character : '_')
                : (SyntaxFacts.IsIdentifierPartCharacter(character) ? character : '_'));
        }

        string sanitized = builder.ToString();
        if(char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }

        if(SyntaxFacts.GetKeywordKind(sanitized) != SyntaxKind.None)
        {
            sanitized = "@" + sanitized;
        }

        if(!SyntaxFacts.IsValidIdentifier(sanitized))
        {
            warnings.Add($"The project's namespace segment '{segment}' could not become a valid C# identifier; 'Segment{index}' was used instead.");

            return $"Segment{index}";
        }

        if(!string.Equals(sanitized, segment, StringComparison.Ordinal))
        {
            warnings.Add($"The project's namespace segment '{segment}' was sanitized to '{sanitized}' to become a valid C# identifier.");
        }

        return sanitized;
    }

    /// <summary>
    /// Folds a translation's text to a single line and escapes it for a <c>///</c> doc comment: each
    /// line terminator is replaced with a space, not dropped, so words on either side of it stay
    /// distinct instead of silently merging (<c>"Hello\nWorld"</c> becomes <c>"Hello World"</c>, not
    /// <c>"HelloWorld"</c>), and then <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> are entitized.
    /// </summary>
    private static string EscapeXmlDocumentation(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach(char character in text)
        {
            if(IsLineTerminator(character))
            {
                builder.Append(' ');

                continue;
            }

            builder.Append(character);
        }

        return builder.ToString()
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    /// Determines whether a character is a line terminator recognized by <see cref="EscapeXmlDocumentation"/>:
    /// CR, LF, NEL, line separator or paragraph separator.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><see langword="true"/> if the character terminates a line; otherwise, <see langword="false"/>.</returns>
    private static bool IsLineTerminator(char character)
    {
        return character is '\r' or '\n' or '\u0085' or '\u2028' or '\u2029';
    }

    /// <summary>
    /// One language's translation table for the generated class: the field it is emitted as, the unit
    /// id to text entries it carries, and the path of the document each entry first came from, so a
    /// later conflicting entry can name both files.
    /// </summary>
    /// <param name="fieldName">The generated field name this table's dictionary is emitted as.</param>
    private sealed class LanguageTable(string fieldName)
    {
        /// <summary>The generated field name this table's dictionary is emitted as.</summary>
        public string FieldName { get; } = fieldName;

        /// <summary>The unit id to folded text entries this table carries, ordered for deterministic emission.</summary>
        public SortedDictionary<string, string> Entries { get; } = new(StringComparer.Ordinal);

        /// <summary>The path of the document each entry first came from, so a later conflict can name both files.</summary>
        public Dictionary<string, string> SourcePaths { get; } = new(StringComparer.Ordinal);
    }
}
