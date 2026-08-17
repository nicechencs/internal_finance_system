import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import ElementPlus from 'element-plus'
import SearchableFilterInput from '@/components/SearchableFilterInput.vue'

const mockFetchOptions = vi.fn()

const mountComponent = (props: Record<string, any> = {}) => {
  return mount(SearchableFilterInput, {
    props: {
      modelValue: '',
      fetchOptions: mockFetchOptions,
      ...props
    },
    global: {
      plugins: [ElementPlus]
    }
  })
}

describe('SearchableFilterInput', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockFetchOptions.mockResolvedValue({ data: { data: [] } })
  })

  it('应该为筛选场景提供统一默认宽度', async () => {
    const wrapper = mountComponent()
    await nextTick()

    expect(wrapper.find('.el-select').attributes('style')).toContain('width: 280px')
  })

  it('应该支持覆盖默认宽度', async () => {
    const wrapper = mountComponent({ width: '320px' })
    await nextTick()

    expect(wrapper.find('.el-select').attributes('style')).toContain('width: 320px')
  })
})
