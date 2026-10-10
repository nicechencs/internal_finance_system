import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReportsPage from '@/features/reporting/pages/ReportsPage.vue'
import * as reportApi from '@/features/reporting/api/report'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'

vi.mock('@/features/reporting/api/report', () => ({
  getMonthlyProfitReport: vi.fn(),
  getCashflowReport: vi.fn(),
  getProjectProfitReport: vi.fn(),
  getPersonCostReport: vi.fn(),
  getSupplierExpenseReport: vi.fn(),
  getAnnualOverviewReport: vi.fn()
}))

const monthlyReport = {
  year: 2026,
  month: 3,
  totalIncome: 150000,
  totalExpense: 80000,
  netProfit: 70000,
  profitRate: 46.67,
  incomeByCategory: [{ categoryName: '项目收入', amount: 150000 }],
  expenseByCategory: [{ categoryName: '人员工资', amount: 80000 }]
}

describe('ReportsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(reportApi.getMonthlyProfitReport).mockResolvedValue(
      mockAxiosResponse({ data: monthlyReport })
    )
  })

  it('应渲染月度利润数据并按所选年月请求', async () => {
    const wrapper = mountWithPlugins(ReportsPage)
    await flushPromises()

    expect(reportApi.getMonthlyProfitReport).toHaveBeenCalled()
    const [year, month] = vi.mocked(reportApi.getMonthlyProfitReport).mock.calls[0]
    expect(year).toBeGreaterThan(2000)
    expect(month).toBeGreaterThanOrEqual(1)
    expect(month).toBeLessThanOrEqual(12)

    expect(wrapper.text()).toContain('经营报表')
    expect(wrapper.text()).toContain('项目收入')
    expect(wrapper.text()).toContain('人员工资')
    expect(wrapper.text()).toContain('150,000.00')
    expect(wrapper.text()).toContain('80,000.00')
    expect(wrapper.text()).toContain('70,000.00')
    expect(wrapper.text()).toContain('46.67%')
  })

  it('分类为空时应显示空状态文案', async () => {
    vi.mocked(reportApi.getMonthlyProfitReport).mockResolvedValue(
      mockAxiosResponse({
        data: {
          ...monthlyReport,
          totalIncome: 0,
          totalExpense: 0,
          netProfit: 0,
          profitRate: 0,
          incomeByCategory: [],
          expenseByCategory: []
        }
      })
    )

    const wrapper = mountWithPlugins(ReportsPage)
    await flushPromises()

    expect(wrapper.text()).toContain('本月暂无收入分类')
    expect(wrapper.text()).toContain('本月暂无支出分类')
    expect(wrapper.text()).toContain('0.00%')
  })

  it('接口失败时应显示错误且不渲染金额表', async () => {
    vi.mocked(reportApi.getMonthlyProfitReport).mockRejectedValue(new Error('Network error'))

    const wrapper = mountWithPlugins(ReportsPage)
    await flushPromises()

    expect(wrapper.text()).toContain('加载月度利润失败')
    expect(wrapper.text()).not.toContain('项目收入')
  })
})
