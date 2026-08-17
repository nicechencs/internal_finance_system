import { vi } from 'vitest'

// Mock Element Plus 组件
vi.mock('element-plus', async () => {
  const actual = await vi.importActual('element-plus')
  return {
    ...actual,
    ElMessage: {
      success: vi.fn(),
      error: vi.fn(),
      warning: vi.fn(),
      info: vi.fn(),
    },
    ElMessageBox: {
      confirm: vi.fn(),
      alert: vi.fn(),
      prompt: vi.fn(),
    },
    ElNotification: {
      success: vi.fn(),
      error: vi.fn(),
      warning: vi.fn(),
      info: vi.fn(),
    },
  }
})

vi.mock('@/features/master-data/tags/api/tag', () => ({
  getTags: vi.fn().mockResolvedValue({ data: { data: [] } }),
  getTagById: vi.fn(),
  createTag: vi.fn(),
  updateTag: vi.fn(),
  deleteTag: vi.fn(),
  getTagBindings: vi.fn().mockResolvedValue({ data: { data: [] } }),
  setTagBindings: vi.fn(),
  addTagBinding: vi.fn(),
  removeTagBinding: vi.fn(),
  batchSetTagBindings: vi.fn(),
  getTagSummary: vi.fn().mockResolvedValue({ data: { data: { totalCount: 0, activeCount: 0 } } }),
  getTagCrossAnalysis: vi.fn().mockResolvedValue({ data: { data: { rows: [], columns: [], matrix: [] } } }),
}))

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return []
  }
  unobserve() {}
} as any

// Mock ResizeObserver
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
} as any
