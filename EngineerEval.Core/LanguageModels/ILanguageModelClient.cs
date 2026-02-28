namespace EngineerEval.Core.LanguageModels;

public interface ILanguageModelClient
{
    Task<ModelResponse> GetCompletionAsync(string prompt, string modelDescriptor, CompletionOptions options);
}