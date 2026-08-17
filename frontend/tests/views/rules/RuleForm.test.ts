import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import RuleForm from '@/views/rules/RuleForm.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as ruleApi from '@/api/rule'
import * as categoryApi from '@/api/category'
import type { Rule } from '@/types/rule'
import type { Category } from '@/types/category'

vi.mock('@/api/rule')
vi.mock('@/api/category')

describe('RuleForm.vue', () => {
  const mockCategories: Category[] = [
    { id: 1, name: '工资薪金', categoryType: 'Expense', level: 1, isActive: true, createdAt: '2024-01-01T00:00:00Z', description: '' },
    { id: 2, name: '房租', categoryType: 'Expense', level: 1, isActive: true, createdAt: '2024-01-01T00:00:00Z', description: '' },
  ]

  const mockRule: Rule = {
    id: 1,
    name: '工资规则',
    categoryId: 1,
    categoryName: '工资薪金',
    matchField: 'CounterpartyName',
    matchOperator: 'Contains',
    matchValue: '工资',
    priority: 10,
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z'
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(categoryApi.getActiveCategories).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockCategories
      })
    )
  })

  afterEach(() => {
    // 清理 teleport 到 body 的 dialog 元素
    document.body.innerHTML = ''
  })

  const mountForm = (props: { visible: boolean; rule: Rule | null } = { visible: true, rule: null }) => {
    return mountWithPlugins(RuleForm, { props })
  }

  it('visible 为 true 时应渲染表单标签', async () => {
    const wrapper = mountForm()
    await flushPromises()
    await nextTick()

    const html = wrapper.html()
    expect(html).toContain('规则名称')
    expect(html).toContain('匹配字段')
    expect(html).toContain('匹配操作符')
    expect(html).toContain('匹配值')
    expect(html).toContain('优先级')
  })

  it('新增模式下标题应为"新增规则"', async () => {
    const wrapper = mountForm({ visible: true, rule: null })
    await flushPromises()
    await nextTick()

    expect(wrapper.html()).toContain('新增规则')
  })

  it('编辑模式下标题应为"编辑规则"', async () => {
    const wrapper = mountForm({ visible: true, rule: mockRule })
    await flushPromises()
    await nextTick()

    expect(wrapper.html()).toContain('编辑规则')
  })

  it('visible 为 true 时应加载分类列表', async () => {
    // 先挂载为 false，然后改为 true 触发 watch
    const wrapper = mountForm({ visible: false, rule: null })
    await nextTick()

    await wrapper.setProps({ visible: true })
    await nextTick()
    await flushPromises()

    expect(categoryApi.getActiveCategories).toHaveBeenCalled()
  })

  it('编辑模式下应回填规则数据到 formData', async () => {
    const wrapper = mountForm({ visible: false, rule: mockRule })
    await nextTick()

    await wrapper.setProps({ visible: true })
    await nextTick()
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('工资规则')
    expect(vm.formData.categoryId).toBe(1)
    expect(vm.formData.matchField).toBe('CounterpartyName')
    expect(vm.formData.matchOperator).toBe('Contains')
    expect(vm.formData.matchValue).toBe('工资')
    expect(vm.formData.priority).toBe(10)
    expect(vm.formData.isActive).toBe(true)
  })

  it('新增模式下 formData 应为默认值', async () => {
    const wrapper = mountForm({ visible: true, rule: null })
    await nextTick()
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('')
    expect(vm.formData.categoryId).toBe(0)
    expect(vm.formData.matchField).toBe('CounterpartyName')
    expect(vm.formData.matchOperator).toBe('Contains')
    expect(vm.formData.matchValue).toBe('')
    expect(vm.formData.priority).toBe(0)
  })

  it('handleClose 应触发 update:visible 事件', async () => {
    const wrapper = mountForm()
    await nextTick()
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleClose()
    await nextTick()

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')![0]).toEqual([false])
  })

  it('编辑模式下应显示状态开关', async () => {
    const wrapper = mountForm({ visible: true, rule: mockRule })
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.isEdit).toBe(true)
    expect(wrapper.html()).toContain('状态')
  })
})
