import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { useUserStore } from '@/features/auth/stores/user'
import AccountDetailPage from '@/features/master-data/accounts/pages/AccountDetailPage.vue'
import * as accountApi from '@/features/master-data/accounts/api/account'
import * as fixedDepositApi from '@/features/master-data/fixed-deposits/api/fixedDeposit'
import * as transactionApi from '@/features/transactions/api/transaction'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'

vi.mock('@/features/master-data/accounts/api/account')
vi.mock('@/features/master-data/fixed-deposits/api/fixedDeposit')
vi.mock('@/features/transactions/api/transaction')

const routeMock = {
  params: { id: '1' }
}

const routerMock = {
  back: vi.fn(),
  push: vi.fn(),
  hasRoute: vi.fn(() => true)
}

vi.mock('vue-router', async () => {
  const actual = await vi.importActual('vue-router')
  return {
    ...actual,
    useRoute: () => routeMock,
    useRouter: () => routerMock
  }
})

const baseStatistics = {
  totalIncome: 0,
  totalExpense: 0,
  netProfit: 0,
  totalTransfer: 100000,
  incomeCount: 0,
  expenseCount: 0,
  transferCount: 1,
  totalCount: 1
}

const bankAccount = {
  id: 1,
  name: '测试账户',
  accountType: 'Bank',
  openingBalance: 10000,
  currentBalance: 15000,
  currency: 'CNY',
  bankName: '测试银行',
  accountNumber: '6222021234567890',
  isActive: true,
  createdAt: '2026-03-01T00:00:00Z'
}

const fixedDepositAccount = {
  id: 1,
  name: '六个月定存',
  accountType: 'FixedDeposit',
  openingBalance: 200000,
  currentBalance: 200000,
  currency: 'CNY',
  bankName: '招商银行',
  accountNumber: '6222000000000001',
  isActive: true,
  createdAt: '2026-03-01T00:00:00Z'
}

const mountDetailPage = () => {
  return mountWithPlugins(AccountDetailPage, {
    global: {
      stubs: {
        BalanceTrendChart: true,
        SummaryOverview: {
          template: '<div><slot /></div>',
          props: ['title', 'subtitle', 'loading', 'empty']
        },
        TransactionSummaryCards: true
      }
    }
  })
}

describe('AccountDetailPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    routeMock.params.id = '1'
    routerMock.hasRoute.mockReturnValue(true)
    vi.mocked(accountApi.getAccountBalanceTrend).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { trends: [] }
      })
    )
    vi.mocked(transactionApi.getAccountTransactionStatistics).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: baseStatistics
      })
    )
  })

  it('普通账户详情默认加载交易记录而不是定期记录', async () => {
    vi.mocked(accountApi.getAccountById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: bankAccount
      })
    )
    vi.mocked(transactionApi.getTransactionsByAccount).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: [
          {
            id: 1,
            transactionDate: '2026-03-20T00:00:00Z',
            transactionType: 'Income',
            amount: 5000,
            accountId: 1,
            accountName: '测试账户',
            categoryId: 11,
            categoryName: '服务收入',
            description: '咨询服务费',
            status: 'Completed',
            isAllocated: false,
            allocations: [],
            tags: [],
            createdAt: '2026-03-20T00:00:00Z'
          }
        ]
      })
    )
    vi.mocked(fixedDepositApi.getFixedDepositsByAccount).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: []
      })
    )

    const wrapper = mountDetailPage()
    await flushPromises()

    expect(accountApi.getAccountById).toHaveBeenCalledWith(1)
    expect(transactionApi.getTransactionsByAccount).toHaveBeenCalledWith(1)
    expect(fixedDepositApi.getFixedDepositsByAccount).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('账户详情')
    expect(wrapper.text()).not.toContain('定期账户主档')
  })

  it('定期账户详情优先显示主档说明和空状态', async () => {
    vi.mocked(accountApi.getAccountById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: fixedDepositAccount
      })
    )
    vi.mocked(transactionApi.getTransactionsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )
    vi.mocked(fixedDepositApi.getFixedDepositsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )

    const wrapper = mountDetailPage()
    const userStore = useUserStore()
    userStore.setUser({ id: 1, userName: 'tester', fullName: 'Tester', role: 'Accountant' } as any)

    await flushPromises()
    await nextTick()

    expect(fixedDepositApi.getFixedDepositsByAccount).toHaveBeenCalledWith(1)
    expect(transactionApi.getTransactionsByAccount).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('定期账户主档')
    expect(wrapper.text()).toContain('当前定期账户还没有登记定期记录')
  })

  it('定期账户空状态入口会跳转到定期台账创建首笔记录', async () => {
    vi.mocked(accountApi.getAccountById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: fixedDepositAccount
      })
    )
    vi.mocked(fixedDepositApi.getFixedDepositsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )
    vi.mocked(transactionApi.getTransactionsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )

    const wrapper = mountDetailPage()
    const userStore = useUserStore()
    userStore.setUser({ id: 1, userName: 'tester', fullName: 'Tester', role: 'Accountant' } as any)

    await flushPromises()

    const vm = wrapper.vm as any
    vm.openFixedDepositLedger('create')

    expect(routerMock.push).toHaveBeenCalledWith({
      name: 'FixedDeposits',
      query: {
        accountId: '1',
        action: 'create'
      }
    })
  })

  it('fixed deposit detail layout keeps the records section after summary and analysis', async () => {
    vi.mocked(accountApi.getAccountById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: fixedDepositAccount
      })
    )
    vi.mocked(transactionApi.getTransactionsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )
    vi.mocked(fixedDepositApi.getFixedDepositsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )

    const wrapper = mountDetailPage()
    const userStore = useUserStore()
    userStore.setUser({ id: 1, userName: 'tester', fullName: 'Tester', role: 'Accountant' } as any)

    await flushPromises()
    await nextTick()

    const detailSections = wrapper.get('.detail-sections')
    const sectionBlocks = detailSections.findAll('.section-block')

    expect(detailSections.classes()).not.toContain('detail-sections--fixed-deposit')
    expect(sectionBlocks.at(0)?.classes()).toContain('fixed-deposit-guide')
    expect(sectionBlocks.at(1)?.classes()).toContain('section-summary')
    expect(sectionBlocks.at(2)?.classes()).toContain('section-analysis')
    expect(sectionBlocks.at(3)?.classes()).toContain('section-records')
  })

  it('should display transaction tags in the record table', async () => {
    vi.mocked(accountApi.getAccountById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: bankAccount
      })
    )
    vi.mocked(transactionApi.getTransactionsByAccount).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: [
          {
            id: 1,
            transactionDate: '2026-03-20T00:00:00Z',
            transactionType: 'Income',
            amount: 5000,
            accountId: 1,
            accountName: '测试账户',
            categoryId: 11,
            categoryName: '服务收入',
            description: '咨询服务费',
            status: 'Completed',
            isAllocated: false,
            allocations: [],
            tags: [
              { tagId: 51, tagName: '对账', tagColor: '#409EFF' },
              { tagId: 52, tagName: '主营', tagColor: '#67C23A' }
            ],
            createdAt: '2026-03-20T00:00:00Z'
          }
        ]
      })
    )
    vi.mocked(fixedDepositApi.getFixedDepositsByAccount).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )

    const wrapper = mountDetailPage()
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('对账')
    expect(text).toContain('主营')
  })
})
