using System.Text.Json;
using EngineerEval.Core.LanguageModels;
using Serilog;

namespace EngineerEval.Core.Judges;

/// <summary>
/// Base class for judge implementations that provides common prompt-dispatch functionality.
/// Subclasses override <see cref="GetPromptTemplate"/> and may override
/// <see cref="EvaluateAsync"/> for multi-step evaluation strategies.
/// </summary>
public abstract class BaseJudge : IJudge
{
    protected readonly ILanguageModelClient _client;

    /// <inheritdoc/>
    public abstract string Role { get; }

    protected BaseJudge(ILanguageModelClient client)
    {
        _client = client;
    }

    /// <summary>Returns the prompt template used by this judge.</summary>
    protected abstract string GetPromptTemplate();

    /// <inheritdoc/>
    public virtual async Task<(JudgeResult Result, ModelResponse Diagnostics)> EvaluateAsync(
        string benchmarkId, string groundTruth, string aiResponse)
    {
        var prompt = GetPromptTemplate()
            .Replace("{{ground_truth}}", groundTruth)
            .Replace("{{ai_response}}", aiResponse);

        Log.Debug("Generating {Role} assessment for benchmark {BenchmarkId} — prompt length: {Length} chars",
            Role, benchmarkId, prompt.Length);

        var options = new CompletionOptions
        {
            Temperature = 0f,
            UseReproducibleResults = true,
            StructuredOutput = typeof(Evaluation)
        };

        try
        {
            var response = await _client.GetCompletionAsync(prompt, Role, options);
            Log.Debug("Received {Role} response — length: {Length} chars", Role, response.Result.Length);

            // Strip markdown fences that some models add even with structured-output mode
            var cleaned = Utilities.RemoveLlmJsonMarkers(response.Result);
            var evaluation = JsonSerializer.Deserialize<Evaluation>(cleaned)!;

            var judgeResult = new JudgeResult
            {
                Role = Role,
                BenchmarkId = benchmarkId,
                Evaluation = evaluation
            };

            return (judgeResult, response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating {Role} judge result for {BenchmarkId}: {Message}",
                Role, benchmarkId, ex.Message);
            throw;
        }
    }
}