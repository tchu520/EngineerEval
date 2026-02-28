using EngineerEval.Core.LanguageModels;
using System.Text.Json;

namespace EngineerEval.Core.Judges;

/// <summary>
/// Evaluates an AI response against a ground-truth answer using the Link &amp; Correct methodology:
/// <list type="number">
///   <item>Extract facts from the ground-truth answer.</item>
///   <item>Extract facts from the AI response.</item>
///   <item>Compare fact sets and compute Link (recall) and Correct (precision) scores.</item>
/// </list>
/// </summary>
public class LinkAndCorrectJudge : BaseJudge
{
    /// <inheritdoc/>
    public override string Role => IJudge.Roles.LinkAndCorrect;

    private readonly string _extractFactsPrompt;
    private readonly string _compareFactsPrompt;

    public LinkAndCorrectJudge(ILanguageModelClient client) : base(client)
    {
        _extractFactsPrompt = ResourceProvider.GetExtractFactsPrompt();
        _compareFactsPrompt = ResourceProvider.GetCompareFactsPrompt();
    }

    /// <inheritdoc/>
    public override async Task<(JudgeResult Result, ModelResponse Diagnostics)> EvaluateAsync(
        string benchmarkId, string groundTruth, string aiResponse)
    {
        try
        {
            Serilog.Log.Information("Starting LinkAndCorrect evaluation for benchmark {BenchmarkId}", benchmarkId);

            // Step 1: Extract facts from the ground-truth answer
            Serilog.Log.Debug("Step 1: Extracting facts from ground truth");
            var (groundTruthFacts, gtDiagnostics) = await ExtractFactsAsync(groundTruth, "ground-truth");
            Serilog.Log.Information("Extracted {Count} facts from ground truth", groundTruthFacts.TotalCount);

            // Step 2: Extract facts from the AI response
            Serilog.Log.Debug("Step 2: Extracting facts from AI response");
            var (aiResponseFacts, aiDiagnostics) = await ExtractFactsAsync(aiResponse, "ai-response");
            Serilog.Log.Information("Extracted {Count} facts from AI response", aiResponseFacts.TotalCount);

            // Step 3: Compare fact sets and calculate scores
            Serilog.Log.Debug("Step 3: Comparing facts");
            var (comparison, cmpDiagnostics) = await CompareFactsAsync(groundTruthFacts.Facts, aiResponseFacts.Facts);
            Serilog.Log.Information("Link: {Link:P2}  Correct: {Correct:P2}",
                comparison.LinkScore, comparison.CorrectScore);

            var evaluation = new LinkAndCorrectEvaluation
            {
                LinkScore      = comparison.LinkScore,
                CorrectScore   = comparison.CorrectScore,
                LinkedFacts    = comparison.LinkedFacts,
                MissingFacts   = comparison.MissingFacts,
                IncorrectFacts = comparison.IncorrectFacts,
                Justification  = comparison.Justification,
                GroundTruthFacts = groundTruthFacts.Facts,
                AiResponseFacts  = aiResponseFacts.Facts
            };

            var judgeResult = new JudgeResult
            {
                Role         = Role,
                BenchmarkId  = benchmarkId,
                Evaluation   = evaluation.ToStandardEvaluation(),
                CustomFormatData = LinkAndCorrectJudgeResult.FromEvaluation(benchmarkId, evaluation)
            };

            var combinedDiagnostics = new ModelResponse
            {
                Result           = "Three-step evaluation completed",
                FinishReason     = "Stop",
                ModelName        = gtDiagnostics.ModelName,
                PromptDescriptor = Role,
                InputTokens      = gtDiagnostics.InputTokens + aiDiagnostics.InputTokens + cmpDiagnostics.InputTokens,
                OutputTokens     = gtDiagnostics.OutputTokens + aiDiagnostics.OutputTokens + cmpDiagnostics.OutputTokens,
                Duration         = gtDiagnostics.Duration + aiDiagnostics.Duration + cmpDiagnostics.Duration
            };

            return (judgeResult, combinedDiagnostics);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "LinkAndCorrect evaluation failed for {BenchmarkId}: {Message}", benchmarkId, ex.Message);
            throw new InvalidOperationException($"LinkAndCorrect evaluation failed for '{benchmarkId}': {ex.Message}", ex);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<(FactExtractionResult Result, ModelResponse Diagnostics)> ExtractFactsAsync(
        string text, string sourceLabel)
    {
        var prompt = _extractFactsPrompt.Replace("{{text}}", text);
        var options = new CompletionOptions { Temperature = 0f, UseReproducibleResults = true };

        var response = await _client.GetCompletionAsync(prompt, $"{Role}-Extract-{sourceLabel}", options);
        var cleaned  = Utilities.RemoveLlmJsonMarkers(response.Result);

        try
        {
            var result = JsonSerializer.Deserialize<FactExtractionResult>(cleaned)!;
            if (result.Facts.Count != result.TotalCount)
            {
                Serilog.Log.Warning("Fact count mismatch for {Source}: reported {Reported}, actual {Actual}. Using actual.",
                    sourceLabel, result.TotalCount, result.Facts.Count);
                result.TotalCount = result.Facts.Count;
            }
            return (result, response);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse fact extraction for '{sourceLabel}': {ex.Message}", ex);
        }
    }

    private async Task<(FactComparisonResult Result, ModelResponse Diagnostics)> CompareFactsAsync(
        List<string> groundTruthFacts, List<string> aiResponseFacts)
    {
        var gtJson  = JsonSerializer.Serialize(groundTruthFacts,  new JsonSerializerOptions { WriteIndented = true });
        var aiJson  = JsonSerializer.Serialize(aiResponseFacts,   new JsonSerializerOptions { WriteIndented = true });

        var prompt = _compareFactsPrompt
            .Replace("{{ground_truth_facts}}", gtJson)
            .Replace("{{ai_response_facts}}",  aiJson);

        var options = new CompletionOptions { Temperature = 0f, UseReproducibleResults = true };
        var response = await _client.GetCompletionAsync(prompt, $"{Role}-Compare", options);
        var cleaned  = Utilities.RemoveLlmJsonMarkers(response.Result);

        try
        {
            return (JsonSerializer.Deserialize<FactComparisonResult>(cleaned)!, response);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse fact comparison: {ex.Message}", ex);
        }
    }

    /// <summary>Not used — this judge overrides <see cref="EvaluateAsync"/> directly.</summary>
    protected override string GetPromptTemplate() => _extractFactsPrompt;
}

