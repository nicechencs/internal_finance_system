import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import Dashboard from '@/views/Dashboard.vue'
import * as dashboardApi from '@/api/dashboard'

// Mock API 模块
vi.mock('@/api/dashboard', () => ({
  getDashboardSummary: vi.fn(),
  getMonthlyStats: vi.fn(),
  getExpenseByCategory: vi.fn(),
  getIncomeByCategory: vi.fn(),
  getRecentTransactions: vi.fn(),
}))

// Mock vue-echarts
vi.mock('vue-echarts', () => ({
  default: {
    name: 'VChart',
    template: '<div class="mock-chart"></div>',
    props: ['option', 'autoresize'],
  },
}))

vi.mock('@/components/MaturityAlert.vue', () => ({
  default: {
    name: 'MaturityAlert',
    template: '<div class="maturity-alert-stub"></div>',
  },
}))

describe('Dashboard.vue', () => {
  const mockSummaryData = {
    totalIncome: 150000.00,
    totalExpense: 80000.00,
    netProfit: 70000.00,
    totalBalance: 250000.00,
    accountCount: 5,
    transactionCount: 120,
    projectCount: 8,
  }

  const mockMonthlyData = [
    { month: '2024-01', income: 50000, expense: 30000, net: 20000 },
    { month: '2024-02', income: 60000, expense: 35000, net: 25000 },
    { month: '2024-03', income: 40000, expense: 15000, net: 25000 },
  ]

  const mockExpenseByCategory = [
    { categoryName: '办公费用', amount: 20000 },
    { categoryName: '人员工资', amount: 40000 },
    { categoryName: '市场推广', amount: 20000 },
  ]

  const mockIncomeByCategory = [
    { categoryName: '项目收入', amount: 100000 },
    { categoryName: '服务费', amount: 50000 },
  ]

  const mockRecentTransactions = [
    {
      id: 1,
      transactionDate: '2024-03-14',
      transactionType: 'Income',
      amount: 5000.00,
      accountName: '工商银行',
      categoryName: '项目收入',
      counterpartyName: '客户A',
      description: '项目款',
    },
    {
      id: 2,
      transactionDate: '2024-03-13',
      transactionType: 'Expense',
      amount: 1200.00,
      accountName: '招商银行',
      categoryName: '办公费用',
      counterpartyName: '供应商B',
      description: '采购办公用品',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()

    // 设置默认 mock 返回值
    vi.mocked(dashboardApi.getDashboardSummary).mockResolvedValue(
      mockAxiosResponse({ data: mockSummaryData })
    )
    vi.mocked(dashboardApi.getMonthlyStats).mockResolvedValue(
      mockAxiosResponse({ data: mockMonthlyData })
    )
    vi.mocked(dashboardApi.getExpenseByCategory).mockResolvedValue(
      mockAxiosResponse({ data: mockExpenseByCategory })
    )
    vi.mocked(dashboardApi.getIncomeByCategory).mockResolvedValue(
      mockAxiosResponse({ data: mockIncomeByCategory })
    )
    vi.mocked(dashboardApi.getRecentTransactions).mockResolvedValue(
      mockAxiosResponse({ data: mockRecentTransactions })
    )
  })

  it('应该正确渲染页面标题和问候语', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    expect(wrapper.find('.page-title').text()).toBe('工作台')
    expect(wrapper.find('.welcome-text').exists()).toBe(true)
    expect(wrapper.find('.welcome-text').text()).toMatch(/好|夜深了/)
  })

  it('应该加载并显示统计卡片数据', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    // 验证 API 调用
    expect(dashboardApi.getDashboardSummary).toHaveBeenCalledOnce()
    expect(dashboardApi.getMonthlyStats).toHaveBeenCalledWith(12)
    expect(dashboardApi.getExpenseByCategory).toHaveBeenCalledOnce()
    expect(dashboardApi.getIncomeByCategory).toHaveBeenCalledOnce()
    expect(dashboardApi.getRecentTransactions).toHaveBeenCalledWith(10)

    // 验证统计卡片渲染
    const cards = wrapper.findAll('.stat-cards .stat-card')
    expect(cards).toHaveLength(4)

    // 验证金额格式化显示
    expect(wrapper.html()).toContain('150,000.00') // 总收入
    expect(wrapper.html()).toContain('80,000.00')  // 总支出
    expect(wrapper.html()).toContain('70,000.00')  // 净利润
    expect(wrapper.html()).toContain('250,000.00') // 总余额
  })

  it('应该渲染图表容器', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    // 验证图表组件存在
    const charts = wrapper.findAll('.mock-chart')
    expect(charts.length).toBeGreaterThanOrEqual(3) // 月度趋势图 + 2个饼图

    // 验证图表卡片标题
    expect(wrapper.html()).toContain('月度收支趋势')
    expect(wrapper.html()).toContain('支出分类占比')
    expect(wrapper.html()).toContain('收入分类占比')
  })

  it('应该显示次要统计指标', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    const secondaryCards = wrapper.findAll('.secondary-stat-cards .stat-card')
    expect(secondaryCards).toHaveLength(3)

    // 验证统计数值
    expect(wrapper.html()).toContain('5')   // 账户数量
    expect(wrapper.html()).toContain('120') // 交易笔数
    expect(wrapper.html()).toContain('8')   // 项目数量
  })

  it('应该显示近期交易表格', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    // 验证表格存在
    expect(wrapper.find('.recent-transactions').exists()).toBe(true)
    expect(wrapper.find('.modern-table').exists()).toBe(true)

    // 验证表格数据
    const tableHtml = wrapper.html()
    expect(tableHtml).toContain('客户A')
    expect(tableHtml).toContain('供应商B')
    expect(tableHtml).toContain('项目款')
    expect(tableHtml).toContain('采购办公用品')
  })

  it('应该正确处理 API 加载失败', async () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

    vi.mocked(dashboardApi.getDashboardSummary).mockRejectedValue(
      new Error('Network error')
    )

    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    // 验证错误被捕获
    expect(consoleErrorSpy).toHaveBeenCalled()

    // 验证组件仍然渲染（使用默认值）
    expect(wrapper.find('.page-title').exists()).toBe(true)

    consoleErrorSpy.mockRestore()
  })

  it('应该正确格式化日期和金额', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    // 验证日期格式
    expect(wrapper.html()).toContain('2024-03-14')
    expect(wrapper.html()).toContain('2024-03-13')

    // 验证金额格式（带千分位和小数）
    expect(wrapper.html()).toContain('5,000.00')
    expect(wrapper.html()).toContain('1,200.00')
  })

  it('应该显示当前日期', async () => {
    const wrapper = mountWithPlugins(Dashboard)
    await flushPromises()

    const headerDate = wrapper.find('.header-date')
    expect(headerDate.exists()).toBe(true)
    expect(headerDate.text()).toMatch(/\d{4}年\d{2}月\d{2}日/)
  })
})
