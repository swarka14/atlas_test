import { memo, useState } from "react";
import type { Message } from "../types/chat";

type MessageItemProps = {
  message: Message;
};

function MessageItemComponent({ message }: MessageItemProps) {
  const isUser = message.role === "user";
  const [isCopied, setIsCopied] = useState(false);
  const time = new Date(message.createdAt).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit"
  });

  const handleCopy = async () => {
    if (!message.content) {
      return;
    }

    try {
      await navigator.clipboard.writeText(message.content);
      setIsCopied(true);
      window.setTimeout(() => setIsCopied(false), 1200);
    } catch {
      setIsCopied(false);
    }
  };

  return (
    <article className={`message-item ${isUser ? "user" : "assistant"}`}>
      <header>
        <strong>{isUser ? "You" : "Assistant"}</strong>
        <span>{time}</span>
      </header>
      <p>{message.content || (message.isStreaming ? "..." : "")}</p>
      {!isUser && !message.isStreaming && message.content && (
        <button type="button" className="copy-button" onClick={handleCopy}>
          {isCopied ? "Copied" : "Copy"}
        </button>
      )}
      {!isUser && message.confidence !== undefined && (
        <span className={`confidence-badge ${message.confidenceWarning ? "low" : "high"}`}>
          Confidence: {(message.confidence * 100).toFixed(0)}%
        </span>
      )}
      {!isUser && message.confidenceWarning && (
        <div className="confidence-warning">{message.confidenceWarning}</div>
      )}
      {!isUser && message.sources && message.sources.length > 0 && (
        <footer>Sources: {message.sources.join(", ")}</footer>
      )}
    </article>
  );
}

export const MessageItem = memo(MessageItemComponent);


