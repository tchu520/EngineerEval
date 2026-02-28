using System.Text;
using EngineerEval.Core;
using EngineerEval.Core.Judges;
using EngineerEval.Core.LanguageModels;
using EngineerEval.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;

namespace EngineerEval.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== EngineerEval - AI Engineering Assistant Evaluation ===\n");

        // Load config from the binary output directory so `dotnet run` works from any directory.
        // appsettings.local.json (git-ignored) overrides appsettings.json — put real API keys there.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/engineereval-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            var options = new EngineerEvalOptions(configuration, 
                DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss"),
                "./benchmarks",
                "./results");

            logger.LogInformation("Starting EngineerEval...");

            Directory.CreateDirectory(options.OutputDirectory);
            var outputDir = Path.Combine(options.OutputDirectory, options.Timestamp);
            Directory.CreateDirectory(outputDir);

            var provider = configuration["Provider"] ?? "Azure";
            ILanguageModelClient llmClient = provider.Equals("Mock", StringComparison.OrdinalIgnoreCase)
                ? new MockLanguageModelClient()
                : new OpenAiModelClient(options);

            if (provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
                logger.LogInformation("Running in MOCK mode — no API key required.");

            var judge = new LinkAndCorrectJudge(llmClient);

            var benchmarkFiles = Directory.GetFiles(options.WorkingDirectory, "*.json", SearchOption.AllDirectories);
            logger.LogInformation($"Found {benchmarkFiles.Length} benchmark files");

            if (benchmarkFiles.Length == 0)
            {
                logger.LogWarning("No benchmark files found!");
                return;
            }

            var results = new List<BenchmarkResult>();

            foreach (var benchmarkFile in benchmarkFiles)
            {
                try
                {
                    logger.LogInformation($"\nProcessing: {Path.GetFileName(benchmarkFile)}");
                    
                    var json = await File.ReadAllTextAsync(benchmarkFile);
                    var benchmark = JsonConvert.DeserializeObject<Benchmark>(json);

                    if (benchmark == null || string.IsNullOrEmpty(benchmark.AiResponse))
                    {
                        logger.LogWarning($"Skipping {Path.GetFileName(benchmarkFile)} - no AI response");
                        continue;
                    }

                    var benchmarkId = Path.GetFileNameWithoutExtension(benchmarkFile);
                    var domain = Path.GetFileName(Path.GetDirectoryName(benchmarkFile)) ?? "unknown";
                    var (judgeResult, diagnostics) = await judge.EvaluateAsync(
                        benchmarkId,
                        benchmark.GroundTruth,
                        benchmark.AiResponse);

                    if (judgeResult.CustomFormatData is LinkAndCorrectJudgeResult linkCorrectResult)
                    {
                        // LLM returns scores in 0-1 range; scale to 0-100 for display
                        results.Add(new BenchmarkResult
                        {
                            BenchmarkName = Path.GetFileName(benchmarkFile),
                            Domain = domain,
                            Question = benchmark.Question,
                            LinkScore = linkCorrectResult.LinkScore * 100.0,
                            CorrectScore = linkCorrectResult.CorrectScore * 100.0,
                            LinkedFacts = linkCorrectResult.LinkedFacts.Count,
                            MissingFacts = linkCorrectResult.MissingFacts.Count,
                            IncorrectFacts = linkCorrectResult.IncorrectFacts.Count
                        });

                        logger.LogInformation($"  Link Score: {linkCorrectResult.LinkScore * 100.0:F1}%");
                        logger.LogInformation($"  Correct Score: {linkCorrectResult.CorrectScore * 100.0:F1}%");
                    }

                    await Task.Delay(options.DelayBetweenJudgesMs);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {Path.GetFileName(benchmarkFile)}");
                }
            }

            if (results.Count > 0)
            {
                var avgLinkScore = results.Average(r => r.LinkScore);
                var avgCorrectScore = results.Average(r => r.CorrectScore);

                Console.WriteLine("\n=== EVALUATION SUMMARY ===");
                Console.WriteLine($"Total Benchmarks Evaluated: {results.Count}");
                Console.WriteLine($"Average Link Score: {avgLinkScore:F1}%");
                Console.WriteLine($"Average Correct Score: {avgCorrectScore:F1}%");
                Console.WriteLine($"\nResults saved to: {outputDir}");

                var resultsJson = JsonConvert.SerializeObject(results, Formatting.Indented);
                var resultsFile = Path.Combine(outputDir, "results.json");
                await File.WriteAllTextAsync(resultsFile, resultsJson);
                logger.LogInformation($"Results saved to {resultsFile}");

                // --- HTML report ---
                var htmlFile = Path.Combine(outputDir, "results.html");
                var html = HtmlReportBuilder.Build(results, resultsJson, options);
                await File.WriteAllTextAsync(htmlFile, html, Encoding.UTF8);
                logger.LogInformation($"HTML report saved to {htmlFile}");
                Console.WriteLine($"HTML report:    {htmlFile}");
            }

            logger.LogInformation("\nEngineerEval completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in EngineerEval");
            Console.WriteLine($"\nError: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

public static class HtmlReportBuilder
{
    public static string Build(List<BenchmarkResult> results, string resultsJson, EngineerEvalOptions options)
    {
        var template = ResourceProvider.GetHtmlTemplate();

        var avgLink = results.Average(r => r.LinkScore);
        var avgCorrect = results.Average(r => r.CorrectScore);

        // Build assessments table
        var tb = new StringBuilder();
        tb.AppendLine("<table>");
        tb.AppendLine("<tr><th>Benchmark</th><th>Domain</th><th>Question</th><th>Link Score</th><th>Correct Score</th><th>Linked</th><th>Missing</th><th>Incorrect</th></tr>");
        foreach (var r in results)
        {
            var linkClass = r.LinkScore >= 90 ? "score-good" : r.LinkScore >= 70 ? "score-warn" : "score-bad";
            var correctClass = r.CorrectScore >= 90 ? "score-good" : r.CorrectScore >= 70 ? "score-warn" : "score-bad";
            var domainClass = $"domain-{r.Domain.ToLower()}";
            var shortQ = r.Question.Length > 80 ? r.Question[..80] + "…" : r.Question;
            tb.AppendLine($"<tr>" +
                $"<td>{System.Net.WebUtility.HtmlEncode(r.BenchmarkName)}</td>" +
                $"<td><span class=\"domain-badge {domainClass}\">{r.Domain}</span></td>" +
                $"<td>{System.Net.WebUtility.HtmlEncode(shortQ)}</td>" +
                $"<td><span class=\"{linkClass}\">{r.LinkScore:F1}%</span></td>" +
                $"<td><span class=\"{correctClass}\">{r.CorrectScore:F1}%</span></td>" +
                $"<td>{r.LinkedFacts}</td><td>{r.MissingFacts}</td><td>{r.IncorrectFacts}</td>" +
                $"</tr>");
        }
        tb.AppendLine("</table>");

        return template
            .Replace("{timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC")
            .Replace("{workingDirectory}", options.WorkingDirectory)
            .Replace("{totalSubmissions}", results.Count.ToString())
            .Replace("{avgLinkScore:F1}", avgLink.ToString("F1"))
            .Replace("{avgCorrectScore:F1}", avgCorrect.ToString("F1"))
            .Replace("{assessmentsTable}", tb.ToString())
            .Replace("{linkAndCorrectAnalysis}", "<p>See per-benchmark rows in the table above.</p>")
            .Replace("{diagnosticsData}", "<p>Diagnostics available in the JSON output.</p>")
            .Replace("{jsonData}", System.Net.WebUtility.HtmlEncode(resultsJson));
    }
}