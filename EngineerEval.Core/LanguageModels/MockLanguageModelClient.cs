using System.Text;
using System.Text.Json;

namespace EngineerEval.Core.LanguageModels;

/// <summary>
/// Offline mock LLM client for demo and testing without an API key.
/// Extract calls: splits the text into sentences and returns them as facts.
/// Compare calls: computes keyword-overlap (Jaccard) between the two fact arrays
/// to produce plausible Link and Correct scores.
/// </summary>
public class MockLanguageModelClient : ILanguageModelClient
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","is","are","was","were","be","been","being","have","has","had",
        "do","does","did","will","would","could","should","may","might","shall","can",
        "to","of","in","on","at","by","for","with","and","or","but","not","this","that",
        "it","its","from","as","which","when","where","who","what","then","than","into",
        "about","up","out","if","so","also","each","their","they","we","you","your"
    };

    public Task<ModelResponse> GetCompletionAsync(string prompt, string modelDescriptor, CompletionOptions options)
    {
        var isExtract = modelDescriptor.Contains("Extract", StringComparison.OrdinalIgnoreCase);
        var json = isExtract ? SimulateExtraction(prompt) : SimulateComparison(prompt);

        var response = new ModelResponse
        {
            Result = json,
            FinishReason = "Stop",
            ModelName = "mock-offline",
            PromptDescriptor = modelDescriptor,
            InputTokens = prompt.Length / 4,
            OutputTokens = json.Length / 4,
            Duration = TimeSpan.FromMilliseconds(20)
        };
        return Task.FromResult(response);
    }

    // ── Extraction ────────────────────────────────────────────────────────────

    private static string SimulateExtraction(string prompt)
    {
        const string marker = "# Text to Extract Facts From";
        var idx = prompt.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var text = idx >= 0 ? prompt[(idx + marker.Length)..].Trim() : prompt;

        var sentences = SplitSentences(text).Where(s => s.Length > 15).ToList();
        if (sentences.Count == 0)
            sentences.Add("The provided text contains engineering information.");

        var result = new
        {
            facts = sentences,
            total_count = sentences.Count,
            explanation = $"Mock extraction: identified {sentences.Count} facts via sentence-boundary detection."
        };
        return JsonSerializer.Serialize(result);
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    private static string SimulateComparison(string prompt)
    {
        var setA = ParseSection(prompt, "# Set A: Facts from Ground Truth");
        var setB = ParseSection(prompt, "# Set B: Facts from AI Response");

        if (setA.Count == 0 && setB.Count == 0)
            return BuildResult(new(), new(), new(), 1.0, 1.0, "No facts to compare.");

        var linked = new List<string>();
        var missing = new List<string>();
        var matchedB = new HashSet<int>();

        foreach (var aFact in setA)
        {
            var aKw = Keywords(aFact);
            double best = 0; int bestIdx = -1;
            for (int i = 0; i < setB.Count; i++)
            {
                if (matchedB.Contains(i)) continue;
                var bKw = Keywords(setB[i]);
                var overlap = aKw.Intersect(bKw, StringComparer.OrdinalIgnoreCase).Count();
                var union   = aKw.Union(bKw, StringComparer.OrdinalIgnoreCase).Count();
                var j = union > 0 ? (double)overlap / union : 0;
                if (j > best) { best = j; bestIdx = i; }
            }
            if (best >= 0.12 && bestIdx >= 0) { linked.Add(aFact); matchedB.Add(bestIdx); }
            else missing.Add(aFact);
        }

        var incorrect = setB.Select((f, i) => (f, i))
                            .Where(x => !matchedB.Contains(x.i))
                            .Select(x => x.f)
                            .ToList();

        var linkScore    = setA.Count > 0 ? (double)linked.Count / setA.Count : 1.0;
        var correctScore = setB.Count > 0 ? (double)(setB.Count - incorrect.Count) / setB.Count : 1.0;
        var justification =
            $"Extracted {setA.Count} ground-truth facts and {setB.Count} AI-response facts. " +
            $"{linked.Count} ground-truth facts were linked ({linked.Count}/{setA.Count} = {linkScore:F2}). " +
            $"{setB.Count - incorrect.Count} AI-response facts were accurate " +
            $"({setB.Count - incorrect.Count}/{setB.Count} = {correctScore:F2}).";

        return BuildResult(linked, missing, incorrect, linkScore, correctScore, justification);
    }

    private static string BuildResult(List<string> linked, List<string> missing,
        List<string> incorrect, double link, double correct, string justification)
    {
        var missingIssues = missing.Select(f => new
        {
            field = "facts", type = "omission", severity = "moderate",
            evidenceFromGroundTruth = f,
            expected = "This fact should be present in the AI response.",
            explanation = "Ground-truth fact not captured by the AI response.",
            aiResponseFact = ""
        }).ToList<object>();

        var incorrectIssues = incorrect.Select(f => new
        {
            field = "facts", type = "hallucination", severity = "minor",
            evidenceFromGroundTruth = "No direct evidence in ground truth.",
            expected = "Only facts supported by ground truth should be included.",
            explanation = "AI-response fact has no clear match in the ground truth.",
            aiResponseFact = f
        }).ToList<object>();

        var result = new
        {
            link_score    = Math.Round(link, 4),
            correct_score = Math.Round(correct, 4),
            linked_facts   = linked,
            missing_facts  = missingIssues,
            incorrect_facts = incorrectIssues,
            justification
        };
        return JsonSerializer.Serialize(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<string> ParseSection(string prompt, string header)
    {
        var idx = prompt.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return new();
        var after = prompt[(idx + header.Length)..].TrimStart();
        var next  = after.IndexOf("\n# ", StringComparison.OrdinalIgnoreCase);
        var jsonText = next >= 0 ? after[..next].Trim() : after.Trim();
        try { return JsonSerializer.Deserialize<List<string>>(jsonText) ?? new(); }
        catch { return new(); }
    }

    private static List<string> SplitSentences(string text)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            sb.Append(text[i]);
            if ((text[i] == '.' || text[i] == '!' || text[i] == '?') &&
                (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1])))
            {
                var s = sb.ToString().Trim();
                if (s.Length > 15) parts.Add(s);
                sb.Clear();
            }
        }
        var rem = sb.ToString().Trim();
        if (rem.Length > 15) parts.Add(rem);
        return parts;
    }

    private static IEnumerable<string> Keywords(string text) =>
        text.Split(new[] { ' ', '\t', '\n', ',', '.', ':', ';', '(', ')', '!', '?' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Select(w => w.ToLowerInvariant());
}

