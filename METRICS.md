# EngineerEval Metrics Guide

## Overview

EngineerEval uses the **Link & Correct** methodology to evaluate AI-generated engineering solutions. This document explains the metrics, their calculation, and interpretation.

## Core Metrics

### 1. Link Score (Retention/Recall)

**Definition**: Percentage of ground truth facts that are present in the AI response.

**Formula**:
```
Link Score = (Number of Linked Facts / Total Ground Truth Facts) × 100
```

**What it measures**:
- **Completeness**: Did the AI include all important information?
- **Retention**: Did the AI remember/include key facts from the problem?
- **Coverage**: How much of the expected answer is present?

**Interpretation**:
- **100%**: AI response contains all ground truth facts (perfect recall)
- **75-99%**: AI response is mostly complete, minor omissions
- **50-74%**: AI response is partially complete, significant gaps
- **<50%**: AI response is incomplete, major information missing

**Example**:

Ground Truth Facts (5 total):
1. Tensile stress formula is σ = F / A
2. Cross-sectional area for circle is A = πr²
3. Diameter is 20mm, radius is 10mm
4. Force is 50 kN = 50,000 N
5. Final stress is 159.15 MPa

AI Response includes facts 1, 2, 3, and 5 (4 facts)

Link Score = 4 / 5 × 100 = **80%**

---

### 2. Correct Score (Precision/Accuracy)

**Definition**: Percentage of AI response facts that are accurate and correct.

**Formula**:
```
Correct Score = (Number of Correct Facts / Total AI Response Facts) × 100
```

**What it measures**:
- **Accuracy**: Are the facts stated by the AI correct?
- **Precision**: How much of what the AI says is accurate?
- **Hallucination Rate**: Inverse of this score shows fabrication

**Interpretation**:
- **100%**: All AI facts are correct (no hallucinations)
- **75-99%**: Mostly accurate, minor errors
- **50-74%**: Partially accurate, significant errors
- **<50%**: Mostly inaccurate, major hallucinations

**Example**:

AI Response Facts (6 total):
1. Tensile stress formula is σ = F / A ✓
2. Cross-sectional area for circle is A = πr² ✓
3. Diameter is 20mm ✓
4. Force is 50 kN ✓
5. Final stress is 159.15 MPa ✓
6. Steel has a yield strength of 250 MPa ✗ (not in ground truth, could be hallucination)

Correct Score = 5 / 6 × 100 = **83.3%**

---

## Metric Relationship

### The Link-Correct Matrix

|                | High Correct (>80%) | Low Correct (<80%) |
|----------------|---------------------|-------------------|
| **High Link (>80%)** | ✅ **Excellent**: Complete and accurate | ⚠️ **Verbose**: Complete but with errors |
| **Low Link (<80%)**  | ⚠️ **Concise**: Accurate but incomplete | ❌ **Poor**: Incomplete and inaccurate |

### Ideal Scores
- **Link Score**: 90-100% (comprehensive coverage)
- **Correct Score**: 95-100% (high accuracy)

### Common Patterns

#### Pattern 1: High Link, High Correct (Best)
- **Link**: 95%, **Correct**: 98%
- **Interpretation**: AI provided complete, accurate answer
- **Action**: None needed, excellent performance

#### Pattern 2: High Link, Low Correct (Verbose/Hallucinating)
- **Link**: 90%, **Correct**: 70%
- **Interpretation**: AI included all facts but added incorrect information
- **Action**: Investigate hallucinations, improve prompt specificity

#### Pattern 3: Low Link, High Correct (Concise/Incomplete)
- **Link**: 60%, **Correct**: 95%
- **Interpretation**: What AI said was accurate, but it missed key facts
- **Action**: Improve prompt to encourage completeness

#### Pattern 4: Low Link, Low Correct (Worst)
- **Link**: 50%, **Correct**: 60%
- **Interpretation**: AI response is both incomplete and inaccurate
- **Action**: Major prompt revision or model change needed

---

## Derived Metrics

### Hallucination Rate

**Formula**:
```
Hallucination Rate = 100 - Correct Score
```

**Interpretation**:
- **0-5%**: Minimal hallucinations (acceptable)
- **5-15%**: Moderate hallucinations (needs attention)
- **>15%**: High hallucinations (critical issue)

### Completeness Gap

**Formula**:
```
Completeness Gap = 100 - Link Score
```

**Interpretation**:
- **0-10%**: Nearly complete (excellent)
- **10-25%**: Somewhat incomplete (acceptable)
- **>25%**: Significantly incomplete (needs improvement)

### Overall Quality Score

**Formula** (weighted average):
```
Quality Score = (Link Score × 0.4) + (Correct Score × 0.6)
```

**Rationale**: Correctness is weighted higher than completeness because incorrect information is more harmful than missing information.

**Interpretation**:
- **90-100**: Excellent
- **80-89**: Good
- **70-79**: Acceptable
- **<70**: Needs improvement

---

## Fact-Level Metrics

### Linked Facts
**Definition**: Facts from ground truth that appear in AI response

**Example**:
- Ground Truth: "The formula is σ = F / A"
- AI Response: "We use σ = F / A to calculate stress"
- **Status**: ✅ Linked

### Missing Facts
**Definition**: Facts from ground truth that are absent in AI response

**Example**:
- Ground Truth: "Convert to MPa by dividing by 1,000,000"
- AI Response: (no mention of unit conversion)
- **Status**: ❌ Missing

### Incorrect Facts
**Definition**: Facts in AI response that are wrong or not in ground truth

**Example**:
- Ground Truth: (no mention of material properties)
- AI Response: "Steel has a density of 7850 kg/m³"
- **Status**: ⚠️ Incorrect (hallucination or irrelevant)

---

## Benchmark-Level Aggregation

### Average Link Score
```
Avg Link = Σ(Link Scores) / Number of Benchmarks
```

### Average Correct Score
```
Avg Correct = Σ(Correct Scores) / Number of Benchmarks
```

### Domain-Specific Metrics

**Mechanical Engineering Average**:
- Link Score: 85%
- Correct Score: 92%

**Electrical Engineering Average**:
- Link Score: 88%
- Correct Score: 90%

**Civil Engineering Average**:
- Link Score: 82%
- Correct Score: 94%

---

## Evaluation Criteria

### Pass/Fail Thresholds

**Passing Criteria** (both must be met):
- Link Score ≥ 80%
- Correct Score ≥ 90%

**Rationale**:
- 80% Link ensures most key information is present
- 90% Correct ensures high accuracy with minimal hallucinations

### Severity Levels

| Link Score | Correct Score | Severity | Action |
|------------|---------------|----------|--------|
| ≥80% | ≥90% | ✅ **Pass** | None |
| ≥80% | 80-89% | ⚠️ **Warning** | Review for hallucinations |
| 70-79% | ≥90% | ⚠️ **Warning** | Review for completeness |
| <70% | Any | ❌ **Fail** | Major revision needed |
| Any | <80% | ❌ **Fail** | Major revision needed |

---

## Use Cases

### 1. Model Comparison

Compare two AI models on the same benchmarks:

| Model | Avg Link | Avg Correct | Quality Score |
|-------|----------|-------------|---------------|
| GPT-4 | 92% | 95% | 93.8% |
| GPT-3.5 | 85% | 88% | 86.8% |

**Conclusion**: GPT-4 outperforms GPT-3.5 by 7 points

### 2. Regression Detection

Track scores over time to detect degradation:

| Date | Link Score | Correct Score | Change |
|------|------------|---------------|--------|
| 2026-01-01 | 90% | 95% | Baseline |
| 2026-02-01 | 88% | 94% | -2% / -1% |
| 2026-03-01 | 85% | 92% | -5% / -3% ⚠️ |

**Alert**: Regression detected, investigate prompt or model changes

### 3. Domain Analysis

Identify which engineering domains need improvement:

| Domain | Link | Correct | Status |
|--------|------|---------|--------|
| Mechanical | 92% | 95% | ✅ Strong |
| Electrical | 88% | 93% | ✅ Good |
| Civil | 78% | 89% | ⚠️ Needs work |

**Action**: Focus on improving civil engineering benchmarks

---

## Limitations

### 1. LLM-as-Judge Bias
- The judge itself is an LLM, which may have biases
- Mitigation: Use multiple judges, human validation

### 2. Fact Granularity
- What constitutes a "fact" is subjective
- Mitigation: Clear prompt instructions, consistent extraction

### 3. Semantic Equivalence
- Different phrasings of the same fact may not be recognized
- Mitigation: LLM judge is good at semantic matching

### 4. Context Dependency
- Some facts may be implied rather than explicit
- Mitigation: Judge prompt includes context awareness

---

## Best Practices

### 1. Consistent Ground Truth
- Write detailed, fact-rich ground truth answers
- Include all steps and reasoning
- Use consistent terminology

### 2. Multiple Runs
- Run evaluations multiple times (LLM stochasticity)
- Average scores across runs
- Track variance

### 3. Human Validation
- Periodically review judge decisions
- Validate fact extraction quality
- Calibrate thresholds

### 4. Continuous Monitoring
- Track metrics over time
- Set up alerts for regressions
- Regular benchmark updates

---

## Metric Evolution

### Future Enhancements

1. **Confidence Scores**: Add confidence levels to each fact
2. **Weighted Facts**: Some facts are more important than others
3. **Partial Credit**: Facts that are partially correct
4. **Reasoning Quality**: Evaluate the logic, not just facts
5. **Efficiency Metrics**: Tokens used, response time

---

**For questions or feedback, contact the EngineerEval team.**