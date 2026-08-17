import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import TransferDialog from '@/features/transactions/components/TransferDialog.vue'
import ConvertTransactionToTransferDialog from '@/features/transactions/components/ConvertTransactionToTransferDialog.vue'
import * as accountApi from '@/features/master-data/accounts/api/account'
import * as transactionApi from '@/features/transactions/api/transaction'

vi.mock('@/features/master-data/accounts/api/account')
vi.mock('@/features/transactions/api/transaction')

const dialogStubs = {
  global: {
    stubs: {
      ElDialog: true,
      ElForm: true,
      ElFormItem: true,
      ElSelect: true,
      ElOption: true,
      ElInput: true,
      ElInputNumber: true,
      ElDatePicker: true,
      ElAlert: true,
      ElTag: true,
      ElRadioGroup: true,
      ElRadio: true,
      ElEmpty: true,
      ElButton: true
    }
  }
}

const baseAccounts = [
  {
    id: 1,
    name: '工商银行',
    accountType: 'Bank',
    currentBalance: 50000,
    openingBalance: 50000,
    isActive: true,
    createdAt: '2026-03-01T00:00:00'
  },
  {
    id: 2,
    name: '建设银行',
    accountType: 'Bank',
    currentBalance: 30000,
    openingBalance: 30000,
    isActive: true,
    createdAt: '2026-03-01T00:00:00'
  }
]

const fixedDepositAccount = {
  id: 3,
  name: '三个月定期',
  accountType: 'FixedDeposit',
  currentBalance: 20000,
  openingBalance: 0,
  isActive: true,
  createdAt: '2026-03-01T00:00:00'
}

const inactiveFixedDepositAccount = {
  id: 4,
  name: '已到期定期',
  accountType: 'FixedDeposit',
  currentBalance: 15000,
  openingBalance: 15000,
  isActive: false,
  createdAt: '2026-02-01T00:00:00'
}

const readSetupState = (wrapper: any, key: string) => {
  const state = wrapper.vm.$?.setupState?.[key]
  return state?.value ?? state
}

describe('Transfer account options', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(accountApi.getActiveAccounts).mockResolvedValue(
      mockAxiosResponse({ data: [...baseAccounts, fixedDepositAccount] })
    )
    vi.mocked(transactionApi.getTransferCandidates).mockResolvedValue(
      mockAxiosResponse({ data: [] })
    )
  })

  it('转账弹窗应补拉活跃账户并允许选择定期存款作为转入账户', async () => {
    const wrapper = mountWithPlugins(TransferDialog, {
      ...dialogStubs,
      props: {
        modelValue: false,
        accounts: baseAccounts
      }
    })

    await wrapper.setProps({ modelValue: true })
    await flushPromises()

    expect(accountApi.getActiveAccounts).toHaveBeenCalledTimes(1)

    const form = readSetupState(wrapper, 'form')
    form.fromAccountId = 1
    wrapper.vm.$?.setupState?.handleFromAccountChange()
    await wrapper.vm.$nextTick()

    const availableToAccounts = readSetupState(wrapper, 'availableToAccounts')
    expect(availableToAccounts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          id: fixedDepositAccount.id,
          accountType: 'FixedDeposit'
        })
      ])
    )
  })

  it('转账弹窗应优先使用补拉账户中的最新余额', async () => {
    const staleAccounts = baseAccounts.map(account =>
      account.id === 1
        ? { ...account, currentBalance: 10000, openingBalance: 10000 }
        : account
    )
    const freshAccounts = baseAccounts.map(account =>
      account.id === 1
        ? { ...account, currentBalance: 80000, openingBalance: 80000 }
        : account
    )

    vi.mocked(accountApi.getActiveAccounts).mockResolvedValue(
      mockAxiosResponse({ data: freshAccounts })
    )

    const wrapper = mountWithPlugins(TransferDialog, {
      ...dialogStubs,
      props: {
        modelValue: false,
        accounts: staleAccounts
      }
    })

    await wrapper.setProps({ modelValue: true })
    await flushPromises()

    const form = readSetupState(wrapper, 'form')
    form.fromAccountId = 1
    wrapper.vm.$?.setupState?.handleFromAccountChange()
    await wrapper.vm.$nextTick()

    const selectableAccounts = readSetupState(wrapper, 'selectableAccounts')
    expect(selectableAccounts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          id: 1,
          currentBalance: 80000
        })
      ])
    )
    expect(readSetupState(wrapper, 'maxAmount')).toBe(80000)
  })

  it('识别为转账弹窗应补拉活跃账户并允许选择定期存款作为目标账户', async () => {
    const wrapper = mountWithPlugins(ConvertTransactionToTransferDialog, {
      ...dialogStubs,
      props: {
        modelValue: false,
        accounts: baseAccounts,
        transaction: {
          id: 100,
          accountId: 1,
          accountName: '工商银行',
          transactionDate: '2026-03-28',
          amount: 20000,
          transactionType: 'Expense',
          description: '转存定期'
        }
      }
    })

    await wrapper.setProps({ modelValue: true })
    await flushPromises()

    expect(accountApi.getActiveAccounts).toHaveBeenCalledTimes(1)

    const targetAccounts = readSetupState(wrapper, 'targetAccounts')
    expect(targetAccounts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          id: fixedDepositAccount.id,
          accountType: 'FixedDeposit'
        })
      ])
    )

    const form = readSetupState(wrapper, 'form')
    form.targetAccountId = fixedDepositAccount.id
    await flushPromises()

    expect(transactionApi.getTransferCandidates).toHaveBeenCalledWith(100, fixedDepositAccount.id)
  })

  it('识别为转账弹窗应保留旧列表中的停用目标账户', async () => {
    vi.mocked(accountApi.getActiveAccounts).mockResolvedValue(
      mockAxiosResponse({ data: [...baseAccounts] })
    )

    const wrapper = mountWithPlugins(ConvertTransactionToTransferDialog, {
      ...dialogStubs,
      props: {
        modelValue: false,
        accounts: [...baseAccounts, inactiveFixedDepositAccount],
        transaction: {
          id: 100,
          accountId: 1,
          accountName: '工商银行',
          transactionDate: '2026-03-28',
          amount: 15000,
          transactionType: 'Expense',
          description: '到期回款转出'
        }
      }
    })

    await wrapper.setProps({ modelValue: true })
    await flushPromises()

    const targetAccounts = readSetupState(wrapper, 'targetAccounts')
    expect(targetAccounts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          id: inactiveFixedDepositAccount.id,
          isActive: false
        })
      ])
    )

    const form = readSetupState(wrapper, 'form')
    form.targetAccountId = inactiveFixedDepositAccount.id
    await flushPromises()

    expect(transactionApi.getTransferCandidates).toHaveBeenCalledWith(100, inactiveFixedDepositAccount.id)
  })
})
