using System.Security.Cryptography;
using System.Text;

namespace EngineerEval.Core.LanguageModels;

public class ModelResponse
{
    public string Result { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public TimeSpan Duration { get; set; }
    public string FinishReason { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string PromptDescriptor { get; set; } = string.Empty;
    public string PromptHash { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }

    public int TotalTokens => InputTokens + OutputTokens;
    public double? TokensPerSecond => Duration.TotalSeconds > 0 
        ? TotalTokens / Duration.TotalSeconds 
        : null;
    
    public static string ComputePromptHash(string prompt)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(prompt);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes); // 64-character hex string
    }
    
    public override string ToString()
    {
        return $"Model={ModelName}, Prompt={PromptDescriptor}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, " +
               $"Tokens/sec={TokensPerSecond:F2}, Duration={Duration.TotalMilliseconds}ms, FinishReason={FinishReason}";
    }
}