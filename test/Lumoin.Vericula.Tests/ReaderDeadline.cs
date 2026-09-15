using System.Runtime.CompilerServices;

namespace Lumoin.Vericula.Tests;

/// <summary>Bounds streaming test workers while preserving the assertions executed on those workers.</summary>
internal static class ReaderDeadline
{
    /// <summary>Runs a synchronous assertion body with a ten-second deadline linked to the test token.</summary>
    public static void Run(Action action, CancellationToken cancellationToken, [CallerMemberName] string property = "")
    {
        Run(() =>
        {
            action();

            return true;
        }, cancellationToken, property);
    }

    /// <summary>
    /// Returns a synchronous worker's result or reports its named lack of progress. Dedicated workers
    /// keep abandoned spins out of the thread pool that services test deadlines and continuations.
    /// </summary>
    public static T Run<T>(Func<T> action, CancellationToken cancellationToken, [CallerMemberName] string property = "")
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .WaitAsync(deadline.Token).GetAwaiter().GetResult();
        }
        catch(OperationCanceledException) when(deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            Assert.Fail($"{property} did not finish within ten seconds; streaming must make progress.");
            throw;
        }
    }

    /// <summary>Starts an asynchronous body on a worker so a synchronous spin cannot block its deadline.</summary>
    public static async Task RunAsync(Func<Task> action, CancellationToken cancellationToken, [CallerMemberName] string property = "")
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await Task.Run(action, cancellationToken).WaitAsync(deadline.Token);
        }
        catch(OperationCanceledException) when(deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            Assert.Fail($"{property} did not finish within ten seconds; streaming must make progress.");
        }
    }
}
