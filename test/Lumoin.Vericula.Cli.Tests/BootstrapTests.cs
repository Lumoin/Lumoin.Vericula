namespace Lumoin.Vericula.Cli.Tests;

[TestClass]
public sealed class BootstrapTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void HostBuilds()
    {
        using var host = HostFactory.Create();

        Assert.IsNotNull(host);
    }
}
