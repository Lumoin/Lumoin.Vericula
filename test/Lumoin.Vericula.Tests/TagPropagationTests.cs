using System.Collections.Immutable;
using System.Text;
using Lumoin.Base;
using Lumoin.Vericula.Cooking;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class TagPropagationTests
{
    private const string WalletXliff = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="fi">
          <file id="wallet">
            <unit id="TabHome"><segment><source>Home</source><target>Koti</target></segment></unit>
          </file>
        </xliff>
        """;

    [TestMethod]
    public void XliffDocumentTagDefaultsToEmpty()
    {
        var document = new XliffDocument(XliffVersion.V20, ImmutableArray<XliffFile>.Empty);

        Assert.AreEqual(Tag.Empty, document.Tag);
    }

    [TestMethod]
    public void XliffFileTagDefaultsToEmpty()
    {
        var file = new XliffFile(
            "wallet",
            new LanguageTag("en"),
            null,
            null,
            null,
            null,
            ImmutableArray<XliffGroup>.Empty,
            ImmutableArray<XliffUnit>.Empty);

        Assert.AreEqual(Tag.Empty, file.Tag);
    }

    [TestMethod]
    public void CookedResourceTagDefaultsToEmpty()
    {
        var resource = new CookedResource("wallet.resx", string.Empty);

        Assert.AreEqual(Tag.Empty, resource.Tag);
    }

    [TestMethod]
    public void XliffDocumentEqualityConsidersTag()
    {
        var withoutTag = new XliffDocument(XliffVersion.V20, ImmutableArray<XliffFile>.Empty);
        var withTag = withoutTag with { Tag = Tag.Create(new SourceLocation("wallet.xliff")) };

        Assert.AreNotEqual(withoutTag, withTag);
        Assert.AreEqual(withTag, withoutTag with { Tag = Tag.Create(new SourceLocation("wallet.xliff")) });
    }

    [TestMethod]
    public void CookedResourceEqualityConsidersTag()
    {
        var withoutTag = new CookedResource("wallet.resx", "content");
        var withTag = withoutTag with { Tag = Tag.Create(new SourceLocation("wallet.xliff")) };

        Assert.AreNotEqual(withoutTag, withTag);
    }

    [TestMethod]
    public void ResxCookerPropagatesTheDocumentTagOntoEveryCookedResource()
    {
        var sourceLocation = new SourceLocation("wallet.xliff");
        XliffDocument document = Read(WalletXliff) with { Tag = Tag.Create(sourceLocation) };

        ImmutableArray<CookedResource> resources = ResxCooker.Cook([document]);

        Assert.HasCount(2, resources);
        foreach(CookedResource resource in resources)
        {
            Assert.AreEqual(sourceLocation, resource.Tag.Get<SourceLocation>());
        }
    }

    [TestMethod]
    public void CookedResourceCarriesTheSourceLocationOfItsOriginatingDocument()
    {
        var sourceLocation = new SourceLocation("translations/wallet.en.xliff");
        XliffDocument document = Read(WalletXliff) with { Tag = Tag.Create(sourceLocation) };

        CookedResource neutral = ResxCooker.Cook([document]).Single(resource => resource.FileName == "wallet.resx");

        Assert.IsTrue(neutral.Tag.TryGet(out SourceLocation? actual));
        Assert.AreEqual(sourceLocation, actual);
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
