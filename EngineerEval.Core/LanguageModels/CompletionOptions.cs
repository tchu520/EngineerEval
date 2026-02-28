namespace EngineerEval.Core.LanguageModels;

public class CompletionOptions
{
    public float Temperature { get; set; } = 0f;
    public bool UseReproducibleResults { get; set; } = false;
    public Type? StructuredOutput { get; set; }
}