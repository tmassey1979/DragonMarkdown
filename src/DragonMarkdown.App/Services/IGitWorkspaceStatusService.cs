namespace DragonMarkdown.App.Services;

public interface IGitWorkspaceStatusService
{
    GitWorkspaceStatus GetStatus(string workspaceRoot);
}
