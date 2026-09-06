using System.Buffers;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text;
using Lumoin.Veritas.Core;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Projections;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class XliffGraphProjectionTests
{
    private static Uri Base { get; } = new("https://example.org/wallet/");

    [TestMethod]
    public void ProjectsFilesAndUnitsWithIdentifiersLabelsAndPartOfRelations()
    {
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(document, Base);

        string[] rendered = quads.Select(quad => $"{quad.Subject} {quad.Predicate} {quad.Object}").ToArray();
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet> <http://purl.org/dc/terms/identifier> \"wallet\"^^<http://www.w3.org/2001/XMLSchema#string>");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet> <http://purl.org/dc/terms/language> \"en\"^^<http://www.w3.org/2001/XMLSchema#string>");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet> <http://purl.org/dc/terms/language> \"fi\"^^<http://www.w3.org/2001/XMLSchema#string>");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet/TabHome> <http://purl.org/dc/terms/isPartOf> <https://example.org/wallet/wallet>");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet/TabHome> <http://www.w3.org/2000/01/rdf-schema#label> \"Home\"@en");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet/TabHome> <http://www.w3.org/2000/01/rdf-schema#label> \"Koti\"@fi");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet/TabHome> <http://www.w3.org/2000/01/rdf-schema#comment> \"Bottom navigation label.\"^^<http://www.w3.org/2001/XMLSchema#string>");
        CollectionAssert.Contains(rendered, "<https://example.org/wallet/wallet/TabIntro> <http://www.w3.org/2000/01/rdf-schema#label> \"The wallet stores the claims you hold.\"@en");
        Assert.IsFalse(rendered.Any(triple => triple.StartsWith("<https://example.org/wallet/wallet/TabIntro>", StringComparison.Ordinal) && triple.Contains("@fi", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PercentEncodesIdsInResourceIris()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet.core"><unit id="Tab:Home"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), Base);

        Assert.IsTrue(quads.Any(quad => quad.Subject.ToString() == "<https://example.org/wallet/wallet.core/Tab%3AHome>"));
    }

    [TestMethod]
    public void PercentEncodesTheFileIdInTheFileResourceIri()
    {
        //Kills the mutant that appends the raw file id in FileIri instead of Uri.EscapeDataString:
        //a colon in the file id would otherwise appear unencoded, changing the IRI's authority.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet:core"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), Base);

        Assert.IsTrue(quads.Any(quad => quad.Subject.ToString() == "<https://example.org/wallet/wallet%3Acore>"));
    }

    [TestMethod]
    public void ProjectsScopesAsSubjects()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:mda="urn:oasis:names:tc:xliff:metadata:2.0" version="2.1" srcLang="en">
              <file id="wallet">
                <unit id="A">
                  <mda:metadata><mda:metaGroup category="vericula:scopes"><mda:meta type="scope">shell</mda:meta></mda:metaGroup></mda:metadata>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), Base);

        Assert.IsTrue(quads.Any(quad => quad.Predicate == WellKnownProjectionTerms.SubjectNode && quad.Object.ToString().Contains("\"shell\"", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProjectsNotesAsUntaggedPlainLiterals()
    {
        //r1-39: XLIFF gives <note> no xml:lang, so a note in a language other than srcLang (a target-language
        //reviewer comment, for example) must not be asserted as though it were in the source language.
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(document, Base);

        Quad comment = quads.Single(quad => quad.Predicate == WellKnownProjectionTerms.CommentNode);
        Assert.AreEqual("\"Bottom navigation label.\"^^<http://www.w3.org/2001/XMLSchema#string>", comment.Object.ToString());
    }

    [TestMethod]
    public void EmitsTheTargetLanguageWithADistinctPredicateFromTheSourceLanguage()
    {
        //r1-40: srcLang == trgLang used to be indistinguishable in the graph; now the target tag also
        //carries vericula:targetLanguage, so a consumer can tell which of two equal dcterms:language values
        //is the target even when the languages coincide.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="en">
              <file id="wallet"><unit id="A"><segment><source>One</source><target>One!</target></segment></unit></file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), Base);

        Quad targetLanguage = quads.Single(quad => quad.Predicate == WellKnownProjectionTerms.TargetLanguageNode);
        Assert.AreEqual("\"en\"^^<http://www.w3.org/2001/XMLSchema#string>", targetLanguage.Object.ToString());
        Assert.AreEqual(2, quads.Count(quad => quad.Predicate == WellKnownProjectionTerms.LanguageNode));
    }

    [TestMethod]
    public void RefusesAFileOrUnitIdOfDotOrDotDot()
    {
        //r1-41: '.' and '..' are legal NMTOKENs but RFC 3986 dot-segment normalization would fold an IRI
        //built from them onto an unrelated resource, so Project must refuse them rather than mint one.
        XliffFile fileNamedDot = new(".", new LanguageTag("en"), null, null, null, null, [], [XliffUnit.FromText("A", "text")]);
        XliffFile unitNamedDotDot = new("wallet", new LanguageTag("en"), null, null, null, null, [], [XliffUnit.FromText("..", "text")]);

        ArgumentException dotFile = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(new XliffDocument(XliffVersion.V20, [fileNamedDot]), Base));
        ArgumentException dotDotUnit = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(new XliffDocument(XliffVersion.V20, [unitNamedDotDot]), Base));

        Assert.Contains("file id '.'", dotFile.Message);
        Assert.Contains("unit id '..'", dotDotUnit.Message);
    }

    [TestMethod]
    public void ResolvesFileAndUnitIrisTheSameWayWhetherTheBaseHasATrailingSlash()
    {
        //r1-42: a base IRI without a trailing delimiter used to be concatenated straight onto the file id,
        //gluing "wallet" onto "w" instead of naming a child resource; resolution must insert the slash.
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        ImmutableArray<Quad> withSlash = XliffGraphProjection.Project(document, new Uri("https://example.org/wallet/"));
        ImmutableArray<Quad> withoutSlash = XliffGraphProjection.Project(document, new Uri("https://example.org/wallet"));

        string[] renderedWithSlash = withSlash.Select(quad => quad.Subject.ToString()).ToArray();
        string[] renderedWithoutSlash = withoutSlash.Select(quad => quad.Subject.ToString()).ToArray();
        CollectionAssert.AreEqual(renderedWithSlash, renderedWithoutSlash);
        Assert.IsTrue(renderedWithSlash.Contains("<https://example.org/wallet/wallet>"));
    }

    [TestMethod]
    public void ResolvesFileAndUnitIrisUnderAHashNamespaceBase()
    {
        //p1-1: a base IRI ending in '#' is the standard hash-namespace convention (this library's own
        //vericula: namespace, WellKnownProjectionTerms.VericulaNamespace, is one). Appending "/" onto
        //the base's AbsoluteUri string used to move the appended text into the fragment; re-parsing
        //that string and then resolving through new Uri(base, relative) per RFC 3986 §5.3 dropped the
        //fragment and the "wallet" path segment entirely, folding every hash-namespaced base sharing a
        //host onto the same IRI. The separator between a file id and a unit id stays '/', as it already
        //was for a slash-style base.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet.core"><unit id="Tab:Home"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), new Uri("https://example.org/wallet#"));

        Assert.IsTrue(quads.Any(quad => quad.Subject.ToString() == "<https://example.org/wallet#wallet.core>"));
        Assert.IsTrue(quads.Any(quad => quad.Subject.ToString() == "<https://example.org/wallet#wallet.core/Tab%3AHome>"));
    }

    [TestMethod]
    public void ResolvesFileIriByInsertingASlashWhenTheBaseHasNeitherAHashNorASlash()
    {
        //p1-1: mirrors ResolvesFileAndUnitIrisUnderAHashNamespaceBase for the plain path-base case,
        //pinning that a base ending in neither '#' nor '/' still gets exactly one '/' inserted.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet.core"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), new Uri("https://example.org/wallet"));

        Assert.IsTrue(quads.Any(quad => quad.Subject.ToString() == "<https://example.org/wallet/wallet.core>"));
    }

    [TestMethod]
    public void RefusesABaseIriWithAQueryComponent()
    {
        //p1-1: resource IRIs are now built by appending onto the base's string form rather than
        //resolving through it, so a query component on the base would silently be extended instead of
        //naming a new resource; the base must be refused up front instead.
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(document, new Uri("https://example.org/wallet?x=1")));

        //XliffGraphProjection.cs:270: kills the mutant that empties the message, dropping the offending base IRI.
        Assert.Contains("https://example.org/wallet?x=1", exception.Message);
    }

    [TestMethod]
    public void UnitsInheritTheScopesOfEveryEnclosingGroupAsSubjects()
    {
        //r1-44: a group's scope documents "metadata over a subtree of the file" (XliffGroup remarks); a
        //unit nested under a scoped group must carry that scope as a subject in addition to its own.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:mda="urn:oasis:names:tc:xliff:metadata:2.0" version="2.1" srcLang="en">
              <file id="wallet">
                <group id="Outer">
                  <mda:metadata><mda:metaGroup category="vericula:scopes"><mda:meta type="scope">shell</mda:meta></mda:metaGroup></mda:metadata>
                  <group id="Inner">
                    <mda:metadata><mda:metaGroup category="vericula:scopes"><mda:meta type="scope">nav</mda:meta></mda:metaGroup></mda:metadata>
                    <unit id="A">
                      <mda:metadata><mda:metaGroup category="vericula:scopes"><mda:meta type="scope">tab</mda:meta></mda:metaGroup></mda:metadata>
                      <segment><source>Home</source></segment>
                    </unit>
                  </group>
                </group>
              </file>
            </xliff>
            """;

        ImmutableArray<Quad> quads = XliffGraphProjection.Project(Read(xliff), Base);

        string[] subjects = quads.Where(quad => quad.Predicate == WellKnownProjectionTerms.SubjectNode).Select(quad => quad.Object.ToString()).ToArray();
        CollectionAssert.Contains(subjects, "\"tab\"^^<http://www.w3.org/2001/XMLSchema#string>");
        CollectionAssert.Contains(subjects, "\"shell\"^^<http://www.w3.org/2001/XMLSchema#string>");
        CollectionAssert.Contains(subjects, "\"nav\"^^<http://www.w3.org/2001/XMLSchema#string>");
    }

    [TestMethod]
    public void RefusesATargetOnAFileWithNoTargetLanguage()
    {
        //r1-45: XliffReader.ValidateFile and XliffWriter.Problems both refuse this state; a document built
        //in memory can still reach it, and Project must refuse it too instead of silently dropping the target.
        var file = new XliffFile("wallet", new LanguageTag("en"), null, null, null, null, [], [XliffUnit.FromText("A", "src", "tgt")]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(document, Base));

        Assert.Contains("declares no target language", exception.Message);
    }

    [TestMethod]
    public void RefusesAnEmptyOrMalformedLanguageTagBuiltByHand()
    {
        //r1-37 (projection half): a hand-built document bypasses the reader's srcLang/trgLang validation,
        //so Project must refuse an empty or non-BCP-47-shaped tag itself rather than mint unparseable Turtle.
        var emptySource = new XliffFile("wallet", new LanguageTag(""), null, null, null, null, [], [XliffUnit.FromText("A", "src")]);
        var malformedTarget = new XliffFile("wallet", new LanguageTag("en"), new LanguageTag("en US"), null, null, null, [], [XliffUnit.FromText("A", "src", "tgt")]);

        ArgumentException sourceException = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(new XliffDocument(XliffVersion.V20, [emptySource]), Base));
        ArgumentException targetException = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(new XliffDocument(XliffVersion.V20, [malformedTarget]), Base));

        //XliffGraphProjection.cs:143 and :148 pass "source"/"target" as the role; kills the mutant that empties either literal.
        Assert.Contains("source language tag", sourceException.Message);
        Assert.Contains("target language tag", targetException.Message);
    }

    [TestMethod]
    public void RefusesALanguageTagWhoseSubtagExceedsEightCharactersEvenWithoutAHyphen()
    {
        //p1-2: the projection's own hand-rolled shape check verified only that each subtag was ASCII
        //letters or digits, with no length bound, so a 9-letter hyphen-free tag like "abcdefghi" passed
        //it even though Lumoin.Vericula.Documents.LanguageTag.IsWellFormed (the same check
        //XliffReader/XliffWriter already enforce srcLang/trgLang against) refuses it. The projection
        //must call that shared check, not a weaker one of its own.
        var file = new XliffFile("wallet", new LanguageTag("abcdefghi"), null, null, null, null, [], [XliffUnit.FromText("A", "src")]);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(new XliffDocument(XliffVersion.V20, [file]), Base));

        Assert.Contains("not a well-formed BCP 47 tag", exception.Message);
    }

    [TestMethod]
    public void WritesDeterministicTurtleWithTheConventionalPrefixes()
    {
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        string first = Turtle(document);
        string second = Turtle(document);

        Assert.AreEqual(first, second);
        Assert.IsTrue(first.Contains("@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#>", StringComparison.Ordinal), first);
        Assert.IsTrue(first.Contains("@prefix dcterms: <http://purl.org/dc/terms/>", StringComparison.Ordinal), first);
        //r1-40: the vericula prefix carries the target-language-distinguishing term, so it must be declared
        //whenever a target language quad is emitted.
        Assert.IsTrue(first.Contains("@prefix vericula: <https://vericula.lumoin.com/vocab#>", StringComparison.Ordinal), first);
        Assert.IsTrue(first.Contains("\"Koti\"@fi", StringComparison.Ordinal), first);
        Assert.IsTrue(first.Contains("\"Home\"@en", StringComparison.Ordinal), first);
    }

    [TestMethod]
    public async Task WritesTurtleToAPipeAndCompletesIt()
    {
        XliffDocument document = Read(XliffReaderTests.WalletXliff);
        var pipe = new Pipe();

        XliffGraphProjection.WriteTurtle(document, Base, pipe.Writer);
        ReadResult result = await pipe.Reader.ReadAsync(TestContext.CancellationToken);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual(Turtle(document), Encoding.UTF8.GetString(result.Buffer.ToArray()));
        pipe.Reader.AdvanceTo(result.Buffer.End);
    }

    [TestMethod]
    public void RejectsARelativeBaseIri()
    {
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => XliffGraphProjection.Project(document, new Uri("wallet/", UriKind.Relative)));

        //XliffGraphProjection.cs:73: kills the mutant that empties the message, the only text saying WHY the IRI was refused.
        Assert.Contains("must be absolute", exception.Message);
    }

    [TestMethod]
    public void ThrowsArgumentNullExceptionForNullPipeWriterBeforeValidatingTheBaseIri()
    {
        //Kills the mutant at XliffGraphProjection.cs:89 that removes ArgumentNullException.ThrowIfNull(output): without
        //the guard, a relative base IRI is evaluated first and surfaces Project's ArgumentException instead of the
        //writer's own ArgumentNullException, since the guard normally runs before Project is ever called.
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        Assert.ThrowsExactly<ArgumentNullException>(() => XliffGraphProjection.WriteTurtle(document, new Uri("wallet/", UriKind.Relative), (PipeWriter)null!));
    }

    [TestMethod]
    public void ThrowsArgumentNullExceptionForNullStreamBeforeValidatingTheBaseIri()
    {
        //Kills the mutant at XliffGraphProjection.cs:104 that removes ArgumentNullException.ThrowIfNull(stream):
        //without the guard, a relative base IRI is evaluated first and surfaces Project's ArgumentException instead
        //of the stream's own ArgumentNullException, since the guard normally runs before Project is ever called.
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        Assert.ThrowsExactly<ArgumentNullException>(() => XliffGraphProjection.WriteTurtle(document, new Uri("wallet/", UriKind.Relative), (Stream)null!));
    }

    [TestMethod]
    public void LeavesTheStreamOpenAfterWritingTurtle()
    {
        //Kills the mutant at XliffGraphProjection.cs:107 that flips leaveOpen: true to false: TurtleWriter.Write
        //completes the pipe writer, and a StreamPipeWriter built with leaveOpen: false disposes its inner stream on
        //Complete, so the caller's stream would no longer be usable once the call returns.
        XliffDocument document = Read(XliffReaderTests.WalletXliff);
        using var stream = new MemoryStream();

        XliffGraphProjection.WriteTurtle(document, Base, stream);

        Assert.IsTrue(stream.CanWrite);
    }

    [TestMethod]
    public void ThrowsForNullArguments()
    {
        XliffDocument document = Read(XliffReaderTests.WalletXliff);

        Assert.ThrowsExactly<ArgumentNullException>(() => XliffGraphProjection.Project(null!, Base));
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffGraphProjection.Project(document, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffGraphProjection.WriteTurtle(document, Base, (Stream)null!));
    }

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Projects <paramref name="document"/> into a graph and writes it as Turtle, against <see cref="Base"/>.
    /// </summary>
    /// <param name="document">The document to project.</param>
    /// <returns>The Turtle text.</returns>
    private static string Turtle(XliffDocument document)
    {
        using var stream = new MemoryStream();
        XliffGraphProjection.WriteTurtle(document, Base, stream);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Reads one document from its raw XLIFF text.
    /// </summary>
    /// <param name="xliff">The raw XLIFF text.</param>
    /// <returns>The parsed document.</returns>
    private static XliffDocument Read(string xliff)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        return XliffReader.Read(stream);
    }
}
