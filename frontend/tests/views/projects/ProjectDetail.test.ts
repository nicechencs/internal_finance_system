import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mountWithPlugins, mockAxiosResponse } from '@tests/utils'
import ProjectDetail from '@/views/projects/ProjectDetail.vue'
import * as projectApi from '@/api/project'
import * as transactionApi from '@/api/transaction'

vi.mock('@/api/project')
vi.mock('@/api/transaction')

const mockRoute = { params: { id: '1' } }
vi.mock('vue-router', async () => {
  const actual = await vi.importActual('vue-router')
  return {
    ...actual,
    useRoute: () => mockRoute,
    useRouter: () => ({ back: vi.fn() })
  }
})

describe('ProjectDetail.vue', () => {
  const emptyProfitAnalysis = {
    monthlyData: [],
    expenseCategories: [],
    totalIncome: 0,
    totalExpense: 0,
    totalProfit: 0
  }

  const mockProject = {
    id: 1,
    projectCode: 'PRJ001',
    name: '测试项目',
    customerId: 1,
    customerName: '客户A',
    contractAmount: 100000,
    receivedAmount: 60000,
    receivableAmount: 40000,
    totalCost: 30000,
    profitAmount: 30000,
    profitRate: 30.0,
    status: 'Active',
    startDate: '2026-01-01',
    endDate: '2026-12-31',
    description: '项目描述内容',
    createdAt: '2026-01-01T00:00:00',
    updatedAt: '2026-01-15T00:00:00'
  }

  const mockTransactions = [
    {
      id: 1,
      transactionDate: '2026-02-01',
      transactionType: 'Income',
      amount: 50000,
      accountName: '主账户',
      categoryName: '项目收入',
      description: '首期款',
      isAllocated: false,
      tags: [
        { tagId: 11, tagName: '回款', tagColor: '#67C23A' }
      ],
      allocations: []
    },
    {
      id: 2,
      transactionDate: '2026-03-01',
      transactionType: 'Expense',
      amount: 15000,
      accountName: '主账户',
      categoryName: '人工成本',
      description: '开发费用',
      tags: [
        { tagId: 12, tagName: '人力', tagColor: '#E6A23C' }
      ],
      isAllocated: true,
      allocations: [
        { id: 1, personName: '张三', amount: 10000, allocationRate: 66.7, description: '主开发' },
        { id: 2, personName: '李四', amount: 5000, allocationRate: 33.3, description: '辅助开发' }
      ]
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(projectApi.getProjectById).mockResolvedValue(
      mockAxiosResponse({ data: mockProject })
    )
    vi.mocked(projectApi.getProjectProfitAnalysis).mockResolvedValue(
      mockAxiosResponse({ data: emptyProfitAnalysis })
    )
    vi.mocked(transactionApi.getTransactionsByProject).mockResolvedValue(
      mockAxiosResponse({ data: mockTransactions })
    )
  })

  it('应该加载并显示项目详情', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    expect(projectApi.getProjectById).toHaveBeenCalledWith(1)
    expect(wrapper.text()).toContain('测试项目')
    expect(wrapper.text()).toContain('PRJ001')
    expect(wrapper.text()).toContain('客户A')
  })

  it('应该显示财务摘要卡片', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('合同金额')
    expect(text).toContain('¥100,000.00')
    expect(text).toContain('已收款')
    expect(text).toContain('¥60,000.00')
    expect(text).toContain('总成本')
    expect(text).toContain('¥30,000.00')
    expect(text).toContain('利润')
    expect(text).toContain('¥30,000.00')
    expect(text).toContain('30.0%')
  })

  it('应该显示项目状态标签', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('进行中')
  })

  it('应该加载并显示交易记录', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    expect(transactionApi.getTransactionsByProject).toHaveBeenCalledWith(1)

    const text = wrapper.text()
    expect(text).toContain('首期款')
    expect(text).toContain('开发费用')
    expect(text).toContain('主账户')
  })

  it('应该显示分摊记录tab', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('交易记录')
    expect(wrapper.text()).toContain('分摊记录')
  })

  it('应该正确筛选有分摊的交易', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    const vm = wrapper.vm as any
    // allocatedTransactions 应只包含 isAllocated=true 的交易
    expect(vm.allocatedTransactions).toHaveLength(1)
    expect(vm.allocatedTransactions[0].id).toBe(2)
  })

  it('应该显示分摊明细信息', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    // 切换到分摊记录 tab
    const vm = wrapper.vm as any
    vm.activeTab = 'allocations'
    await wrapper.vm.$nextTick()
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('张三')
    expect(text).toContain('李四')
  })

  it('应该正确格式化日期和金额', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.formatCurrency(12345.6)).toBe('¥12,345.60')
    expect(vm.getStatusText('Active')).toBe('进行中')
    expect(vm.getStatusText('Completed')).toBe('已完成')
    expect(vm.getStatusText('Cancelled')).toBe('已取消')
  })

  it('应该处理加载失败的情况', async () => {
    vi.mocked(projectApi.getProjectById).mockRejectedValue(new Error('网络错误'))

    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.project).toBeNull()
  })

  it('应该包含返回按钮', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('返回')
  })

  it('should display transaction tags in the record table', async () => {
    const wrapper = mountWithPlugins(ProjectDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('回款')
    expect(text).toContain('人力')
  })
})
