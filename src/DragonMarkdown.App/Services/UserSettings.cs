namespace DragonMarkdown.App.Services;

public sealed record UserSettings(string? LastWorkspacePath, string Theme, bool WordWrap)
{
    public static UserSettings Default { get; } = new(null, "System", true);
}
