using System.Text;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class XliffReaderTests
{
    internal const string WalletXliff = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
          <file id="wallet">
            <group id="Shell">
              <unit id="TabHome">
                <notes><note>Bottom navigation label.</note></notes>
                <segment>
                  <source>Home</source>
                  <target>Koti</target>
                </segment>
              </unit>
              <unit id="TabIntro">
                <segment>
                  <source>The wallet stores the claims you hold.</source>
                </segment>
              </unit>
            </group>
          </file>
        </xliff>
        """;

    [TestMethod]
    public void ReadsVersionLanguagesAndFileId()
    {
        XliffDocument document = Read(WalletXliff);

        Assert.AreEqual(XliffVersion.V20, document.Version);
        Assert.HasCount(1, document.Files);
        Assert.AreEqual("wallet", document.Files[0].Id);
        Assert.AreEqual("en", document.Files[0].SourceLanguage.Value);
        Assert.AreEqual("fi", document.Files[0].TargetLanguage?.Value);
    }

    [TestMethod]
    public void ReadsUnitsSourceTargetAndNotesUnderGroups()
    {
        XliffDocument document = Read(WalletXliff);

        XliffGroup shell = document.Files[0].Groups.Single();
        Assert.AreEqual("Shell", shell.Id);

        XliffUnit home = shell.Units.Single(unit => unit.Id == "TabHome");
        Assert.AreEqual("Home", home.Source);
        Assert.AreEqual("Koti", home.Target);
        Assert.AreEqual("Bottom navigation label.", home.Notes.Single());
    }

    [TestMethod]
    public void LeavesTargetNullWhenNoTargetElementIsPresent()
    {
        XliffDocument document = Read(WalletXliff);

        XliffUnit intro = document.Files[0].Groups.Single().Units.Single(unit => unit.Id == "TabIntro");
        Assert.IsNull(intro.Target);
        Assert.AreEqual("The wallet stores the claims you hold.", intro.Source);
    }

    [TestMethod]
    public void RecognizesXliff21()
    {
        XliffDocument document = Read(WalletXliff.Replace("version=\"2.0\"", "version=\"2.1\"", StringComparison.Ordinal));

        Assert.AreEqual(XliffVersion.V21, document.Version);
    }

    [TestMethod]
    public async Task ReadAsyncMatchesRead()
    {
        //The old assertion only checked one target string, so a mutant that made ReadAsync's
        //parsing diverge from Read's for any other field (a note, a scope, an id) would survive.
        //Writing each parsed document back out serializes every field of the model, so a
        //structural difference between the sync and async results shows up as a byte difference.
        XliffDocument syncDocument = Read(WalletXliff);
        using var asyncStream = new MemoryStream(Encoding.UTF8.GetBytes(WalletXliff));

        XliffDocument asyncDocument = await XliffReader.ReadAsync(asyncStream, TestContext.CancellationToken);

        using var syncOutput = new MemoryStream();
        using var asyncOutput = new MemoryStream();
        XliffWriter.Write(syncDocument, syncOutput);
        XliffWriter.Write(asyncDocument, asyncOutput);
        CollectionAssert.AreEqual(syncOutput.ToArray(), asyncOutput.ToArray());
        Assert.AreEqual("Koti", asyncDocument.Files[0].Groups.Single().Units.Single(unit => unit.Id == "TabHome").Target);
    }

    [TestMethod]
    public void ThrowsOnMalformedXml()
    {
        Assert.ThrowsExactly<XliffFormatException>(() => Read("<xliff version=\"2.0\" srcLang=\"en\""));
    }

    [TestMethod]
    public void ThrowsWhenSrcLangIsMissing()
    {
        const string missing = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0">
              <file id="wallet"><unit id="A"><segment><source>x</source></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(missing));

        //XliffReader.cs:618 throw ... => ; (statement removed) and the message mutated to "": without
        //the throw, a missing srcLang falls through to the well-formedness check instead, whose
        //different wording ("is not a well-formed BCP 47 language tag") would hide that the real
        //problem is a missing attribute, not a malformed one.
        Assert.Contains("does not declare the required srcLang attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ThrowsOnUnsupportedVersion()
    {
        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(
            () => Read(WalletXliff.Replace("version=\"2.0\"", "version=\"1.2\"", StringComparison.Ordinal)));

        //XliffReader.cs:663 unsupported-version message mutated to "": the offending version value
        //must appear so a caller can see which version string was rejected.
        Assert.Contains("1.2", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ThrowsWhenVersionIsMissing()
    {
        //XliffReader.cs:660 ParseVersion: the null branch's message names the missing version
        //attribute; asserting only the exception type would let a mutant blank that message survive.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" srcLang="en">
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("required version attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ThrowsWhenTargetsArePresentButNoTrgLangIsDeclared()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="A"><segment><source>Home</source><target>Koti</target></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:1528 ValidateFile's message => "": this text is the only way the caller learns
        //the specific invariant that failed is a missing trgLang, not any other format problem, so the
        //wording itself must survive.
        Assert.Contains("declares no trgLang", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ThrowsWhenAPartiallyTranslatedUnitHasNoTrgLang()
    {
        //r1-58: unit.Target folds to null while any translatable segment is untranslated, so the
        //trgLang check must look at the segments directly, not the folded Target, or a partially
        //translated document without trgLang would slip past silently.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="A">
                  <segment><source>First.</source><target>Eka.</target></segment>
                  <segment><source>Second.</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));
    }

    [TestMethod]
    public void ThrowsOnDuplicateUnitIdWithinAFile()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="A"><segment><source>One</source><target>Yksi</target></segment></unit>
                <group id="G"><unit id="A"><segment><source>Two</source><target>Kaksi</target></segment></unit></group>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:1516 ValidateFile's interpolated message => "": the unit id and file id are the
        //only way the caller learns which id collided and where; an empty message would still throw the
        //same exception type, so only a wording check catches this mutant.
        Assert.Contains("Duplicate unit id 'A' in file 'wallet'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ThrowsWhenAUnitInsideANestedGroupHasATargetButNoTrgLangIsDeclared()
    {
        //XliffReader.cs:1571 yield return unit; => ;: this is Flatten(XliffGroup)'s recursive-step
        //yield, the one that surfaces a NESTED group's units (via `foreach(XliffUnit unit in
        //Flatten(nested))`) up through its enclosing group; a group's own direct units use a separate
        //yield a few lines above and are unaffected. Dropping this one leaves a unit that lives inside
        //a group nested within another group invisible to ValidateFile's Flatten walk, so the
        //trgLang-required check silently never sees its target.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <group id="Outer"><group id="Inner"><unit id="A"><segment><source>Home</source><target>Koti</target></segment></unit></group></group>
              </file>
            </xliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));
    }

    [TestMethod]
    public void AcceptsAPlaceholderInSourceAndTarget()
    {
        //Was ThrowsOnUnsupportedInlineMarkup: a <ph> used to be refused outright (r1-1 predates the
        //inline content model); step 2 (5.3) parses it into a PlaceholderPart instead. Detailed
        //per-element and per-attribute coverage lives in XliffReaderInlineContentTests.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>Hello <ph id="1"/></source><target>Hei <ph id="1"/></target></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        Assert.IsInstanceOfType<PlaceholderPart>(unit.Segments[0].SourceContent.Parts[1]);
        Assert.IsInstanceOfType<PlaceholderPart>(unit.Segments[0].TargetContent!.Parts[1]);
    }

    [TestMethod]
    public void AcceptsAnIsolatedEndCode()
    {
        //Was ThrowsOnUnsupportedSpanningEndCodeMarkup: a standalone <ec> used to be refused outright
        //because it carries no text; step 2 (5.3.2) accepts it when isolated="yes" names its own id.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>Hello <ec id="1" isolated="yes"/></source><target>Hei <ec id="1" isolated="yes"/></target></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        Assert.IsInstanceOfType<EndCodePart>(unit.Segments[0].SourceContent.Parts[1]);
    }

    [TestMethod]
    public void AcceptsACodePointDecodedIntoTheText()
    {
        //Was ThrowsOnCodePointMarkup. XLIFF 2.1 §4.2.3.1 cp: "Represents a Unicode character that is
        //invalid in XML." Step 2 (5.3.1) decodes it into the text instead of refusing it.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>Hello <cp hex="A0"/> there</source></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        Assert.AreEqual("Hello   there", ((InlineTextPart)unit.Segments[0].SourceContent.Parts.Single()).Text);
    }

    [TestMethod]
    public void AcceptsAnIsolatedStartCode()
    {
        //Was ThrowsOnUnsupportedStartCodeMarkup: an unclosed <sc> used to be refused outright; step 2
        //(5.3.2) accepts it when isolated="yes" says it has no matching <ec> in this unit.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>Hello <sc id="1" isolated="yes"/> there</source></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        Assert.IsInstanceOfType<StartCodePart>(unit.Segments[0].SourceContent.Parts[1]);
    }

    [TestMethod]
    public void AcceptsAPairedCode()
    {
        //Was ThrowsOnPairedCodeMarkup (r1-1): a <pc> used to be flattened to bare text, silently
        //dropping its id and can* attributes; the reader refused it instead of flattening it. Step 2
        //(5.3) now parses it into a StartCodePart/EndCodePart pair that keeps all of that.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>Click <pc id="1" canDelete="no">here</pc> now</source></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        var start = (StartCodePart)unit.Segments[0].SourceContent.Parts[1];
        Assert.AreEqual("1", start.Id);
        Assert.IsFalse(start.CanDelete);
    }

    [TestMethod]
    public void AcceptsAMrkAnnotationMarkingTranslateNo()
    {
        //Was ThrowsOnMrkAnnotationMarkup (r1-2): a <mrk translate="no"> used to be flattened to its
        //wrapped text, silently dropping the annotation; the reader refused it instead. Step 2 (5.3)
        //now keeps the annotation as an AnnotationStartPart/AnnotationEndPart pair.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>Keep <mrk id="m1" translate="no">ACME</mrk> as is</source></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        var start = (AnnotationStartPart)unit.Segments[0].SourceContent.Parts[1];
        Assert.AreEqual(false, start.Translate);
    }

    [TestMethod]
    public void AcceptsStandaloneAnnotationMarkers()
    {
        //Was ThrowsOnStandaloneAnnotationMarkers (r1-2): standalone <sm>/<em> used to vanish silently
        //since they carry no text; the reader refused them instead. Step 2 (5.3) parses the pair into
        //an AnnotationStartPart/AnnotationEndPart with AnnotationForm.Split.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet"><unit id="A"><segment><source>A <sm id="m1" translate="no"/>B<em startRef="m1"/> C</source></segment></unit></file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();

        InlineContent content = unit.Segments[0].SourceContent;
        Assert.AreEqual(AnnotationForm.Split, ((AnnotationStartPart)content.Parts[1]).Form);
        Assert.AreEqual(AnnotationForm.Split, ((AnnotationEndPart)content.Parts[3]).Form);
    }

    [TestMethod]
    public void RejectsAUnitIdThatIsNotAnXmlNameToken()
    {
        //r1-9: the reader used to accept ids with embedded whitespace that the writer would then refuse
        //(NameTokenProblems), so a document Read() accepted could fail on Write() with no way to fix it
        //via the reader's own diagnostics. XLIFF 2.1 §4.3.1.21 id: "Value description: NMTOKEN."
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="Tab Home"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:1400 RequireNameToken's interpolated message => "": the element name and the
        //offending id value are the only way the caller learns which id was rejected and why; an empty
        //message would still throw the same exception type, so only a wording check catches this mutant.
        Assert.Contains("The <unit> id 'Tab Home' is not an XML name token", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAUnitWithOnlyIgnorableSegments()
    {
        //r1-24: a unit made only of <ignorable> content used to be accepted because the old check
        //counted ignorables too. XLIFF 2.1 §4.2.2.5 unit, Constraints: "A <unit> MUST contain at least
        //one <segment> element."
        //XliffReader.cs:799, message => "": the message names the unit that has no <segment>; without
        //asserting on it, an emptied message would still pass.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="A"><ignorable><source> </source></ignorable></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("Unit 'A' has no <segment> element", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsDuplicateSegmentIdsWithinAUnit()
    {
        //r1-25: segment/ignorable ids were never checked for uniqueness within their unit. XLIFF 2.1
        //§4.3.1.21 id: a value used on <segment> or <ignorable> "MUST be unique among all of the above
        //... within the enclosing <unit> element."
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="A">
                  <segment id="s1"><source>One</source></segment>
                  <ignorable id="s1"><source> </source></ignorable>
                </unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:829, message => "": the message names the duplicate id and its unit; without
        //asserting on it, an emptied message would still pass.
        Assert.Contains("Duplicate segment id 's1' in unit 'A'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsDuplicateFileIds()
    {
        //Root cause behind r1-38: two <file> elements sharing an id used to be accepted by the reader,
        //so the projection later merged two distinct files into one resource. XLIFF 2.1, <file> id,
        //Constraints: the value "MUST be unique among all <file> id attribute values within the
        //enclosing <xliff> element.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="A"><segment><source>One</source></segment></unit></file>
              <file id="wallet"><unit id="B"><segment><source>Two</source></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:597 duplicate-file-id message mutated to "": the id must appear so a caller
        //can find which <file> to fix.
        Assert.Contains("wallet", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsARootElementThatIsNotAnXliffElement()
    {
        //XliffReader.cs:572 Parse's throw new XliffFormatException("The document root is not an
        //<xliff> element in the XLIFF 2.x core namespace.") was mutated to a no-op (statement) and to
        //an empty message (string). The streaming reader's own root check (Inspect) is already pinned
        //by StreamingRejectsARootThatIsNotXliff in XliffUnitStreamTests, but the whole-document Read()
        //path runs Parse's separate check on the loaded XDocument, which no test exercised.
        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read("<root/>"));

        Assert.Contains("is not an <xliff> element", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsARootElementThatIsNotNamedXliff()
    {
        //XliffReader.cs:570 root is null || root.Name != Core + "xliff" => root is null && ...: with
        //an otherwise fully valid document whose root element is simply misnamed, root is not null so
        //the mutant's && short-circuits to false and never checks the name, letting the document
        //through instead of refusing it.
        const string xliff = """
            <notxliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </notxliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));
    }

    [TestMethod]
    public void IgnoresANonCoreElementDirectlyUnderTheXliffRoot()
    {
        //XliffReader.cs:585 Parse: removing the `continue;` for a non-core-namespace child of
        //<xliff> would fall through to the file-name check, which only looks at LocalName and
        //ignores namespace, so a foreign element like <ext:log> would be wrongly refused as "not a
        //<file>" element even though it belongs to another tool's schema, not this reader's.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:ext="urn:example:tool" version="2.0" srcLang="en">
              <ext:log>ignored</ext:log>
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        XliffDocument document = Read(xliff);

        Assert.AreEqual("wallet", document.Files[0].Id);
    }

    [TestMethod]
    public void RejectsAUnitDirectlyUnderTheXliffRoot()
    {
        //r1-6: the whole-document read used to silently drop a <unit> that was not inside a <file>,
        //while ReadUnits yielded it, so the two entry points disagreed on a malformed document. XLIFF
        //2.1 §4.2.2.1 xliff, Contains: "One or more <file> elements" and nothing else.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <unit id="stray"><segment><source>x</source></segment></unit>
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));
    }

    [TestMethod]
    public void RejectsAFileWithoutUnitsOrGroups()
    {
        //r1-10: an empty <file> used to be accepted by the reader although the writer already refused
        //it, so a read-write round trip failed on a document Read() had accepted. XLIFF 2.1 §4.2.2.2
        //file, Contains: "One or more <unit> or <group> elements in any order."
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:705 empty-file message mutated to "": the file id must appear so a caller
        //can find which <file> is missing content.
        Assert.Contains("wallet", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAMalformedSrcLang()
    {
        //r1-27: srcLang was only checked for blankness, so a value with an embedded space reached the
        //model as a bare string. XLIFF 2.1 §4.3.1.29 srcLang, Value description: "A language code as
        //described in [BCP 47]".
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en US">
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:623 malformed-srcLang message mutated to "": the offending value must appear
        //so a caller can see which srcLang failed BCP 47 validation.
        Assert.Contains("en US", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnEmptyTrgLang()
    {
        //r1-8: trgLang="" used to be accepted as a target language, so the model carried
        //LanguageTag("") and the writer re-emitted the invalid attribute. XLIFF 2.1 §4.3.1.37 trgLang,
        //Value description: "A language code as described in [BCP 47]", which an empty string is not.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="">
              <file id="wallet"><unit id="A"><segment><source>a</source><target>b</target></segment></unit></file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        //XliffReader.cs:643 malformed-trgLang message mutated to "": the message must name the
        //trgLang attribute so a caller can tell it apart from a srcLang failure.
        Assert.Contains("trgLang attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsGroupNestingDeeperThan64LevelsButAcceptsExactly64()
    {
        //r1-95: unbounded <group> nesting recursion could exhaust the call stack (measured to crash the
        //process around 4,000 levels). XLIFF 2.1 §4.2.2.4 group places no limit, so the bound is the
        //reader's own safety cap; this proves the cap is enforced at 65 and does not misfire at 64.
        string sixtyFive = NestedGroups(65);
        string sixtyFour = NestedGroups(64);

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(sixtyFive));

        //XliffReader.cs:736 depth-exceeded message mutated to "": the configured limit must appear so
        //a caller knows how deep nesting is allowed to go.
        Assert.Contains("more than 64 levels deep", exception.Message, StringComparison.Ordinal);
        XliffUnit unit = DeepestUnit(Read(sixtyFour).Files[0]);
        Assert.AreEqual("Home", unit.Source);
    }

    private static string NestedGroups(int depth)
    {
        var builder = new StringBuilder();
        builder.Append("""<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en"><file id="wallet">""");
        for(int level = 0; level < depth; level++)
        {
            builder.Append($"""<group id="g{level}">""");
        }

        builder.Append("""<unit id="A"><segment><source>Home</source></segment></unit>""");
        for(int level = 0; level < depth; level++)
        {
            builder.Append("</group>");
        }

        builder.Append("</file></xliff>");

        return builder.ToString();
    }

    private static XliffUnit DeepestUnit(XliffFile file)
    {
        XliffGroup group = file.Groups.Single();
        while(group.Units.IsEmpty)
        {
            group = group.Groups.Single();
        }

        return group.Units.Single();
    }

    [TestMethod]
    public void MalformedXmlExceptionCarriesTheInnerXmlExceptionMessageAndLineInfo()
    {
        //r1-7: the outer message used to be a constant, discarding the XmlException's own message and
        //position even though XmlException always carries them.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.IsNotNull(exception.InnerException);
        Assert.Contains(exception.InnerException.Message, exception.Message, StringComparison.Ordinal);
        Assert.IsNotNull(exception.Line);
        Assert.IsNotNull(exception.Position);
    }

    [TestMethod]
    public void MalformedXmlExceptionLeavesLineAndPositionNullWhenTheUnderlyingErrorHasNoLineInfo()
    {
        //XliffReader.cs:482 exception.LineNumber > 0 ? ... : null (ternary-always-true and the >0/>=0
        //boundary) and XliffReader.cs:483 exception.LinePosition > 0 ? ... : null (the same two shapes):
        //a comment-only document has no root element at all, so XmlReader reports "Root element is
        //missing" through its own line-info-less path, which carries no position at all (LineNumber and
        //LinePosition both 0, confirmed empirically against System.Xml.XmlReader). Line and Position
        //must both stay null rather than surface the meaningless 0 any of these four mutants would let
        //through.
        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read("<!--x-->"));

        Assert.IsNull(exception.Line);
        Assert.IsNull(exception.Position);
    }

    [TestMethod]
    public async Task ThrowsOnMalformedXmlAsynchronously()
    {
        //XliffReader.cs:541 LoadAsync's throw NotWellFormed(exception) was mutated to a no-op;
        //ReadAsync(Stream) loads through LoadAsync's own catch, which no test exercised, unlike the
        //synchronous Load path ThrowsOnMalformedXml already pins.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<xliff version=\"2.0\" srcLang=\"en\""));

        await Assert.ThrowsExactlyAsync<XliffFormatException>(() => XliffReader.ReadAsync(stream, TestContext.CancellationToken));
    }

    [TestMethod]
    public void MissingIdExceptionCarriesTheElementsLineAndPosition()
    {
        //r1-7: a semantic refusal such as a missing id used to carry no location at all, even though
        //the document was already parsed into an XElement tree that has one available.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.AreEqual(3, exception.Line);
        Assert.IsNotNull(exception.Position);
        //XliffReader.cs:1378 RequireId's interpolated message => "": the element name is the only way
        //the caller learns which kind of element is missing its id; an empty message would still throw
        //with the right line, so only a wording check catches this mutant.
        Assert.Contains("A <unit> element does not declare the required id attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void FoldsIgnorableWhitespaceBetweenSegments()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
              <file id="wallet">
                <unit id="A">
                  <segment><source>First.</source><target>Eka.</target></segment>
                  <ignorable><source> </source><target> </target></ignorable>
                  <segment><source>Second.</source><target>Toka.</target></segment>
                </unit>
              </file>
            </xliff>
            """;

        XliffUnit unit = Read(xliff).Files[0].Units.Single();
        Assert.AreEqual("First. Second.", unit.Source);
        Assert.AreEqual("Eka. Toka.", unit.Target);
    }

    [TestMethod]
    public async Task ThrowsForNullArguments()
    {
        //XliffReader.cs:107 ArgumentNullException.ThrowIfNull(stream) => ; in Read(Stream): removing
        //the guard still lets an ArgumentNullException through, because Load's own
        //XmlReader.Create(stream, settings) null-checks its own "input" parameter -- but that leaves
        //ParamName "input" instead of the documented "stream", so only checking ParamName distinguishes
        //our guard firing from that BCL fallback.
        ArgumentNullException syncException = Assert.ThrowsExactly<ArgumentNullException>(() => XliffReader.Read((Stream)null!));
        Assert.AreEqual("stream", syncException.ParamName);

        //XliffReader.cs:122 ArgumentNullException.ThrowIfNull(stream) => ; in ReadAsync(Stream,
        //CancellationToken); same BCL-fallback reasoning as above, checked through the faulted task.
        ArgumentNullException asyncException = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => XliffReader.ReadAsync((Stream)null!, TestContext.CancellationToken));
        Assert.AreEqual("stream", asyncException.ParamName);
    }

    public TestContext TestContext { get; set; } = null!;

    private static XliffDocument Read(string xliff)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        return XliffReader.Read(stream);
    }
}
