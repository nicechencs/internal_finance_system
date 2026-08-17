import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ElMessage } from 'element-plus'
import PersonDetail from '@/features/master-data/persons/pages/PersonDetailPage.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as personApi from '@/features/master-data/persons/api/person'
import * as transactionApi from '@/features/transactions/api/transaction'
import type { Person } from '@/features/master-data/persons/types/person'
import type { Transaction } from '@/features/transactions/types/transaction'

vi.mock('@/api/person')
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

describe('PersonDetail.vue', () => {
  const mockPerson: Person = {
    id: 1,
    name: '张三',
    personType: 'Employee',
    idNumber: '110101199001011234',
    phone: '13800138000',
    email: 'zhangsan@example.com',
    bankName: '工商银行',
    bankAccount: '6222021234567890',
    joinDate: '2023-01-15',
    leaveDate: '',
    isActive: true,
    createdAt: '2023-01-15T08:00:00Z'
  }

  const mockTransactions: Transaction[] = [
    {
      id: 1,
      transactionDate: '2023-05-10',
      transactionType: 'Expense',
      amount: 5000,
      accountId: 1,
      accountName: '公司账户',
      categoryId: 1,
      categoryName: '工资',
      projectId: 1,
      projectName: '项目A',
      personId: 1,
      personName: '张三',
      description: '5月工资',
      status: 'Completed',
      isAllocated: false,
      allocations: [],
      tags: [
        { tagId: 21, tagName: '工资', tagColor: '#F56C6C' }
      ],
      createdAt: '2023-05-10T08:00:00Z'
    },
    {
      id: 2,
      transactionDate: '2023-06-10',
      transactionType: 'Income',
      amount: 8000,
      accountId: 1,
      accountName: '公司账户',
      categoryId: 2,
      categoryName: '咨询费',
      projectId: 1,
      projectName: '项目A',
      personId: 1,
      personName: '张三',
      description: '6月咨询费',
      status: 'Completed',
      isAllocated: false,
      allocations: [],
      tags: [
        { tagId: 22, tagName: '提成', tagColor: '#67C23A' }
      ],
      createdAt: '2023-06-10T08:00:00Z'
    },
    {
      id: 3,
      transactionDate: '2023-06-15',
      transactionType: 'Transfer',
      transferDirection: 'Out',
      amount: 2000,
      accountId: 1,
      accountName: '公司账户',
      projectId: 1,
      projectName: '项目A',
      personId: 1,
      personName: '张三',
      description: '账户转出',
      status: 'Completed',
      isAllocated: false,
      allocations: [],
      tags: [],
      createdAt: '2023-06-15T08:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    mockRouteParams.id = '1'
    vi.spyOn(personApi, 'getPersonById').mockResolvedValue(
      mockAxiosResponse({ data: mockPerson }) as any
    )
    vi.spyOn(transactionApi, 'getTransactionsByPerson').mockResolvedValue(
      mockAxiosResponse({ data: mockTransactions }) as any
    )
    vi.spyOn(transactionApi, 'getPersonTransactionStatistics').mockResolvedValue(
      mockAxiosResponse({
        data: {
          totalIncome: 8000,
          totalExpense: 5000,
          netProfit: 3000,
          totalTransfer: 0,
          incomeCount: 1,
          expenseCount: 1,
          transferCount: 0,
          totalCount: 2
        }
      }) as any
    )
  })

  it('应该在挂载时调用 getPersonById 加载人员详情', async () => {
    mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(personApi.getPersonById).toHaveBeenCalledWith(1)
  })

  it('应该正确显示人员基本信息', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('张三')
    expect(wrapper.text()).toContain('13800138000')
    expect(wrapper.text()).toContain('zhangsan@example.com')
    expect(wrapper.text()).toContain('工商银行')
    expect(wrapper.text()).toContain('6222021234567890')
  })

  it('应该正确格式化人员类型为中文', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('员工')
  })

  it('应该正确显示在职状态标签', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    const tags = wrapper.findAll('.el-tag')
    const tagTexts = tags.map(t => t.text())
    expect(tagTexts).toContain('在职')
  })

  it('应该在挂载时自动加载交易记录', async () => {
    mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(transactionApi.getTransactionsByPerson).toHaveBeenCalledWith(1)
  })

  it('应该正确显示交易记录列表', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('5月工资')
    expect(wrapper.text()).toContain('6月咨询费')
    expect(wrapper.text()).toContain('-5,000.00')
    expect(wrapper.text()).toContain('8,000.00')
    expect(wrapper.text()).toContain('2,000.00')
  })

  it('应该正确显示交易类型标签（收入/支出/转账）', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    const tags = wrapper.findAll('.el-tag')
    const tagTexts = tags.map(t => t.text())
    expect(tagTexts).toContain('支出')
    expect(tagTexts).toContain('收入')
    expect(tagTexts).toContain('转出')
    expect(wrapper.text()).toContain('转账总额')
  })

  it('应该渲染返回按钮和页面标题', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('返回')
    expect(wrapper.text()).toContain('人员详情')
  })

  it('加载人员详情失败时应该显示错误消息', async () => {
    vi.spyOn(personApi, 'getPersonById').mockRejectedValue(new Error('Network error'))

    mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载人员详情失败')
  })

  it('加载交易记录失败时应该显示错误消息', async () => {
    vi.spyOn(transactionApi, 'getTransactionsByPerson').mockRejectedValue(new Error('Network error'))

    mountWithPlugins(PersonDetail)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载交易记录失败')
  })

  it('should display transaction tags in the record table', async () => {
    const wrapper = mountWithPlugins(PersonDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('工资')
    expect(text).toContain('提成')
  })
})
