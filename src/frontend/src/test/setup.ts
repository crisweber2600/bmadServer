import '@testing-library/jest-dom';
import { vi } from 'vitest';

// Mock window.matchMedia for Ant Design components
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => {},
  }),
});

// Mock scrollTo for ChatContainer
Element.prototype.scrollTo = function() {};

// Mock ResizeObserver for Ant Design components
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

const mockRect = {
  top: 0,
  left: 0,
  bottom: 100,
  right: 100,
  width: 100,
  height: 100,
  x: 0,
  y: 0,
  toJSON: () => mockRect,
};

Element.prototype.getBoundingClientRect = vi.fn().mockReturnValue(mockRect);

const originalGetComputedStyle = window.getComputedStyle;
window.getComputedStyle = (element: Element, pseudoElt?: string | null) => {
  try {
    return originalGetComputedStyle(element, pseudoElt);
  } catch {
    return {
      getPropertyValue: () => '',
      visibility: 'visible',
      display: 'block',
    } as unknown as CSSStyleDeclaration;
  }
};

vi.mock('antd/lib/_util/wave', () => ({
  default: () => null,
}));
