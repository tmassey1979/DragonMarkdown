namespace DragonMarkdown.App.Services;

public sealed record GitWorkspaceStatus(
    bool IsRepository,
    string BranchName,
    int ChangedFileCount,
    int UntrackedFileCount)
{
    public static GitWorkspaceStatus NotRepository { get; } = new(false, string.Empty, 0, 0);
}
