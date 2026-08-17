import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ElMessageBox } from 'element-plus'
import { useUserStore } from '@/features/auth/stores/user'
import AccountListPage from '@/features/master-data/accounts/pages/AccountListPage.vue'
import * as accountApi from '@/features/master-data/accounts/api/account'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'

vi.mock('@/features/master-data/accounts/api/account')

const mockAccounts = [
  {
    id: 1,
    name: '工商银行账户',
    accountType: 'Bank',
    openingBalance: 10000,
    currentBalance: 15000,
    currency: 'CNY',
    bankName: '工商银行',
    accountNumber: '6222021234567890',
    isActive: true,
    createdAt: '2026-03-01T00:00:00Z'
  },
  {
    id: 2,
    name: '三个月定期',
    accountType: 'FixedDeposit',
    openingBalance: 200000,
    currentBalance: 200000,
    currency: 'CNY',
    bankName: '招商银行',
    accountNumber: '6222000000000001',
    isActive: true,
    createdAt: '2026-03-02T00:00:00Z'
  }
]

const mountPage = () => {
  return mountWithPlugins(AccountListPage, {
    global: {
      stubs: {
        SearchableFilterInput: true,
        StatCard: true,
        BatchLinkDialog: true
      }
    }
  })
}

describe('AccountListPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(accountApi.getAccounts).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          items: mockAccounts,
          total: mockAccounts.length,
          page: 1,
          pageSize: 20
        }
      })
    )
    vi.mocked(accountApi.getAccountStatistics).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          totalCount: 2,
          activeCount: 2,
          totalBalance: 215000,
          fixedDepositCount: 1
        }
      })
    )
  })

  it('挂载时应加载账户列表和统计数据', async () => {
    const wrapper = mountPage()
    const userStore = useUserStore()
    userStore.setUser({ id: 1, userName: 'tester', fullName: 'Tester', role: 'Admin' } as any)

    await flushPromises()

    expect(accountApi.getAccounts).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      name: undefined
    })
    expect(accountApi.getAccountStatistics).toHaveBeenCalledTimes(1)
    expect(wrapper.text()).toContain('账户管理')
    expect(wrapper.text()).toContain('定期账户主档')
  })

  it('定期账户创建成功后应弹出下一步建议并跳转到定期台账创建首笔记录', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)

    const wrapper = mountPage()
    const userStore = useUserStore()
    userStore.setUser({ id: 1, userName: 'tester', fullName: 'Tester', role: 'Admin' } as any)

    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleFormSuccess({
      accountId: 9,
      accountType: 'FixedDeposit',
      name: '六个月定期'
    })
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      '定期账户「六个月定期」创建成功，是否立即登记第一笔定期记录？',
      '下一步建议',
      expect.objectContaining({
        confirmButtonText: '立即登记',
        cancelButtonText: '稍后再说'
      })
    )
    expect(wrapper.vm.$router.currentRoute.value.name).toBe('FixedDeposits')
    expect(wrapper.vm.$router.currentRoute.value.query).toEqual({
      accountId: '9',
      action: 'create'
    })
  })

  it('普通账户创建成功后不应弹出定期登记引导', async () => {
    const wrapper = mountPage()
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleFormSuccess({
      accountId: 10,
      accountType: 'Bank',
      name: '建设银行'
    })
    await flushPromises()

    expect(ElMessageBox.confirm).not.toHaveBeenCalled()
    expect(wrapper.vm.$router.currentRoute.value.name).not.toBe('FixedDeposits')
  })
})
