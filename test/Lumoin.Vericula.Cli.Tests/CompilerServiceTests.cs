using System.Collections.Immutable;
using Lumoin.Vericula.Cli.Services;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Cli.Tests;

[TestClass]
public sealed class CompilerServiceTests
{
    private const string WalletXliff = """
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en">
          <file id="wallet">
            <unit id="TabHome"><segment><source>Home</source></segment></unit>
          </file>
        </xliff>
        """;

    private readonly List<string> tempDirectories = [];

    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void DeleteTempDirectories()
    {
        foreach(string directory in tempDirectories)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch(IOException)
            {
            }
            catch(UnauthorizedAccessException)
            {
            }
        }
    }

    [TestMethod]
    public async Task CompilesXlfFilesFoundInADirectory()
    {
        //Two findings folded from the first review pass: the conventional .xlf extension must be
        //accepted from a directory input, not just .xliff.
        string directory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory, "wallet.xlf"), WalletXliff, TestContext.CancellationToken);
        string output = CreateTempDirectory();

        int exitCode = await new CompilerService().CompileAsync(directory, output, null, TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(File.Exists(Path.Combine(output, "wallet.resx")));
    }

    [TestMethod]
    public async Task CompilesASingleXlfFileGivenDirectly()
    {
        string directory = CreateTempDirectory();
        string input = Path.Combine(directory, "wallet.xlf");
        await File.WriteAllTextAsync(input, WalletXliff, TestContext.CancellationToken);
        string output = CreateTempDirectory();

        int exitCode = await new CompilerService().CompileAsync(input, output, null, TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(File.Exists(Path.Combine(output, "wallet.resx")));
    }

    [TestMethod]
    public void RefusesATargetLanguageThatWouldEscapeTheOutputDirectory()
    {
        //f-19: this test used to feed the hostile trgLang through a file on disk, read by
        //XliffReader.ReadAsync. But the reader validates every trgLang it accepts against BCP 47's
        //shape (letters, digits and hyphens only, r1-8/r1-27), so "../../evil" is refused by the
        //reader's own well-formedness check (XliffFormatException, caught by CompileAsync's read
        //loop) before ValidateNames -- the guard this test names -- ever runs; the assertions passed
        //for the wrong reason and would still pass with that guard's trgLang branch deleted. Building
        //the document by hand, bypassing the reader, drives ValidateNames' own check directly and
        //names its actual refusal.
        var unit = XliffUnit.FromText("A", "Home", "Koti");
        var file = new XliffFile("wallet", new LanguageTag("en"), new LanguageTag("../../evil"), null, null, null, ImmutableArray<XliffGroup>.Empty, [unit]);
        var document = new XliffDocument(XliffVersion.V20, [file]);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => CompilerService.ValidateNames([document], baseName: null, fileIds: ["wallet"]));

        Assert.Contains("target language", exception.Message, StringComparison.Ordinal);
        Assert.Contains("../../evil", exception.Message, StringComparison.Ordinal);
        Assert.Contains("contains a character that cannot appear in a file name", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task RefusesABaseNameContainingAColon()
    {
        //r1-94: ':' is a legal XML NMTOKEN character but the NTFS alternate-data-stream separator, so
        //an id like "app:wallet" must not reach Path.Combine unchecked.
        string directory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory, "wallet.xliff"), WalletXliff, TestContext.CancellationToken);
        string output = CreateTempDirectory();

        int exitCode = await new CompilerService().CompileAsync(directory, output, "app:wallet", TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(output).Any());
    }

    [TestMethod]
    public async Task RefusesABaseNameThatEndsWithACultureSegment()
    {
        //r1-65 surfaced through the CLI: a --name (or a defaulted file id) ending in a culture segment
        //would make MSBuild's AssignCulture mistake the neutral resx for a satellite of that culture.
        string directory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory, "wallet.xliff"), WalletXliff, TestContext.CancellationToken);
        string output = CreateTempDirectory();

        int exitCode = await new CompilerService().CompileAsync(directory, output, "wallet.en", TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(output).Any());
    }

    [TestMethod]
    public async Task RejectsATargetCultureThePlatformDoesNotPredefine()
    {
        //IsCulture must ask CultureInfo.GetCultureInfo for a predefined culture only: without that
        //flag, ICU (Linux, macOS) accepts any well-formed word as a synthesized culture, so a target
        //language the platform does not predefine would slip past ValidateNames there while NLS
        //(Windows) already rejects it; predefinedOnly makes the two platforms agree. "zzzzzzz" (not
        //"notaculture": at 11 letters that exceeds LanguageTag.Shape's 8-letter primary-subtag limit
        //and would be refused by the reader itself, exercising the wrong guard) is both a well-formed
        //BCP 47 primary subtag and a name no platform predefines.
        string directory = CreateTempDirectory();
        const string xliff = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="zzzzzzz">
              <file id="wallet">
                <unit id="TabHome"><segment><source>Home</source><target>Home</target></segment></unit>
              </file>
            </xliff>
            """;
        await File.WriteAllTextAsync(Path.Combine(directory, "wallet.xliff"), xliff, TestContext.CancellationToken);
        string output = CreateTempDirectory();

        int exitCode = await new CompilerService().CompileAsync(directory, output, null, TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(output).Any());
    }

    [TestMethod]
    public async Task RemovesOnlyStaleFilesShapedLikeAProducedSatellite()
    {
        //r1-63: the stale-satellite sweep globbed "{base}.*.resx", which also matched a Designer.resx
        //and any other file merely sharing the base name. Only "{base}.{culture}.resx" for a culture
        //the cooker itself could have produced may be removed. On ICU (Linux, macOS),
        //GetCultureInfo("Designer") succeeds unless predefinedOnly is true, so dropping that flag from
        //IsCulture brings this failure back on Linux and macOS (wallet.Designer.resx wrongly deleted)
        //while Windows (NLS, which already rejects "Designer") stays green.
        string directory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory, "wallet.xliff"), WalletXliff, TestContext.CancellationToken);
        string output = CreateTempDirectory();
        string designer = Path.Combine(output, "wallet.Designer.resx");
        string satelliteDesigner = Path.Combine(output, "wallet.fi.Designer.resx");
        string staleSatellite = Path.Combine(output, "wallet.de.resx");
        await File.WriteAllTextAsync(designer, "designer", TestContext.CancellationToken);
        await File.WriteAllTextAsync(satelliteDesigner, "designer", TestContext.CancellationToken);
        await File.WriteAllTextAsync(staleSatellite, "stale", TestContext.CancellationToken);

        int exitCode = await new CompilerService().CompileAsync(directory, output, null, TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(File.Exists(designer));
        Assert.IsTrue(File.Exists(satelliteDesigner));
        Assert.IsFalse(File.Exists(staleSatellite));
    }

    private string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "vericula-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        tempDirectories.Add(path);

        return path;
    }
}
