import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useScrollManagement } from './useScrollManagement';

describe('useScrollManagement', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize at bottom', () => {
    const { result } = renderHook(() => useScrollManagement());
    expect(result.current.isAtBottom).toBe(true);
    expect(result.current.showNewMessageBadge).toBe(false);
  });

  it('should check if at bottom within threshold', () => {
    const { result } = renderHook(() => useScrollManagement({ autoScrollThreshold: 100 }));

    const mockContainer = {
      scrollTop: 450,
      scrollHeight: 1000,
      clientHeight: 500,
      scrollTo: vi.fn(),
    } as any;

    // @ts-ignore - Override ref for testing
    result.current.scrollContainerRef.current = mockContainer;

    let atBottom;
    act(() => {
      atBottom = result.current.checkIfAtBottom();
    });

    // Distance from bottom = 1000 - 450 - 500 = 50, which is <= 100 threshold
    expect(atBottom).toBe(true);
  });

  it('should detect when not at bottom', () => {
    const { result } = renderHook(() => useScrollManagement({ autoScrollThreshold: 100 }));

    const mockContainer = {
      scrollTop: 0,
      scrollHeight: 1000,
      clientHeight: 500,
    } as any;

    // @ts-ignore - Override ref for testing
    result.current.scrollContainerRef.current = mockContainer;

    let atBottom;
    act(() => {
      atBottom = result.current.checkIfAtBottom();
    });

    // Distance from bottom = 1000 - 0 - 500 = 500, which is > 100 threshold
    expect(atBottom).toBe(false);
  });

  it('should handle scroll events', () => {
    const onScrollToTop = vi.fn();
    const { result } = renderHook(() => useScrollManagement({ onScrollToTop }));

    const mockContainer = {
      scrollTop: 0,
      scrollHeight: 1000,
      clientHeight: 500,
    } as any;

    // @ts-ignore - Override ref for testing
    result.current.scrollContainerRef.current = mockContainer;

    act(() => {
      result.current.handleScroll();
    });

    expect(onScrollToTop).toHaveBeenCalled();
  });
});
