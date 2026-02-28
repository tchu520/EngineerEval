using EngineerEval.Core.LanguageModels;

namespace EngineerEval.Core.Judges;

/// <summary>
/// Contract for all judge implementations used to evaluate AI responses against a ground truth.
/// </summary>
public interface IJudge
{
    /// <summary>The role name that identifies this judge (e.g. "LinkAndCorrect").</summary>
    string Role { get; }

    /// <summary>
    /// Evaluates an AI response against the ground-truth answer for a benchmark.
    /// </summary>
    /// <param name="benchmarkId">Unique identifier of the benchmark being evaluated.</param>
    /// <param name="groundTruth">The reference (correct) answer for the benchmark.</param>
    /// <param name="aiResponse">The AI-generated response to evaluate.</param>
    /// <returns>A tuple containing the structured judge result and raw model diagnostics.</returns>
    Task<(JudgeResult Result, ModelResponse Diagnostics)> EvaluateAsync(
        string benchmarkId, string groundTruth, string aiResponse);

    /// <summary>Well-known role-name constants.</summary>
    public static class Roles
    {
        public const string LinkAndCorrect = "LinkAndCorrect";
    }
}