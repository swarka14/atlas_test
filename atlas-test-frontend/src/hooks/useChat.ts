import { useCallback, useMemo, useReducer, useRef } from "react";
import { sendMessage } from "../services/chatService";
import type { Message } from "../types/chat";

const STATIC_COMPANIES = [
  "Acme Corp",
  "Globex",
  "Initech",
  "Umbrella",
  "Wayne Enterprises"
];

type UseChatResult = {
  messages: Message[];
  isStreaming: boolean;
  isWaitingFirstToken: boolean;
  error: string | null;
  selectedCompanyName: string | null;
  companies: string[];
  setSelectedCompanyName: (companyName: string | null) => void;
  send: (question: string) => Promise<void>;
  cancelCurrentStream: () => void;
};

type ChatState = {
  messages: Message[];
  isStreaming: boolean;
  isWaitingFirstToken: boolean;
  error: string | null;
  selectedCompanyName: string | null;
};

type ChatAction =
  | { type: "start_stream"; userMessage: Message; assistantMessage: Message }
  | { type: "append_token"; assistantId: string; content: string }
  | { type: "append_sources"; assistantId: string; sources: string[] }
  | { type: "set_confidence"; assistantId: string; confidence: number; warning?: string }
  | { type: "set_error"; error: string }
  | { type: "finish_stream"; assistantId: string }
  | { type: "set_company"; companyName: string | null }
  | { type: "cancel_stream" };

const initialState: ChatState = {
  messages: [],
  isStreaming: false,
  isWaitingFirstToken: false,
  error: null,
  selectedCompanyName: null
};

function nowIso(): string {
  return new Date().toISOString();
}

function createUserMessage(content: string): Message {
  return {
    id: crypto.randomUUID(),
    role: "user",
    content,
    createdAt: nowIso()
  };
}

function createAssistantMessage(): Message {
  return {
    id: crypto.randomUUID(),
    role: "assistant",
    content: "",
    sources: [],
    isStreaming: true,
    createdAt: nowIso()
  };
}

function updateAssistantMessage(
  messages: Message[],
  assistantId: string,
  updater: (message: Message) => Message
): Message[] {
  return messages.map((message) =>
    message.id === assistantId && message.role === "assistant" ? updater(message) : message
  );
}

function dedupeSources(current: string[] | undefined, next: string[]): string[] {
  return Array.from(new Set([...(current ?? []), ...next]));
}

function chatReducer(state: ChatState, action: ChatAction): ChatState {
  switch (action.type) {
    case "start_stream":
      return {
        ...state,
        error: null,
        isStreaming: true,
        isWaitingFirstToken: true,
        messages: [...state.messages, action.userMessage, action.assistantMessage]
      };
    case "append_token":
      return {
        ...state,
        isWaitingFirstToken: false,
        messages: updateAssistantMessage(state.messages, action.assistantId, (message) => ({
          ...message,
          content: `${message.content}${action.content}`
        }))
      };
    case "append_sources":
      return {
        ...state,
        messages: updateAssistantMessage(state.messages, action.assistantId, (message) => ({
          ...message,
          sources: dedupeSources(message.sources, action.sources)
        }))
      };
    case "set_confidence":
      return {
        ...state,
        messages: updateAssistantMessage(state.messages, action.assistantId, (message) => ({
          ...message,
          content: message.content.replace(/\[CONFIDENCE:\s*\d+\.?\d*\]/gi, "").trimEnd(),
          confidence: action.confidence,
          confidenceWarning: action.warning
        }))
      };
    case "set_error":
      return { ...state, error: action.error };
    case "finish_stream":
      return {
        ...state,
        isStreaming: false,
        isWaitingFirstToken: false,
        messages: updateAssistantMessage(state.messages, action.assistantId, (message) => ({
          ...message,
          isStreaming: false
        }))
      };
    case "cancel_stream":
      return {
        ...state,
        isStreaming: false,
        isWaitingFirstToken: false,
        messages: state.messages.map((message) =>
          message.isStreaming ? { ...message, isStreaming: false } : message
        )
      };
    case "set_company":
      return { ...state, selectedCompanyName: action.companyName };
    default:
      return state;
  }
}

export function useChat(): UseChatResult {
  const [state, dispatch] = useReducer(chatReducer, initialState);
  const abortRef = useRef<AbortController | null>(null);

  const companies = useMemo(() => STATIC_COMPANIES, []);

  const cancelCurrentStream = useCallback(() => {
    if (abortRef.current) {
      abortRef.current.abort();
      abortRef.current = null;
    }
    dispatch({ type: "cancel_stream" });
  }, []);

  const send = useCallback(
    async (question: string) => {
      const trimmed = question.trim();
      if (!trimmed || state.isStreaming) {
        return;
      }

      cancelCurrentStream();

      const controller = new AbortController();
      abortRef.current = controller;

      const userMessage = createUserMessage(trimmed);
      const assistantMessage = createAssistantMessage();
      const assistantId = assistantMessage.id;
      dispatch({ type: "start_stream", userMessage, assistantMessage });

      try {
        for await (const chunk of sendMessage(
          trimmed,
          state.selectedCompanyName,
          controller.signal
        )) {
          switch (chunk.type) {
            case "token":
              dispatch({ type: "append_token", assistantId, content: chunk.content });
              break;
            case "sources":
              dispatch({ type: "append_sources", assistantId, sources: chunk.sources });
              break;
            case "confidence":
              dispatch({ type: "set_confidence", assistantId, confidence: chunk.confidence, warning: chunk.warning });
              break;
            case "error":
              dispatch({ type: "set_error", error: chunk.error });
              break;
            case "done":
              break;
          }
        }
      } finally {
        if (abortRef.current === controller) {
          abortRef.current = null;
        }
        dispatch({ type: "finish_stream", assistantId });
      }
    },
    [cancelCurrentStream, state.isStreaming, state.selectedCompanyName]
  );

  return {
    messages: state.messages,
    isStreaming: state.isStreaming,
    isWaitingFirstToken: state.isWaitingFirstToken,
    error: state.error,
    selectedCompanyName: state.selectedCompanyName,
    companies,
    setSelectedCompanyName: (companyName) => dispatch({ type: "set_company", companyName }),
    send,
    cancelCurrentStream
  };
}



