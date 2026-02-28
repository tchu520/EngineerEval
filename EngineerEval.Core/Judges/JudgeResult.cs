namespace EngineerEval.Core.Judges;

/// <summary>
/// The result produced by a single judge for a single benchmark evaluation.
/// </summary>
public class JudgeResult
{
    /// <summary>The role name of the judge that produced this result.</summary>
    public string Role { get; set; } = "";

    /// <summary>The unique identifier of the benchmark that was evaluated.</summary>
    public string BenchmarkId { get; set; } = "";

    /// <summary>The standard evaluation data (score, rationale, issues).</summary>
    public Evaluation Evaluation { get; set; } = new();

    /// <summary>
    /// Optional strongly-typed result for judges that produce richer output.
    /// For example, <see cref="LinkAndCorrectJudge"/> stores a
    /// <see cref="LinkAndCorrectJudgeResult"/> here.
    /// </summary>
    public object? CustomFormatData { get; set; }
}