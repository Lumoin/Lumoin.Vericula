namespace Lumoin.Vericula.Tests;

/// <summary>Records captured continuations while allowing them to finish on the thread pool.</summary>
internal sealed class RecordingSynchronizationContext : SynchronizationContext
{
    /// <summary>The number of continuations posted to this context.</summary>
    private int postCount;

    /// <summary>Gets the number of continuations posted to this context.</summary>
    public int PostCount => Volatile.Read(ref postCount);

    /// <summary>Records a continuation and dispatches it without installing this context.</summary>
    public override void Post(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref postCount);
        ThreadPool.QueueUserWorkItem(_ => d(state));
    }
}
