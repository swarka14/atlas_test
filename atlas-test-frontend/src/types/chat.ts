export type MessageRole = "user" | "assistant";
export type MessageSource = string;

export type Message = {
  id: string;
  role: MessageRole;
  content: string;
  sources?: MessageSource[];
  confidence?: number;
  confidenceWarning?: string;
  isStreaming?: boolean;
  createdAt: string;
};

export type StreamChunk =
  | { type: "token"; content: string }
  | { type: "sources"; sources: MessageSource[] }
  | { type: "confidence"; confidence: number; warning?: string }
  | { type: "error"; error: string }
  | { type: "done" };

export type ChatRequest = {
  question: string;
  companyName?: string | null;
};


