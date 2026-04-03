import { memo, useEffect, useRef } from "react";
import type { Message } from "../types/chat";
import { MessageItem } from "./MessageItem";

type MessageListProps = {
  messages: Message[];
  isWaitingFirstToken: boolean;
};

function MessageListComponent({ messages, isWaitingFirstToken }: MessageListProps) {
  const endRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: isWaitingFirstToken ? "smooth" : "auto" });
  }, [messages, isWaitingFirstToken]);

  return (
    <section className="message-list" aria-live="polite">
      {messages.length === 0 && <div className="empty-state">Start by asking a question.</div>}
      {messages.map((message) => (
        <MessageItem key={message.id} message={message} />
      ))}
      {isWaitingFirstToken && (
        <div className="waiting-indicator">Assistant is thinking...</div>
      )}
      <div ref={endRef} />
    </section>
  );
}

export const MessageList = memo(MessageListComponent);


