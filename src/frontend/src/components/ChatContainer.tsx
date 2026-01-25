import React, { useEffect, useRef } from 'react';
import './ChatContainer.css';

export interface ChatContainerProps {
  children: React.ReactNode;
  autoScroll?: boolean;
}

export const ChatContainer: React.FC<ChatContainerProps> = ({
  children,
  autoScroll = true,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const shouldAutoScrollRef = useRef(autoScroll);

  useEffect(() => {
    shouldAutoScrollRef.current = autoScroll;
  }, [autoScroll]);

  useEffect(() => {
    if (shouldAutoScrollRef.current && containerRef.current) {
      const container = containerRef.current;
      
      // Use smooth scrolling
      container.scrollTo({
        top: container.scrollHeight,
        behavior: 'smooth',
      });
    }
  }, [children]);

  return (
    <div
      ref={containerRef}
      className="chat-container"
      role="log"
      aria-live="polite"
      aria-label="Chat messages"
    >
      {children}
    </div>
  );
};
