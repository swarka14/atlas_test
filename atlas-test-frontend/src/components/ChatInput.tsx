import { FormEvent, useCallback, useMemo, useState } from "react";

type ChatInputProps = {
  onSend: (question: string) => Promise<void>;
  onCancel: () => void;
  disabled?: boolean;
  canCancel?: boolean;
};

export function ChatInput({ onSend, onCancel, disabled, canCancel }: ChatInputProps) {
  const [value, setValue] = useState("");
  const trimmedValue = value.trim();
  const canSubmit = useMemo(() => !disabled && trimmedValue.length > 0, [disabled, trimmedValue]);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!canSubmit) {
        return;
      }

      const text = trimmedValue;
      setValue("");
      await onSend(text);
    },
    [canSubmit, onSend, trimmedValue]
  );

  return (
    <form className="chat-input" onSubmit={handleSubmit}>
      <input
        type="text"
        value={value}
        onChange={(event) => setValue(event.target.value)}
        placeholder="Ask a question about your knowledge base..."
        disabled={disabled}
      />
      <button type="submit" disabled={!canSubmit}>
        Send
      </button>
      <button type="button" className="secondary" onClick={onCancel} disabled={!canCancel}>
        Stop
      </button>
    </form>
  );
}


