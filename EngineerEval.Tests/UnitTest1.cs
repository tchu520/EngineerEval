using EngineerEval.Core;
using EngineerEval.Core.Judges;
using EngineerEval.Core.LanguageModels;
using System.Text.Json;

namespace EngineerEval.Tests;

// ── Utilities ────────────────────────────────────────────────────────────────

public class UtilitiesTests
{
    [Theory]
    [InlineData("```json\n{\"a\":1}\n```", "{\"a\":1}")]
    [InlineData("```\n{\"a\":1}\n```",    "{\"a\":1}")]
    [InlineData("{\"a\":1}",             "{\"a\":1}")]  // no fences — passthrough
    public void RemoveLlmJsonMarkers_StripsFencesCorrectly(string input, string expected)
    {
        var result = Utilities.RemoveLlmJsonMarkers(input);
        Assert.Equal(expected, result);
    }
}

// ── MockLanguageModelClient ───────────────────────────────────────────────────

public class MockLanguageModelClientTests
{
    private readonly MockLanguageModelClient _client = new();

    [Fact]
    public async Task GetCompletionAsync_ExtractCall_ReturnsValidFactJson()
    {
        var prompt = "# Text to Extract Facts From\nThe gear ratio is 4:1. The output speed is 250 RPM. Torque is 200 Nm.";

        var response = await _client.GetCompletionAsync(prompt, "LinkAndCorrect-Extract-ground-truth", new CompletionOptions());

        Assert.False(string.IsNullOrWhiteSpace(response.Result));

        using var doc = JsonDocument.Parse(response.Result);
        var root  = doc.RootElement;
        Assert.True(root.TryGetProperty("facts",       out var factsEl));
        Assert.True(root.TryGetProperty("total_count", out var countEl));
        Assert.Equal(JsonValueKind.Array, factsEl.ValueKind);
        Assert.True(countEl.GetInt32() >= 1, "At least one fact should be extracted");
    }

    [Fact]
    public async Task GetCompletionAsync_ExtractCall_CountMatchesFacts()
    {
        var prompt = "# Text to Extract Facts From\nStress equals force divided by area. Force is 1000 N. Area is 50 mm squared.";

        var response = await _client.GetCompletionAsync(prompt, "LinkAndCorrect-Extract-ai-response", new CompletionOptions());

        using var doc = JsonDocument.Parse(response.Result);
        var root  = doc.RootElement;
        var facts = root.GetProperty("facts").EnumerateArray().ToList();
        var count = root.GetProperty("total_count").GetInt32();

        Assert.Equal(facts.Count, count);
    }

    [Fact]
    public async Task GetCompletionAsync_CompareCall_ReturnsValidScoreJson()
    {
        var gtFacts = new[] { "The applied force is 500 N.", "The cross-sectional area is 25 mm²." };
        var aiFacts = new[] { "The applied force is 500 N.", "The cross-sectional area is 25 mm²." };

        var prompt =
            $"# Set A: Facts from Ground Truth\n{JsonSerializer.Serialize(gtFacts)}\n" +
            $"# Set B: Facts from AI Response\n{JsonSerializer.Serialize(aiFacts)}";

        var response = await _client.GetCompletionAsync(prompt, "LinkAndCorrect-Compare", new CompletionOptions());

        using var doc = JsonDocument.Parse(response.Result);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("link_score",    out _));
        Assert.True(root.TryGetProperty("correct_score", out _));
        Assert.True(root.TryGetProperty("linked_facts",  out _));
    }

    [Fact]
    public async Task GetCompletionAsync_CompareCall_PerfectOverlap_ScoresAreHigh()
    {
        // Identical fact sets should yield high Link and Correct scores
        var facts = new[] { "The gear ratio is 4.", "The output speed is 250 RPM.", "The input speed is 1000 RPM." };
        var prompt =
            $"# Set A: Facts from Ground Truth\n{JsonSerializer.Serialize(facts)}\n" +
            $"# Set B: Facts from AI Response\n{JsonSerializer.Serialize(facts)}";

        var response = await _client.GetCompletionAsync(prompt, "LinkAndCorrect-Compare", new CompletionOptions());

        using var doc = JsonDocument.Parse(response.Result);
        var root = doc.RootElement;
        var link    = root.GetProperty("link_score").GetDouble();
        var correct = root.GetProperty("correct_score").GetDouble();

        Assert.True(link    >= 0.5, $"Expected high link score, got {link}");
        Assert.True(correct >= 0.5, $"Expected high correct score, got {correct}");
    }

    [Fact]
    public async Task GetCompletionAsync_CompareCall_NoOverlap_HasMissingAndIncorrect()
    {
        var gtFacts = new[] { "The beam length is 5 m.", "The load is 10 kN." };
        var aiFacts = new[] { "The voltage is 12 V.", "The resistance is 100 ohms." };

        var prompt =
            $"# Set A: Facts from Ground Truth\n{JsonSerializer.Serialize(gtFacts)}\n" +
            $"# Set B: Facts from AI Response\n{JsonSerializer.Serialize(aiFacts)}";

        var response = await _client.GetCompletionAsync(prompt, "LinkAndCorrect-Compare", new CompletionOptions());

        using var doc = JsonDocument.Parse(response.Result);
        var root    = doc.RootElement;
        var missing   = root.GetProperty("missing_facts").EnumerateArray().ToList();
        var incorrect = root.GetProperty("incorrect_facts").EnumerateArray().ToList();

        Assert.True(missing.Count   > 0, "Expected missing facts when fact sets have no overlap");
        Assert.True(incorrect.Count > 0, "Expected incorrect facts when fact sets have no overlap");
    }
}
