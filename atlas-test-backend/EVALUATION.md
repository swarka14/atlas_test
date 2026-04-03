# Evaluation Write-Up

This document explains the scoring metrics used by the API and how to interpret the results from `POST /api/evaluate`.

## Metrics

- `Precision@5`
  - Definition: Among the top 5 retrieved chunks, the fraction that belongs to the ground-truth ticket IDs.
  - Formula: `hits_in_top5 / 5`
  - Range: `0.0` to `1.0`

- `Recall@5`
  - Definition: Among the ground-truth ticket IDs for a question, the fraction that appear in the top 5 retrieved chunks.
  - Formula: `relevant_tickets_found_in_top5 / total_relevant_tickets`
  - Range: `0.0` to `1.0`

- `Answer Correctness`
  - Definition: LLM-as-judge score comparing generated answer to expected answer.
  - Method: `gpt-4o-mini` (or configured `OpenAI:JudgeModel`) returns JSON score in `[0,1]`.
  - Range: `0.0` to `1.0`

## How to run

1) Ingest the dataset

```bash
curl -s -X POST "http://localhost:8080/api/ingest" -H "Content-Type: application/json"
```

2) Run baseline evaluation on the 15 golden questions

```bash
curl -s -X POST "http://localhost:8080/api/evaluate" -H "Content-Type: application/json"
```

3) Run chunk-size experiment (bonus)

```bash
curl -s -X POST "http://localhost:8080/api/evaluate/chunk-experiment" \
  -H "Content-Type: application/json" \
  -d '{"chunkSizes":[500,1000,2000]}'
```

## Baseline scores

### Baseline summary

| Metric | Score |
|--------|-------|
| `avgPrecision` | 0.533 |
| `avgRecall` | 1.000 |
| `avgCorrectness` | 0.900 |

### Per-question breakdown

| QuestionId | Precision@5 | Recall@5 | Correctness | Retrieved Ticket IDs |
|------------|-------------|----------|-------------|---------------------|
| Q01 | 0.40 | 1.00 | 1.00 | TKT-001, TKT-009, TKT-005 |
| Q02 | 0.40 | 1.00 | 0.80 | TKT-003, TKT-016 |
| Q03 | 0.40 | 1.00 | 0.90 | TKT-005, TKT-032, TKT-001 |
| Q04 | 1.00 | 1.00 | 0.90 | TKT-022, TKT-032 |
| Q05 | 0.60 | 1.00 | 0.90 | TKT-031, TKT-015 |
| Q06 | 0.60 | 1.00 | 0.90 | TKT-029, TKT-035, TKT-047 |
| Q07 | 0.60 | 1.00 | 0.80 | TKT-036, TKT-030 |
| Q08 | 0.60 | 1.00 | 1.00 | TKT-004, TKT-044 |
| Q09 | 0.40 | 1.00 | 0.90 | TKT-017, TKT-028, TKT-033, TKT-002 |
| Q10 | 0.60 | 1.00 | 0.90 | TKT-049, TKT-039, TKT-040 |
| Q11 | 0.40 | 1.00 | 0.90 | TKT-010, TKT-003, TKT-016, TKT-012 |
| Q12 | 0.40 | 1.00 | 0.90 | TKT-026, TKT-016, TKT-020 |
| Q13 | 0.60 | 1.00 | 0.90 | TKT-008, TKT-003, TKT-025 |
| Q14 | 0.60 | 1.00 | 0.90 | TKT-038, TKT-028, TKT-003 |
| Q15 | 0.40 | 1.00 | 0.90 | TKT-014, TKT-035, TKT-024, TKT-009 |

### Interpretation

**Recall is perfect (1.0)** — every ground-truth ticket appears in the top 5 results for all 15 questions. The system never misses the right ticket.

**Precision is moderate (0.533)** — on average, about half the retrieved chunks come from irrelevant tickets. This is the classic "high recall, low precision" pattern: retrieval finds the right tickets but also pulls in noise from related-but-wrong tickets (e.g., Q01 retrieves TKT-009 and TKT-005 alongside the correct TKT-001, likely because they share VPN/certificate keywords).

**Correctness is strong (0.900)** — the LLM synthesizes good answers from the retrieved context despite the noisy chunks. Only Q02 and Q07 scored 0.8; all others are 0.9–1.0. This tells us the LLM is robust enough to filter noise in-context.

**Key takeaway**: Retrieval is the bottleneck, not generation. Precision can be improved without touching the prompt.

### What I would change to improve scores

- **Add a reranking stage** (cross-encoder or Cohere Rerank) after initial vector search to push irrelevant chunks out of the top 5 — this directly targets the precision gap.
- **Hybrid retrieval (BM25 + vectors)** — questions like Q09 (Git push, PAT expiry) have specific keywords that sparse retrieval handles better than embeddings alone.
- **Chunk strategy refinement** — currently chunking per-field. Combining `description + resolution` into one chunk per ticket would reduce the chunk count and naturally improve precision since each ticket contributes fewer slots.
- **Metadata boosting** — give chunks from tickets whose `type`/`subType` matches the query topic a small score boost before final ranking.

## Current design strengths

- Uses ticket-aware metadata (`ticketId`, `companyName`, `type`) for filtering and traceability.
- Enforces real SSE streaming for token-level response delivery.
- Exposes per-question retrieval details (`expectedTicketIds`, `retrievedTicketIds`) to debug weak cases.
- Supports chunk-size experiments through an API endpoint without code changes.

## Weaknesses and next improvements

- **Precision is the main weakness (0.533)** — nearly half the retrieved chunks are noise. A reranking step would have the highest ROI.
- Add hybrid retrieval (BM25 + vectors) for sparse keyword-heavy questions (e.g., Q09, Q11).
- Add deterministic secondary judge (string overlap/ROUGE) alongside LLM judge for stability checks.
- Track evaluation history over time in a persisted report file for regression detection.
- Consider merging `description` + `resolution` into a single chunk per ticket to reduce noise.

## PII redaction approach (bonus)

- Redacts emails and phone numbers before indexing using regex rules.
- Replaces matches with `[REDACTED]`.
- Logs redaction counts for auditability.

## Chunk size experiment (bonus)

Ran the evaluation pipeline with three chunk sizes (500, 1000, 2000 characters). The collection is deleted and re-ingested for each size to ensure clean data.

### Results

| Chunk Size | Avg Precision@5 | Avg Recall@5 | Avg Correctness |
|------------|-----------------|--------------|-----------------|
| 500 | 0.573 | 1.000 | 0.900 |
| 1000 | 0.533 | 1.000 | 0.893 |
| 2000 | 0.533 | 1.000 | 0.900 |

### Analysis

- **Recall is unaffected** — all three sizes achieve perfect recall (1.0). The correct tickets always appear in the top 5 regardless of chunk granularity.
- **Smaller chunks give slightly better precision** — 500-char chunks score 0.573 vs 0.533 for 1000/2000. Smaller chunks produce more focused embeddings, so fewer irrelevant fragments get pulled into the top 5.
- **Correctness is stable** — all three are within 0.007 of each other (~0.90). The LLM synthesizes equally good answers across chunk sizes because it receives sufficient context in all cases.
- **1000 and 2000 are identical** — ticket fields (description, resolution, notes) rarely exceed 1000 characters, so increasing to 2000 doesn't change the actual chunks produced.
- **Conclusion**: 500 is the best chunk size for this dataset. The improvement is modest because the ticket texts are short — most fields fit in a single chunk at any size. For longer documents, the difference would be more pronounced.

## Confidence scoring (bonus)

### Approach

The LLM is instructed to end every answer with a `[CONFIDENCE: X.X]` tag (0.0–1.0) reflecting how confident it is that the answer is correct and complete based on the retrieved context. The backend:

1. Parses the confidence tag from the full response using regex.
2. Strips the tag from the answer text shown to the user.
3. Sends a separate SSE event (`type: "confidence"`) with the score.
4. Compares against a configurable threshold (default `0.6`). Below threshold, a warning is included: *"Not confident enough — check with a senior engineer."*

### How it helps

- **User trust calibration** — users see a confidence % badge on each response and know when to double-check.
- **Evaluation insight** — the evaluation pipeline tracks `confidence` and `isLowConfidence` per question, plus `avgConfidence` and `lowConfidenceCount` in the summary. This reveals whether the model "knows what it doesn't know."
- **Correctness correlation** — if low-confidence answers also have low correctness, the threshold is well-calibrated. If high-confidence answers have low correctness, the LLM is overconfident and the prompt needs adjustment.

### Configuration

```json
{
  "Retrieval": {
    "ConfidenceThreshold": 0.6
  }
}
```

Adjusting the threshold changes what triggers the warning. A higher threshold (e.g., 0.8) is stricter and will flag more answers.

### Impact on evaluation

Run `POST /api/evaluate` — the response now includes `confidence`, `isLowConfidence` per question and `avgConfidence`, `lowConfidenceCount` in the summary. Compare `isLowConfidence` against correctness to assess whether the model's self-assessment is reliable.

