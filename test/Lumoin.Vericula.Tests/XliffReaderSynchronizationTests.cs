using System.IO.Pipelines;
using System.Text;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Tests;

/// <summary>Places the first suspension at each reader await family under a recording context.</summary>
[TestClass]
public sealed class XliffReaderSynchronizationTests
{
    /// <summary>The runner's cancellation token.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>A document long enough to suspend independently before and inside its unit.</summary>
    private const string Document = "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\"><file id=\"f\"><unit id=\"A\"><segment><source>Text that crosses several small reads.</source></segment></unit></file></xliff>";


    /// <summary>Completes the reader operation without capturing the caller's synchronization context.</summary>
    [TestMethod]
    public async Task ReadAsyncFromAPipeDoesNotPostToTheCallersContext()
    {
        //S-037 at XliffReader.cs:112 and S-048 at line 594 change false to true.
        //Both awaits are pending at the first read; the zero-post assertion sees their capture.
        await AssertResumesWithoutPostingToTheCallersContext(pipe: true, streaming: false, insideUnit: false);
    }


    /// <summary>Completes the reader operation without capturing the caller's synchronization context.</summary>
    [TestMethod]
    public async Task ReadAsyncFromAStreamDoesNotPostToTheCallersContext()
    {
        //S-038 at XliffReader.cs:141 and S-048 at line 594 change false to true.
        //The gated initial read leaves both awaits pending; any captured continuation adds a post.
        await AssertResumesWithoutPostingToTheCallersContext(pipe: false, streaming: false, insideUnit: false);
    }

    /// <summary>Completes the reader operation without capturing the caller's synchronization context.</summary>
    [TestMethod]
    public async Task PrimingUnitsFromAPipeDoesNotPostToTheCallersContext()
    {
        //T-004 at XliffReader.cs:283, T-007 at line 488 and T-008 at line 492 can stop
        //asynchronous progress. Fail by name after ten seconds and abandon a spinning worker;
        //the existing assertions still execute unchanged inside the deadline.
        await ReaderDeadline.RunAsync(async () =>
        {
            //S-039 at XliffReader.cs:205, S-042 at line 283 and S-046 at line 488
            //change false to true. Priming suspends with each await under the caller context; posts expose it.
            await AssertResumesWithoutPostingToTheCallersContext(pipe: true, streaming: true, insideUnit: false);
        }, TestContext.CancellationToken);
    }

    /// <summary>Completes the reader operation without capturing the caller's synchronization context.</summary>
    [TestMethod]
    public async Task PrimingUnitsFromAStreamDoesNotPostToTheCallersContext()
    {
        //T-004 at XliffReader.cs:283, T-007 at line 488 and T-008 at line 492 can stop
        //asynchronous progress. Fail by name after ten seconds and abandon a spinning worker;
        //the existing assertions still execute unchanged inside the deadline.
        await ReaderDeadline.RunAsync(async () =>
        {
            //S-042 at XliffReader.cs:283 and S-046 at line 488 change false to true.
            //The first advance suspends under the caller context, making the post-count assertion sensitive.
            await AssertResumesWithoutPostingToTheCallersContext(pipe: false, streaming: true, insideUnit: false);
        }, TestContext.CancellationToken);
    }

    /// <summary>Completes the reader operation without capturing the caller's synchronization context.</summary>
    [TestMethod]
    public async Task ReadingAUnitFromAPipeDoesNotPostToTheCallersContext()
    {
        //T-004 at XliffReader.cs:283, T-007 at line 488 and T-008 at line 492 can stop
        //asynchronous progress. Fail by name after ten seconds and abandon a spinning worker;
        //the existing assertions still execute unchanged inside the deadline.
        await ReaderDeadline.RunAsync(async () =>
        {
            //S-041 at XliffReader.cs:277 and S-047 at line 522 change false to true.
            //Synchronous priming preserves the caller context until the unit read suspends; posts expose capture.
            await AssertResumesWithoutPostingToTheCallersContext(pipe: true, streaming: true, insideUnit: true);
        }, TestContext.CancellationToken);
    }

    /// <summary>Completes the reader operation without capturing the caller's synchronization context.</summary>
    [TestMethod]
    public async Task ReadingAUnitFromAStreamDoesNotPostToTheCallersContext()
    {
        //T-004 at XliffReader.cs:283, T-007 at line 488 and T-008 at line 492 can stop
        //asynchronous progress. Fail by name after ten seconds and abandon a spinning worker;
        //the existing assertions still execute unchanged inside the deadline.
        await ReaderDeadline.RunAsync(async () =>
        {
            //S-041 at XliffReader.cs:277 and S-047 at line 522 change false to true.
            //The first suspension is inside the unit, so both pending awaits can capture the caller context.
            await AssertResumesWithoutPostingToTheCallersContext(pipe: false, streaming: true, insideUnit: true);
        }, TestContext.CancellationToken);
    }

    /// <summary>Starts the entire operation under the recorder and releases exactly one pending read.</summary>
    private async Task AssertResumesWithoutPostingToTheCallersContext(bool pipe, bool streaming, bool insideUnit)
    {
        int suspendAt = insideUnit ? Document.IndexOf("Text that", StringComparison.Ordinal) + 3 : 0;
        using var stream = new GatedReadStream(Encoding.UTF8.GetBytes(Document), suspendAt);
        PipeReader? input = pipe ? PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true)) : null;
        var recorder = new RecordingSynchronizationContext();
        Task pending = StartUnderContext(async () => await Consume(stream, input, streaming).ConfigureAwait(false), recorder);

        try
        {
            Assert.IsFalse(pending.IsCompleted, "The operation must suspend at the selected read.");
            Assert.IsTrue(stream.Suspended, "The selected read must be the first asynchronous completion.");
        }
        finally
        {
            stream.Release();
        }

        try
        {
            await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
            Assert.AreEqual(0, recorder.PostCount, "The reader must not post its continuation to the caller's context.");
        }
        finally
        {
            if(input is not null)
            {
                await input.CompleteAsync();
            }
        }
    }

    /// <summary>Starts the synchronous segment under the recorder and restores the caller before returning.</summary>
    private static Task StartUnderContext(Func<Task> start, RecordingSynchronizationContext recorder)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(recorder);
        try
        {
            return start();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    /// <summary>Consumes the public entry point without adding caller-side context captures.</summary>
    private async Task Consume(Stream stream, PipeReader? input, bool streaming)
    {
        if(streaming)
        {
            IAsyncEnumerable<XliffUnit> units = input is null
                ? XliffReader.ReadUnitsAsync(stream, TestContext.CancellationToken)
                : XliffReader.ReadUnitsAsync(input, TestContext.CancellationToken);
            int count = 0;
            await foreach(XliffUnit unit in units.ConfigureAwait(false))
            {
                Assert.AreEqual("A", unit.Id);
                count++;
            }

            Assert.AreEqual(1, count);

            return;
        }

        XliffDocument document = input is null
            ? await XliffReader.ReadAsync(stream, TestContext.CancellationToken).ConfigureAwait(false)
            : await XliffReader.ReadAsync(input, TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual("A", document.Files.Single().Units.Single().Id);
    }

    /// <summary>Returns small synchronous chunks except for exactly one explicitly released read.</summary>
    private sealed class GatedReadStream : MemoryStream
    {
        /// <summary>The byte offset at which the first pending read must begin.</summary>
        private readonly int suspendAt;

        /// <summary>The single asynchronous completion, dispatched without the caller context.</summary>
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Creates a stream with one controlled suspension within its byte sequence.</summary>
        public GatedReadStream(byte[] bytes, int suspendAt) : base(bytes)
        {
            this.suspendAt = suspendAt;
        }

        /// <summary>Whether the controlled asynchronous read has been reached.</summary>
        public bool Suspended { get; private set; }

        /// <summary>Releases the pending read onto the thread pool.</summary>
        public void Release() => gate.TrySetResult();

        /// <summary>Routes array reads through the same chunk and suspension control.</summary>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <summary>Returns synchronously before the selected offset and suspends exactly once there.</summary>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(!Suspended && Position >= suspendAt)
            {
                Suspended = true;

                return CompleteRead(buffer, cancellationToken);
            }

            return ValueTask.FromResult(ReadChunk(buffer));
        }

        /// <summary>Completes the gated read without posting to the recording context itself.</summary>
        private async ValueTask<int> CompleteRead(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return ReadChunk(buffer);
        }

        /// <summary>Limits read-ahead so the selected suspension remains inside the intended XML operation.</summary>
        private int ReadChunk(Memory<byte> buffer)
        {
            int count = Math.Min(buffer.Length, 8);
            if(Position < suspendAt)
            {
                count = Math.Min(count, suspendAt - (int)Position);
            }

            return base.Read(buffer.Span[..count]);
        }
    }
}
