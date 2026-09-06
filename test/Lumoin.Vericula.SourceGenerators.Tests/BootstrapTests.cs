using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lumoin.Vericula.SourceGenerators.Tests;

[TestClass]
public sealed class BootstrapTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void GeneratorRunsAgainstEmptyCompilation()
    {
        var generator = new XliffSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            assemblyName: "Bootstrap",
            syntaxTrees: [],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var result = driver.RunGenerators(compilation).GetRunResult();

        Assert.HasCount(0, result.Diagnostics);
    }
}
