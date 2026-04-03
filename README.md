# IT Support RAG System

A Retrieval-Augmented Generation (RAG) system for IT support tickets with real-time streaming, evaluation pipeline, and multi-tenant data isolation.

## Architecture

```
┌──────────────┐     SSE      ┌──────────────────┐    vectors    ┌─────────┐
│  React + TS  │◄────────────►│  ASP.NET Core 8  │◄────────────►│  Qdrant │
│  Frontend    │   /api/chat  │  Web API         │              │  Vector │
│  (Vite)      │              │                  │──embeddings──│  Store  │
└──────────────┘              │                  │              └─────────┘
                              │                  │──completions─┐
                              └──────────────────┘              │
                                                          ┌─────▼─────┐
                                                          │  OpenAI   │
                                                          │  API      │
                                                          └───────────┘
```

## Features

### Core
- **Ingestion & Chunking** — per-field chunking of ticket description, resolution, and notes with configurable chunk size/overlap
- **Vector Search** — OpenAI `text-embedding-3-small` embeddings stored in Qdrant with metadata filtering
- **Multi-Tenant Isolation** — `companyName` filter restricts search to a single company's tickets
- **Real SSE Streaming** — token-by-token streaming from backend to frontend (not buffered or faked)
- **Chat UI** — React + TypeScript with streaming render, source citations, company filter, session history
- **Evaluation Pipeline** — Precision@5, Recall@5, LLM-as-judge correctness with per-question + aggregate reporting

### Bonus
- **PII Redaction** — emails and phone numbers redacted before indexing via regex
- **Chunk Size Experiment** — endpoint to compare precision/recall across different chunk sizes
- **Confidence Scoring** — LLM self-assessed confidence (0–1) with threshold-based warnings

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 8, C# |
| Frontend | React 18, TypeScript, Vite |
| Vector Store | Qdrant |
| LLM | OpenAI GPT-4o-mini |
| Embeddings | OpenAI text-embedding-3-small |
| Containerization | Docker, docker-compose |

## Quick Start

### Prerequisites
- Docker & Docker Compose
- OpenAI API key

### Run with Docker Compose

```bash
cd atlas-test-backend
export OPENAI_API_KEY="your-openai-key"
docker compose up --build
```

This starts:
- **Qdrant** on `localhost:6333`
- **API** on `localhost:8080` (Swagger at `/swagger`)
- **Frontend** on `localhost:3000`

### Local Development

**Backend:**
```bash
cd atlas-test-backend
export OPENAI__APIKEY="your-openai-key"
export QDRANT__BASEURL="http://localhost:6333"
dotnet run --project "atlas test/atlas test.csproj"
```

**Frontend:**
```bash
cd atlas-test-frontend
npm install
npm run dev
```

The Vite dev server proxies `/api` requests to `localhost:5102`.

## Usage

### 1. Ingest tickets
```bash
curl -s -X POST http://localhost:8080/api/ingest
```

### 2. Ask a question
```bash
curl -N -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{"question":"How do you fix VPN disconnections?"}'
```

### 3. Ask with company filter
```bash
curl -N -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{"question":"What security incidents happened?","companyName":"Globex"}'
```

### 4. Run evaluation
```bash
curl -s -X POST http://localhost:8080/api/evaluate
```

### 5. Run chunk size experiment
```bash
curl -s -X POST http://localhost:8080/api/evaluate/chunk-experiment \
  -H "Content-Type: application/json" \
  -d '{"chunkSizes":[500,1000,2000]}'
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/ingest` | Ingest tickets.json into vector store |
| POST | `/api/chat` | Stream a RAG answer (SSE) |
| POST | `/api/evaluate` | Run 15 golden questions, return metrics |
| POST | `/api/evaluate/chunk-experiment` | Compare chunk sizes |

## Project Structure

```
atlas-test/
├── atlas-test-backend/
│   ├── atlas test/
│   │   ├── Api/Controllers/        # ChatController, EvaluationController, IngestionController
│   │   ├── Application/            # DTOs, interfaces, configuration
│   │   ├── Common/                 # TextChunker, PiiRedactionService
│   │   ├── Domain/Models/          # SupportTicket, TicketChunk, RetrievedChunk
│   │   ├── Infrastructure/         # OpenAiClient, QdrantVectorStore
│   │   ├── Services/               # ChatService, EvaluationService, IngestionService, RetrievalService
│   │   └── Program.cs
│   ├── Dockerfile
│   ├── docker-compose.yml
│   ├── EVALUATION.md               # Metrics, scores, analysis
│   └── README.md
├── atlas-test-frontend/
│   ├── src/
│   │   ├── components/             # Chat, ChatInput, CompanyFilter, MessageItem, MessageList
│   │   ├── hooks/useChat.ts        # Chat state management with useReducer
│   │   ├── services/chatService.ts # SSE fetch + async generator
│   │   ├── utils/sse.ts            # SSE frame parser
│   │   └── types/chat.ts
│   ├── Dockerfile
│   └── nginx.conf
└── README.md                       # This file
```

## Evaluation Results

| Metric | Score |
|--------|-------|
| Avg Precision@5 | 0.533 |
| Avg Recall@5 | 1.000 |
| Avg Correctness | 0.900 |

See [EVALUATION.md](atlas-test-backend/EVALUATION.md) for full per-question breakdown, chunk size experiment results, confidence scoring details, and improvement analysis.
