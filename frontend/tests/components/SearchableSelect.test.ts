import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ElementPlus from 'element-plus'
import SearchableSelect from '@/components/SearchableSelect.vue'

const mountComponent = (props: Record<string, any> = {}) => {
  return mount(SearchableSelect, {
    props: {
      modelValue: null,
      options: [],
      ...props
    },
    global: {
      plugins: [ElementPlus]
    }
  })
}

describe('SearchableSelect', () => {
  const mockOptions = [
    { id: 1, name: '项目A' },
    { id: 2, name: '项目B' },
    { id: 3, name: '测试项目C' }
  ]

  it('应该正确渲染基础组件', () => {
    const wrapper = mountComponent({ options: mockOptions })
    expect(wrapper.find('.el-select').exists()).toBe(true)
  })

  it('应该根据 entityName 自动生成 placeholder', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      entityName: '项目'
    })
    expect(wrapper.html()).toContain('输入关键字搜索项目')
  })

  it('应该支持自定义 placeholder 覆盖自动生成', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      entityName: '项目',
      placeholder: '自定义占位文本'
    })
    expect(wrapper.html()).toContain('自定义占位文本')
  })

  it('没有 entityName 和 placeholder 时应显示默认文本', () => {
    const wrapper = mountComponent({ options: mockOptions })
    expect(wrapper.html()).toContain('请选择')
  })

  it('应该将所有选项传递给 ElSelect', () => {
    const wrapper = mountComponent({ options: mockOptions })
    const elOptions = wrapper.findAllComponents({ name: 'ElOption' })
    expect(elOptions.length).toBe(3)
  })

  it('应该支持 clearable 属性（默认为 true）', () => {
    const wrapper = mountComponent({ options: mockOptions })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('clearable')).toBe(true)
  })

  it('应该支持禁用 clearable', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      clearable: false
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('clearable')).toBe(false)
  })

  it('应该默认启用 filterable', () => {
    const wrapper = mountComponent({ options: mockOptions })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('filterable')).toBe(true)
  })

  it('应该支持 disabled 属性', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      disabled: true
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('disabled')).toBe(true)
  })

  it('应该在选择时触发 update:modelValue 和 change 事件', async () => {
    const wrapper = mountComponent({ options: mockOptions })
    const select = wrapper.findComponent({ name: 'ElSelect' })

    await select.vm.$emit('update:modelValue', 1)

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')![0]).toEqual([1])
    expect(wrapper.emitted('change')).toBeTruthy()
    expect(wrapper.emitted('change')![0]).toEqual([1])
  })

  it('应该支持自定义 labelField 和 valueField', () => {
    const customOptions = [
      { code: 'A', title: '选项A' },
      { code: 'B', title: '选项B' }
    ]
    const wrapper = mountComponent({
      options: customOptions,
      labelField: 'title',
      valueField: 'code'
    })
    const elOptions = wrapper.findAllComponents({ name: 'ElOption' })
    expect(elOptions.length).toBe(2)
    expect(elOptions[0].props('label')).toBe('选项A')
    expect(elOptions[0].props('value')).toBe('A')
  })

  it('应该支持自定义宽度', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      width: '220px'
    })
    const select = wrapper.find('.el-select')
    expect(select.attributes('style')).toContain('220px')
  })

  it('应该根据 entityName 生成 noMatchText', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      entityName: '客户'
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('noMatchText')).toBe('无匹配客户')
  })

  it('应该根据 entityName 生成 noDataText', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      entityName: '供应商'
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('noDataText')).toBe('暂无供应商数据')
  })

  it('应该支持自定义 noMatchText 覆盖自动生成', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      entityName: '项目',
      noMatchText: '找不到对应项目'
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('noMatchText')).toBe('找不到对应项目')
  })

  it('应该支持自定义 noDataText 覆盖自动生成', () => {
    const wrapper = mountComponent({
      options: mockOptions,
      entityName: '项目',
      noDataText: '没有项目可选'
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('noDataText')).toBe('没有项目可选')
  })

  it('应该支持 loading 状态', () => {
    const wrapper = mountComponent({
      options: [],
      loading: true
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('loading')).toBe(true)
  })

  it('选择值后应正确绑定 modelValue', async () => {
    const wrapper = mountComponent({
      modelValue: 2,
      options: mockOptions
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })
    expect(select.props('modelValue')).toBe(2)
  })

  it('清除值时应触发 null', async () => {
    const wrapper = mountComponent({
      modelValue: 1,
      options: mockOptions
    })
    const select = wrapper.findComponent({ name: 'ElSelect' })

    await select.vm.$emit('update:modelValue', null)

    expect(wrapper.emitted('update:modelValue')![0]).toEqual([null])
    expect(wrapper.emitted('change')![0]).toEqual([null])
  })
})
