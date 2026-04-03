# RAG Chat Frontend (React + TypeScript)

Production-ready chat UI for a Retrieval-Augmented Generation workflow with real SSE streaming.

## Features

- Incremental token streaming over `POST /api/chat`
- In-memory conversation history for the current browser session
- Company filter (`companyName`) included in request when selected
- Source `ticketIds` shown under assistant replies
- Loading state while waiting for first token
- Request cancellation with `AbortController`
- Basic copy-to-clipboard for assistant messages

## API Contract

- Endpoint: `POST /api/chat`
- Request body:

```json
{
  "question": "string",
  "companyName": "optional string"
}
```

- Response: `text/event-stream`
- Supported stream payloads:
  - `data: plain token`
  - `data: {"token":"..."}`
  - `data: {"content":"..."}`
  - `data: {"ticketIds":["TCK-001","TCK-002"]}`
  - `data: [DONE]`

## Run

```bash
npm install
npm run dev
```

Optional: set backend base URL explicitly (recommended for non-proxied setups).

```bash
export VITE_API_BASE_URL="http://localhost:5102"
npm run dev
```

Notes:
- In development, the app falls back to `http://localhost:5102` if `VITE_API_BASE_URL` is not set.
- Vite dev server also proxies `/api` to `http://localhost:5102`.

## Build

```bash
npm run build
```


