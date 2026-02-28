using System.Diagnostics;
using System.Globalization;
using Google.Cloud.AIPlatform.V1;

namespace EngineerEval.Core.LanguageModels;

public class GeminiModelClient : ILanguageModelClient
{
    private readonly PredictionServiceClient _client;
    private readonly EngineerEvalOptions _options;
    
    public GeminiModelClient(EngineerEvalOptions options)
    {
        _client = new PredictionServiceClientBuilder()
        {
            Endpoint = $"{options.Google!.Vertex!.Location}-aiplatform.googleapis.com"
        }.Build();
        _options = options;
    }

    public async Task<ModelResponse> GetCompletionAsync(string prompt, string modelDescriptor, CompletionOptions options)
    {
        var generationConfig = new GenerationConfig
        {
            Temperature = options.Temperature
        };
        
        if (options?.UseReproducibleResults ?? false)
        {
            generationConfig.Seed = 42;
        }

        if (options?.StructuredOutput != null)
        {
            generationConfig.ResponseMimeType = "application/json";
            generationConfig.ResponseSchema = OpenApiSchemaGenerator.Generate(options.StructuredOutput);
        }

        var generateRequest = new GenerateContentRequest
        {
            Model = $"projects/{_options.Google!.Vertex!.ProjectId}/locations/{_options.Google!.Vertex!.Location}/publishers/{_options.Google!.Vertex!.Publisher}/models/{_options.Google!.Vertex!.ModelName}",
            Contents =
            {
                new Content
                {
                    Role = "USER",
                    Parts = { new Part {Text = prompt}}
                }
            },
            GenerationConfig = generationConfig
        };

        var stopwatch = Stopwatch.StartNew();
        var prediction = await _client.GenerateContentAsync(generateRequest);
        stopwatch.Stop();
        
        if(prediction.Candidates.Count == 0)
            throw new Exception($"{modelDescriptor} model response contained no results");

        return new ModelResponse
        {
            Result = prediction.Candidates[0].Content.Parts[0].Text.Trim(),
            Duration = stopwatch.Elapsed,
            FinishReason = prediction.Candidates[0].FinishReason.ToString(),
            InputTokens = prediction.UsageMetadata.PromptTokenCount,
            OutputTokens = prediction.UsageMetadata.CandidatesTokenCount,
            ModelName = _options.Google!.Vertex!.ModelName,
            PromptDescriptor = modelDescriptor,
            PromptHash = ModelResponse.ComputePromptHash(prompt),
            Metadata = new Dictionary<string, string>
            {
                { "avgLogprobs", prediction.Candidates[0].AvgLogprobs.ToString(CultureInfo.InvariantCulture) },
                { "responseId", prediction.ResponseId },
                { "modelVersion", prediction.ModelVersion }
            }
        };
    }
}