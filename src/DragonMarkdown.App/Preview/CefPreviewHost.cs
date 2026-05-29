using Avalonia.Controls;
using System.Globalization;
using System.Reflection;
using WebViewControl;

namespace DragonMarkdown.App.Preview;

public sealed class CefPreviewHost : IPreviewHost
{
    private readonly WebView browser = new();

    public Control View => browser;

    public void ShowHtml(string html)
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        browser.Address = $"data:text/html;base64,{encoded}";
    }

    public void ScrollToAnchor(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        ExecuteScript($$"""
            (function() {
                const target = document.getElementById('{{EscapeJavaScript(slug)}}');
                if (target) {
                    target.scrollIntoView({ block: 'start', behavior: 'smooth' });
                }
            })();
            """);
    }

    public void ScrollToRatio(double ratio)
    {
        var clampedRatio = Math.Clamp(ratio, 0, 1).ToString(CultureInfo.InvariantCulture);
        ExecuteScript($$"""
            (function() {
                const scrollableHeight = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
                window.scrollTo(0, scrollableHeight * {{clampedRatio}});
            })();
            """);
    }

    public async Task<double?> GetScrollRatioAsync()
    {
        object? result = await EvaluateScriptAsync("""
            (function() {
                const scrollableHeight = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
                if (scrollableHeight === 0) {
                    return 0;
                }

                return Math.max(0, Math.min(1, window.scrollY / scrollableHeight));
            })();
            """).ConfigureAwait(false);

        return result switch
        {
            double value => Math.Clamp(value, 0, 1),
            float value => Math.Clamp(value, 0, 1),
            decimal value => (double)Math.Clamp(value, 0, 1),
            int value => Math.Clamp(value, 0, 1),
            long value => Math.Clamp(value, 0, 1),
            string value when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => Math.Clamp(parsed, 0, 1),
            _ => null
        };
    }

    public void Dispose()
    {
        browser.Dispose();
    }

    private void ExecuteScript(string script)
    {
        MethodInfo? method = FindScriptMethod("ExecuteScript");
        if (method is null)
        {
            return;
        }

        method.Invoke(browser, BuildScriptArguments(method, script));
    }

    private async Task<object?> EvaluateScriptAsync(string script)
    {
        MethodInfo? method = FindScriptMethod("EvaluateScript");
        if (method is null)
        {
            return null;
        }

        MethodInfo callableMethod = method.IsGenericMethodDefinition
            ? method.MakeGenericMethod(typeof(object))
            : method;

        object? invocationResult = callableMethod.Invoke(browser, BuildScriptArguments(callableMethod, script));
        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);
            PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return invocationResult;
    }

    private MethodInfo? FindScriptMethod(string methodName)
    {
        return browser.GetType()
            .GetMethods()
            .Where(candidate => candidate.Name == methodName)
            .FirstOrDefault(candidate =>
            {
                ParameterInfo[] parameters = candidate.GetParameters();
                return parameters.Length > 0
                    && parameters[0].ParameterType == typeof(string)
                    && parameters.Skip(1).All(parameter => parameter.IsOptional || parameter.HasDefaultValue);
            });
    }

    private static object?[] BuildScriptArguments(MethodInfo method, string script)
    {
        ParameterInfo[] parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        arguments[0] = script;

        for (var index = 1; index < parameters.Length; index++)
        {
            arguments[index] = parameters[index].HasDefaultValue
                ? parameters[index].DefaultValue
                : Type.Missing;
        }

        return arguments;
    }

    private static string EscapeJavaScript(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);
    }
}
