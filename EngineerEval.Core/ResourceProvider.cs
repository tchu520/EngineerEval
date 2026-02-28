using System.Collections.Concurrent;
using System.Reflection;

namespace EngineerEval.Core;

/// <summary>
/// Provides cached access to embedded resources in the EngineerEval.Core assembly.
/// </summary>
public static class ResourceProvider
{
    private static readonly ConcurrentDictionary<string, string> ResourceCache = new();

    /// <summary>File-name constants for all embedded resources.</summary>
    public static class ResourceNames
    {
        public const string ExtractFactsPrompt  = "judge-linkandcorrect-extract-facts.txt";
        public const string CompareFactsPrompt  = "judge-linkandcorrect-compare-facts.txt";
        public const string HtmlTemplate        = "html-template.html";
    }

    /// <summary>Returns the fact-extraction prompt template.</summary>
    public static string GetExtractFactsPrompt()  => GetResource(ResourceNames.ExtractFactsPrompt);

    /// <summary>Returns the fact-comparison prompt template.</summary>
    public static string GetCompareFactsPrompt()  => GetResource(ResourceNames.CompareFactsPrompt);

    /// <summary>Returns the HTML report template.</summary>
    public static string GetHtmlTemplate()        => GetResource(ResourceNames.HtmlTemplate);

    /// <summary>
    /// Loads an embedded resource by short file name (e.g. "html-template.html").
    /// Results are cached in memory after the first load.
    /// </summary>
    public static string GetResource(string resourceName)
    {
        if (ResourceCache.TryGetValue(resourceName, out var cached))
            return cached;

        var assembly = Assembly.GetExecutingAssembly();
        var fullName = $"{assembly.GetName().Name}.Resources.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullName)
            ?? throw new FileNotFoundException(
                $"Embedded resource not found: '{fullName}'. " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        ResourceCache[resourceName] = content;
        return content;
    }
}