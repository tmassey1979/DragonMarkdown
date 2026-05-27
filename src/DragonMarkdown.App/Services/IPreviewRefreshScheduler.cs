namespace DragonMarkdown.App.Services;

public interface IPreviewRefreshScheduler : IDisposable
{
    void Schedule(Func<CancellationToken, Task> refreshAsync);

    void RunNow(Func<CancellationToken, Task> refreshAsync);

    void CancelPending();
}
