using System.Text.Json.Serialization;

namespace EngineerEval.Core.Judges;

/// <summary>
/// The structured output from a fact-extraction LLM call for a single source
/// (ground-truth answer or AI response).
/// </summary>
public class FactExtractionResult
{
    [JsonPropertyName("facts")]
    public List<string> Facts { get; set; } = new();
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
}

