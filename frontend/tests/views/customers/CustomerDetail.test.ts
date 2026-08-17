import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ElMessage } from 'element-plus'
import CustomerDetail from '@/views/customers/CustomerDetail.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as customerApi from '@/api/customer'
import * as transactionApi from '@/api/transaction'
import type { Customer } from '@/types/customer'
import type { Transaction } from '@/types/transaction'

vi.mock('@/api/customer')
vi.mock('@/api/transaction')

// Mock vue-router 的 useRoute 和 useRouter
const mockRouteParams = { id: '1' }
const mockBack = vi.fn()

vi.mock('vue-router', async () => {
  const actual = await vi.importActual('vue-router')
  return {
    ...actual,
    useRoute: () => ({
      params: mockRouteParams
    }),
    useRouter: () => ({
      back: mockBack
    })
  }
})

describe('CustomerDetail.vue', () => {
  const mockCustomer: Customer = {
    id: 1,
    name: '测试客户',
    shortName: '客户A',
    contactPerson: '张三',
    contactPhone: '13800138000',
    contactEmail: 'zhangsan@test.com',
    address: '北京市朝阳区',
    taxNumber: '91110000000000001X',
    description: '测试描述',
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z'
  }

  const mockTransactions: Transaction[] = [
    {
      id: 1,
      transactionDate: '2024-01-15',
      transactionType: 'Income',
      amount: 10000,
      accountId: 1,
      accountName: '工商银行',
      categoryId: 1,
      categoryName: '销售收入',
      projectId: 1,
      projectName: '项目A',
      customerId: 1,
      customerName: '测试客户',
      supplierId: null,
      supplierName: null,
      personId: null,
      personName: null,
      description: '销售收入',
      status: 'Completed',
      isAllocated: false,
      allocations: [],
      tags: [
        { tagId: 31, tagName: '合同款', tagColor: '#409EFF' }
      ],
      createdAt: '2024-01-15T00:00:00Z'
    },
    {
      id: 2,
      transactionDate: '2024-01-20',
      transactionType: 'Expense',
      amount: 5000,
      accountId: 1,
      accountName: '工商银行',
      categoryId: 2,
      categoryName: '采购成本',
      projectId: 1,
      projectName: '项目A',
      customerId: 1,
      customerName: '测试客户',
      supplierId: null,
      supplierName: null,
      personId: null,
      personName: null,
      description: '采购支出',
      status: 'Completed',
      isAllocated: false,
      allocations: [],
      tags: [
        { tagId: 32, tagName: '售后', tagColor: '#E6A23C' }
      ],
      createdAt: '2024-01-20T00:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    mockRouteParams.id = '1'
    vi.mocked(customerApi.getCustomerById).mockResolvedValue(
      mockAxiosResponse({ data: mockCustomer })
    )
    vi.mocked(transactionApi.getTransactionsByCustomer).mockResolvedValue(
      mockAxiosResponse({ data: mockTransactions })
    )
    vi.mocked(transactionApi.getCustomerTransactionStatistics).mockResolvedValue(
      mockAxiosResponse({
        data: {
          totalIncome: 10000,
          totalExpense: 5000,
          netProfit: 5000,
          totalTransfer: 0,
          incomeCount: 1,
          expenseCount: 1,
          transferCount: 0,
          totalCount: 2
        }
      })
    )
  })

  it('应该在挂载时调用 getCustomerById 加载客户详情', async () => {
    mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(customerApi.getCustomerById).toHaveBeenCalledWith(1)
  })

  it('应该正确显示客户基本信息', async () => {
    const wrapper = mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('测试客户')
    expect(wrapper.text()).toContain('客户A')
    expect(wrapper.text()).toContain('张三')
    expect(wrapper.text()).toContain('13800138000')
    expect(wrapper.text()).toContain('zhangsan@test.com')
  })

  it('应该显示客户状态标签', async () => {
    const wrapper = mountWithPlugins(CustomerDetail)
    await flushPromises()

    const tags = wrapper.findAll('.el-tag')
    const tagTexts = tags.map(t => t.text())
    expect(tagTexts).toContain('启用')
  })

  it('应该在挂载时自动加载交易记录', async () => {
    mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(transactionApi.getTransactionsByCustomer).toHaveBeenCalledWith(1)
  })

  it('应该正确显示交易记录列表', async () => {
    const wrapper = mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('销售收入')
    expect(wrapper.text()).toContain('采购支出')
    expect(wrapper.text()).toContain('10,000.00')
    expect(wrapper.text()).toContain('-5,000.00')
  })

  it('应该正确显示交易类型标签（收入/支出）', async () => {
    const wrapper = mountWithPlugins(CustomerDetail)
    await flushPromises()

    const tags = wrapper.findAll('.el-tag')
    const tagTexts = tags.map(t => t.text())
    expect(tagTexts).toContain('收入')
    expect(tagTexts).toContain('支出')
  })

  it('应该渲染返回按钮和页面标题', async () => {
    const wrapper = mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('返回')
    expect(wrapper.text()).toContain('客户详情')
  })

  it('加载客户详情失败时应该显示错误消息', async () => {
    vi.mocked(customerApi.getCustomerById).mockRejectedValue(new Error('Network error'))

    mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载客户详情失败')
  })

  it('加载交易记录失败时应该显示错误消息', async () => {
    vi.mocked(transactionApi.getTransactionsByCustomer).mockRejectedValue(new Error('Network error'))

    mountWithPlugins(CustomerDetail)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载交易记录失败')
  })

  it('should display transaction tags in the record table', async () => {
    const wrapper = mountWithPlugins(CustomerDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('合同款')
    expect(text).toContain('售后')
  })
})
