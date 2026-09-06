namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class BootstrapTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void HarnessLoads()
    {
        Assert.IsNotNull(TestContext);
    }
}
