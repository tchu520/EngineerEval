# EngineerEval Architecture

## System Overview

EngineerEval is a production-quality AI evaluation framework that uses the **Link & Correct** methodology to assess the accuracy and completeness of AI-generated engineering solutions.

## Architecture Diagram

\\\
┌─────────────────────────────────────────────────────────────┐
│                     EngineerEval.CLI                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Program    │→ │ Configuration│→ │   Logging    │     │
│  │   (Main)     │  │   (JSON)     │  │  (Serilog)   │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│         ↓                                                   │
│  ┌──────────────────────────────────────────────────┐     │
│  │         Benchmark Loading & Orchestration         │     │
│  └──────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    EngineerEval.Core                        │
│                                                             │
│  ┌──────────────────────────────────────────────────┐     │
│  │                  Judges Layer                     │     │
│  │  ┌────────────────────────────────────────┐     │     │
│  │  │      LinkAndCorrectJudge               │     │     │
│  │  │  ┌──────────────┐  ┌──────────────┐   │     │     │
│  │  │  │ Extract Facts│→ │ Compare Facts│   │     │     │
│  │  │  └──────────────┘  └──────────────┘   │     │     │
│  │  └────────────────────────────────────────┘     │     │
│  │  ┌────────────────────────────────────────┐     │     │
│  │  │         BaseJudge (Abstract)           │     │     │
│  │  └────────────────────────────────────────┘     │     │
│  └──────────────────────────────────────────────────┘     │
│                           ↓                                 │
│  ┌──────────────────────────────────────────────────┐     │
│  │            Language Model Clients                 │     │
│  │  ┌──────────────┐      ┌──────────────┐         │     │
│  │  │   OpenAI     │      │    Gemini    │         │     │
│  │  │   Client     │      │    Client    │         │     │
│  │  └──────────────┘      └──────────────┘         │     │
│  └──────────────────────────────────────────────────┘     │
│                           ↓                                 │
│  ┌──────────────────────────────────────────────────┐     │
│  │              Resource Provider                    │     │
│  │  ┌──────────────┐  ┌──────────────┐             │     │
│  │  │   Prompts    │  │ HTML Template│             │     │
│  │  │  (Embedded)  │  │  (Embedded)  │             │     │
│  │  └──────────────┘  └──────────────┘             │     │
│  └──────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    External Services                        │
│  ┌──────────────┐              ┌──────────────┐           │
│  │ Azure OpenAI │              │ Google Gemini│           │
│  │     API      │              │     API      │           │
│  └──────────────┘              └──────────────┘           │
└─────────────────────────────────────────────────────────────┘
\\\

## Core Components

### 1. EngineerEval.CLI
**Purpose**: Command-line interface for running evaluations

**Key Files**:
- \Program.cs\: Main entry point, orchestrates evaluation flow
- \ppsettings.json\: Configuration (API keys, timeouts, delays)

**Responsibilities**:
- Load configuration and initialize logging
- Discover benchmark files
- Instantiate judges and LLM clients
- Execute evaluations
- Generate summary reports
- Save results to JSON

### 2. EngineerEval.Core
**Purpose**: Core evaluation engine and business logic

#### 2.1 Judges
**Location**: \EngineerEval.Core/Judges/\

**Key Classes**:
- \IJudge\: Interface defining judge contract
- \BaseJudge\: Abstract base class with common functionality
- \LinkAndCorrectJudge\: Implements Link & Correct methodology
- \JudgeResult\: Standard result format
- \LinkAndCorrectJudgeResult\: Specialized result with fact details

**Judge Pattern**:
\\\csharp
public interface IJudge
{
    Task<(JudgeResult Result, ModelResponse Diagnostics)> EvaluateAsync(
        string transcriptId, 
        string transcript, 
        string modelOutput);
}
\\\

#### 2.2 Language Model Clients
**Location**: \EngineerEval.Core/LanguageModels/\

**Key Classes**:
- \ILanguageModelClient\: Interface for LLM providers
- \OpenAiModelClient\: Azure OpenAI implementation
- \GeminiModelClient\: Google Gemini implementation
- \OpenAiSchemaGenerator\: Generates JSON schemas for structured output

**Features**:
- Structured output with JSON schema validation
- Configurable timeouts and retries
- Diagnostic information capture
- Support for multiple LLM providers

#### 2.3 Resource Provider
**Location**: \EngineerEval.Core/ResourceProvider.cs\

**Purpose**: Manages embedded resources (prompts, templates)

**Resources**:
- \judge-linkandcorrect-extract-facts.txt\: Fact extraction prompt
- \judge-linkandcorrect-compare-facts.txt\: Fact comparison prompt
- \html-template.html\: Report template

**Benefits**:
- Self-contained deployment (no external files needed)
- Version control for prompts
- Easy distribution

### 3. EngineerEval.Tests
**Purpose**: Unit and integration tests

**Test Coverage**:
- Judge logic
- LLM client integration
- Resource loading
- Configuration parsing

## Data Flow

### Evaluation Flow

\\\
1. Load Benchmark
   ↓
2. Extract Facts from Ground Truth
   ↓
3. Extract Facts from AI Response
   ↓
4. Compare Facts
   ↓
5. Calculate Scores
   ↓
6. Generate Result
   ↓
7. Save to JSON
\\\

### Detailed Link & Correct Flow

\\\
┌─────────────────────────────────────────────────────────┐
│ Step 1: Extract Facts from Ground Truth                │
│                                                         │
│ Input: Ground truth answer                             │
│ LLM Prompt: "Extract all technical facts..."           │
│ Output: List of facts (e.g., ["σ = F/A", "A = πr²"])  │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ Step 2: Extract Facts from AI Response                 │
│                                                         │
│ Input: AI-generated answer                             │
│ LLM Prompt: "Extract all technical facts..."           │
│ Output: List of facts                                  │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ Step 3: Compare Facts                                  │
│                                                         │
│ Input: Ground truth facts + AI response facts          │
│ LLM Prompt: "Compare these facts..."                   │
│ Output:                                                 │
│  - Linked facts (present in both)                      │
│  - Missing facts (in ground truth, not in AI)          │
│  - Incorrect facts (in AI, but wrong)                  │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ Step 4: Calculate Scores                               │
│                                                         │
│ Link Score = (Linked Facts / Ground Truth Facts) × 100 │
│ Correct Score = (Correct Facts / AI Facts) × 100       │
└─────────────────────────────────────────────────────────┘
\\\

## Configuration

### appsettings.json Structure

\\\json
{
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://your-endpoint.openai.azure.com/",
      "Key": "your-api-key",
      "DeploymentName": "gpt-4"
    }
  },
  "Google": {
    "ProjectId": "your-project-id",
    "Location": "us-central1",
    "ModelId": "gemini-1.5-pro-002"
  },
  "DelayBetweenJudgesMs": 1000,
  "TimeoutSeconds": 120
}
\\\

### EngineerEvalOptions

**Key Properties**:
- \WorkingDirectory\: Where to find benchmarks
- \OutputDirectory\: Where to save results
- \Timestamp\: Run identifier
- \DelayBetweenJudgesMs\: Rate limiting
- \TimeoutSeconds\: LLM call timeout

## Extensibility

### Adding a New Judge

1. Create class inheriting from \BaseJudge\
2. Implement \GetPromptTemplate()\
3. Override \EvaluateAsync()\ if needed
4. Add prompt template to Resources
5. Register in CLI

### Adding a New LLM Provider

1. Implement \ILanguageModelClient\
2. Add configuration section
3. Update \EngineerEvalOptions\
4. Add to CLI initialization

### Adding New Benchmark Domains

1. Create subdirectory in \enchmarks/\
2. Add JSON files with question/ground_truth/ai_response
3. No code changes needed!

## Design Principles

### 1. Separation of Concerns
- CLI handles orchestration
- Core handles business logic
- Clear boundaries between layers

### 2. Dependency Injection
- Judges receive LLM clients via constructor
- Easy to mock for testing
- Flexible configuration

### 3. Open/Closed Principle
- Open for extension (new judges, new LLMs)
- Closed for modification (core logic stable)

### 4. Single Responsibility
- Each judge has one evaluation strategy
- Each client handles one LLM provider
- Each class has one reason to change

### 5. Interface Segregation
- Small, focused interfaces
- Clients only depend on what they use

## Performance Considerations

### Rate Limiting
- Configurable delay between judge calls
- Prevents API throttling
- Default: 1000ms

### Timeout Management
- Configurable timeout per LLM call
- Default: 120 seconds
- Prevents hanging on slow responses

### Parallel Processing
- Currently sequential (one benchmark at a time)
- Future: Parallel evaluation with semaphore

### Caching
- Future: Cache fact extractions
- Avoid re-extracting same ground truth

## Security

### API Key Management
- Stored in appsettings.json (not committed)
- Environment variables supported
- Azure Key Vault integration possible

### Input Validation
- JSON schema validation for benchmarks
- Sanitization of user inputs
- Timeout protection against long-running calls

## Deployment

### Standalone Executable
\\\ash
dotnet publish -c Release -r win-x64 --self-contained
\\\

### Docker Container
\\\dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "EngineerEval.CLI.dll"]
\\\

### CI/CD Integration
- GitHub Actions workflow
- Automated testing on PR
- Benchmark regression detection

## Future Enhancements

### Planned Features
1. **HTML Report Generation**: Visual dashboard with charts
2. **Batch Processing**: Evaluate multiple benchmarks in parallel
3. **Regression Detection**: Compare runs over time
4. **Custom Judges**: Hallucination detection, consistency checking
5. **Python Port**: For broader adoption

### Scalability
- Azure Functions for serverless execution
- Queue-based processing for large batches
- Distributed evaluation across multiple workers

---

**Built with ❤️ for the P-1 AI Lead QA Engineer role**
