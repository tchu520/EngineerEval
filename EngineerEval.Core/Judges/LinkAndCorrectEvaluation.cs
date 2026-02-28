using System.Text.Json.Serialization;

namespace EngineerEval.Core.Judges;

/// <summary>
/// The raw structured output from a LinkAndCorrect evaluation, as returned by the LLM.
/// Contains Link/Correct scores plus the full fact lists for traceability.
/// </summary>
public class LinkAndCorrectEvaluation
{
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
    /// Converts this LinkAndCorrectEvaluation to a standard Evaluation object
    /// </summary>
    /// <returns>A standard Evaluation object with computed values</returns>
    public Evaluation ToStandardEvaluation()
    {
        // Calculate a composite score from link and correct scores (1-5 scale)
        // Average the two scores and scale to 1-5 range
        var averageScore = (LinkScore + CorrectScore) / 2.0;
        var scaledScore = Math.Max(1, Math.Min(5, (int)Math.Round(averageScore * 5)));
        
        var evaluation = new Evaluation
        {
            Value = scaledScore,
            Rationale = Justification,
            Issues = new List<Issue>()
        };

        // Add missing facts as issues
        evaluation.Issues.AddRange(MissingFacts);

        // Add incorrect facts as issues
        evaluation.Issues.AddRange(IncorrectFacts);

        // Note: LinkedFacts are not added as issues since they represent correct behavior
        
        return evaluation;
    }
}
