using Microsoft.Extensions.Configuration;

namespace EngineerEval.Core;

// Options for the EngineerEval service
public class EngineerEvalOptions
{
    public string Timestamp { get; set; }
    public string WorkingDirectory { get; set; } = "./";
    public string OutputDirectory { get; set; } = "./output";
    public int DelayBetweenJudgesMs { get; set; } = 0;
    public int NetworkTimeoutSeconds { get; set; } = 300; // Default 5 minutes
    public AzureOpenAIOptions? Azure { get; set; }
    public GoogleOptions? Google { get; set; }

    public EngineerEvalOptions()
    {
        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
        Azure = new AzureOpenAIOptions { 
            OpenAI = new OpenAIOptions()
        };
        Google = new GoogleOptions {
            Vertex = new VertexOptions()
        };
    }
    
    public EngineerEvalOptions(IConfiguration configuration, string timestamp, string? workingDirectory, string? outputDirectory)
    {
        configuration.GetSection("EngineerEval").Bind(this);
        Azure = new AzureOpenAIOptions();
        configuration.GetSection("Azure").Bind(Azure);
        Google = new GoogleOptions();
        configuration.GetSection("Google").Bind(Google);
        Timestamp = timestamp;

        if (workingDirectory != null)
        {
            WorkingDirectory = workingDirectory;
        }

        if (outputDirectory != null)
        {
            OutputDirectory = outputDirectory;
        }
    }
}

public class GoogleOptions
{
    public VertexOptions? Vertex { get; set; }
}

public class VertexOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
}

public class AzureOpenAIOptions
{
    public OpenAIOptions? OpenAI { get; set; }
}

public class OpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2025-01-01-preview";
}