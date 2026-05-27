namespace DragonMarkdown.App.Services;

public sealed class DebouncedPreviewRefreshScheduler : IPreviewRefreshScheduler
{
    private readonly TimeSpan debounceInterval;
    private readonly Lock syncRoot = new();
    private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
    private CancellationTokenSource? pendingRefresh;
    private bool disposed;

    public DebouncedPreviewRefreshScheduler(TimeSpan? debounceInterval = null)
    {
        this.debounceInterval = debounceInterval ?? TimeSpan.FromMilliseconds(250);
    }

    public void Schedule(Func<CancellationToken, Task> refreshAsync)
    {
        ArgumentNullException.ThrowIfNull(refreshAsync);

        CancellationTokenSource tokenSource = ReplacePendingRefresh();
        _ = RunScheduledRefreshAsync(refreshAsync, tokenSource);
    }

    public void RunNow(Func<CancellationToken, Task> refreshAsync)
    {
        ArgumentNullException.ThrowIfNull(refreshAsync);

        CancellationTokenSource tokenSource = ReplacePendingRefresh();
        _ = RunRefreshAsync(refreshAsync, tokenSource);
    }

    public void CancelPending()
    {
        CancellationTokenSource? tokenSource;
        lock (syncRoot)
        {
            tokenSource = pendingRefresh;
            pendingRefresh = null;
        }

        tokenSource?.Cancel();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CancelPending();
    }

    private CancellationTokenSource ReplacePendingRefresh()
    {
        CancellationTokenSource? previous;
        var next = new CancellationTokenSource();
        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            previous = pendingRefresh;
            pendingRefresh = next;
        }

        previous?.Cancel();
        return next;
    }

    private async Task RunScheduledRefreshAsync(
        Func<CancellationToken, Task> refreshAsync,
        CancellationTokenSource tokenSource)
    {
        try
        {
            await Task.Delay(debounceInterval, tokenSource.Token).ConfigureAwait(false);
            await RunRefreshAsync(refreshAsync, tokenSource).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunRefreshAsync(
        Func<CancellationToken, Task> refreshAsync,
        CancellationTokenSource tokenSource)
    {
        try
        {
            if (synchronizationContext is null)
            {
                await refreshAsync(tokenSource.Token).ConfigureAwait(false);
                return;
            }

            await PostRefreshAsync(refreshAsync, tokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task PostRefreshAsync(Func<CancellationToken, Task> refreshAsync, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        synchronizationContext!.Post(
            async _ =>
            {
                try
                {
                    await refreshAsync(cancellationToken);
                    completion.SetResult();
                }
                catch (OperationCanceledException)
                {
                    completion.SetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            },
            null);

        return completion.Task;
    }
}
