import { useState, useCallback, useRef } from 'react';

export interface ScrollPosition {
  scrollTop: number;
  scrollHeight: number;
  clientHeight: number;
}

export interface UseScrollManagementOptions {
  autoScrollThreshold?: number; // px from bottom to consider "at bottom"
  onScrollToTop?: () => void;
  onNewMessageWhileScrolledUp?: () => void;
}

export function useScrollManagement(options?: UseScrollManagementOptions) {
  const {
    autoScrollThreshold = 100,
    onScrollToTop,
    onNewMessageWhileScrolledUp,
  } = options || {};

  const [isAtBottom, setIsAtBottom] = useState(true);
  const [showNewMessageBadge, setShowNewMessageBadge] = useState(false);
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const lastScrollPosition = useRef<ScrollPosition | null>(null);

  const checkIfAtBottom = useCallback(() => {
    const container = scrollContainerRef.current;
    if (!container) return false;

    const { scrollTop, scrollHeight, clientHeight } = container;
    const distanceFromBottom = scrollHeight - scrollTop - clientHeight;
    const atBottom = distanceFromBottom <= autoScrollThreshold;

    setIsAtBottom(atBottom);
    return atBottom;
  }, [autoScrollThreshold]);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = 'smooth') => {
    const container = scrollContainerRef.current;
    if (!container) return;

    container.scrollTo({
      top: container.scrollHeight,
      behavior,
    });

    setIsAtBottom(true);
    setShowNewMessageBadge(false);
  }, []);

  const handleScroll = useCallback(() => {
    const container = scrollContainerRef.current;
    if (!container) return;

    const { scrollTop, scrollHeight, clientHeight } = container;
    lastScrollPosition.current = { scrollTop, scrollHeight, clientHeight };

    const atBottom = checkIfAtBottom();

    // Check if scrolled to top (for load more) - only trigger if not already loading
    if (scrollTop === 0 && onScrollToTop) {
      onScrollToTop();
    }

    // Hide badge when scrolling to bottom
    if (atBottom) {
      setShowNewMessageBadge(false);
    }
  }, [checkIfAtBottom, onScrollToTop]);

  const onNewMessage = useCallback(() => {
    const atBottom = checkIfAtBottom();

    if (atBottom) {
      // Auto-scroll to new message
      scrollToBottom('smooth');
    } else {
      // Show "new message" badge
      setShowNewMessageBadge(true);
      onNewMessageWhileScrolledUp?.();
    }
  }, [checkIfAtBottom, scrollToBottom, onNewMessageWhileScrolledUp]);

  const restoreScrollPosition = useCallback((position: ScrollPosition) => {
    const container = scrollContainerRef.current;
    if (!container) return;

    // Restore scroll position after content changes (e.g., loading more messages)
    requestAnimationFrame(() => {
      const newScrollHeight = container.scrollHeight;
      const heightDifference = newScrollHeight - position.scrollHeight;
      container.scrollTop = position.scrollTop + heightDifference;
    });
  }, []);

  const saveScrollPosition = useCallback((): ScrollPosition | null => {
    const container = scrollContainerRef.current;
    if (!container) return null;

    return {
      scrollTop: container.scrollTop,
      scrollHeight: container.scrollHeight,
      clientHeight: container.clientHeight,
    };
  }, []);

  // Dismiss badge when clicking it
  const dismissNewMessageBadge = useCallback(() => {
    setShowNewMessageBadge(false);
    scrollToBottom('smooth');
  }, [scrollToBottom]);

  return {
    scrollContainerRef,
    isAtBottom,
    showNewMessageBadge,
    scrollToBottom,
    handleScroll,
    onNewMessage,
    restoreScrollPosition,
    saveScrollPosition,
    dismissNewMessageBadge,
    checkIfAtBottom,
  };
}
