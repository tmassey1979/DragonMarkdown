using System.Text.RegularExpressions;

namespace DragonMarkdown.Core.Exporting;

public sealed class ExportValidationService
{
    private static readonly Regex ImageRegex = new(@"!\[[^\]]*\]\((?<path>[^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new(@"(?<!!)\[[^\]]+\]\((?<path>[^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex MermaidFenceRegex = new(@"```mermaid\s*(?<source>.*?)```", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public ExportValidationReport Validate(MarkdownExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ExportValidationIssue>();
        var warnings = new List<ExportValidationIssue>();
        var baseFolder = Path.GetDirectoryName(Path.GetFullPath(request.SourcePath)) ?? Directory.GetCurrentDirectory();

        foreach (Match match in ImageRegex.Matches(request.Markdown))
        {
            var imagePath = NormalizeMarkdownReference(match.Groups["path"].Value);
            if (IsRemoteReference(imagePath) || Path.IsPathFullyQualified(imagePath) && File.Exists(imagePath))
            {
                continue;
            }

            var resolvedPath = Path.IsPathFullyQualified(imagePath)
                ? imagePath
                : Path.GetFullPath(Path.Combine(baseFolder, imagePath));

            if (!File.Exists(resolvedPath))
            {
                errors.Add(new ExportValidationIssue(
                    ExportValidationCodes.MissingLocalImage,
                    $"Local image does not exist: {imagePath}",
                    resolvedPath));
            }
        }

        foreach (Match match in LinkRegex.Matches(request.Markdown))
        {
            var reference = NormalizeMarkdownReference(match.Groups["path"].Value);
            if (IsRawFileReference(reference))
            {
                warnings.Add(new ExportValidationIssue(
                    ExportValidationCodes.RawFileReference,
                    $"Raw local file reference may not be portable: {reference}",
                    reference));
            }
        }

        foreach (Match match in MermaidFenceRegex.Matches(request.Markdown))
        {
            var source = match.Groups["source"].Value.Trim();
            if (MermaidDiagramRenderer.TryRender(source) is null)
            {
                warnings.Add(new ExportValidationIssue(
                    ExportValidationCodes.UnsupportedMermaid,
                    "Mermaid diagram syntax is not supported by the built-in exporter.",
                    source));
            }
        }

        return new ExportValidationReport(errors, warnings);
    }

    private static string NormalizeMarkdownReference(string reference)
    {
        var trimmed = reference.Trim();
        var titleIndex = trimmed.IndexOf(" \"", StringComparison.Ordinal);
        return titleIndex > 0 ? trimmed[..titleIndex] : trimmed;
    }

    private static bool IsRemoteReference(string reference) =>
        Uri.TryCreate(reference, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "data");

    private static bool IsRawFileReference(string reference) =>
        reference.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
        || reference.Contains('\\', StringComparison.Ordinal)
        || Path.IsPathFullyQualified(reference);
}
