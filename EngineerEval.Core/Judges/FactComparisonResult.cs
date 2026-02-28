using System.Text.Json.Serialization;

namespace EngineerEval.Core.Judges;

/// <summary>
/// The structured output from a fact-comparison LLM call.
/// Contains Link (recall) and Correct (precision) scores plus the supporting fact lists.
/// </summary>
public class FactComparisonResult
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
}

