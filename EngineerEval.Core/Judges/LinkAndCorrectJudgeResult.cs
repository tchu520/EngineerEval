using System.Text.Json.Serialization;

namespace EngineerEval.Core.Judges;

/// <summary>
/// The persisted result for a single LinkAndCorrect evaluation, combining the
/// Link/Correct scores with the full fact lists for traceability and reporting.
/// </summary>
public class LinkAndCorrectJudgeResult
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("benchmarkId")]
    public string BenchmarkId { get; set; } = "";

    [JsonPropertyName("link_score")]
    public double LinkScore { get; set; }

    [JsonPropertyName("correct_score")]
    public double CorrectScore { get; set; }

    [JsonPropertyName("linked_facts")]
    public List<string> LinkedFacts { get; set; } = new();

    [JsonPropertyName("missing_facts")]
    public List<Issue> MissingFacts { get; set; } = new();

    [JsonPropertyName("incorrect_facts")]
    public List<Issue> IncorrectFacts { get; set; } = new();

    [JsonPropertyName("justification")]
    public string Justification { get; set; } = "";

    [JsonPropertyName("ground_truth_facts")]
    public List<string> GroundTruthFacts { get; set; } = new();

    [JsonPropertyName("ai_response_facts")]
    public List<string> AiResponseFacts { get; set; } = new();

    /// <summary>
    /// Builds a <see cref="LinkAndCorrectJudgeResult"/> from a raw <see cref="LinkAndCorrectEvaluation"/>.
    /// </summary>
    public static LinkAndCorrectJudgeResult FromEvaluation(string benchmarkId, LinkAndCorrectEvaluation evaluation) =>
        new()
        {
            Role           = IJudge.Roles.LinkAndCorrect,
            BenchmarkId    = benchmarkId,
            LinkScore      = evaluation.LinkScore,
            CorrectScore   = evaluation.CorrectScore,
            LinkedFacts    = evaluation.LinkedFacts,
            MissingFacts   = evaluation.MissingFacts,
            IncorrectFacts = evaluation.IncorrectFacts,
            Justification  = evaluation.Justification,
            GroundTruthFacts = evaluation.GroundTruthFacts,
            AiResponseFacts  = evaluation.AiResponseFacts
        };
}
