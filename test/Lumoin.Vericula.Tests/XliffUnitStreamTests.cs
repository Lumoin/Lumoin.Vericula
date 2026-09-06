using System.IO.Pipelines;
using System.Text;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class XliffUnitStreamTests
{
    private static string[] ExpectedIds { get; } = ["TabHome", "CardTitle", "AppTitle", "TabHome"];

    private const string NestedXliff = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:mda="urn:oasis:names:tc:xliff:metadata:2.0" version="2.1" srcLang="en" trgLang="fi">
          <file id="wallet">
            <group id="Shell">
              <mda:metadata><mda:metaGroup category="vericula:scopes"><mda:meta type="scope">shell</mda:meta></mda:metaGroup></mda:metadata>
              <unit id="TabHome">
                <mda:metadata><mda:metaGroup category="vericula:metadata"><mda:meta type="owner">growth</mda:meta></mda:metaGroup></mda:metadata>
                <notes><note>Bottom navigation label.</note></notes>
                <segment id="s1" state="translated"><source>Home</source><target>Koti</target></segment>
              </unit>
              <group id="Cards">
                <unit id="CardTitle"><segment><source>Card</source><target>Kortti</target></segment></unit>
              </group>
            </group>
            <unit id="AppTitle"><segment><source>Wallet</source><target>Lompakko</target></segment></unit>
          </file>
          <file id="help">
            <unit id="TabHome"><segment><source>Help</source><target>Ohje</target></segment></unit>
          </file>
        </xliff>
        """;

    [TestMethod]
    public void StreamsUnitsInDocumentOrderFlatteningGroups()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NestedXliff));

        string[] ids = XliffReader.ReadUnits(stream).Select(unit => unit.Id).ToArray();

        CollectionAssert.AreEqual(ExpectedIds, ids);
    }

    [TestMethod]
    public async Task StreamsUnitsAsynchronouslyFromAPipe()
    {
        PipeReader input = PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(NestedXliff)));

        List<string> ids = [];
        await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(input, TestContext.CancellationToken))
        {
            ids.Add(unit.Id);
        }

        CollectionAssert.AreEqual(ExpectedIds, ids);
    }

    [TestMethod]
    public void StreamedUnitsCarryTheirOwnSegmentsNotesAndMetadataButNotTheirGroupsScopes()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NestedXliff));

        XliffUnit home = XliffReader.ReadUnits(stream).First();

        XliffSegment segment = home.Segments.Single();
        Assert.AreEqual("s1", segment.Id);
        Assert.AreEqual(SegmentState.Translated, segment.State);
        Assert.AreEqual("Home", home.Source);
        Assert.AreEqual("Koti", home.Target);
        Assert.AreEqual("Bottom navigation label.", home.Notes.Single());
        Assert.AreEqual("growth", home.Metadata["owner"]);
        Assert.IsTrue(home.Scopes.IsEmpty);
    }

    [TestMethod]
    public void StreamingRejectsDuplicateUnitIdsWithinAFile()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="A"><segment><source>One</source></segment></unit>
                <unit id="A"><segment><source>Two</source></segment></unit>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        //XliffReader.cs:378 message => "": the message names the duplicated unit id, the caller's
        //only way to learn which id collided.
        Assert.Contains("Duplicate unit id 'A'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsTargetsWithoutATargetLanguage()
    {
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <unit id="A"><segment><source>One</source><target>Yksi</target></segment></unit>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        //XliffReader.cs:383 message => "": this text is the only way a caller learns trgLang is
        //required because the unit carries <target> content.
        Assert.Contains("declares no trgLang", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsARootThatIsNotXliff()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsARootInTheCoreNamespaceButNotNamedXliff()
    {
        //Kills XliffReader.cs:306 !core || !IsXliff(name) => !core && !IsXliff(name): a root element
        //in the core namespace named <group> instead of <xliff> has core=true, so the mutant's &&
        //never trips (a false operand), and the walk treats the empty root as though it were the
        //<xliff> root, yielding no units with no exception instead of refusing the document.
        const string xliff = """<group xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en"/>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        Assert.Contains("is not an <xliff> element", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsAMissingSourceLanguage()
    {
        const string xliff = """<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0"><file id="w"><unit id="A"><segment><source>x</source></segment></unit></file></xliff>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsAnUnsupportedVersion()
    {
        const string xliff = """<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="1.2" srcLang="en"><file id="w"><unit id="A"><segment><source>x</source></segment></unit></file></xliff>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsMalformedXmlAsAFormatException()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\"><file id=\"w\"><unit id=\"A\">"));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsMalformedXmlTruncatedBeforeAnyUnit()
    {
        //XliffReader.cs:417 Advance's throw NotWellFormed(exception) was mutated to a no-op. Truncating
        //right after <file>, before any <unit> starts, means the malformed-XML exception surfaces from
        //Advance's plain reader.Read() rather than from ReadElement's XNode.ReadFrom, which
        //StreamingRejectsMalformedXmlAsAFormatException already covers.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\"><file id=\"w\">"));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public async Task StreamingAsyncRejectsMalformedXmlTruncatedBeforeAnyUnit()
    {
        //XliffReader.cs:435 AdvanceAsync's throw NotWellFormed(exception) was mutated to a no-op; the
        //async counterpart of StreamingRejectsMalformedXmlTruncatedBeforeAnyUnit, truncated before any
        //<unit> so the exception surfaces from AdvanceAsync's plain reader.ReadAsync() rather than from
        //ReadElementAsync's XNode.ReadFromAsync.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\"><file id=\"w\">"));

        async Task Enumerate()
        {
            await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(stream, TestContext.CancellationToken))
            {
                _ = unit;
            }
        }

        Task enumerateTask = Enumerate();

        await Assert.ThrowsExactlyAsync<XliffFormatException>(() => enumerateTask);
    }

    [TestMethod]
    public async Task StreamingAsyncRejectsMalformedXmlTruncatedInsideAUnit()
    {
        //XliffReader.cs:469 ReadElementAsync's throw NotWellFormed(exception) was mutated to a no-op;
        //the async counterpart of StreamingRejectsMalformedXmlAsAFormatException, truncated inside the
        //<unit> so XNode.ReadFromAsync itself hits the malformed XML while materializing the unit element.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\"><file id=\"w\"><unit id=\"A\">"));

        async Task Enumerate()
        {
            await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(stream, TestContext.CancellationToken))
            {
                _ = unit;
            }
        }

        Task enumerateTask = Enumerate();

        await Assert.ThrowsExactlyAsync<XliffFormatException>(() => enumerateTask);
    }

    [TestMethod]
    public void StreamingYieldsNothingButStillRequiresARoot()
    {
        //A comment-only or PI-only document (no element at all) is not well-formed XML by XmlReader's
        //own Document-conformance check, so it throws "Root element is missing" from the
        //well-formedness branch (see Advance's NotWellFormed) before the walk ever gets a chance to
        //notice the missing root itself; that made the old version of this test pass for the wrong
        //reason. A well-formed document whose root is not <xliff>, by contrast, is well-formed XML,
        //so it reaches the walk's own root check in Inspect and is refused with that check's message.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        Assert.Contains("is not an <xliff> element", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsAFileWithoutAnId()
    {
        //r1-5: ReadUnits used to accept a <file> with no id and simply reset its unit-id-dedup scope,
        //although Read() already refused it.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsAGroupWithoutAnId()
    {
        //r1-5: ReadUnits used to accept a <group> with no id, although Read() already refused it, so
        //the two entry points disagreed on the same malformed bytes.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><group><unit id="A"><segment><source>Home</source></segment></unit></group></file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsValidationRulesOnAGroup()
    {
        //r1-5: ReadUnits used to silently drop a Validation module attached to a <group>, while Read()
        //already refused it loudly (RejectsValidationOnGroupsAndUnits in XliffModuleReaderTests pins
        //that on the whole-document path).
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:val="urn:oasis:names:tc:xliff:validation:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <group id="G"><val:validation><val:rule isPresent="x"/></val:validation><unit id="A"><segment><source>Home</source></segment></unit></group>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        //XliffReader.cs:320 message => "": the wording is the caller's only way to learn a <group>
        //carried the disallowed validation rules rather than being refused for some other reason.
        Assert.Contains("Validation rules on a <group> are not supported", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsValidationOnAGroupEvenAfterANonGroupCoreElementInsideIt()
    {
        //XliffReader.cs:291 core && WellKnownXliffElements.IsGroup(name) =>
        //core || WellKnownXliffElements.IsGroup(name): the OR lets any core-namespace closing tag other
        //than <file> (here </note>, placed loosely under the group purely to probe the walker's
        //per-node EndElement handling) decrement OpenGroups just like a real </group> close would. By
        //the time <val:validation> is reached OpenGroups has already dropped to 0, so the rule that
        //validation cannot sit on a <group> is silently skipped instead of enforced.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:val="urn:oasis:names:tc:xliff:validation:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <group id="G">
                  <unit id="A"><segment><source>Home</source></segment></unit>
                  <note>x</note>
                  <val:validation><val:rule isPresent="x"/></val:validation>
                </group>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        Assert.Contains("Validation rules on a <group> are not supported", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingKeepsFileValidationDistinctFromGroupValidationAcrossACompactUnitClose()
    {
        //Kills XliffReader.cs:231 continue; => ; (IterateUnits), XliffReader.cs:291
        //core && WellKnownXliffElements.IsGroup(name) => !(core && WellKnownXliffElements.IsGroup(name))
        //and XliffReader.cs:293 state.OpenGroups - 1 => state.OpenGroups + 1: with no whitespace between
        //</unit> and </group>, dropping the continue folds the group's own EndElement into the raw
        //Advance that follows taking the unit, so Inspect (and its OpenGroups decrement) never runs on
        //it; negating the && or flipping the decrement to an increment corrupts the same decrement while
        //still running it. Any of the three leaves OpenGroups stuck open, so the file-level
        //<val:validation> right after the group is wrongly rejected as being on the group it just left.
        const string xliff = """<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:val="urn:oasis:names:tc:xliff:validation:2.0" version="2.0" srcLang="en"><file id="wallet"><group id="G"><unit id="A"><segment><source>Home</source></segment></unit></group><val:validation><val:rule isPresent="x"/></val:validation><unit id="B"><segment><source>Two</source></segment></unit></file></xliff>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        string[] ids = XliffReader.ReadUnits(stream).Select(unit => unit.Id).ToArray();

        CollectionAssert.AreEqual(TwoIds, ids);
    }

    [TestMethod]
    public async Task StreamingAsyncKeepsFileValidationDistinctFromGroupValidationAcrossACompactUnitClose()
    {
        //Kills XliffReader.cs:263 continue; => ; (IterateUnitsAsync): the async counterpart of
        //StreamingKeepsFileValidationDistinctFromGroupValidationAcrossACompactUnitClose, proven
        //separately because IterateUnitsAsync has its own continue statement rather than sharing the
        //sync loop's. With no whitespace between </unit> and </group>, dropping it skips Inspect (and
        //its OpenGroups decrement) for the group's own close, so the file-level <val:validation> right
        //after the group is wrongly rejected as being on the group it just left.
        const string xliff = """<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:val="urn:oasis:names:tc:xliff:validation:2.0" version="2.0" srcLang="en"><file id="wallet"><group id="G"><unit id="A"><segment><source>Home</source></segment></unit></group><val:validation><val:rule isPresent="x"/></val:validation><unit id="B"><segment><source>Two</source></segment></unit></file></xliff>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        List<string> ids = [];
        await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(stream, TestContext.CancellationToken))
        {
            ids.Add(unit.Id);
        }

        CollectionAssert.AreEqual(TwoIds, ids);
    }

    private static string[] TwoIds { get; } = ["A", "B"];

    [TestMethod]
    public void StreamingSkipsElementsOutsideTheCoreNamespaceEvenWhenTheirLocalNameMatchesXliffElements()
    {
        //Kills XliffReader.cs:324 the !core early return's body => {}: without the return, a
        //foreign-namespaced <foo:unit> sharing the local name "unit" falls through to the
        //core-element checks below and is taken as a real unit, which then throws for having no
        //<segment>, even though the reader is supposed to ignore anything outside the XLIFF core
        //namespace entirely.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:foo="urn:example:foo" version="2.0" srcLang="en">
              <file id="wallet">
                <foo:unit id="fake"/>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        string[] ids = XliffReader.ReadUnits(stream).Select(unit => unit.Id).ToArray();

        CollectionAssert.AreEqual(SingleId, ids);
    }

    [TestMethod]
    public void StreamingAcceptsValidationRulesOnAFileOutsideAnyGroup()
    {
        //Kills a mutant that would make the r1-5 group-validation refusal too broad by tripping on any
        //val:validation regardless of nesting: file-level validation, which the streaming reader does
        //not extract but also must not refuse, still has to yield its unit.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:val="urn:oasis:names:tc:xliff:validation:2.0" version="2.0" srcLang="en">
              <file id="wallet">
                <val:validation><val:rule isPresent="x"/></val:validation>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        string[] ids = XliffReader.ReadUnits(stream).Select(unit => unit.Id).ToArray();

        CollectionAssert.AreEqual(SingleId, ids);
    }

    private static string[] SingleId { get; } = ["A"];

    [TestMethod]
    public void StreamingAcceptsAFileWhoseOnlyDirectChildIsATopLevelGroup()
    {
        //XliffReader.cs:373 state.OpenGroups == 0 ? state.FileMembers + 1 : state.FileMembers, mutated to
        //(false?state.FileMembers + 1 :state.FileMembers ) (id 363) and to state.OpenGroups != 0 (id 364):
        //both leave FileMembers stuck at 0 for a file whose only direct child is a <group>, because the
        //group itself opens at OpenGroups==0 (the branch either mutant breaks) and the unit nested inside
        //it never counts either way (it is not a direct child of the file). With FileMembers never
        //reaching 1, the </file> EndElement check in Inspect wrongly throws "has no <unit> or <group>
        //element" for a file that legitimately has one.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><group id="G"><unit id="A"><segment><source>Home</source></segment></unit></group></file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        string[] ids = XliffReader.ReadUnits(stream).Select(unit => unit.Id).ToArray();

        CollectionAssert.AreEqual(SingleId, ids);
    }

    [TestMethod]
    public void StreamingRejectsAUnitDirectlyUnderTheXliffRoot()
    {
        //r1-6: ReadUnits used to yield a <unit> that was not inside any <file>, while Read() dropped it
        //silently; both must now refuse the same malformed document.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <unit id="stray"><segment><source>x</source></segment></unit>
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        //XliffReader.cs:360 message => "": the wording is how a caller learns the <unit> sits
        //directly under <xliff> rather than being refused for some other reason.
        Assert.Contains("A <unit> element is not inside a <file>", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsAUnitDirectlyUnderTheXliffRootAfterAPrecedingFileCloses()
    {
        //Kills XliffReader.cs:288 state with { InFile = false } => state with { InFile = true }: without
        //the reset on </file>, a <unit> that follows a closed <file> directly under <xliff> is wrongly
        //accepted as still being inside a file, instead of tripping the same "not inside a <file>"
        //refusal a leading stray unit gets (see StreamingRejectsAUnitDirectlyUnderTheXliffRoot, which
        //only covers a stray unit before any file and so cannot see this reset on its own).
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="A"><segment><source>Home</source></segment></unit></file>
              <unit id="stray"><segment><source>x</source></segment></unit>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void StreamingRejectsAGroupDirectlyUnderTheXliffRootLikeTheWholeDocumentRead()
    {
        //p1-6: an empty <group> directly under <xliff> (outside any <file>) used to stream out as zero
        //units with no exception, while Read() already refused the identical bytes: the streaming
        //IsGroup branch advanced past it unconditionally, unlike the IsUnit branch right below it,
        //which already checked state.InFile. Both entry points on the same bytes must now refuse, and
        //with the same message.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en">
              <group id="g1"></group>
            </xliff>
            """;

        XliffFormatException fromRead = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(xliff))));
        XliffFormatException fromReadUnits = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.ReadUnits(new MemoryStream(Encoding.UTF8.GetBytes(xliff))).ToArray());

        Assert.Contains("contains a <group> element", fromRead.Message);
        Assert.AreEqual(fromRead.Message, fromReadUnits.Message);
    }

    [TestMethod]
    public void StreamingRejectsAnEmptySelfClosingFileLikeTheWholeDocumentRead()
    {
        //f-1: a self-closing <file/> produces no EndElement node, so ReadUnits used to see InFile
        //flip true and never back to false, yielding zero units with no exception, while Read()
        //already refused the identical bytes from ParseFile's unit-or-group count. Both entry points
        //on the same bytes must now refuse, with the same message.
        const string xliff = """<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en"><file id="f1"/></xliff>""";

        XliffFormatException fromRead = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(xliff))));
        XliffFormatException fromReadUnits = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.ReadUnits(new MemoryStream(Encoding.UTF8.GetBytes(xliff))).ToArray());

        Assert.Contains("File 'f1' has no <unit> or <group> element", fromRead.Message, StringComparison.Ordinal);
        Assert.AreEqual(fromRead.Message, fromReadUnits.Message);
    }

    [TestMethod]
    public void StreamingRejectsAnEmptyPairedFileLikeTheWholeDocumentRead()
    {
        //f-1: the paired-tag counterpart of StreamingRejectsAnEmptySelfClosingFileLikeTheWholeDocumentRead,
        //proven separately because a paired </file> end tag reaches Inspect's EndElement branch, which
        //carries its own, independently added count check.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en">
              <file id="f1"></file>
            </xliff>
            """;

        XliffFormatException fromRead = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(xliff))));
        XliffFormatException fromReadUnits = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.ReadUnits(new MemoryStream(Encoding.UTF8.GetBytes(xliff))).ToArray());

        Assert.Contains("File 'f1' has no <unit> or <group> element", fromRead.Message, StringComparison.Ordinal);
        Assert.AreEqual(fromRead.Message, fromReadUnits.Message);
    }

    [TestMethod]
    public void StreamingRejectsANonFileCoreElementUnderTheXliffRootLikeTheWholeDocumentRead()
    {
        //f-2: Inspect only special-cased IsFile/IsGroup/IsUnit at the top level, so any other
        //core-namespace element directly under <xliff> (here <notes>) fell through to a plain Advance
        //and was silently skipped, while Read() already refused the identical bytes from Parse()'s
        //per-child-of-root check. Both entry points on the same bytes must now refuse, with the same
        //message, and the valid unit that follows must never be reached.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en">
              <notes><note>x</note></notes>
              <file id="f1"><unit id="u1"><segment><source>s</source></segment></unit></file>
            </xliff>
            """;

        XliffFormatException fromRead = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(xliff))));
        XliffFormatException fromReadUnits = Assert.ThrowsExactly<XliffFormatException>(
            () => XliffReader.ReadUnits(new MemoryStream(Encoding.UTF8.GetBytes(xliff))).ToArray());

        Assert.Contains("contains a <notes> element", fromRead.Message, StringComparison.Ordinal);
        Assert.AreEqual(fromRead.Message, fromReadUnits.Message);
    }

    [TestMethod]
    public void StreamingRejectsDuplicateFileIds()
    {
        //Root cause behind r1-38: streaming used to accept two <file> elements sharing an id, the same
        //way the whole-document read once did.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
              <file id="wallet"><unit id="A"><segment><source>One</source></segment></unit></file>
              <file id="wallet"><unit id="B"><segment><source>Two</source></segment></unit></file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());

        //XliffReader.cs:333 message => "": the message names the colliding id, which is the caller's
        //only way to learn which file id was duplicated.
        Assert.Contains("Duplicate file id 'wallet'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StreamingRejectsAMalformedTargetLanguage()
    {
        //r1-27: the streaming root check used to take trgLang raw with no BCP 47 shape validation.
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi FI">
              <file id="wallet"><unit id="A"><segment><source>x</source><target>y</target></segment></unit></file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        Assert.ThrowsExactly<XliffFormatException>(() => XliffReader.ReadUnits(stream).ToArray());
    }

    [TestMethod]
    public void ThrowsForNullArguments()
    {
        //XliffReader.cs:152 ArgumentNullException.ThrowIfNull(stream) => ; in ReadUnits(Stream) would
        //let a null stream reach IterateUnits() and surface as a NullReferenceException instead of the
        //documented ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffReader.ReadUnits((Stream)null!));

        //XliffReader.cs:171 ArgumentNullException.ThrowIfNull(input) => ; in ReadUnitsAsync(PipeReader,
        //CancellationToken); this overload is not itself async (it delegates to a private async
        //iterator), so the guard still throws synchronously when the method is called, before any
        //enumeration begins.
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffReader.ReadUnitsAsync((PipeReader)null!, TestContext.CancellationToken));
    }

    [TestMethod]
    public void ReadUnitsAsyncFromAStreamThrowsForANullStream()
    {
        //Kills XliffReader.cs:205 ArgumentNullException.ThrowIfNull(stream); => ;: no existing test calls
        //this Stream overload of ReadUnitsAsync with a null stream, so the guard was never exercised.
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffReader.ReadUnitsAsync((Stream)null!, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ReadUnitsAsyncFromAStreamThrowsWhenTheTokenIsAlreadyCancelled()
    {
        //Kills XliffReader.cs:254 cancellationToken.ThrowIfCancellationRequested(); => ;: distinct from
        //ReadUnitsAsyncFromAStalledPipeIsCancelledPromptly in XliffModuleReaderTests, which cancels a
        //pipe mid-read via CancelPendingRead; this proves the loop's own per-node guard fires too, using
        //a Stream (no pipe registration involved) with a token that is already cancelled before
        //enumeration starts.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        async Task Enumerate()
        {
            await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(stream, cts.Token))
            {
                _ = unit;
            }
        }

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(Enumerate);
    }

    [TestMethod]
    public void ReadUnitsThrowsWhenThePipeReaderIsNull()
    {
        //XliffReader.cs:137 ArgumentNullException.ThrowIfNull(input) was mutated to a no-op; a null
        //PipeReader must still be rejected before AsStream is ever reached.
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffReader.ReadUnits((PipeReader)null!));
    }

    [TestMethod]
    public void ReadUnitsFromAPipeAsksAsStreamToLeaveThePipeReaderOpen()
    {
        //XliffReader.cs:139 input.AsStream(leaveOpen: true) was mutated to leaveOpen: false. AsStream is
        //virtual purely so a caller can be told which value it was called with, since the flag only
        //changes Dispose-time behaviour that ReadUnits' own call chain never triggers otherwise (the
        //stream AsStream returns is never itself disposed; only the inner XmlReader is, and its default
        //CloseInput=false does not cascade). ReadUnits(PipeReader) is not an iterator, so calling it
        //alone -- without enumerating the result -- already runs the AsStream call this test observes.
        using var innerStream = new MemoryStream(Encoding.UTF8.GetBytes("<xliff/>"));
        PipeReader inner = PipeReader.Create(innerStream);
        var spy = new LeaveOpenRecordingPipeReader(inner);

        _ = XliffReader.ReadUnits(spy);

        Assert.AreEqual(true, spy.CapturedLeaveOpen);
    }

    [TestMethod]
    public async Task ReadUnitsAsyncFromAPipeAsksAsStreamToLeaveThePipeReaderOpen()
    {
        //XliffReader.cs:188 IterateUnitsFromPipeAsync's input.AsStream(leaveOpen: true) was mutated to
        //leaveOpen: false. Unlike ReadUnits(PipeReader), ReadUnitsAsync(PipeReader) is itself an async
        //iterator, so the AsStream call is lazy; starting enumeration once already runs it, before any
        //unit is yielded, since it sits before the await foreach in IterateUnitsFromPipeAsync's body.
        using var innerStream = new MemoryStream(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));
        PipeReader inner = PipeReader.Create(innerStream);
        var spy = new LeaveOpenRecordingPipeReader(inner);

        await using IAsyncEnumerator<XliffUnit> enumerator = XliffReader.ReadUnitsAsync(spy, TestContext.CancellationToken).GetAsyncEnumerator();
        await enumerator.MoveNextAsync();

        Assert.AreEqual(true, spy.CapturedLeaveOpen);
    }

    /// <summary>
    /// A <see cref="PipeReader"/> that forwards every operation to an inner reader but records the
    /// leaveOpen argument it was called with, so a test can observe that otherwise Dispose-time-only flag.
    /// </summary>
    private sealed class LeaveOpenRecordingPipeReader : PipeReader
    {
        private readonly PipeReader _inner;

        /// <summary>Creates a spy that forwards every operation to <paramref name="inner"/>.</summary>
        /// <param name="inner">The reader to delegate to.</param>
        public LeaveOpenRecordingPipeReader(PipeReader inner)
        {
            _inner = inner;
        }

        /// <summary>The leaveOpen argument most recently passed to <see cref="AsStream"/>; false until then.</summary>
        public bool CapturedLeaveOpen { get; private set; }

        /// <inheritdoc/>
        public override Stream AsStream(bool leaveOpen = false)
        {
            CapturedLeaveOpen = leaveOpen;

            return _inner.AsStream(leaveOpen);
        }

        /// <inheritdoc/>
        public override void AdvanceTo(SequencePosition consumed) => _inner.AdvanceTo(consumed);

        /// <inheritdoc/>
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _inner.AdvanceTo(consumed, examined);

        /// <inheritdoc/>
        public override void CancelPendingRead() => _inner.CancelPendingRead();

        /// <inheritdoc/>
        public override void Complete(Exception? exception = null) => _inner.Complete(exception);

        /// <inheritdoc/>
        public override bool TryRead(out ReadResult result) => _inner.TryRead(out result);

        /// <inheritdoc/>
        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _inner.ReadAsync(cancellationToken);
    }

    public TestContext TestContext { get; set; } = null!;
}
