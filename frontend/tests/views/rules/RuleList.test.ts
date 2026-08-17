import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import RuleList from '@/views/rules/RuleList.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as ruleApi from '@/api/rule'
import type { Rule } from '@/types/rule'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/rule')
vi.mock('@/features/reconciliation/components/RuleForm.vue', () => ({
  default: {
    name: 'RuleForm',
    template: '<div class="mock-rule-form"></div>',
    props: ['visible', 'rule']
  }
}))

describe('RuleList.vue', () => {
  const mountAsAdmin = () => {
    const wrapper = mountWithPlugins(RuleList)
    const userStore = useUserStore()
    userStore.setUser({
      id: 1,
      username: 'admin',
      email: 'admin@example.com',
      fullName: 'Admin',
      role: 'Admin',
      isActive: true
    })
    return wrapper
  }

  const mockRules: Rule[] = [
    {
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
    },
    {
      id: 2,
      name: '房租规则',
      categoryId: 2,
      categoryName: '房租',
      matchField: 'Description',
      matchOperator: 'Contains',
      matchValue: '租金',
      priority: 5,
      isActive: false,
      createdAt: '2024-01-02T00:00:00Z'
    },
    {
      id: 3,
      name: '金额规则',
      categoryId: 3,
      categoryName: '其他',
      matchField: 'Amount',
      matchOperator: 'Equals',
      matchValue: '1000',
      priority: 1,
      isActive: true,
      createdAt: '2024-01-03T00:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(ruleApi.getRules).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          items: mockRules,
          total: 3,
          page: 1,
          pageSize: 20,
          totalPages: 1
        }
      })
    )
  })

  it('挂���时应调用 getRules 加载数据', async () => {
    mountWithPlugins(RuleList)
    await flushPromises()

    expect(ruleApi.getRules).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20
    })
  })

  it('应正确渲染规则列表数据', async () => {
    const wrapper = mountAsAdmin()
    await flushPromises()
    await nextTick()

    const html = wrapper.html()
    expect(html).toContain('工资规则')
    expect(html).toContain('房租规则')
    expect(html).toContain('金额规则')
  })

  it('应正确显示规则启用/禁用状态标签', async () => {
    const wrapper = mountAsAdmin()
    await flushPromises()
    await nextTick()

    const html = wrapper.html()
    expect(html).toContain('启用')
    expect(html).toContain('禁用')
  })

  it('应正确转换匹配字段和操作符的显示标签', async () => {
    const wrapper = mountAsAdmin()
    await flushPromises()
    await nextTick()

    const html = wrapper.html()
    expect(html).toContain('对方名称')
    expect(html).toContain('交易描述')
    expect(html).toContain('金额')
    expect(html).toContain('包含')
    expect(html).toContain('等于')
  })

  it('点击新增按钮应打开表单对话框', async () => {
    const wrapper = mountAsAdmin()
    await flushPromises()
    await nextTick()

    ;(wrapper.vm as any).handleAdd()
    await nextTick()

    expect(wrapper.vm.dialogVisible).toBe(true)
    expect(wrapper.vm.currentRule).toBeNull()
  })

  it('点击编辑按钮应打开表单并传入当前规则', async () => {
    const wrapper = mountAsAdmin()
    await flushPromises()
    await nextTick()

    ;(wrapper.vm as any).handleEdit(mockRules[0])
    await nextTick()

    expect(wrapper.vm.dialogVisible).toBe(true)
    expect(wrapper.vm.currentRule).toEqual(mockRules[0])
  })

  it('点击删除按钮应弹出确认框并调用删除接口', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')
    vi.mocked(ruleApi.deleteRule).mockResolvedValue(mockAxiosResponse({ code: 200, message: 'success' }))

    const wrapper = mountAsAdmin()
    await flushPromises()
    await nextTick()

    await (wrapper.vm as any).handleDelete(mockRules[0])
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalled()
    expect(ruleApi.deleteRule).toHaveBeenCalledWith(mockRules[0].id)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('加载数据失败时不应崩溃', async () => {
    vi.mocked(ruleApi.getRules).mockRejectedValue(new Error('Network Error'))

    const wrapper = mountWithPlugins(RuleList)
    await flushPromises()
    await nextTick()

    expect(wrapper.html()).toContain('分类规则')
  })
})
