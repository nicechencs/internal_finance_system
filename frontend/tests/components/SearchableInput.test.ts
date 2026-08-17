import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import ElementPlus from 'element-plus'
import SearchableInput from '@/components/SearchableInput.vue'

const mockFetchOptions = vi.fn()

const mountComponent = (props: Record<string, any> = {}) => {
  return mount(SearchableInput, {
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

const flushPromises = () => new Promise(r => setTimeout(r, 0))

describe('SearchableInput', () => {
  const mockData = {
    data: {
      data: [
        { name: '选项A' },
        { name: '选项B' },
        { name: '选项C' }
      ]
    }
  }

  beforeEach(() => {
    vi.clearAllMocks()
    mockFetchOptions.mockResolvedValue(mockData)
  })

  it('应该正确渲染 el-select 组件', () => {
    const wrapper = mountComponent()
    expect(wrapper.find('.el-select').exists()).toBe(true)
  })

  it('mounted 时应调用 fetchOptions 加载选项', async () => {
    mountComponent()
    await nextTick()
    expect(mockFetchOptions).toHaveBeenCalledTimes(1)
  })

  it('应该正确加载选项数据', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.options.length).toBe(3)
    expect(vm.options[0].name).toBe('选项A')
  })

  it('fetchOptions 失败时应优雅降级', async () => {
    mockFetchOptions.mockRejectedValue(new Error('网络错误'))
    const wrapper = mountComponent()
    await flushPromises()
    await nextTick()

    expect(wrapper.find('.el-select').exists()).toBe(true)
    const vm = wrapper.vm as any
    expect(vm.options.length).toBe(0)
  })

  it('应该支持 v-model 双向绑定', async () => {
    const wrapper = mountComponent({ modelValue: '选项A' })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('modelValue')).toBe('选项A')

    await select.vm.$emit('update:modelValue', '选项B')
    expect(wrapper.emitted('update:modelValue')![0]).toEqual(['选项B'])
  })

  it('应该传递 clearable 属性', () => {
    const wrapper = mountComponent({ clearable: true })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('clearable')).toBe(true)
  })

  it('应该传递 size 属性', () => {
    const wrapper = mountComponent({ size: 'small' })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('size')).toBe('small')
  })

  it('应该传递 placeholder 属性', () => {
    const wrapper = mountComponent({ placeholder: '请输入或选择客户名称' })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('placeholder')).toBe('请输入或选择客户名称')
  })

  it('应该传递 disabled 属性', () => {
    const wrapper = mountComponent({ disabled: true })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('disabled')).toBe(true)
  })

  it('应该默认占满容器宽度', () => {
    const wrapper = mountComponent()
    const select = wrapper.find('.el-select')

    expect(select.attributes('style')).toContain('width: 100%')
  })

  it('应该支持自定义宽度', () => {
    const wrapper = mountComponent({ width: '320px' })
    const select = wrapper.find('.el-select')

    expect(select.attributes('style')).toContain('width: 320px')
  })

  it('应该默认启用 filterable 和 allow-create', () => {
    const wrapper = mountComponent()
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('filterable')).toBe(true)
    expect(select.props('allowCreate')).toBe(true)
  })

  it('应该支持自定义 labelField', async () => {
    mockFetchOptions.mockResolvedValue({
      data: { data: [{ title: '标题A' }, { title: '标题B' }] }
    })
    const wrapper = mountComponent({ labelField: 'title' })
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.options.length).toBe(2)
    expect(vm.options[0].title).toBe('标题A')
  })

  it('应该支持直接数组格式的响应', async () => {
    mockFetchOptions.mockResolvedValue([
      { name: '直接A' },
      { name: '直接B' }
    ])
    const wrapper = mountComponent()
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.options.length).toBe(2)
    expect(vm.options[0].name).toBe('直接A')
  })
})
