# EngineerEval

A **.NET 9.0** framework for evaluating AI engineering assistants using the **Link & Correct** methodology — a precision/recall approach that measures both the *completeness* and the *accuracy* of an AI-generated engineering answer against a reference solution.

---

## Background

AI assistants are increasingly used in technical roles — generating code, solving engineering problems, and explaining complex concepts. Evaluating those responses objectively is hard. Traditional rubrics (correct/incorrect) miss nuance; human review doesn't scale.

**EngineerEval** addresses this with a two-dimensional score:

| Metric | Question answered | Analogy |
|---|---|---|
| **Link Score** | *How much of the ground truth did the AI capture?* | Recall |
| **Correct Score** | *How accurate were the AI's statements?* | Precision |

An AI that covers all key facts scores high on Link. An AI that makes no unsupported claims scores high on Correct. The ideal response scores high on both.

---

## How It Works — The Link & Correct Methodology

Each benchmark is evaluated through a three-step pipeline:

1. **Extract** — A judge prompt identifies every discrete, verifiable technical fact in the ground-truth answer (Set A) and the AI response (Set B): values, formulas, units, intermediate steps, and final results.
2. **Compare** — A second prompt measures how many Set A facts appear in Set B (Link) and how many Set B facts are supported by Set A (Correct).
3. **Report** — Scores, missing facts, and hallucinated facts are written to `results.json` and a colour-coded HTML report.

---

## Project Structure

```
EngineerEval/
├── EngineerEval.Core/            # Core evaluation library
│   ├── Judges/                   # IJudge, BaseJudge, LinkAndCorrectJudge
│   ├── LanguageModels/           # ILanguageModelClient, OpenAI, Gemini, Mock clients
│   ├── Models/                   # Benchmark and BenchmarkResult models
│   └── Resources/                # Embedded prompt templates and HTML report template
├── EngineerEval.CLI/             # Console runner — loads benchmarks, runs judges, writes results
│   └── appsettings.json          # Provider selection (Mock / Azure / Google)
├── EngineerEval.Tests/           # xUnit unit tests
└── benchmarks/                   # 12 engineering benchmark JSON files
    ├── mechanical/               # Tensile stress, gear trains, pump power, flywheel energy
    ├── electrical/               # Transformer voltage, DC motor, induction motor, RC circuit
    └── civil/                    # Beam bending, cantilever deflection, column buckling, moment of inertia
```

---

## Benchmarks

12 benchmarks spanning three engineering domains and three skill levels:

| Domain | Benchmark | Skill Level |
|---|---|---|
| Mechanical | Tensile Stress | Entry |
| Mechanical | Gear Train Speed | Entry |
| Mechanical | Pump Power | Mid |
| Mechanical | Flywheel Energy | Mid |
| Electrical | Transformer Voltage | Entry |
| Electrical | DC Motor Power | Entry |
| Electrical | Induction Motor Speed | Mid |
| Electrical | RC Circuit Impedance | Mid |
| Civil | Beam Bending Moment | Entry |
| Civil | Column Safety Factor | Mid |
| Civil | Cantilever Deflection | Mid |
| Civil | Moment of Inertia | Senior |

---

## Quick Start — No API Key Required

The default provider is **Mock** — a fully offline implementation that uses sentence-boundary detection for fact extraction and keyword-overlap (Jaccard similarity) for fact comparison. It produces realistic, deterministic scores and is ideal for demos and CI pipelines.

```bash
# Clone and build
git clone https://github.com/your-username/EngineerEval.git
cd EngineerEval
dotnet build

# Run all 12 benchmarks
dotnet run --project EngineerEval.CLI
```

Results are written to `results/<timestamp>/`:
- `results.json` — machine-readable scores and full fact lists
- `results.html` — colour-coded HTML summary report (open in any browser)

---

## Using a Real LLM

| `Provider` value | Requires |
|---|---|
| `"Mock"` | Nothing — fully offline |
| `"Azure"` | Azure OpenAI endpoint + key |
| `"Google"` | Google Cloud credentials + Vertex AI project |

Create `EngineerEval.CLI/appsettings.local.json` (git-ignored, never committed) and add your credentials there:

```json
{
  "Provider": "Azure",
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
      "Key": "YOUR_KEY",
      "DeploymentName": "gpt-4o"
    }
  }
}
```

The local file takes precedence over `appsettings.json` at runtime. Your keys stay off GitHub.

---

## Running the Tests

```bash
dotnet test
```

The test suite covers:
- `Utilities.RemoveLlmJsonMarkers` — strips markdown fences from LLM output
- `MockLanguageModelClient` fact extraction — returns valid JSON with correct fact counts
- `MockLanguageModelClient` fact comparison — produces Link/Correct scores in [0, 1]
- Edge cases: perfect overlap (high scores), zero overlap (missing + incorrect facts reported)

---

## Adding a Benchmark

Drop a JSON file into the appropriate `benchmarks/<domain>/` folder:

```json
{
  "question": "A shaft transmits 50 kW at 1500 RPM. Calculate the torque.",
  "ground_truth": "Torque T = P / ω.  ω = 1500 × 2π / 60 = 157.08 rad/s.  T = 50,000 / 157.08 = 318.3 Nm.",
  "ai_response": "Using T = P/ω, ω = 157.1 rad/s, so T = 50000/157.1 ≈ 318.3 Nm."
}
```

The runner automatically discovers all `.json` files in the `benchmarks/` tree — no code changes needed.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9.0 / C# 13 |
| LLM providers | Azure OpenAI, Google Vertex AI (Gemini), Mock (offline) |
| Logging | Serilog (console + structured output) |
| Serialisation | System.Text.Json, Newtonsoft.Json |
| Testing | xUnit |

---

## License

MIT — see `LICENSE` for details.
