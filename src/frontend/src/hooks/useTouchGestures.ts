import { useEffect, useRef, useCallback } from 'react';

export interface TouchGestureOptions {
  onLongPress?: (element: HTMLElement) => void;
  onSwipeDown?: () => void;
  longPressDuration?: number;
  swipeThreshold?: number;
}

export function useTouchGestures(options?: TouchGestureOptions) {
  const {
    onLongPress,
    onSwipeDown,
    longPressDuration = 500,
    swipeThreshold = 50,
  } = options || {};

  const touchStartRef = useRef<{ x: number; y: number; time: number } | null>(null);
  const longPressTimerRef = useRef<NodeJS.Timeout | null>(null);
  const longPressTargetRef = useRef<HTMLElement | null>(null);

  const handleTouchStart = useCallback(
    (e: TouchEvent) => {
      const touch = e.touches[0];
      touchStartRef.current = {
        x: touch.clientX,
        y: touch.clientY,
        time: Date.now(),
      };

      // Start long press timer
      if (onLongPress && e.target instanceof HTMLElement) {
        longPressTargetRef.current = e.target;
        longPressTimerRef.current = setTimeout(() => {
          if (longPressTargetRef.current) {
            longPressTargetRef.current.classList.add('long-press-active');
            onLongPress(longPressTargetRef.current);
          }
        }, longPressDuration);
      }
    },
    [onLongPress, longPressDuration]
  );

  const handleTouchMove = useCallback(() => {
    // Cancel long press if user moves finger
    if (longPressTimerRef.current) {
      clearTimeout(longPressTimerRef.current);
      longPressTimerRef.current = null;
    }
    if (longPressTargetRef.current) {
      longPressTargetRef.current.classList.remove('long-press-active');
      longPressTargetRef.current = null;
    }
  }, []);

  const handleTouchEnd = useCallback(
    (e: TouchEvent) => {
      // Clear long press timer
      if (longPressTimerRef.current) {
        clearTimeout(longPressTimerRef.current);
        longPressTimerRef.current = null;
      }
      if (longPressTargetRef.current) {
        longPressTargetRef.current.classList.remove('long-press-active');
        longPressTargetRef.current = null;
      }

      // Check for swipe down gesture
      if (touchStartRef.current && onSwipeDown) {
        const touch = e.changedTouches[0];
        const deltaY = touch.clientY - touchStartRef.current.y;
        const deltaX = Math.abs(touch.clientX - touchStartRef.current.x);
        const duration = Date.now() - touchStartRef.current.time;

        // Swipe down: significant vertical movement, minimal horizontal, quick gesture
        if (
          deltaY > swipeThreshold &&
          deltaX < swipeThreshold &&
          duration < 300 &&
          touchStartRef.current.y < 100 // Near top of screen
        ) {
          onSwipeDown();
        }
      }

      touchStartRef.current = null;
    },
    [onSwipeDown, swipeThreshold]
  );

  const attachGestureListeners = useCallback(
    (element: HTMLElement) => {
      element.addEventListener('touchstart', handleTouchStart, { passive: false });
      element.addEventListener('touchmove', handleTouchMove, { passive: false });
      element.addEventListener('touchend', handleTouchEnd, { passive: false });

      return () => {
        element.removeEventListener('touchstart', handleTouchStart);
        element.removeEventListener('touchmove', handleTouchMove);
        element.removeEventListener('touchend', handleTouchEnd);
      };
    },
    [handleTouchStart, handleTouchMove, handleTouchEnd]
  );

  const copyToClipboard = useCallback(async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch (error) {
      console.error('Failed to copy to clipboard:', error);
      return false;
    }
  }, []);

  return {
    attachGestureListeners,
    copyToClipboard,
  };
}
