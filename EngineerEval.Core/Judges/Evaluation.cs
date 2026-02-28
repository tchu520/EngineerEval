namespace EngineerEval.Core.Judges;

public class Evaluation
{
    public int Value { get; set; }
    public string Rationale { get; set; } = "";
    public List<Issue> Issues { get; set; } = [];
    
    public int CountByType(string type) =>
        Issues.Count(i => i.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

    public bool HasIssuesForField(string fieldName) =>
        Issues.Any(i => i.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

    public List<Issue> GetIssuesByField(string fieldName) =>
        Issues.Where(i => i.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<Issue> GetCriticalIssues() =>
        Issues.Where(i => i.IsCritical()).ToList();
}