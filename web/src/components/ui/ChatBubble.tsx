import type { ChatMessage } from '../../types/chat';

interface ChatBubbleProps {
  message: ChatMessage;
}

function renderMarkdown(text: string) {
  const lines = text.split('\n').filter((l, i, arr) => !(l.trim() === '' && arr[i - 1]?.trim() === ''));
  const elements: React.ReactNode[] = [];
  let listBuffer: string[] = [];

  const flushList = (key: string) => {
    if (listBuffer.length > 0) {
      elements.push(
        <ul key={key} style={{ margin: '4px 0 8px', paddingLeft: '18px' }}>
          {listBuffer.map((item, i) => (
            <li key={i} style={{ marginBottom: '4px' }}>{renderInline(item)}</li>
          ))}
        </ul>
      );
      listBuffer = [];
    }
  };

  lines.forEach((line, i) => {
    const bulletMatch = line.match(/^\s*[\*\-]\s+(.*)/);
    if (bulletMatch) {
      listBuffer.push(bulletMatch[1]);
    } else {
      flushList(`list-${i}`);
      if (line.trim()) {
        elements.push(
          <p key={i} style={{ margin: '0 0 8px', lineHeight: '1.6' }}>
            {renderInline(line)}
          </p>
        );
      }
    }
  });

  flushList('list-end');
  return elements;
}

function renderInline(text: string): React.ReactNode[] {
  const parts = text.split(/\*\*([^*]+)\*\*/g);
  return parts.map((part, i) =>
    i % 2 === 1 ? <strong key={i}>{part}</strong> : part
  );
}

export default function ChatBubble({ message }: ChatBubbleProps) {
  const isUser = message.role === 'user';

  return (
    <div style={{
      display: 'flex',
      justifyContent: isUser ? 'flex-end' : 'flex-start',
      marginBottom: '12px',
    }}>
      <div style={{
        maxWidth: '78%',
        padding: '12px 16px',
        borderRadius: isUser ? '18px 18px 4px 18px' : '18px 18px 18px 4px',
        background: isUser ? '#00B2A9' : '#fff',
        color: isUser ? '#fff' : '#1B2559',
        fontSize: '14px',
        boxShadow: '0 1px 4px rgba(43,54,116,0.1)',
        textAlign: 'left',
      }}>
        {isUser
          ? message.content
          : renderMarkdown(message.content)
        }
      </div>
    </div>
  );
}
