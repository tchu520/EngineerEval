namespace EngineerEval.Core.Models;

/// <summary>
/// The aggregated evaluation result for a single benchmark after the
/// Link &amp; Correct judge has run.  Scores are expressed on a 0–100 scale.
/// </summary>
public class BenchmarkResult
{
    /// <summary>The file name of the benchmark JSON (e.g. "tensile-stress.json").</summary>
    public string BenchmarkName { get; set; } = string.Empty;

    /// <summary>Engineering domain: mechanical, electrical, or civil.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>The original engineering question posed to the AI.</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Link score (0–100): the percentage of ground-truth facts that were
    /// correctly captured by the AI response (recall).
    /// </summary>
    public double LinkScore { get; set; }

    /// <summary>
    /// Correct score (0–100): the percentage of AI-response facts that are
    /// supported by the ground truth (precision).
    /// </summary>
    public double CorrectScore { get; set; }

    /// <summary>Number of ground-truth facts that were matched in the AI response.</summary>
    public int LinkedFacts { get; set; }

    /// <summary>Number of ground-truth facts not found in the AI response (omissions).</summary>
    public int MissingFacts { get; set; }

    /// <summary>Number of AI-response facts not supported by the ground truth (hallucinations).</summary>
    public int IncorrectFacts { get; set; }
}

