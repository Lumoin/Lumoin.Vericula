using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Xml;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Tone;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// Writes XLIFF 2.0 and 2.1 documents to a pipe or a stream.
/// </summary>
/// <remarks>
/// <para>
/// The writer emits exactly the vocabulary <see cref="XliffReader"/> consumes, so a read-write-read
/// cycle reproduces a document the reader accepted: the root <c>version</c>, <c>srcLang</c> and
/// <c>trgLang</c>; file, group, unit and segment ids; group nesting; every segment and ignorable with
/// its source, target, state and sub-state; notes; the Validation module on files; the Glossary
/// module on units; and, through the Metadata module, tone profiles, file-wide glossaries, scopes
/// and named metadata. Text round-trips byte for byte: a carriage return is entitized so no reader's
/// line-ending normalization can collapse it, and <c>xml:space="preserve"</c> is declared on the root.
/// </para>
/// <para>
/// The whole document is validated and serialized into memory before a single byte reaches the
/// output, so a rejected document never leaves a truncated file behind. A document assembled from
/// several files with differing source or target languages cannot be expressed, because XLIFF
/// declares one language pair on the root; the writer takes the pair from the first file and, for
/// the target language, the first file that declares one. Output is deterministic: the same document
/// always yields the same bytes.
/// </para>
/// </remarks>
public static class XliffWriter
{
    /// <summary>
    /// Serializes the document as XLIFF and writes the bytes to a pipe, completing it.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="output">The pipe to write UTF-8 bytes to.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="output"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// If <paramref name="document"/> cannot be expressed as XLIFF the reader accepts: an unknown
    /// version, no files, a file without units or groups, a unit without segments, an id that is not
    /// an XML name token, duplicate unit ids within a file, a target without a target language, a
    /// scope that is empty or contains whitespace, an empty metadata key, or text with characters XML
    /// cannot carry. The message lists every problem found.
    /// </exception>
    public static void Write(XliffDocument document, PipeWriter output)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        output.Write(Serialize(document));
        output.Complete();
    }

    /// <summary>
    /// Serializes the document as XLIFF and writes the bytes to a pipe asynchronously, completing it.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="output">The pipe to write UTF-8 bytes to.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the bytes have been written and the pipe completed.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="output"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="document"/> cannot be expressed as XLIFF the reader accepts; see <see cref="Write(XliffDocument, PipeWriter)"/>.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled before the bytes are written.</exception>
    public static async Task WriteAsync(XliffDocument document, PipeWriter output, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        cancellationToken.ThrowIfCancellationRequested();

        await output.WriteAsync(Serialize(document), cancellationToken).ConfigureAwait(false);
        await output.CompleteAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes the document as XLIFF and writes the bytes to a stream, which is left open.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The stream to write UTF-8 bytes to.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="document"/> cannot be expressed as XLIFF the reader accepts; see <see cref="Write(XliffDocument, PipeWriter)"/>.</exception>
    public static void Write(XliffDocument document, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);

        stream.Write(Serialize(document));
        stream.Flush();
    }

    /// <summary>
    /// Serializes the document as XLIFF and writes the bytes to a stream asynchronously, leaving it open.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The stream to write UTF-8 bytes to.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the bytes have been written and flushed.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="document"/> cannot be expressed as XLIFF the reader accepts; see <see cref="Write(XliffDocument, PipeWriter)"/>.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled before the bytes are written.</exception>
    public static async Task WriteAsync(XliffDocument document, Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);

        cancellationToken.ThrowIfCancellationRequested();

        await stream.WriteAsync(Serialize(document), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates the document and serializes it to UTF-8 bytes, or throws with every problem found.
    /// </summary>
    private static byte[] Serialize(XliffDocument document)
    {
        ImmutableArray<string> problems = [.. Problems(document)];
        if(!problems.IsEmpty)
        {
            throw new ArgumentException(string.Join(" ", problems), nameof(document));
        }

        using var buffer = new MemoryStream();
        using(XmlWriter writer = XmlWriter.Create(buffer, CreateWriterSettings()))
        {
            WriteDocument(writer, document);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Creates writer settings that entitize carriage returns, which is the only way one survives the
    /// line-ending normalization every conformant XML reader performs on input, and that fix the
    /// indentation newline so the bytes are identical across host platforms.
    /// </summary>
    private static XmlWriterSettings CreateWriterSettings()
    {
        return new XmlWriterSettings
        {
            Indent = true,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Entitize,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = false,
            CloseOutput = false
        };
    }

    /// <summary>
    /// Writes the XML declaration and the &lt;xliff&gt; root element with its version, language pair
    /// and <c>xml:space="preserve"</c>, then every file.
    /// </summary>
    private static void WriteDocument(XmlWriter writer, XliffDocument document)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement(null, WellKnownXliffElements.Xliff, WellKnownXliffNamespaces.Core);
        writer.WriteAttributeString(WellKnownXliffAttributes.Version, VersionText(document.Version));

        (string sourceLanguage, string? targetLanguage) = RootLanguages(document);
        writer.WriteAttributeString(WellKnownXliffAttributes.SourceLanguage, sourceLanguage);
        if(targetLanguage is not null)
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.TargetLanguage, targetLanguage);
        }

        writer.WriteAttributeString(WellKnownXliffNamespaces.XmlPrefix, WellKnownXliffAttributes.Space, null, WellKnownXliffAttributeValues.Preserve);

        foreach(XliffFile file in document.Files)
        {
            WriteFile(writer, file);
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    /// <summary>
    /// Writes one &lt;file&gt; element: its id, the Metadata module carrying its tone profile and
    /// file-wide glossary, the Validation module's rules, and its groups and units.
    /// </summary>
    private static void WriteFile(XmlWriter writer, XliffFile file)
    {
        writer.WriteStartElement(WellKnownXliffElements.File);
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, file.Id);

        if(file.ToneProfile is not null || file.Glossary is not null)
        {
            writer.WriteStartElement(WellKnownXliffNamespaces.MetadataPrefix, WellKnownXliffElements.Metadata, WellKnownXliffNamespaces.Metadata);
            if(file.ToneProfile is not null)
            {
                WriteTone(writer, file.ToneProfile);
            }

            if(file.Glossary is not null)
            {
                WriteGlossaryMetadata(writer, file.Glossary);
            }

            writer.WriteEndElement();
        }

        if(file.ValidationRules is { Rules.Length: > 0 } validationRules)
        {
            WriteValidation(writer, validationRules);
        }

        foreach(XliffGroup group in file.Groups)
        {
            WriteGroup(writer, group);
        }

        foreach(XliffUnit unit in file.Units)
        {
            WriteUnit(writer, unit);
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes one &lt;group&gt; element: its id, scopes and metadata, then its nested groups and
    /// units in document order.
    /// </summary>
    private static void WriteGroup(XmlWriter writer, XliffGroup group)
    {
        writer.WriteStartElement(WellKnownXliffElements.Group);
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, group.Id);
        WriteScopesAndMetadata(writer, group.Scopes, group.Metadata);

        foreach(XliffGroup nested in group.Groups)
        {
            WriteGroup(writer, nested);
        }

        foreach(XliffUnit unit in group.Units)
        {
            WriteUnit(writer, unit);
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes one &lt;unit&gt; element: its id, scopes and metadata, the Glossary module, its notes,
    /// and its segments and ignorables in document order.
    /// </summary>
    private static void WriteUnit(XmlWriter writer, XliffUnit unit)
    {
        writer.WriteStartElement(WellKnownXliffElements.Unit);
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, unit.Id);
        WriteScopesAndMetadata(writer, unit.Scopes, unit.Metadata);

        if(unit.Glossary is not null)
        {
            WriteGlossaryModule(writer, unit.Glossary);
        }

        if(!unit.Notes.IsEmpty)
        {
            writer.WriteStartElement(WellKnownXliffElements.Notes);
            foreach(string note in unit.Notes)
            {
                writer.WriteElementString(WellKnownXliffElements.Note, note);
            }

            writer.WriteEndElement();
        }

        foreach(XliffSegment segment in unit.Segments)
        {
            WriteSegment(writer, segment);
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes one &lt;segment&gt; or &lt;ignorable&gt; element with its id, source and target; a
    /// translatable segment also carries its state and sub-state, which XLIFF 2.1 §4.3.1.31 state
    /// and §4.3.1.35 subState reserve for &lt;segment&gt; alone.
    /// </summary>
    private static void WriteSegment(XmlWriter writer, XliffSegment segment)
    {
        writer.WriteStartElement(segment.Kind == SegmentKind.Ignorable ? WellKnownXliffElements.Ignorable : WellKnownXliffElements.Segment);
        if(segment.Id is not null)
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.Id, segment.Id);
        }

        if(segment.Kind == SegmentKind.Translatable)
        {
            (string? state, string? subState) = StateAttributes(segment);
            if(state is not null)
            {
                writer.WriteAttributeString(WellKnownXliffAttributes.State, state);
            }

            if(subState is not null)
            {
                writer.WriteAttributeString(WellKnownXliffAttributes.SubState, subState);
            }
        }

        writer.WriteElementString(WellKnownXliffElements.Source, segment.Source);
        if(segment.Target is not null)
        {
            writer.WriteElementString(WellKnownXliffElements.Target, segment.Target);
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Maps a segment's state to the XLIFF attributes. The initial state is XLIFF's default and stays
    /// implicit only when there is no sub-state to go with it: XLIFF 2.1 §4.3.1.35 subState,
    /// Constraints, "If the attribute subState is used, the attribute state MUST be explicitly set."
    /// <see cref="SegmentState.NeedsTranslation"/> has no XLIFF state of its own and travels as the
    /// initial state, explicitly written, refined by a Vericula sub-state.
    /// </summary>
    private static (string? State, string? SubState) StateAttributes(XliffSegment segment)
    {
        return segment.State switch
        {
            SegmentState.Initial => (segment.SubState is null ? null : WellKnownXliffAttributeValues.StateInitial, segment.SubState),
            SegmentState.Translated => (WellKnownXliffAttributeValues.StateTranslated, segment.SubState),
            SegmentState.Reviewed => (WellKnownXliffAttributeValues.StateReviewed, segment.SubState),
            SegmentState.Final => (WellKnownXliffAttributeValues.StateFinal, segment.SubState),
            SegmentState.NeedsTranslation => (WellKnownXliffAttributeValues.StateInitial, WellKnownVericulaMetadata.NeedsTranslationSubState),
            _ => throw new ArgumentOutOfRangeException(nameof(segment), segment.State, "Unknown segment state.")
        };
    }

    /// <summary>
    /// Writes the Metadata module &lt;mda:metadata&gt; element carrying a group or unit's scopes and
    /// named metadata, each as its own &lt;mda:metaGroup&gt;; omitted entirely when both are empty.
    /// </summary>
    private static void WriteScopesAndMetadata(XmlWriter writer, ImmutableArray<Scope> scopes, ImmutableDictionary<string, string> metadata)
    {
        if(scopes.IsEmpty && metadata.IsEmpty)
        {
            return;
        }

        writer.WriteStartElement(WellKnownXliffNamespaces.MetadataPrefix, WellKnownXliffElements.Metadata, WellKnownXliffNamespaces.Metadata);
        if(!scopes.IsEmpty)
        {
            WriteMetaGroupStart(writer, WellKnownVericulaMetadata.ScopesCategory);
            foreach(Scope scope in scopes)
            {
                WriteMeta(writer, WellKnownVericulaMetadata.ScopeType, scope.Value);
            }

            writer.WriteEndElement();
        }

        if(!metadata.IsEmpty)
        {
            WriteMetaGroupStart(writer, WellKnownVericulaMetadata.MetadataCategory);
            WriteDictionary(writer, metadata);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes a file's tone profile as a Vericula metadata group: its version, authority, register,
    /// voice, orientation, calendar, date format and digit style, followed by its own free-form
    /// metadata group when present.
    /// </summary>
    private static void WriteTone(XmlWriter writer, ToneProfile tone)
    {
        WriteMetaGroupStart(writer, WellKnownVericulaMetadata.ToneCategory);
        WriteMeta(writer, WellKnownVericulaMetadata.VersionType, tone.Version);
        WriteOptionalMeta(writer, WellKnownVericulaMetadata.AuthorityType, tone.Authority);
        WriteMeta(writer, WellKnownVericulaMetadata.RegisterType, tone.Register.ToString());
        WriteOptionalMeta(writer, WellKnownVericulaMetadata.VoiceType, tone.Voice);
        WriteMeta(writer, WellKnownVericulaMetadata.OrientationType, tone.Orientation.ToString());
        WriteOptionalMeta(writer, WellKnownVericulaMetadata.CalendarType, tone.Calendar);
        WriteOptionalMeta(writer, WellKnownVericulaMetadata.DateFormatType, tone.DateFormat);
        WriteMeta(writer, WellKnownVericulaMetadata.DigitStyleType, tone.DigitStyle.ToString());

        if(!tone.Metadata.IsEmpty)
        {
            WriteMetaGroupStart(writer, WellKnownVericulaMetadata.ToneMetadataCategory);
            WriteDictionary(writer, tone.Metadata);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes a file-wide glossary as a Vericula metadata group, one nested entry group per
    /// <see cref="GlossaryEntry"/> carrying its term, translation, definition, status, rationale
    /// and scopes.
    /// </summary>
    private static void WriteGlossaryMetadata(XmlWriter writer, Glossary glossary)
    {
        WriteMetaGroupStart(writer, WellKnownVericulaMetadata.GlossaryCategory);
        foreach(GlossaryEntry entry in glossary.Entries)
        {
            WriteMetaGroupStart(writer, WellKnownVericulaMetadata.GlossaryEntryCategory);
            WriteMeta(writer, WellKnownVericulaMetadata.TermType, entry.Term);
            WriteMeta(writer, WellKnownVericulaMetadata.TranslationType, entry.Translation);
            WriteOptionalMeta(writer, WellKnownVericulaMetadata.DefinitionType, entry.Definition);
            WriteMeta(writer, WellKnownVericulaMetadata.StatusType, entry.Status.ToString());
            WriteOptionalMeta(writer, WellKnownVericulaMetadata.RationaleType, entry.Rationale);
            foreach(Scope scope in entry.Scopes)
            {
                WriteMeta(writer, WellKnownVericulaMetadata.ScopeType, scope.Value);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes a unit's glossary as the Glossary module's &lt;gls:glossary&gt; element, one
    /// &lt;gls:glossEntry&gt; per entry with its term, translation (carrying the Vericula status
    /// attribute) and optional definition; the rationale and scopes travel as Vericula attributes
    /// on the entry since the Glossary module has no element for either.
    /// </summary>
    private static void WriteGlossaryModule(XmlWriter writer, Glossary glossary)
    {
        writer.WriteStartElement(WellKnownXliffNamespaces.GlossaryPrefix, WellKnownXliffElements.Glossary, WellKnownXliffNamespaces.Glossary);
        foreach(GlossaryEntry entry in glossary.Entries)
        {
            writer.WriteStartElement(WellKnownXliffNamespaces.GlossaryPrefix, WellKnownXliffElements.GlossEntry, WellKnownXliffNamespaces.Glossary);
            if(entry.Rationale is not null)
            {
                WriteVericulaAttribute(writer, WellKnownVericulaMetadata.RationaleAttribute, entry.Rationale);
            }

            if(!entry.Scopes.IsEmpty)
            {
                WriteVericulaAttribute(writer, WellKnownVericulaMetadata.ScopesAttribute, JoinScopes(entry.Scopes));
            }

            writer.WriteElementString(WellKnownXliffNamespaces.GlossaryPrefix, WellKnownXliffElements.Term, WellKnownXliffNamespaces.Glossary, entry.Term);
            writer.WriteStartElement(WellKnownXliffNamespaces.GlossaryPrefix, WellKnownXliffElements.Translation, WellKnownXliffNamespaces.Glossary);
            WriteVericulaAttribute(writer, WellKnownVericulaMetadata.StatusAttribute, entry.Status.ToString());
            writer.WriteString(entry.Translation);
            writer.WriteEndElement();
            if(entry.Definition is not null)
            {
                writer.WriteElementString(WellKnownXliffNamespaces.GlossaryPrefix, WellKnownXliffElements.Definition, WellKnownXliffNamespaces.Glossary, entry.Definition);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Joins scopes into the single space-separated value the <c>vcl:scopes</c> attribute carries.
    /// </summary>
    private static string JoinScopes(ImmutableArray<Scope> scopes)
    {
        var joined = new StringBuilder();
        foreach(Scope scope in scopes)
        {
            if(joined.Length > 0)
            {
                joined.Append(' ');
            }

            joined.Append(scope.Value);
        }

        return joined.ToString();
    }

    /// <summary>
    /// Writes a file's &lt;val:validation&gt; element, one &lt;val:rule&gt; per rule with the
    /// attribute its kind maps to, plus <c>disabled</c> and <c>normalization</c> when the rule sets
    /// them.
    /// </summary>
    private static void WriteValidation(XmlWriter writer, ValidationRuleSet rules)
    {
        writer.WriteStartElement(WellKnownXliffNamespaces.ValidationPrefix, WellKnownXliffElements.Validation, WellKnownXliffNamespaces.Validation);
        foreach(ValidationRule rule in rules.Rules)
        {
            writer.WriteStartElement(WellKnownXliffNamespaces.ValidationPrefix, WellKnownXliffElements.Rule, WellKnownXliffNamespaces.Validation);
            (string? prefix, string name, string? ns, string value) = rule switch
            {
                PresenceRule presence => (null, WellKnownXliffAttributes.IsPresent, null, presence.Text),
                AbsenceRule absence => (null, WellKnownXliffAttributes.IsNotPresent, null, absence.Text),
                StartsWithRule starts => (null, WellKnownXliffAttributes.StartsWith, null, starts.Text),
                EndsWithRule ends => (null, WellKnownXliffAttributes.EndsWith, null, ends.Text),
                LengthBudgetRule budget => (WellKnownXliffNamespaces.VericulaPrefix, WellKnownVericulaMetadata.MaxLengthAttribute, WellKnownXliffNamespaces.Vericula, budget.MaximumLength.ToString(CultureInfo.InvariantCulture)),
                RegexRule regex => (WellKnownXliffNamespaces.VericulaPrefix, WellKnownVericulaMetadata.RegexAttribute, WellKnownXliffNamespaces.Vericula, regex.Pattern),
                _ => throw new ArgumentOutOfRangeException(nameof(rules), rule, "Unknown validation rule.")
            };
            writer.WriteAttributeString(prefix, name, ns, value);
            if(rule.Disabled)
            {
                writer.WriteAttributeString(WellKnownXliffAttributes.Disabled, WellKnownXliffAttributeValues.Yes);
            }

            if(rule.Normalization is TextNormalization.None)
            {
                writer.WriteAttributeString(WellKnownXliffAttributes.Normalization, WellKnownXliffAttributeValues.NormalizationNone);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes an attribute in the Vericula namespace with the given local name and value.
    /// </summary>
    private static void WriteVericulaAttribute(XmlWriter writer, string localName, string value)
    {
        writer.WriteAttributeString(WellKnownXliffNamespaces.VericulaPrefix, localName, WellKnownXliffNamespaces.Vericula, value);
    }

    /// <summary>
    /// Opens a Metadata module &lt;mda:metaGroup&gt; element with the given <c>category</c>, left
    /// for the caller to fill and close.
    /// </summary>
    private static void WriteMetaGroupStart(XmlWriter writer, string category)
    {
        writer.WriteStartElement(WellKnownXliffNamespaces.MetadataPrefix, WellKnownXliffElements.MetaGroup, WellKnownXliffNamespaces.Metadata);
        writer.WriteAttributeString(WellKnownXliffAttributes.Category, category);
    }

    /// <summary>
    /// Writes one Metadata module &lt;mda:meta&gt; element with the given <c>type</c> and text
    /// value.
    /// </summary>
    private static void WriteMeta(XmlWriter writer, string type, string value)
    {
        writer.WriteStartElement(WellKnownXliffNamespaces.MetadataPrefix, WellKnownXliffElements.Meta, WellKnownXliffNamespaces.Metadata);
        writer.WriteAttributeString(WellKnownXliffAttributes.Type, type);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes a &lt;mda:meta&gt; element for <paramref name="value"/> when it is present, and
    /// writes nothing when it is null.
    /// </summary>
    private static void WriteOptionalMeta(XmlWriter writer, string type, string? value)
    {
        if(value is not null)
        {
            WriteMeta(writer, type, value);
        }
    }

    /// <summary>
    /// Writes a dictionary as metadata elements in ordinal key order, so the output is deterministic.
    /// </summary>
    private static void WriteDictionary(XmlWriter writer, ImmutableDictionary<string, string> dictionary)
    {
        foreach(KeyValuePair<string, string> entry in dictionary.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            WriteMeta(writer, entry.Key, entry.Value);
        }
    }

    /// <summary>
    /// The root language pair the reader hands to every file it parses: the first file's source
    /// language, and the target language of the first file that declares one, if any.
    /// </summary>
    private static (string SourceLanguage, string? TargetLanguage) RootLanguages(XliffDocument document)
    {
        string? targetLanguage = null;
        foreach(XliffFile file in document.Files)
        {
            if(file.TargetLanguage is not null)
            {
                targetLanguage = file.TargetLanguage.Value;

                break;
            }
        }

        return (document.Files[0].SourceLanguage.Value, targetLanguage);
    }

    /// <summary>
    /// Maps an <see cref="XliffVersion"/> to the "2.0" or "2.1" text the <c>version</c> attribute
    /// carries.
    /// </summary>
    private static string VersionText(XliffVersion version)
    {
        return version switch
        {
            XliffVersion.V20 => WellKnownXliffAttributeValues.Version20,
            XliffVersion.V21 => WellKnownXliffAttributeValues.Version21,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported XLIFF version.")
        };
    }

    /// <summary>
    /// Yields every reason the document cannot be expressed as XLIFF the reader accepts; empty when
    /// it can.
    /// </summary>
    private static IEnumerable<string> Problems(XliffDocument document)
    {
        if(!Enum.IsDefined(document.Version))
        {
            yield return $"The XLIFF version {document.Version} is not one the writer can emit.";
        }

        if(document.Files.IsEmpty)
        {
            yield return "An XLIFF document must contain at least one file.";

            yield break;
        }

        (string rootSourceLanguage, string? targetLanguage) = RootLanguages(document);
        var fileIds = new HashSet<string>(StringComparer.Ordinal);
        foreach(XliffFile file in document.Files)
        {
            if(!fileIds.Add(file.Id))
            {
                yield return $"Duplicate file id '{file.Id}'; file ids must be unique within a document.";
            }

            if(!string.Equals(file.SourceLanguage.Value, rootSourceLanguage, StringComparison.Ordinal))
            {
                yield return $"File '{file.Id}' declares source language '{file.SourceLanguage.Value}', but XLIFF has one srcLang per document and the writer takes '{rootSourceLanguage}' from the first file.";
            }

            if(file.TargetLanguage is { } fileTargetLanguage && !string.Equals(fileTargetLanguage.Value, targetLanguage, StringComparison.Ordinal))
            {
                yield return $"File '{file.Id}' declares target language '{fileTargetLanguage.Value}', but XLIFF has one trgLang per document and the writer takes '{targetLanguage}' from the first file that declares one.";
            }

            if(file.TargetLanguage is null && targetLanguage is not null && HasAnyTarget(file))
            {
                yield return $"File '{file.Id}' declares no target language, but it carries target text and the document will be written with trgLang '{targetLanguage}'; reading it back would wrongly give file '{file.Id}' that target language.";
            }

            foreach(string problem in Problems(file, file.TargetLanguage?.Value))
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Reports whether any segment of the file, including those under a nested group, carries a
    /// target, mirroring <see cref="XliffReader"/>'s own <c>HasAnyTarget</c> check.
    /// </summary>
    private static bool HasAnyTarget(XliffFile file)
    {
        foreach(XliffUnit unit in Flatten(file))
        {
            foreach(XliffSegment segment in unit.Segments)
            {
                if(segment.Target is not null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Yields every reason one file cannot be written: an id that is not an XML name token, a
    /// malformed source or target language tag, neither units nor groups, an invalid tone profile
    /// or file-wide glossary, invalid or empty validation rules, a duplicate unit id anywhere in
    /// the file, and the problems of its groups and units.
    /// </summary>
    private static IEnumerable<string> Problems(XliffFile file, string? fileTargetLanguage)
    {
        foreach(string problem in NameTokenProblems(file.Id, $"file id '{file.Id}'"))
        {
            yield return problem;
        }

        if(!LanguageTag.IsWellFormed(file.SourceLanguage.Value))
        {
            yield return $"The source language '{file.SourceLanguage.Value}' of file '{file.Id}' is not a well-formed BCP 47 language tag.";
        }

        if(file.TargetLanguage is { } fileTarget && !LanguageTag.IsWellFormed(fileTarget.Value))
        {
            yield return $"The target language '{fileTarget.Value}' of file '{file.Id}' is not a well-formed BCP 47 language tag.";
        }

        if(file.Groups.IsEmpty && file.Units.IsEmpty)
        {
            yield return $"File '{file.Id}' has neither units nor groups; XLIFF requires at least one.";
        }

        if(file.ToneProfile is { } tone)
        {
            foreach(string problem in Problems(tone))
            {
                yield return problem;
            }
        }

        if(file.Glossary is { } glossary)
        {
            foreach(string problem in Problems(glossary, $"the glossary of file '{file.Id}'", requireTokenScopes: false))
            {
                yield return problem;
            }
        }

        if(file.ValidationRules is { } rules)
        {
            if(rules.Rules.IsEmpty)
            {
                yield return $"The <val:validation> of file '{file.Id}' has no rules; XLIFF 2.1 §5.8.4.2 requires one or more <rule> elements.";
            }

            foreach(ValidationRule rule in rules.Rules)
            {
                foreach(string problem in XmlTextProblems(RuleText(rule), $"a validation rule of file '{file.Id}'"))
                {
                    yield return problem;
                }

                if(rule is (PresenceRule or AbsenceRule or StartsWithRule or EndsWithRule) && RuleText(rule) is { Length: 0 })
                {
                    yield return $"A validation rule of file '{file.Id}' has an empty value; a rule text must not be empty.";
                }

                if(rule is LengthBudgetRule { MaximumLength: < 1 } budget)
                {
                    yield return $"A validation rule of file '{file.Id}' has the length budget {budget.MaximumLength}, which must be at least 1.";
                }

                if(rule is not (PresenceRule or AbsenceRule or StartsWithRule or EndsWithRule or LengthBudgetRule or RegexRule))
                {
                    yield return $"A validation rule of file '{file.Id}' is a rule kind the writer does not know how to emit.";
                }
            }
        }

        foreach(IGrouping<string, XliffUnit> duplicate in Flatten(file).GroupBy(static unit => unit.Id, StringComparer.Ordinal).Where(static group => group.Count() > 1))
        {
            yield return $"Duplicate unit id '{duplicate.Key}' in file '{file.Id}'; unit ids must be unique within a file.";
        }

        foreach(XliffGroup group in file.Groups)
        {
            foreach(string problem in Problems(group, file, fileTargetLanguage, depth: 1))
            {
                yield return problem;
            }
        }

        foreach(XliffUnit unit in file.Units)
        {
            foreach(string problem in Problems(unit, file, fileTargetLanguage))
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Yields every reason a tone profile cannot be written: text XML cannot carry in its version,
    /// authority, voice, calendar or date format, or in its free-form metadata.
    /// </summary>
    private static IEnumerable<string> Problems(ToneProfile tone)
    {
        foreach(string problem in XmlTextProblems(tone.Version, "the tone profile version"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(tone.Authority, "the tone profile authority"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(tone.Voice, "the tone profile voice"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(tone.Calendar, "the tone profile calendar"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(tone.DateFormat, "the tone profile date format"))
        {
            yield return problem;
        }

        foreach(string problem in Problems(tone.Metadata, "the tone profile metadata"))
        {
            yield return problem;
        }
    }

    /// <summary>
    /// The deepest a chain of nested &lt;group&gt; elements the writer will emit, matching the reader's
    /// own bound (see <c>XliffReader.MaxGroupDepth</c>) so a document the writer accepts is one the
    /// reader can read back rather than overflow on.
    /// </summary>
    private const int MaxGroupDepth = 64;

    /// <summary>
    /// Yields every reason one group cannot be written: nesting past <see cref="MaxGroupDepth"/>,
    /// an id that is not an XML name token, invalid scopes or metadata, and the problems of its
    /// nested groups and units.
    /// </summary>
    private static IEnumerable<string> Problems(XliffGroup group, XliffFile file, string? fileTargetLanguage, int depth)
    {
        if(depth > MaxGroupDepth)
        {
            yield return $"A group nests more than {MaxGroupDepth} levels deep under file '{file.Id}', which the reader does not support.";

            yield break;
        }

        foreach(string problem in NameTokenProblems(group.Id, $"group id '{group.Id}' in file '{file.Id}'"))
        {
            yield return problem;
        }

        foreach(string problem in ScopeProblems(group.Scopes, $"group '{group.Id}'"))
        {
            yield return problem;
        }

        foreach(string problem in Problems(group.Metadata, $"the metadata of group '{group.Id}'"))
        {
            yield return problem;
        }

        foreach(XliffGroup nested in group.Groups)
        {
            foreach(string problem in Problems(nested, file, fileTargetLanguage, depth + 1))
            {
                yield return problem;
            }
        }

        foreach(XliffUnit unit in group.Units)
        {
            foreach(string problem in Problems(unit, file, fileTargetLanguage))
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Yields every reason one unit cannot be written: an id that is not an XML name token, no
    /// translatable segment (XLIFF 2.1 §4.2.2.5 requires a &lt;unit&gt; to contain at least one
    /// &lt;segment&gt;), a duplicate segment or ignorable id, invalid scopes or metadata, invalid
    /// notes, an invalid glossary, and the problems of its segments.
    /// </summary>
    private static IEnumerable<string> Problems(XliffUnit unit, XliffFile file, string? fileTargetLanguage)
    {
        foreach(string problem in NameTokenProblems(unit.Id, $"unit id '{unit.Id}' in file '{file.Id}'"))
        {
            yield return problem;
        }

        if(!unit.Segments.Any(static segment => segment.Kind == SegmentKind.Translatable))
        {
            yield return $"Unit '{unit.Id}' in file '{file.Id}' has no translatable segment; XLIFF 2.1 §4.2.2.5 requires a <unit> to contain at least one <segment>.";
        }

        foreach(IGrouping<string, XliffSegment> duplicate in unit.Segments.Where(static segment => segment.Id is not null).GroupBy(static segment => segment.Id!, StringComparer.Ordinal).Where(static group => group.Count() > 1))
        {
            yield return $"Duplicate segment id '{duplicate.Key}' in unit '{unit.Id}'; segment and ignorable ids must be unique within a unit.";
        }

        foreach(string problem in ScopeProblems(unit.Scopes, $"unit '{unit.Id}'"))
        {
            yield return problem;
        }

        foreach(string problem in Problems(unit.Metadata, $"the metadata of unit '{unit.Id}'"))
        {
            yield return problem;
        }

        foreach(string note in unit.Notes)
        {
            foreach(string problem in XmlTextProblems(note, $"a note of unit '{unit.Id}'"))
            {
                yield return problem;
            }
        }

        if(unit.Glossary is { } glossary)
        {
            foreach(string problem in Problems(glossary, $"the glossary of unit '{unit.Id}'", requireTokenScopes: true))
            {
                yield return problem;
            }
        }

        foreach(XliffSegment segment in unit.Segments)
        {
            foreach(string problem in Problems(segment, unit, file, fileTargetLanguage))
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Yields every reason one segment or ignorable cannot be written: an id that is not an XML
    /// name token, source, target or sub-state text XML cannot carry, a target with no file target
    /// language to pair it with, an undefined state, a state or sub-state on an
    /// &lt;ignorable&gt;, which XLIFF gives no attribute to carry either, and a
    /// <see cref="SegmentState.NeedsTranslation"/> segment carrying its own, different sub-state,
    /// since that state's reserved sub-state is the only channel it is written through.
    /// </summary>
    private static IEnumerable<string> Problems(XliffSegment segment, XliffUnit unit, XliffFile file, string? fileTargetLanguage)
    {
        if(segment.Id is not null)
        {
            foreach(string problem in NameTokenProblems(segment.Id, $"segment id '{segment.Id}' in unit '{unit.Id}'"))
            {
                yield return problem;
            }
        }

        foreach(string problem in XmlTextProblems(segment.Source, $"the source of unit '{unit.Id}'"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(segment.Target, $"the target of unit '{unit.Id}'"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(segment.SubState, $"the sub-state of a segment in unit '{unit.Id}'"))
        {
            yield return problem;
        }

        if(segment.Target is not null && fileTargetLanguage is null)
        {
            yield return $"Unit '{unit.Id}' in file '{file.Id}' carries a target but file '{file.Id}' declares no target language.";
        }

        if(!Enum.IsDefined(segment.State))
        {
            yield return $"A segment in unit '{unit.Id}' has the state {segment.State}, which is not a known segment state.";
        }

        if(segment.Kind == SegmentKind.Ignorable && (segment.State != SegmentState.Initial || segment.SubState is not null))
        {
            yield return $"An ignorable segment in unit '{unit.Id}' carries a state or sub-state, which <ignorable> has no attribute to express.";
        }

        if(segment.State == SegmentState.NeedsTranslation && segment.SubState is not null)
        {
            yield return $"A segment in unit '{unit.Id}' has state NeedsTranslation and a sub-state '{segment.SubState}'; NeedsTranslation itself travels as the reserved sub-state, so it cannot carry a different one too.";
        }
    }

    /// <summary>
    /// Yields every reason a glossary cannot be written: term, translation, definition or
    /// rationale text XML cannot carry, and, for each entry's scopes, either the stricter
    /// token-shaped problems the Glossary module's <c>vcl:scopes</c> attribute requires or the
    /// looser text problems the Metadata module's scope metadata carries as element content,
    /// depending on <paramref name="requireTokenScopes"/>.
    /// </summary>
    private static IEnumerable<string> Problems(Glossary glossary, string what, bool requireTokenScopes)
    {
        foreach(GlossaryEntry entry in glossary.Entries)
        {
            foreach(string problem in XmlTextProblems(entry.Term, $"a term in {what}"))
            {
                yield return problem;
            }

            foreach(string problem in XmlTextProblems(entry.Translation, $"a translation in {what}"))
            {
                yield return problem;
            }

            foreach(string problem in XmlTextProblems(entry.Definition, $"a definition in {what}"))
            {
                yield return problem;
            }

            foreach(string problem in XmlTextProblems(entry.Rationale, $"a rationale in {what}"))
            {
                yield return problem;
            }

            IEnumerable<string> scopeProblems = requireTokenScopes
                ? ScopeProblems(entry.Scopes, what)
                : entry.Scopes.SelectMany(scope => XmlTextProblems(scope.Value, $"a scope in {what}"));
            foreach(string problem in scopeProblems)
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Yields the problems of scopes that travel as metadata text or as a space-separated attribute;
    /// a scope with whitespace inside could not be told apart from two scopes.
    /// </summary>
    private static IEnumerable<string> ScopeProblems(ImmutableArray<Scope> scopes, string what)
    {
        foreach(Scope scope in scopes)
        {
            foreach(string problem in XmlTextProblems(scope.Value, $"a scope of {what}"))
            {
                yield return problem;
            }

            if(scope.Value.Length == 0 || scope.Value.Any(char.IsWhiteSpace))
            {
                yield return $"The scope '{scope.Value}' of {what} is empty or contains whitespace.";
            }
        }
    }

    /// <summary>
    /// Yields every reason a metadata dictionary cannot be written: an empty key, and key or value
    /// text XML cannot carry.
    /// </summary>
    private static IEnumerable<string> Problems(ImmutableDictionary<string, string> dictionary, string what)
    {
        foreach(KeyValuePair<string, string> entry in dictionary)
        {
            if(string.IsNullOrWhiteSpace(entry.Key))
            {
                yield return $"A key in {what} is empty.";
            }

            foreach(string problem in XmlTextProblems(entry.Key, $"a key in {what}"))
            {
                yield return problem;
            }

            foreach(string problem in XmlTextProblems(entry.Value, $"a value in {what}"))
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Extracts the text or pattern a validation rule carries, or null for a rule kind with none
    /// (a length budget), for the problem-reporting and validity checks common to every rule kind.
    /// </summary>
    private static string? RuleText(ValidationRule rule)
    {
        return rule switch
        {
            PresenceRule presence => presence.Text,
            AbsenceRule absence => absence.Text,
            StartsWithRule starts => starts.Text,
            EndsWithRule ends => ends.Text,
            RegexRule regex => regex.Pattern,
            _ => null
        };
    }

    /// <summary>
    /// Yields every reason a value cannot be written as an XML id attribute: empty or
    /// whitespace-only, or not a well-formed NMTOKEN per XLIFF 2.1 §4.3.1.21 id's value
    /// description.
    /// </summary>
    private static IEnumerable<string> NameTokenProblems(string? value, string what)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            yield return $"The {what} is empty.";

            yield break;
        }

        if(!IsNameToken(value))
        {
            yield return $"The {what} is not an XML name token.";
        }
    }

    /// <summary>
    /// Yields the one reason text cannot be written: it contains a character XML forbids, such as
    /// an unpaired surrogate or most C0 control characters; null is always writable, since it means
    /// the element or attribute is simply omitted.
    /// </summary>
    private static IEnumerable<string> XmlTextProblems(string? value, string what)
    {
        if(value is not null && !IsXmlText(value))
        {
            yield return $"The text of {what} contains a character XML cannot carry.";
        }
    }

    /// <summary>
    /// Reports whether a value is a well-formed XML NMTOKEN, using
    /// <see cref="XmlConvert.VerifyNMTOKEN(string)"/> to apply the same character rules XLIFF 2.1
    /// §4.3.1.21 id relies on.
    /// </summary>
    private static bool IsNameToken(string value)
    {
        try
        {
            XmlConvert.VerifyNMTOKEN(value);

            return true;
        }
        catch(XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reports whether a value contains only characters XML can carry, using
    /// <see cref="XmlConvert.VerifyXmlChars(string)"/> to apply the XML 1.0 Char production.
    /// </summary>
    private static bool IsXmlText(string value)
    {
        try
        {
            XmlConvert.VerifyXmlChars(value);

            return true;
        }
        catch(XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Yields every unit in a file, depth-first through its groups, mirroring
    /// <see cref="XliffReader"/>'s own flattening so duplicate-id checks see the ids exactly as the
    /// reader will.
    /// </summary>
    private static IEnumerable<XliffUnit> Flatten(XliffFile file)
    {
        foreach(XliffUnit unit in file.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup group in file.Groups)
        {
            foreach(XliffUnit unit in Flatten(group))
            {
                yield return unit;
            }
        }
    }

    /// <summary>
    /// Yields every unit under a group, depth-first through its nested groups.
    /// </summary>
    private static IEnumerable<XliffUnit> Flatten(XliffGroup group)
    {
        foreach(XliffUnit unit in group.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup nested in group.Groups)
        {
            foreach(XliffUnit unit in Flatten(nested))
            {
                yield return unit;
            }
        }
    }
}
