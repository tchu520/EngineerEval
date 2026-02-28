using Serilog;

namespace EngineerEval.Core;

/// <summary>
/// Common utility methods for the EngineerEval application
/// </summary>
public static class Utilities
{
    /// <summary>
    /// Removes common markdown code block markers from LLM JSON responses
    /// </summary>
    /// <param name="response">The raw response text that may contain markdown formatting</param>
    /// <returns>A cleaned JSON string ready for parsing</returns>
    public static string RemoveLlmJsonMarkers(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        try
        {
            // Check for markdown code block with language identifier (```json ... ```)
            if (response.StartsWith("```json") || response.StartsWith("```JSON"))
            {
                // Find where the closing ``` is and extract just the JSON content
                int endIndex = response.LastIndexOf("```");
                if (endIndex > 6) // Must be at least 6 to have room for the content after ```json
                {
                    string jsonContent = response.Substring(7, endIndex - 7).Trim();
                    Log.Debug("Removed Markdown JSON code block delimiters from response");
                    return jsonContent;
                }
            }
            
            // Check for simple markdown code block (``` ... ```)
            else if (response.StartsWith("```"))
            {
                // Find where the closing ``` is and extract just the JSON content
                int endIndex = response.LastIndexOf("```");
                if (endIndex > 3) // Must be at least 3 to have room for the content after ```
                {
                    string jsonContent = response.Substring(3, endIndex - 3).Trim();
                    Log.Debug("Removed simple Markdown code block delimiters from response");
                    return jsonContent;
                }
            }
            
            // No special handling needed, return as-is
            return response;
        }
        catch (Exception ex)
        {
            // If something goes wrong, log and return the original response
            Log.Warning(ex, "Error removing markdown code block markers: {ErrorMessage}", ex.Message);
            return response;
        }
    }
}