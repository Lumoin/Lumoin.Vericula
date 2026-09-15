using System.IO.Pipelines;
using System.Text;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

/// <summary>Proves the nested-file refusal agrees across every reader entry point.</summary>
[TestClass]
public sealed class XliffNestedFileTests
{
    /// <summary>The test runner's cancellation and deadline context.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>The shared structural diagnostic, including the enclosing file identity.</summary>
    private const string ExpectedMessage = "A <file> element is nested inside file 'outer'; XLIFF 2.1 §4.2.2.1 allows <file> only directly under <xliff>.";

    /// <summary>Refuses direct nesting and both H-04 group counterexamples with the enclosing file id.</summary>
    [TestMethod]
    [DataRow(0, false, false)]
    [DataRow(1, false, false)]
    [DataRow(2, false, false)]
    [DataRow(1, true, false)]
    [DataRow(0, false, true)]
    [DataRow(1, false, true)]
    [DataRow(2, false, true)]
    [DataRow(1, true, true)]
    public void ReadRefusesAFileNestedOutsideAUnit(int depth, bool wrapInnerUnit, bool pipe)
    {
        //The nested-file guard must fire before inner ids, empty-file checks or member counting.
        //Depth one includes H-04's bare inner unit (S-045) and grouped inner unit (S-044);
        //depth zero and two prove direct file nesting and refusal at any group depth.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NestedFileDocument(depth, wrapInnerUnit)));
        PipeReader? input = pipe ? PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true)) : null;
        try
        {
            var exception = Assert.ThrowsExactly<XliffFormatException>(() =>
            {
                if(pipe)
                {
                    XliffReader.Read(input!);
                }
                else
                {
                    XliffReader.Read(stream);
                }
            });
            Assert.AreEqual(ExpectedMessage, exception.Message);
        }
        finally
        {
            input?.Complete();
        }
    }

    /// <summary>Refuses direct nesting and both H-04 group counterexamples with the enclosing file id.</summary>
    [TestMethod]
    [DataRow(0, false, false)]
    [DataRow(1, false, false)]
    [DataRow(2, false, false)]
    [DataRow(1, true, false)]
    [DataRow(0, false, true)]
    [DataRow(1, false, true)]
    [DataRow(2, false, true)]
    [DataRow(1, true, true)]
    public async Task ReadAsyncRefusesAFileNestedOutsideAUnit(int depth, bool wrapInnerUnit, bool pipe)
    {
        //The nested-file guard must fire before inner ids, empty-file checks or member counting.
        //Depth one includes H-04's bare inner unit (S-045) and grouped inner unit (S-044);
        //depth zero and two prove direct file nesting and refusal at any group depth.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NestedFileDocument(depth, wrapInnerUnit)));
        PipeReader? input = pipe ? PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true)) : null;
        try
        {
            var exception = await Assert.ThrowsExactlyAsync<XliffFormatException>(async () =>
            {
                if(pipe)
                {
                    await XliffReader.ReadAsync(input!, TestContext.CancellationToken);
                }
                else
                {
                    await XliffReader.ReadAsync(stream, TestContext.CancellationToken);
                }
            });
            Assert.AreEqual(ExpectedMessage, exception.Message);
        }
        finally
        {
            input?.Complete();
        }
    }

    /// <summary>Refuses direct nesting and both H-04 group counterexamples with the enclosing file id.</summary>
    [TestMethod]
    [DataRow(0, false, false)]
    [DataRow(1, false, false)]
    [DataRow(2, false, false)]
    [DataRow(1, true, false)]
    [DataRow(0, false, true)]
    [DataRow(1, false, true)]
    [DataRow(2, false, true)]
    [DataRow(1, true, true)]
    public void ReadUnitsRefusesAFileNestedOutsideAUnit(int depth, bool wrapInnerUnit, bool pipe)
    {
        //T-003 at XliffReader.cs:251 and T-005 at line 470 remove synchronous progress;
        //T-006 at line 474 swallows a read error. Abandon a spinning worker after a named failure;
        //the existing assertions still execute unchanged inside the deadline.
        ReaderDeadline.Run(() =>
        {
            //The nested-file guard must fire before inner ids, empty-file checks or member counting.
            //Depth one includes H-04's bare inner unit (S-045) and grouped inner unit (S-044);
            //depth zero and two prove direct file nesting and refusal at any group depth.
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NestedFileDocument(depth, wrapInnerUnit)));
            PipeReader? input = pipe ? PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true)) : null;
            try
            {
                var exception = Assert.ThrowsExactly<XliffFormatException>(() =>
                    (pipe ? XliffReader.ReadUnits(input!) : XliffReader.ReadUnits(stream)).ToArray());
                Assert.AreEqual(ExpectedMessage, exception.Message);
            }
            finally
            {
                input?.Complete();
            }
        }, TestContext.CancellationToken);
    }

    /// <summary>Refuses direct nesting and both H-04 group counterexamples with the enclosing file id.</summary>
    [TestMethod]
    [DataRow(0, false, false)]
    [DataRow(1, false, false)]
    [DataRow(2, false, false)]
    [DataRow(1, true, false)]
    [DataRow(0, false, true)]
    [DataRow(1, false, true)]
    [DataRow(2, false, true)]
    [DataRow(1, true, true)]
    public async Task ReadUnitsAsyncRefusesAFileNestedOutsideAUnit(int depth, bool wrapInnerUnit, bool pipe)
    {
        //T-004 at XliffReader.cs:283, T-007 at line 488 and T-008 at line 492 can stop
        //asynchronous progress. Fail by name after ten seconds and abandon a spinning worker;
        //the existing assertions still execute unchanged inside the deadline.
        await ReaderDeadline.RunAsync(async () =>
        {
            //The nested-file guard must fire before inner ids, empty-file checks or member counting.
            //Depth one includes H-04's bare inner unit (S-045) and grouped inner unit (S-044);
            //depth zero and two prove direct file nesting and refusal at any group depth.
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NestedFileDocument(depth, wrapInnerUnit)));
            PipeReader? input = pipe ? PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true)) : null;
            try
            {
                var exception = await Assert.ThrowsExactlyAsync<XliffFormatException>(async () =>
                {
                    IAsyncEnumerable<XliffUnit> units = pipe
                        ? XliffReader.ReadUnitsAsync(input!, TestContext.CancellationToken)
                        : XliffReader.ReadUnitsAsync(stream, TestContext.CancellationToken);
                    await foreach(XliffUnit unit in units)
                    {
                        Assert.Fail($"Nested file yielded unit '{unit.Id}' before its structural refusal.");
                    }
                });
                Assert.AreEqual(ExpectedMessage, exception.Message);
            }
            finally
            {
                input?.Complete();
            }
        }, TestContext.CancellationToken);
    }

    /// <summary>Builds direct nesting and the two honesty-review counterexamples at the requested depth.</summary>
    private static string NestedFileDocument(int depth, bool wrapInnerUnit)
    {
        const string unit = "<unit id=\"u\"><segment><source>x</source></segment></unit>";
        string inner = wrapInnerUnit ? $"<group id=\"h\">{unit}</group>" : unit;
        string nested = $"<file id=\"inner\">{inner}</file>";
        for(int index = 0; index < depth; index++)
        {
            nested = $"<group id=\"g{index}\">{nested}</group>";
        }

        return $"<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\"><file id=\"outer\">{nested}</file></xliff>";
    }
}
