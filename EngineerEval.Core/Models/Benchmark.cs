using Newtonsoft.Json;

namespace EngineerEval.Core.Models;

/// <summary>
/// Represents a single engineering evaluation benchmark loaded from a JSON file.
/// Each benchmark contains a question, the authoritative ground-truth answer, and
/// the AI-generated response that is to be evaluated.
/// </summary>
public class Benchmark
{
    /// <summary>The engineering question posed to the AI system.</summary>
    [JsonProperty("question")]
    public string Question { get; set; } = string.Empty;

    /// <summary>The authoritative reference answer against which the AI response is scored.</summary>
    [JsonProperty("ground_truth")]
    public string GroundTruth { get; set; } = string.Empty;

    /// <summary>The AI-generated response to be evaluated.</summary>
    [JsonProperty("ai_response")]
    public string AiResponse { get; set; } = string.Empty;
}
