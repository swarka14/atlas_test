import type { ChatRequest, StreamChunk } from "../types/chat";
import { parseSseBuffer } from "../utils/sse";

function getApiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL;
  if (typeof configured === "string" && configured.trim().length > 0) {
    return configured.replace(/\/$/, "");
  }

  // Local fallback for setups running frontend and backend on separate ports.
  if (import.meta.env.DEV) {
    return "http://localhost:5102";
  }

  return "";
}

function buildRequestBody(question: string, companyName: string | null): ChatRequest {
  return {
    question,
    companyName: companyName || undefined
  };
}

function toApiError(response: Response): string {
  return `Request failed (${response.status})`;
}

export async function* sendMessage(
  question: string,
  companyName: string | null,
  signal?: AbortSignal
): AsyncGenerator<StreamChunk, void, void> {
  const requestBody = buildRequestBody(question, companyName);

  const response = await fetch(`${getApiBaseUrl()}/api/chat`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "text/event-stream"
    },
    body: JSON.stringify(requestBody),
    signal
  });

  if (!response.ok) {
    yield { type: "error", error: toApiError(response) };
    return;
  }

  if (!response.body) {
    yield { type: "error", error: "No response stream available." };
    return;
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let carryOver = "";

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      const text = decoder.decode(value, { stream: true });
      const parsed = parseSseBuffer(text, carryOver);
      carryOver = parsed.carryOver;

      for (const event of parsed.events) {
        yield event;
      }
    }

    const flush = decoder.decode();
    if (flush) {
      const parsed = parseSseBuffer(flush, carryOver);
      carryOver = parsed.carryOver;
      for (const event of parsed.events) {
        yield event;
      }
    }

    if (carryOver.trim().length > 0) {
      const parsed = parseSseBuffer("\n", carryOver);
      for (const event of parsed.events) {
        yield event;
      }
    }
  } catch (error) {
    if (signal?.aborted) {
      yield { type: "error", error: "Request canceled." };
    } else {
      const message = error instanceof Error ? error.message : "Stream interrupted.";
      yield { type: "error", error: message };
    }
  } finally {
    reader.releaseLock();
  }
}


