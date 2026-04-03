import type { StreamChunk } from "../types/chat";

const DONE_SENTINELS = new Set(["[DONE]", "DONE"]);

export function parseSseBuffer(
  input: string,
  carryOver: string
): { events: StreamChunk[]; carryOver: string } {
  const data = carryOver + input;
  const normalized = data.replace(/\r\n/g, "\n");
  const frames = normalized.split("\n\n");
  const nextCarryOver = frames.pop() ?? "";
  const events: StreamChunk[] = [];

  for (const frame of frames) {
    const payload = extractDataPayload(frame);
    if (!payload) {
      continue;
    }

    if (DONE_SENTINELS.has(payload)) {
      events.push({ type: "done" });
      continue;
    }

    const parsed = tryParseJson(payload);
    if (!parsed) {
      events.push({ type: "token", content: payload });
      continue;
    }

    const sources = extractSources(parsed);
    if (sources.length > 0) {
      events.push({ type: "sources", sources });
    }

    const confidence = extractConfidence(parsed);
    if (confidence !== null) {
      events.push(confidence);
    }

    const token = extractToken(parsed);
    if (token) {
      events.push({ type: "token", content: token });
    }

    if (parsed.done === true || parsed.isDone === true) {
      events.push({ type: "done" });
    }

    const error = typeof parsed.error === "string" ? parsed.error : undefined;
    if (error) {
      events.push({ type: "error", error });
    }
  }

  return { events, carryOver: nextCarryOver };
}

function tryParseJson(payload: string): Record<string, unknown> | null {
  try {
    const value = JSON.parse(payload) as unknown;
    if (value && typeof value === "object") {
      return value as Record<string, unknown>;
    }
    return null;
  } catch {
    return null;
  }
}

function extractDataPayload(frame: string): string {
  const dataLines = frame
    .split("\n")
    .map((line) => line.trim())
    .filter((line) => line.startsWith("data:"))
    .map((line) => line.slice(5).trim())
    .filter(Boolean);

  if (dataLines.length === 0) {
    return "";
  }

  return dataLines.join("\n");
}

function extractToken(parsed: Record<string, unknown>): string {
  const candidates = [parsed.token, parsed.content, parsed.delta];
  for (const value of candidates) {
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return "";
}

function extractSources(parsed: Record<string, unknown>): string[] {
  const candidateKeys = ["ticketIds", "sources", "citations"];
  const collected: string[] = [];

  for (const key of candidateKeys) {
    const value = parsed[key];
    if (Array.isArray(value)) {
      for (const item of value) {
        if (typeof item === "string" && item.trim()) {
          collected.push(item.trim());
        }
      }
    }
  }

  return Array.from(new Set(collected));
}

function extractConfidence(parsed: Record<string, unknown>): StreamChunk | null {
  if (parsed.type !== "confidence" || typeof parsed.confidence !== "number") {
    return null;
  }

  return {
    type: "confidence",
    confidence: parsed.confidence,
    warning: typeof parsed.warning === "string" ? parsed.warning : undefined
  };
}


