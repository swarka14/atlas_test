# IT Support RAG API (.NET 8)

API-only backend for ingestion, retrieval, real token streaming, and evaluation on the provided `tickets.json` and `golden_eval.json` datasets.

## What this project covers

- Ingestion and chunking of ticket `description`, `resolution`, and `notes`
- OpenAI embeddings (`text-embedding-3-small`) with Qdrant vector storage
- Retrieval with optional `companyName` filter (multi-tenant isolation)
- Real SSE streaming from `POST /api/chat` (not buffered/faked)
- Evaluation pipeline with `Precision@5`, binary `Recall@5`, and LLM-judge correctness
- Bonus: PII redaction and chunk-size experiment endpoint

## Tech stack

- ASP.NET Core Web API (.NET 8)
- OpenAI API (`text-embedding-3-small`, `gpt-4o-mini`)
- Qdrant
- Docker / docker-compose

## API endpoints

- `POST /api/ingest`
  - Rebuilds vector index from `tickets.json`
- `POST /api/chat`
  - Request:
    - `{ "question": "...", "companyName": "optional" }`
  - Response:
    - `text/event-stream` with events: `start`, `token`, `sources`, `done`, `error`
- `POST /api/evaluate`
  - Runs 15 golden questions and returns per-question + aggregate metrics
- `POST /api/evaluate/chunk-experiment`
  - Request example:
    - `{ "chunkSizes": [500, 1000, 2000] }`
  - Re-ingests and evaluates for each chunk size

## Run locally

```bash
cd "/Users/yadick/test folder/atlas test/atlas test"
export OPENAI__APIKEY="your-openai-key"
export QDRANT__BASEURL="http://localhost:6333"
dotnet restore "atlas test/atlas test.csproj"
dotnet run --project "atlas test/atlas test.csproj"
```

## Run with Docker

```bash
cd "/Users/yadick/test folder/atlas test/atlas test"
export OPENAI_API_KEY="your-openai-key"
docker compose up --build
```

- API base: `http://localhost:8080`
- Swagger (Development): `http://localhost:8080/swagger`

## Streaming test

```bash
curl -N -X POST "http://localhost:8080/api/chat" \
  -H "Content-Type: application/json" \
  -d '{"question":"How do you fix VPN disconnections caused by KB5034441?"}'
```

## Evaluation workflow

```bash
curl -s -X POST "http://localhost:8080/api/evaluate" \
  -H "Content-Type: application/json"

curl -s -X POST "http://localhost:8080/api/evaluate/chunk-experiment" \
  -H "Content-Type: application/json" \
  -d '{"chunkSizes":[500,1000,2000]}'
```

See `EVALUATION.md` for metric definitions, interpretation guidance, and report template.

