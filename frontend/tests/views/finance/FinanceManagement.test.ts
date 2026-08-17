import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import FinanceManagement from '@/views/finance/FinanceManagement.vue'
import * as receivableApi from '@/api/receivable'
import * as payableApi from '@/api/payable'
import * as projectApi from '@/api/project'
import * as customerApi from '@/api/customer'
import * as supplierApi from '@/api/supplier'

vi.mock('@/api/receivable')
vi.mock('@/api/payable')
vi.mock('@/api/project')
vi.mock('@/api/customer')
vi.mock('@/api/supplier')
vi.mock('vue-echarts', () => ({
  default: { template: '<div class="v-chart-mock"></div>' }
}))
vi.mock('echarts/core', () => ({ use: vi.fn() }))
vi.mock('echarts/renderers', () => ({ CanvasRenderer: {} }))
vi.mock('echarts/charts', () => ({ BarChart: {}, LineChart: {} }))
vi.mock('echarts/components', () => ({
  TitleComponent: {},
  TooltipComponent: {},
  LegendComponent: {},
  GridComponent: {}
}))

const emptySummary = {
  totalReceivable: 0,
  totalReceived: 0,
  totalPayable: 0,
  totalPaid: 0,
  totalRemaining: 0,
  pendingCount: 0,
  partialCount: 0,
  settledCount: 0,
  overdueCount: 0
}

const defaultStubs = {
  ReceivableList: { template: '<div class="receivable-stub">应收列表</div>' },
  PayableList: { template: '<div class="payable-stub">应付列表</div>' }
}

const emptyOptionsResponse = {
  items: [],
  total: 0,
  page: 1,
  pageSize: 1000,
  totalPages: 0
}

describe('FinanceManagement', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(receivableApi.getReceivableSummary).mockResolvedValue(
      mockAxiosResponse({ data: emptySummary })
    )
    vi.mocked(payableApi.getPayableSummary).mockResolvedValue(
      mockAxiosResponse({ data: emptySummary })
    )
    vi.mocked(receivableApi.getReceivableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: [], amounts: [] } })
    )
    vi.mocked(payableApi.getPayableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: [], amounts: [] } })
    )
    vi.mocked(receivableApi.getReceivableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: [], amounts: [] } })
    )
    vi.mocked(payableApi.getPayableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: [], amounts: [] } })
    )
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({ data: emptyOptionsResponse })
    )
    vi.mocked(customerApi.getCustomers).mockResolvedValue(
      mockAxiosResponse({ data: emptyOptionsResponse })
    )
    vi.mocked(supplierApi.getSuppliers).mockResolvedValue(
      mockAxiosResponse({ data: emptyOptionsResponse })
    )
  })

  it('应该正确渲染页面标题', async () => {
    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.find('.page-title').text()).toBe('应收应付管理')
  })

  it('应该同时渲染应收和应付列表组件（使用 v-show 保持状态）', async () => {
    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    // v-show 不会移除元素，两个组件都应存在于 DOM 中
    expect(wrapper.find('.receivable-stub').exists()).toBe(true)
    expect(wrapper.find('.payable-stub').exists()).toBe(true)
  })

  it('默认应该激活应收账款 Tab', async () => {
    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.activeTab).toBe('receivable')
    expect(wrapper.find('.receivable-stub').isVisible()).toBe(true)
  })

  it('应该加载汇总统计数据', async () => {
    mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(receivableApi.getReceivableSummary).toHaveBeenCalled()
    expect(payableApi.getPayableSummary).toHaveBeenCalled()
  })

  it('应该渲染统计卡片', async () => {
    vi.mocked(receivableApi.getReceivableSummary).mockResolvedValue(
      mockAxiosResponse({
        data: { ...emptySummary, totalReceivable: 100000, totalReceived: 30000, totalRemaining: 70000, settledCount: 5 }
      })
    )
    vi.mocked(payableApi.getPayableSummary).mockResolvedValue(
      mockAxiosResponse({
        data: { ...emptySummary, totalPayable: 50000, totalPaid: 20000, totalRemaining: 30000, settledCount: 3 }
      })
    )

    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.text()).toContain('总应收')
    expect(wrapper.text()).toContain('已收款')
    expect(wrapper.text()).toContain('总应付')
    expect(wrapper.text()).toContain('已付款')
  })

  it('接口缺失金额字段时仍应渲染默认值，避免白屏', async () => {
    vi.mocked(receivableApi.getReceivableSummary).mockResolvedValue(
      mockAxiosResponse({
        data: { totalReceivable: 8800, totalRemaining: 1200 }
      })
    )
    vi.mocked(payableApi.getPayableSummary).mockResolvedValue(
      mockAxiosResponse({
        data: { totalPayable: 5600 }
      })
    )

    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.text()).toContain('¥8,800.00')
    expect(wrapper.text()).toContain('¥1,200.00')
    expect(wrapper.text()).toContain('¥0.00')
  })

  it('空数组图表数据时应显示空状态而不是图表', async () => {
    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.text()).toContain('暂无趋势数据')
    expect(wrapper.findAll('.v-chart-mock')).toHaveLength(0)
  })

  it('图表数据全为 0 时也应显示空状态', async () => {
    vi.mocked(receivableApi.getReceivableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [0, 0] } })
    )
    vi.mocked(payableApi.getPayableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [0, 0] } })
    )
    vi.mocked(receivableApi.getReceivableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [0, 0] } })
    )
    vi.mocked(payableApi.getPayableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [0, 0] } })
    )

    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.text()).toContain('暂无趋势数据')
    expect(wrapper.findAll('.v-chart-mock')).toHaveLength(0)
  })

  it('存在非零图表数据时应继续渲染图表', async () => {
    vi.mocked(receivableApi.getReceivableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [1200, 0] } })
    )
    vi.mocked(payableApi.getPayableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [0, 300] } })
    )
    vi.mocked(receivableApi.getReceivableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [500, 0] } })
    )
    vi.mocked(payableApi.getPayableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [0, 200] } })
    )

    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.findAll('.v-chart-mock')).toHaveLength(1)
    expect(wrapper.text()).not.toContain('暂无趋势数据')
  })
})
