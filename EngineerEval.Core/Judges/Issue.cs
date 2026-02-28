using System.Text.Json.Serialization;

namespace EngineerEval.Core.Judges;

/// <summary>
/// Represents a single discrepancy found during evaluation —
/// either an omission (ground-truth fact missing from the AI response) or
/// a hallucination (AI-response fact not supported by the ground truth).
/// </summary>
public class Issue
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = "";

    /// <summary>"omission" or "hallucination"</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>"critical", "moderate", or "minor"</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "";

    [JsonPropertyName("evidenceFromGroundTruth")]
    public string EvidenceFromGroundTruth { get; set; } = "";

    [JsonPropertyName("expected")]
    public string Expected { get; set; } = "";

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";

    /// <summary>The specific AI-response fact that is unsupported (populated for hallucinations).</summary>
    [JsonPropertyName("aiResponseFact")]
    public string AiResponseFact { get; set; } = "";

    public bool IsCritical()     => Severity.Equals("critical",     StringComparison.OrdinalIgnoreCase);
    public bool IsOmission()     => Type.Equals("omission",         StringComparison.OrdinalIgnoreCase);
    public bool IsHallucination()=> Type.Equals("hallucination",    StringComparison.OrdinalIgnoreCase);
}