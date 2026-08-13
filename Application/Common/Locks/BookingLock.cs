namespace Application.Common.Locks;

public static class BookingLock
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public static async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try { await action(); }
        finally { _semaphore.Release(); }
    }
}