namespace DragonMarkdown.Core.Exporting;

public sealed record ExportHeaderFooterOptions(
    bool Enabled = false,
    string? HeaderText = null,
    string? FooterText = null);
