using System.Diagnostics;
using System.ComponentModel;

namespace DragonMarkdown.App.Services;

public sealed class GitWorkspaceStatusService : IGitWorkspaceStatusService
{
    public GitWorkspaceStatus GetStatus(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
        {
            return GitWorkspaceStatus.NotRepository;
        }

        string branchName = RunGit(workspaceRoot, "branch --show-current").Trim();
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return GitWorkspaceStatus.NotRepository;
        }

        string status = RunGit(workspaceRoot, "status --short");
        int changed = 0;
        int untracked = 0;

        foreach (string line in status.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("??", StringComparison.Ordinal))
            {
                untracked++;
            }
            else
            {
                changed++;
            }
        }

        return new GitWorkspaceStatus(true, branchName, changed, untracked);
    }

    private static string RunGit(string workingDirectory, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return string.Empty;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            return string.Empty;
        }
    }
}
