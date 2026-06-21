import "@testing-library/jest-dom/vitest";

// recharts' ResponsiveContainer relies on ResizeObserver, which jsdom does not implement.
class ResizeObserverStub {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

globalThis.ResizeObserver ??= ResizeObserverStub as unknown as typeof ResizeObserver;
