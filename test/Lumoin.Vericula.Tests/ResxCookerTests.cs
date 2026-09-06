using System.Text;
using System.Xml.Linq;
using Lumoin.Vericula.Cooking;
using Lumoin.Vericula.Parsing;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class ResxCookerTests
{
    private const string EnFi = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
          <file id="wallet">
            <unit id="TabHome"><segment><source>Home</source><target>Koti</target></segment></unit>
            <unit id="Quote"><segment><source>The wallet is "locked".</source><target>Lompakko on "lukittu".</target></segment></unit>
            <unit id="Untranslated"><segment><source>Only English.</source></segment></unit>
          </file>
        </xliff>
        """;

    private const string EnDe = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="de">
          <file id="wallet">
            <unit id="TabHome"><segment><source>Home</source><target>Startseite</target></segment></unit>
          </file>
        </xliff>
        """;

    private static readonly string[] EnFiResources = ["wallet.resx", "wallet.fi.resx"];
    private static readonly string[] EnFiDeResources = ["wallet.resx", "wallet.fi.resx", "wallet.de.resx"];
    private static readonly string[] WalletBaseResources = ["Wallet.resx", "Wallet.fi.resx"];

    [TestMethod]
    public void EmitsNeutralAndOneSatellitePerCulture()
    {
        ImmutableArrayLike resources = Cook(EnFi);

        CollectionAssert.AreEquivalent(EnFiResources, resources.FileNames);
    }

    [TestMethod]
    public void NeutralCarriesSourceTextForEveryUnit()
    {
        Dictionary<string, string> neutral = DataOf(Cook(EnFi), "wallet.resx");

        Assert.AreEqual("Home", neutral["TabHome"]);
        Assert.AreEqual("The wallet is \"locked\".", neutral["Quote"]);
        Assert.AreEqual("Only English.", neutral["Untranslated"]);
    }

    [TestMethod]
    public void SatelliteCarriesOnlyTranslatedUnits()
    {
        Dictionary<string, string> finnish = DataOf(Cook(EnFi), "wallet.fi.resx");

        Assert.AreEqual("Koti", finnish["TabHome"]);
        Assert.AreEqual("Lompakko on \"lukittu\".", finnish["Quote"]);
        Assert.IsFalse(finnish.ContainsKey("Untranslated"));
    }

    private const string WalletEnglishFirstDraft = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
          <file id="wallet">
            <unit id="TabHome"><segment><source>Home v1</source></segment></unit>
          </file>
        </xliff>
        """;

    private const string WalletEnglishSecondDraft = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
          <file id="wallet">
            <unit id="TabHome"><segment><source>Home v2</source></segment></unit>
          </file>
        </xliff>
        """;

    [TestMethod]
    public void TheLastDocumentToDeclareAUnitIdWinsTheNeutralSource()
    {
        //Kills the mutant that folds the neutral source with TryAdd (first-write-wins) instead of
        //the indexer assignment (last-write-wins): the two documents below share the unit id
        //"TabHome" with different source text, so only last-write-wins produces "Home v2".
        Dictionary<string, string> neutral = DataOf(Cook(WalletEnglishFirstDraft, WalletEnglishSecondDraft), "wallet.resx");

        Assert.AreEqual("Home v2", neutral["TabHome"]);
    }

    [TestMethod]
    public void FoldsMultipleLanguageDocumentsIntoOneSet()
    {
        ImmutableArrayLike resources = Cook(EnFi, EnDe);

        CollectionAssert.AreEquivalent(EnFiDeResources, resources.FileNames);
        Assert.AreEqual("Startseite", DataOf(resources, "wallet.de.resx")["TabHome"]);
    }

    [TestMethod]
    public void BaseNameOptionOverridesTheFileId()
    {
        ImmutableArrayLike resources = Cook(new ResxCookOptions { BaseName = "Wallet" }, EnFi);

        CollectionAssert.AreEquivalent(WalletBaseResources, resources.FileNames);
    }

    [TestMethod]
    public void EmitsTheStandardResxHeaderAndPreservesSpace()
    {
        string neutral = Cook(EnFi).Single("wallet.resx");
        XDocument document = TestXml.Parse(neutral);

        XElement mime = document.Root!.Elements("resheader").Single(header => (string?)header.Attribute("name") == "resmimetype");
        Assert.AreEqual("text/microsoft-resx", mime.Element("value")!.Value);

        XElement home = document.Root!.Elements("data").Single(data => (string?)data.Attribute("name") == "TabHome");
        Assert.AreEqual("preserve", (string?)home.Attribute(XNamespace.Xml + "space"));
    }

    //r1-74: the exact bytes cooked before ResxCooker's element, attribute and resheader literals moved
    //into WellKnownResxElements, WellKnownResxAttributes and WellKnownResxHeaderValues, captured from
    //the committed cooker prior to that refactor. Pins that moving the literals into the vocabulary
    //classes changed no byte of the output.
    private const string GoldenNeutralResx = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype">
            <value>text/microsoft-resx</value>
          </resheader>
          <resheader name="version">
            <value>2.0</value>
          </resheader>
          <resheader name="reader">
            <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
          </resheader>
          <resheader name="writer">
            <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
          </resheader>
          <data name="Quote" xml:space="preserve">
            <value>The wallet is "locked".</value>
          </data>
          <data name="TabHome" xml:space="preserve">
            <value>Home</value>
          </data>
          <data name="Untranslated" xml:space="preserve">
            <value>Only English.</value>
          </data>
        </root>
        """;

    private const string GoldenFinnishSatelliteResx = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype">
            <value>text/microsoft-resx</value>
          </resheader>
          <resheader name="version">
            <value>2.0</value>
          </resheader>
          <resheader name="reader">
            <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
          </resheader>
          <resheader name="writer">
            <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
          </resheader>
          <data name="Quote" xml:space="preserve">
            <value>Lompakko on "lukittu".</value>
          </data>
          <data name="TabHome" xml:space="preserve">
            <value>Koti</value>
          </data>
        </root>
        """;

    [TestMethod]
    public void CooksByteIdenticalResxAfterTheVocabularyMoveToWellKnownResxClasses()
    {
        ImmutableArrayLike resources = Cook(EnFi);

        Assert.AreEqual(GoldenNeutralResx, resources.Single("wallet.resx"));
        Assert.AreEqual(GoldenFinnishSatelliteResx, resources.Single("wallet.fi.resx"));
    }

    [TestMethod]
    public void SkipsEmptyTargetSoTheNeutralFallbackWins()
    {
        const string emptyTarget = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="Done"><segment><source>Home</source><target>Koti</target></segment></unit>
                <unit id="Blank"><segment><source>Settings</source><target></target></segment></unit>
              </file>
            </xliff>
            """;

        Dictionary<string, string> finnish = DataOf(Cook(emptyTarget), "wallet.fi.resx");
        Assert.AreEqual("Koti", finnish["Done"]);
        Assert.IsFalse(finnish.ContainsKey("Blank"));
    }

    [TestMethod]
    public void ACultureWithNoNonEmptyTargetGetsNoSatellite()
    {
        //Kills the mutant that calls TableFor for the culture unconditionally: a file that declares
        //a target language but whose only unit has an empty target must produce no satellite at all,
        //not an empty one, since an emitted-but-empty satellite would still shadow the neutral resx.
        const string allTargetsEmpty = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="Blank"><segment><source>Settings</source><target></target></segment></unit>
              </file>
            </xliff>
            """;

        ImmutableArrayLike resources = Cook(allTargetsEmpty);

        CollectionAssert.DoesNotContain(resources.FileNames, "wallet.fi.resx");
    }

    [TestMethod]
    public void PreservesBareLineFeedsInValues()
    {
        const string multiline =
            "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\" trgLang=\"fi\">" +
            "<file id=\"wallet\"><unit id=\"M\"><segment><source>line1\nline2</source><target>rivi1\nrivi2</target></segment></unit></file></xliff>";

        string finnish = Cook(multiline).Single("wallet.fi.resx");
        Assert.Contains("rivi1\nrivi2", finnish, StringComparison.Ordinal);
        Assert.DoesNotContain("rivi1\r\nrivi2", finnish, StringComparison.Ordinal);
    }

    [TestMethod]
    public void PreservesCarriageReturnsAndCrlfInValuesThroughAnXmlReadBack()
    {
        //r1-59: NewLineHandling.None wrote a raw CR/CRLF into the resx; every conformant XML reader
        //(XML 1.0 section 2.11), ResXResourceReader included, then normalizes a literal CR or CRLF in
        //text content to a bare LF on input, silently losing the distinction. Entitize survives that
        //normalization because only the character reference &#xD; is preserved. The input below spells
        //CR as the character reference &#xD; (as XliffWriter itself does) so the model text genuinely
        //contains \r rather than having it normalized away by XliffReader's own XML parsing.
        const string multiline =
            "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\" trgLang=\"fi\">" +
            "<file id=\"wallet\"><unit id=\"M\"><segment><source>a</source><target>rivi1&#xD;\nrivi2&#xD;rivi3</target></segment></unit></file></xliff>";

        ImmutableArrayLike resources = Cook(multiline);
        string finnish = resources.Single("wallet.fi.resx");

        Assert.Contains("&#xD;", finnish, StringComparison.Ordinal);
        Assert.AreEqual("rivi1\r\nrivi2\rrivi3", DataOf(resources, "wallet.fi.resx")["M"]);
    }

    [TestMethod]
    public void CookedBytesUseLineFeedIndentationRegardlessOfHostNewLine()
    {
        //r1-60: XmlWriterSettings.NewLineChars defaults to Environment.NewLine, so the indentation
        //between elements differed between Windows and Linux/macOS. Pinning it to "\n" (mirroring
        //XliffWriter.CreateWriterSettings) makes the cooked bytes reproducible across hosts.
        string neutral = Cook(EnFi).Single("wallet.resx");

        Assert.DoesNotContain("\r\n", neutral, StringComparison.Ordinal);
    }

    [TestMethod]
    public void UnitFlaggedNeedsTranslationGetsNoSatelliteEntry()
    {
        //r1-62: a stale target left on a segment marked NeedsTranslation must not ship, because it
        //translates the unit's old source, not its current one. This falls out of XliffUnit.Target
        //folding to null while any translatable segment needs translation (r1-58's contract), so the
        //cooker needs no code of its own; this test is the killer that would catch a regression there.
        const string staleTranslation = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="Settings"><segment subState="vericula:needsTranslation"><source>Settings v2</source><target>Asetukset (vanha)</target></segment></unit>
              </file>
            </xliff>
            """;

        ImmutableArrayLike resources = Cook(staleTranslation);

        Assert.AreEqual("Settings v2", DataOf(resources, "wallet.resx")["Settings"]);
        CollectionAssert.DoesNotContain(resources.FileNames, "wallet.fi.resx");
    }

    [TestMethod]
    public void ThrowsWhenTwoFilesOfOneDocumentContributeTheSameUnitIdWithDifferentSource()
    {
        //r1-61: two <file> elements are only unique by unit id within themselves (XLIFF 2.1 section
        //4.2.3), so this is legal input, but folding it into one neutral resx under one key would
        //silently drop whichever file's text loses the last-write race.
        const string collidingFiles = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="ui"><unit id="Title"><segment><source>Wallet</source></segment></unit></file>
              <file id="errors"><unit id="Title"><segment><source>Error</source></segment></unit></file>
            </xliff>
            """;

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => Cook(new ResxCookOptions { BaseName = "app" }, collidingFiles));
        Assert.Contains("Title", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ui", exception.Message, StringComparison.Ordinal);
        Assert.Contains("errors", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AllowsTheSameUnitIdWithTheSameSourceAcrossTwoFilesOfOneDocument()
    {
        //r1-61: the collision refusal must not fire when the source text agrees, so a document built
        //from otherwise-identical per-file fragments (or a genuine coincidence) still cooks.
        const string agreeingFiles = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="ui"><unit id="Ok"><segment><source>OK</source></segment></unit></file>
              <file id="dialogs"><unit id="Ok"><segment><source>OK</source></segment></unit></file>
            </xliff>
            """;

        Dictionary<string, string> neutral = DataOf(Cook(new ResxCookOptions { BaseName = "app" }, agreeingFiles), "app.resx");

        Assert.AreEqual("OK", neutral["Ok"]);
    }

    [TestMethod]
    public void ThrowsWhenTwoFilesOfOneDocumentContributeTheSameUnitIdWithDifferentTarget()
    {
        //f-13: source text agrees ("Wallet" in both files) so the existing source-collision guard
        //never fires, but the two files disagree on the fr target for the same unit id; folding both
        //into cultures["fr"]["Title"] unconditionally would silently let whichever file is enumerated
        //last discard the other's translation with no diagnostic.
        const string collidingTargets = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fr">
              <file id="ui" trgLang="fr"><unit id="Title"><segment><source>Wallet</source><target>Portefeuille</target></segment></unit></file>
              <file id="errors" trgLang="fr"><unit id="Title"><segment><source>Wallet</source><target>Erreur</target></segment></unit></file>
            </xliff>
            """;

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => Cook(new ResxCookOptions { BaseName = "app" }, collidingTargets));
        Assert.Contains("Title", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ui", exception.Message, StringComparison.Ordinal);
        Assert.Contains("errors", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AllowsTheSameUnitIdWithTheSameTargetAcrossTwoFilesOfOneDocument()
    {
        //f-13: the target collision refusal must not fire when the target text agrees, so two files
        //that happen to translate a shared unit id the same way still cook.
        const string agreeingTargets = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fr">
              <file id="ui" trgLang="fr"><unit id="Ok"><segment><source>OK</source><target>OK</target></segment></unit></file>
              <file id="dialogs" trgLang="fr"><unit id="Ok"><segment><source>OK</source><target>OK</target></segment></unit></file>
            </xliff>
            """;

        Dictionary<string, string> fr = DataOf(Cook(new ResxCookOptions { BaseName = "app" }, agreeingTargets), "app.fr.resx");

        Assert.AreEqual("OK", fr["Ok"]);
    }

    [TestMethod]
    public void ThrowsWhenTheBaseNamesTrailingSegmentIsACultureName()
    {
        //r1-65: a base name such as "wallet.en" would make MSBuild's AssignCulture treat the neutral
        //resource as an English satellite instead of the neutral resource the framework looks up first.
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => Cook(new ResxCookOptions { BaseName = "wallet.en" }, EnFi));
        Assert.Contains("wallet.en", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ThrowsArgumentNullExceptionWhenDocumentsIsNull()
    {
        //ResxCooker.cs:40, ArgumentNullException.ThrowIfNull(documents); => ; — removing the guard
        //would let a null documents collection reach the foreach below and fail with a
        //NullReferenceException instead of the documented ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(() => ResxCooker.Cook(null!));
    }

    [TestMethod]
    public void CookingNoDocumentsReturnsAnEmptyResourceSet()
    {
        //ResxCooker.cs:91, { return ImmutableArray<CookedResource>.Empty; } => {} — without the early
        //return, baseName stays null and execution falls into baseName.LastIndexOf('.') below,
        //throwing a NullReferenceException instead of yielding the documented empty result.
        ImmutableArrayLike resources = Cook();

        Assert.HasCount(0, resources.FileNames);
    }

    [TestMethod]
    public void ThrowsWhenTheBaseNameStartsWithADotFollowedByACultureName()
    {
        //ResxCooker.cs:100, lastDot >= 0 => lastDot > 0 — when the base name's only dot is its first
        //character (lastDot == 0), >= 0 still reads the segment after it and finds a culture name,
        //while > 0 would treat lastDot as "no dot found" and let the base name through unchecked.
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => Cook(new ResxCookOptions { BaseName = ".en" }, EnFi));

        Assert.Contains(".en", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void FoldsUnitsFromNestedGroupsIntoTheNeutralResource()
    {
        //ResxCooker.cs:134,148,155, each yield return unit; => ; in EnumerateUnits — dropping any one
        //of these yields loses either every unit under a <group>, a group's own units, or units
        //bubbled up from a nested <group>, so a unit one or two group levels deep would go missing
        //from the neutral resource instead of being enumerated depth-first.
        const string nestedGroups = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <group id="g1">
                  <unit id="InGroup"><segment><source>In group</source></segment></unit>
                  <group id="g2">
                    <unit id="InNestedGroup"><segment><source>In nested group</source></segment></unit>
                  </group>
                </group>
              </file>
            </xliff>
            """;

        Dictionary<string, string> neutral = DataOf(Cook(nestedGroups), "wallet.resx");

        Assert.AreEqual("In group", neutral["InGroup"]);
        Assert.AreEqual("In nested group", neutral["InNestedGroup"]);
    }

    [TestMethod]
    public void BaseNameWhoseTrailingSegmentIsNotACultureCooksWithoutThrowing()
    {
        //ResxCooker.cs:169, CultureInfo.GetCultureInfo(value); => ; and ResxCooker.cs:175, return
        //false; => return true; both make IsCulture report every non-empty trailing segment as a
        //culture. A base name ending in an unregistered word like "notaculture" must still cook
        //without throwing under the correct code, since CultureInfo.GetCultureInfo rejects it and the
        //catch block returns false. On ICU (Linux, macOS), GetCultureInfo("notaculture") succeeds
        //unless predefinedOnly is true, so dropping that flag from IsCulture brings the failure back
        //on Linux and macOS while Windows (NLS, which already rejects the name) stays green.
        ImmutableArrayLike resources = Cook(new ResxCookOptions { BaseName = "Wallet.notaculture" }, EnFi);

        CollectionAssert.Contains(resources.FileNames, "Wallet.notaculture.resx");
    }

    /// <summary>
    /// Reads and cooks the given XLIFF documents with default options.
    /// </summary>
    /// <param name="xliffDocuments">The raw XLIFF text of each document to cook.</param>
    /// <returns>The cooked resources.</returns>
    private static ImmutableArrayLike Cook(params string[] xliffDocuments)
    {
        return Cook(options: null, xliffDocuments);
    }

    /// <summary>
    /// Reads and cooks the given XLIFF documents with the given options.
    /// </summary>
    /// <param name="options">The cook options, or null for defaults.</param>
    /// <param name="xliffDocuments">The raw XLIFF text of each document to cook.</param>
    /// <returns>The cooked resources.</returns>
    private static ImmutableArrayLike Cook(ResxCookOptions? options, params string[] xliffDocuments)
    {
        XliffDocument[] documents = xliffDocuments.Select(Read).ToArray();

        return new ImmutableArrayLike(ResxCooker.Cook(documents, options));
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

    /// <summary>
    /// Parses one cooked resx file's <c>&lt;data&gt;</c> entries into a name-to-value dictionary.
    /// </summary>
    /// <param name="resources">The cooked resources to pick <paramref name="fileName"/> from.</param>
    /// <param name="fileName">The resx file's name.</param>
    /// <returns>The file's data entries, keyed by name.</returns>
    private static Dictionary<string, string> DataOf(ImmutableArrayLike resources, string fileName)
    {
        XDocument document = TestXml.Parse(resources.Single(fileName));

        return document.Root!
            .Elements("data")
            .ToDictionary(
                data => (string)data.Attribute("name")!,
                data => data.Element("value")!.Value,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// A thin wrapper so the test bodies read clearly without repeating LINQ over the cooked set.
    /// </summary>
    private sealed class ImmutableArrayLike
    {
        /// <summary>The cooked resources this instance wraps.</summary>
        private readonly IReadOnlyList<CookedResource> resources;

        /// <summary>
        /// Initializes a new instance wrapping <paramref name="resources"/>.
        /// </summary>
        /// <param name="resources">The cooked resources to wrap.</param>
        public ImmutableArrayLike(IReadOnlyList<CookedResource> resources)
        {
            this.resources = resources;
        }

        /// <summary>The file name of every wrapped resource.</summary>
        public string[] FileNames => resources.Select(resource => resource.FileName).ToArray();

        /// <summary>
        /// Returns the content of the one resource named <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The resource's file name.</param>
        /// <returns>The resource's content.</returns>
        public string Single(string fileName) => resources.Single(resource => resource.FileName == fileName).Content;
    }
}
