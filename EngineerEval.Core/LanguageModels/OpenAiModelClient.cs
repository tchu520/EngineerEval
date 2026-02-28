using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Serilog;

namespace EngineerEval.Core.LanguageModels;

public class OpenAiModelClient : ILanguageModelClient
{
    private readonly ChatClient _chatClient;
    private readonly EngineerEvalOptions _options;

    public OpenAiModelClient(EngineerEvalOptions options)
    {
        _chatClient = CreateChatClient(options);
        _options = options;
    }
   
    public async Task<ModelResponse> GetCompletionAsync(string prompt, string modelDescriptor, CompletionOptions options)
    {
        var chatCompletionOptions = new ChatCompletionOptions()
        {
            Temperature = options?.Temperature ?? 0f
            // No MaxOutputTokenCount limit - allow model to use as many tokens as needed
        };

        if (options?.UseReproducibleResults ?? false)
        {
            #pragma warning disable OPENAI001
            chatCompletionOptions.Seed = 42;
            #pragma warning restore OPENAI001
        }

        if (options?.StructuredOutput != null)
        {
            var schema = OpenAiSchemaGenerator.Generate(options.StructuredOutput, "responseFormat");
            chatCompletionOptions.ResponseFormat = schema;
        }


        var stopwatch = Stopwatch.StartNew();
        var completion = await _chatClient.CompleteChatAsync([new UserChatMessage(prompt)], chatCompletionOptions);
        stopwatch.Stop();

        if (completion.Value.Content.Count == 0)
            throw new Exception($"{modelDescriptor} model response contained no content");

        var resultText = completion.Value.Content[0].Text.Trim();

        // Log warning if response was truncated due to length
        if (completion.Value.FinishReason.ToString() == "Length")
        {
            Log.Warning("{ModelDescriptor} response was truncated due to length limit. Output tokens: {OutputTokens}",
                modelDescriptor, completion.Value.Usage?.OutputTokenCount ?? 0);
        }

        return new ModelResponse
        {
            Result = resultText,
            Duration = stopwatch.Elapsed,
            InputTokens = completion.Value.Usage?.InputTokenCount ?? 0,
            OutputTokens = completion.Value.Usage?.OutputTokenCount ?? 0,
            FinishReason = completion.Value.FinishReason.ToString(),
            ModelName = _options.Azure!.OpenAI!.DeploymentName,
            PromptDescriptor = modelDescriptor,
            PromptHash = ModelResponse.ComputePromptHash(prompt),
            Metadata = new Dictionary<string, string>
            {
                { "responseId", completion.Value.Id }
            }
        };
    }

    public static ChatClient CreateChatClient(EngineerEvalOptions options)
    {
        Log.Debug("Initializing OpenAI client");

        try
        {
            if (!options.Azure!.OpenAI!.Endpoint.Contains("localhost"))
            {
                Log.Debug("Using Azure OpenAI client with endpoint: {Endpoint}", options.Azure.OpenAI.Endpoint);
                var clientOptions = new AzureOpenAIClientOptions
                {
                    RetryPolicy = new ClientRetryPolicy(),
                    NetworkTimeout = TimeSpan.FromSeconds(options.NetworkTimeoutSeconds)
                };

                var client = new AzureOpenAIClient(new Uri(options.Azure.OpenAI.Endpoint), new AzureKeyCredential(options.Azure.OpenAI.Key), clientOptions);

                var chatClient = client.GetChatClient(options.Azure.OpenAI.DeploymentName);
                Log.Information("Initialized Azure OpenAI client successfully with deployment: {Deployment}, timeout: {Timeout}s",
                    options.Azure.OpenAI.DeploymentName, options.NetworkTimeoutSeconds);
                return chatClient;
            }
            else
            {
                Log.Debug("Using local OpenAI client with endpoint: {Endpoint}", options.Azure.OpenAI.Endpoint);
                var clientOptions = new OpenAIClientOptions
                {
                    Endpoint = new Uri(options.Azure.OpenAI.Endpoint),
                    NetworkTimeout = TimeSpan.FromSeconds(options.NetworkTimeoutSeconds)
                };

                var client = new OpenAIClient(new ApiKeyCredential(options.Azure.OpenAI.Key), clientOptions);

                var chatClient = client.GetChatClient(options.Azure.OpenAI.DeploymentName);
                Log.Information("Initialized local OpenAI client successfully with model: {Model}, timeout: {Timeout}s",
                    options.Azure.OpenAI.DeploymentName, options.NetworkTimeoutSeconds);
                return chatClient;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize OpenAI client: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}