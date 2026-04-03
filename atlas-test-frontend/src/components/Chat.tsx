import { ChatInput } from "./ChatInput";
import { CompanyFilter } from "./CompanyFilter";
import { MessageList } from "./MessageList";
import { useChat } from "../hooks/useChat";

export function Chat() {
  const {
    messages,
    isStreaming,
    isWaitingFirstToken,
    error,
    selectedCompanyName,
    companies,
    setSelectedCompanyName,
    send,
    cancelCurrentStream
  } = useChat();

  const hasActiveStream = isStreaming;

  return (
    <main className="chat-shell">
      <header className="chat-header">
        <h1>RAG Chat</h1>
        <CompanyFilter
          companies={companies}
          selectedCompanyName={selectedCompanyName}
          onChange={setSelectedCompanyName}
          disabled={hasActiveStream}
        />
      </header>

      <MessageList messages={messages} isWaitingFirstToken={isWaitingFirstToken} />

      {error && <div className="error-banner">{error}</div>}

      <ChatInput
        onSend={send}
        onCancel={cancelCurrentStream}
        disabled={hasActiveStream}
        canCancel={hasActiveStream}
      />
    </main>
  );
}


