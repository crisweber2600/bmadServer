# Chat Message Components Documentation

## Overview

This documentation covers the chat message components created for Story 3.2: Chat Message Component with Ant Design. The components provide a complete chat interface with user/agent messages, typing indicators, and auto-scrolling functionality.

## Components

### ChatMessage

A component that renders individual chat messages with support for markdown, syntax highlighting, and accessibility features.

#### Props

```typescript
interface ChatMessageProps {
  content: string;        // The message content (supports markdown)
  isUser: boolean;        // true for user messages, false for agent messages
  timestamp: Date;        // Message timestamp
  agentName?: string;     // Agent name (default: 'BMAD Agent')
}
```

#### Features

- **User Messages**: Aligned right with blue background
- **Agent Messages**: Aligned left with gray background, includes avatar and agent name
- **Markdown Support**: Full markdown rendering with GitHub Flavored Markdown (GFM)
- **Code Highlighting**: Syntax highlighting for code blocks using react-syntax-highlighter
- **Clickable Links**: All links open in new tabs with `rel="noopener noreferrer"` for security
- **Timestamps**: Formatted in 12-hour format with AM/PM
- **Accessibility**: Proper ARIA labels and semantic HTML
- **Animations**: Smooth fade-in animation for new messages

#### Usage Example

```typescript
import { ChatMessage } from './components';

function ChatExample() {
  return (
    <>
      <ChatMessage
        content="Hello, how can I help you?"
        isUser={false}
        timestamp={new Date()}
        agentName="BMAD Assistant"
      />
      <ChatMessage
        content="I need help with **something important**"
        isUser={true}
        timestamp={new Date()}
      />
    </>
  );
}
```

#### Markdown Examples

The ChatMessage component supports various markdown features:

```typescript
// Bold and italic
<ChatMessage content="**bold** and *italic* text" isUser={false} timestamp={new Date()} />

// Code blocks with syntax highlighting
<ChatMessage 
  content={`\`\`\`javascript
function hello() {
  console.log('Hello, world!');
}
\`\`\``} 
  isUser={false} 
  timestamp={new Date()} 
/>

// Links
<ChatMessage content="Check out [this link](https://example.com)" isUser={false} timestamp={new Date()} />

// Lists
<ChatMessage 
  content={`
- Item 1
- Item 2
- Item 3
  `} 
  isUser={false} 
  timestamp={new Date()} 
/>
```

---

### TypingIndicator

A component that displays an animated typing indicator when an agent is composing a response.

#### Props

```typescript
interface TypingIndicatorProps {
  agentName?: string;     // Agent name (default: 'BMAD Agent')
}
```

#### Features

- **Animated Dots**: Three dots with staggered bounce animation
- **Agent Name Display**: Shows which agent is typing
- **Fast Response**: Appears within 500ms (animation optimized)
- **Accessibility**: ARIA live region for screen reader announcements
- **Consistent Styling**: Matches agent message bubble style

#### Usage Example

```typescript
import { TypingIndicator } from './components';

function ChatExample() {
  const [isTyping, setIsTyping] = useState(false);

  return (
    <>
      {messages.map(msg => (
        <ChatMessage key={msg.id} {...msg} />
      ))}
      {isTyping && <TypingIndicator agentName="BMAD Assistant" />}
    </>
  );
}
```

---

### ChatContainer

A container component that handles auto-scrolling and provides proper accessibility attributes for the chat message area.

#### Props

```typescript
interface ChatContainerProps {
  children: React.ReactNode;  // Chat messages and typing indicator
  autoScroll?: boolean;       // Enable auto-scroll (default: true)
}
```

#### Features

- **Auto-Scroll**: Automatically scrolls to bottom when new messages arrive
- **Smooth Animation**: Uses CSS smooth scrolling for better UX
- **Custom Scrollbar**: Styled scrollbar for consistent appearance
- **Accessibility**: Proper ARIA live region for screen readers
- **Responsive**: Adapts to container height

#### Usage Example

```typescript
import { ChatContainer, ChatMessage, TypingIndicator } from './components';

function ChatInterface() {
  const [messages, setMessages] = useState([]);
  const [isTyping, setIsTyping] = useState(false);

  return (
    <ChatContainer autoScroll={true}>
      {messages.map(msg => (
        <ChatMessage
          key={msg.id}
          content={msg.content}
          isUser={msg.isUser}
          timestamp={msg.timestamp}
          agentName={msg.agentName}
        />
      ))}
      {isTyping && <TypingIndicator agentName="BMAD Agent" />}
    </ChatContainer>
  );
}
```

---

## Accessibility Features

All components follow WCAG 2.1 Level AA guidelines:

### ChatMessage
- **ARIA Label**: Descriptive label including sender, content, and time
- **Semantic HTML**: Uses `<article>` role for individual messages
- **Keyboard Navigation**: All interactive elements (links) are keyboard accessible
- **Color Contrast**: Meets WCAG AA contrast requirements

### TypingIndicator
- **ARIA Live Region**: Announces typing status to screen readers
- **Role**: Uses `role="status"` for status updates
- **Aria-live**: Set to "polite" to avoid interrupting other content

### ChatContainer
- **ARIA Live Region**: Set to "polite" for new message announcements
- **Role**: Uses `role="log"` for message history
- **Aria-label**: Descriptive label "Chat messages"

---

## Styling

All components use CSS modules for encapsulation. The styling follows Ant Design principles:

- **Color Scheme**: Uses Ant Design's default blue (`#1890ff`) for primary elements
- **Typography**: Consistent with Ant Design typography scale
- **Spacing**: Uses Ant Design's spacing system (multiples of 8px)
- **Shadows**: Subtle shadows for depth (matching Ant Design)
- **Animations**: Smooth, performant CSS animations

### Customization

You can customize styles by overriding the CSS classes:

```css
/* Example: Custom user message color */
.user-bubble {
  background-color: #52c41a; /* Green instead of blue */
}

/* Example: Larger message bubbles */
.message-bubble {
  padding: 16px 20px;
  font-size: 16px;
}
```

---

## Testing

All components have comprehensive test coverage using Vitest and React Testing Library.

### Running Tests

```bash
# Run tests once
npm test

# Run tests in watch mode
npm test -- --watch

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

### Test Coverage

- **ChatMessage**: 20+ test cases covering rendering, markdown, accessibility
- **TypingIndicator**: 8+ test cases covering animation and accessibility
- **ChatContainer**: 6+ test cases covering auto-scroll and children rendering

### Example Test

```typescript
import { render, screen } from '@testing-library/react';
import { ChatMessage } from './ChatMessage';

it('renders markdown with syntax highlighting', () => {
  render(
    <ChatMessage
      content="```js\nconst x = 1;\n```"
      isUser={false}
      timestamp={new Date()}
    />
  );
  
  expect(screen.getByText(/const x = 1/)).toBeInTheDocument();
});
```

---

## Performance Considerations

1. **Memoization**: Consider wrapping components with `React.memo()` for large chat histories
2. **Virtualization**: For very long conversations (1000+ messages), implement virtual scrolling
3. **Code Splitting**: Syntax highlighter is loaded asynchronously
4. **CSS Animations**: Use GPU-accelerated properties (transform, opacity)

### Example Optimization

```typescript
import { memo } from 'react';

export const ChatMessage = memo(({ content, isUser, timestamp, agentName }) => {
  // Component implementation
}, (prevProps, nextProps) => {
  // Custom comparison for better performance
  return prevProps.content === nextProps.content &&
         prevProps.timestamp === nextProps.timestamp;
});
```

---

## Dependencies

- **antd**: ^6.2.1 - UI component library
- **@ant-design/icons**: ^6.1.0 - Icon components
- **react-markdown**: ^10.1.0 - Markdown renderer
- **remark-gfm**: ^4.0.1 - GitHub Flavored Markdown support
- **react-syntax-highlighter**: ^16.1.0 - Code syntax highlighting
- **@types/react-syntax-highlighter**: ^15.5.13 - TypeScript types

### Development Dependencies

- **vitest**: ^4.0.18 - Testing framework
- **@testing-library/react**: ^16.3.2 - React testing utilities
- **@testing-library/jest-dom**: ^6.9.1 - Custom Jest matchers
- **jsdom**: ^27.4.0 - DOM implementation for testing

---

## Browser Support

The components are tested and work in:

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

**Note**: Some CSS features (smooth scrolling, custom scrollbars) may have limited support in older browsers.

---

## Future Enhancements

Potential improvements for future iterations:

1. **Message Reactions**: Add emoji reactions to messages
2. **Message Actions**: Edit, delete, copy message functionality
3. **File Attachments**: Support for images, documents
4. **Voice Messages**: Audio message playback
5. **Message Search**: Search through conversation history
6. **Read Receipts**: Show when messages are read
7. **Message Threading**: Support for threaded conversations
8. **Dark Mode**: Theme support for light/dark modes

---

## Troubleshooting

### Common Issues

**Issue**: Markdown not rendering correctly
- **Solution**: Ensure `react-markdown` and `remark-gfm` are installed

**Issue**: Code highlighting not working
- **Solution**: Verify the language identifier in code blocks (e.g., ` ```javascript `)

**Issue**: Auto-scroll not working
- **Solution**: Check that `ChatContainer` has a defined height

**Issue**: Links not opening in new tab
- **Solution**: The component automatically adds `target="_blank"`, ensure you're using markdown link syntax

### Debug Mode

Enable debug logging:

```typescript
// Add to ChatMessage component
useEffect(() => {
  console.log('ChatMessage rendered:', { content, isUser, timestamp });
}, [content, isUser, timestamp]);
```

---

## Support

For issues or questions:
1. Check the test files for usage examples
2. Review the component props interface
3. Consult Ant Design documentation: https://ant.design
4. Review React Markdown docs: https://github.com/remarkjs/react-markdown

---

## Changelog

### Version 1.0.0 (Story 3.2)
- Initial implementation
- User and agent message rendering
- Markdown and syntax highlighting support
- Typing indicator with animation
- Auto-scroll functionality
- Comprehensive test coverage
- Full accessibility support
