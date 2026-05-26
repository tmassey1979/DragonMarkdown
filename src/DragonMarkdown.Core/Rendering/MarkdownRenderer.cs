using System.Text;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DragonMarkdown.Core.Rendering;

public sealed class MarkdownRenderer
{
    private const string BlockedLocalReferenceUrl = "#blocked-local-reference";
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private static readonly IReadOnlyDictionary<string, string> MathJaxDelimiterPlaceholders =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["\\("] = "DRAGONMARKDOWN_MATH_INLINE_OPEN",
            ["\\)"] = "DRAGONMARKDOWN_MATH_INLINE_CLOSE",
            ["\\["] = "DRAGONMARKDOWN_MATH_BLOCK_OPEN",
            ["\\]"] = "DRAGONMARKDOWN_MATH_BLOCK_CLOSE"
        };

    public MarkdownRenderResult RenderDocument(string markdown, MarkdownRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);

        var protectedMarkdown = ProtectMathJaxDelimiters(markdown);
        var document = Markdown.Parse(protectedMarkdown, Pipeline);
        var blockedReferences = RewriteLocalReferences(document, options);
        var bodyHtml = RestoreMathJaxDelimiters(Markdown.ToHtml(document, Pipeline));

        return new MarkdownRenderResult(BuildHtmlDocument(bodyHtml), blockedReferences);
    }

    private static string ProtectMathJaxDelimiters(string markdown)
    {
        var protectedMarkdown = markdown;

        foreach (var delimiter in MathJaxDelimiterPlaceholders)
        {
            protectedMarkdown = protectedMarkdown.Replace(delimiter.Key, delimiter.Value, StringComparison.Ordinal);
        }

        return protectedMarkdown;
    }

    private static string RestoreMathJaxDelimiters(string html)
    {
        var restoredHtml = html;

        foreach (var delimiter in MathJaxDelimiterPlaceholders)
        {
            restoredHtml = restoredHtml.Replace(delimiter.Value, delimiter.Key, StringComparison.Ordinal);
        }

        return restoredHtml;
    }

    private static IReadOnlyList<BlockedMarkdownReference> RewriteLocalReferences(
        MarkdownDocument document,
        MarkdownRenderOptions options)
    {
        var blockedReferences = new List<BlockedMarkdownReference>();

        foreach (var link in document.Descendants<LinkInline>())
        {
            if (string.IsNullOrWhiteSpace(link.Url))
            {
                continue;
            }

            var referenceKind = link.IsImage ? MarkdownReferenceKind.Image : MarkdownReferenceKind.Link;
            var originalUrl = link.Url;
            var rewrite = RewriteReference(originalUrl, options);

            if (rewrite.BlockReason is null)
            {
                link.Url = rewrite.Url;
                continue;
            }

            link.Url = BlockedLocalReferenceUrl;
            link.GetAttributes().AddProperty("data-dragonmarkdown-blocked-reference", "true");
            blockedReferences.Add(new BlockedMarkdownReference(originalUrl, referenceKind, rewrite.BlockReason.Value));
        }

        return blockedReferences;
    }

    private static ReferenceRewrite RewriteReference(string reference, MarkdownRenderOptions options)
    {
        var trimmedReference = reference.Trim();

        if (IsInPageReference(trimmedReference) || IsAllowedAbsoluteUri(trimmedReference, options.AppUrlScheme))
        {
            return ReferenceRewrite.Allowed(trimmedReference);
        }

        if (IsRawLocalPath(trimmedReference))
        {
            return ReferenceRewrite.Blocked(MarkdownReferenceBlockReason.RawLocalPath);
        }

        var referenceParts = SplitReference(trimmedReference);

        if (string.IsNullOrWhiteSpace(referenceParts.Path))
        {
            return ReferenceRewrite.Allowed(trimmedReference);
        }

        var resolvedPath = ResolveReferencePath(referenceParts.Path, options);

        if (!PathIsWithinWorkspace(resolvedPath, options.WorkspaceRootPath))
        {
            return ReferenceRewrite.Blocked(MarkdownReferenceBlockReason.OutsideWorkspace);
        }

        return ReferenceRewrite.Allowed(ToWorkspaceUrl(resolvedPath, referenceParts.Suffix, options));
    }

    private static bool IsInPageReference(string reference) =>
        reference.StartsWith('#');

    private static bool IsAllowedAbsoluteUri(string reference, string appUrlScheme)
    {
        if (!Uri.TryCreate(reference, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (uri.Scheme.Length == 1)
        {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, "tel", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, "data", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, appUrlScheme, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRawLocalPath(string reference)
    {
        if (reference.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (reference.StartsWith(@"\\", StringComparison.Ordinal) || reference.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return Path.IsPathFullyQualified(reference) || LooksLikeWindowsDrivePath(reference);
    }

    private static bool LooksLikeWindowsDrivePath(string reference) =>
        reference.Length >= 3
        && char.IsAsciiLetter(reference[0])
        && reference[1] == ':'
        && (reference[2] == '\\' || reference[2] == '/');

    private static ReferenceParts SplitReference(string reference)
    {
        var queryIndex = reference.IndexOf('?', StringComparison.Ordinal);
        var fragmentIndex = reference.IndexOf('#', StringComparison.Ordinal);
        var suffixIndex = FirstPositiveIndex(queryIndex, fragmentIndex);

        return suffixIndex < 0
            ? new ReferenceParts(reference, string.Empty)
            : new ReferenceParts(reference[..suffixIndex], reference[suffixIndex..]);
    }

    private static int FirstPositiveIndex(int first, int second)
    {
        if (first < 0)
        {
            return second;
        }

        if (second < 0)
        {
            return first;
        }

        return Math.Min(first, second);
    }

    private static string ResolveReferencePath(string referencePath, MarkdownRenderOptions options)
    {
        if (referencePath.StartsWith('/'))
        {
            return Path.GetFullPath(Path.Combine(options.WorkspaceRootPath, referencePath.TrimStart('/', '\\')));
        }

        var documentDirectory = Path.GetDirectoryName(options.DocumentPath) ?? options.WorkspaceRootPath;
        return Path.GetFullPath(Path.Combine(documentDirectory, referencePath));
    }

    private static bool PathIsWithinWorkspace(string candidatePath, string workspaceRootPath)
    {
        var fullCandidatePath = Path.GetFullPath(candidatePath);
        var fullWorkspaceRootPath = Path.GetFullPath(workspaceRootPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(fullCandidatePath, fullWorkspaceRootPath, comparison)
            || fullCandidatePath.StartsWith(EnsureTrailingDirectorySeparator(fullWorkspaceRootPath), comparison);
    }

    private static string EnsureTrailingDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static string ToWorkspaceUrl(string resolvedPath, string suffix, MarkdownRenderOptions options)
    {
        var relativePath = Path.GetRelativePath(options.WorkspaceRootPath, resolvedPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        return $"{options.AppUrlScheme}://{options.WorkspaceHost}/{EscapeUrlPath(relativePath)}{suffix}";
    }

    private static string EscapeUrlPath(string relativePath)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var escapedSegments = segments.Select(Uri.EscapeDataString);
        return string.Join('/', escapedSegments);
    }

    private static string BuildHtmlDocument(string bodyHtml) =>
        $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/prismjs/themes/prism.min.css">
          <style>
            :root { color-scheme: light dark; }
            body { font-family: system-ui, sans-serif; line-height: 1.5; margin: 24px; }
            img { max-width: 100%; }
            pre { overflow-x: auto; }
            [data-dragonmarkdown-blocked-reference] { opacity: 0.7; }
          </style>
          <script>
            window.MathJax = {
              tex: {
                inlineMath: [['\\(', '\\)']],
                displayMath: [['\\[', '\\]'], ['$$', '$$']]
              }
            };
          </script>
          <script defer src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-chtml.js"></script>
          <script defer src="https://cdn.jsdelivr.net/npm/prismjs/prism.min.js"></script>
          <script defer src="https://cdn.jsdelivr.net/npm/prismjs/components/prism-csharp.min.js"></script>
        </head>
        <body>
        {{bodyHtml}}
          <script type="module">
            import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
            mermaid.initialize({ startOnLoad: true });
            window.addEventListener('load', () => {
              if (window.Prism) {
                Prism.highlightAll();
              }
            });
          </script>
        </body>
        </html>
        """;

    private sealed record ReferenceParts(string Path, string Suffix);

    private sealed record ReferenceRewrite(string Url, MarkdownReferenceBlockReason? BlockReason)
    {
        public static ReferenceRewrite Allowed(string url) => new(url, null);

        public static ReferenceRewrite Blocked(MarkdownReferenceBlockReason reason) => new(BlockedLocalReferenceUrl, reason);
    }
}
