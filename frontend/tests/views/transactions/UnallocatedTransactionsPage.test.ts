import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mountWithPlugins } from '@tests/utils'
import { useUserStore } from '@/features/auth/stores/user'
import UnallocatedTransactionsPage from '@/features/transactions/pages/UnallocatedTransactionsPage.vue'
import * as transactionApi from '@/features/transactions/api/transaction'
import * as receivableApi from '@/features/finance/api/receivable'
import * as payableApi from '@/features/finance/api/payable'
import * as projectApi from '@/features/master-data/projects/api/project'
import * as customerApi from '@/features/master-data/customers/api/customer'
import * as supplierApi from '@/features/master-data/suppliers/api/supplier'
import * as personApi from '@/features/master-data/persons/api/person'

vi.mock('@/features/transactions/api/transaction')
vi.mock('@/features/finance/api/receivable')
vi.mock('@/features/finance/api/payable')
vi.mock('@/features/master-data/projects/api/project')
vi.mock('@/features/master-data/customers/api/customer')
vi.mock('@/features/master-data/suppliers/api/supplier')
vi.mock('@/features/master-data/persons/api/person')

const incomeTx = {
  id: 1,
  transactionDate: '2026-08-16T00:00:00Z',
  transactionType: 'Income',
  amount: 1000,
  availableAmount: 1000,
  accountId: 1,
  accountName: '基本户',
  projectId: 11,
  projectName: '项目A',
  customerId: 21,
  customerName: '客户A',
  description: '到账',
  status: 'Confirmed',
  isAllocated: false,
  allocations: [],
  tags: [],
  createdAt: '2026-08-16T00:00:00Z'
}

const mountPage = () => mountWithPlugins(UnallocatedTransactionsPage, {
  global: {
    stubs: {
      TransactionDetail: true,
      SearchableSelect: { template: '<div />' },
      ElDatePicker: { template: '<div />' },
      ElInputNumber: { template: '<div />' }
    }
  }
})

describe('UnallocatedTransactionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(transactionApi.getTransactions).mockResolvedValue({
      data: { data: { items: [incomeTx], total: 1, page: 1, pageSize: 20, totalPages: 1 } }
    } as any)
    vi.mocked(receivableApi.getAvailableReceivablesForTransaction).mockResolvedValue({
      data: { data: [{ id: 88, projectId: 11, projectName: '项目A', customerName: '客户A', remainingAmount: 800 }] }
    } as any)
    vi.mocked(receivableApi.receivePayment).mockResolvedValue({ data: { data: {} } } as any)
    vi.mocked(receivableApi.createReceivable).mockResolvedValue({ data: { data: { id: 99 } } } as any)
    vi.mocked(projectApi.getActiveProjects).mockResolvedValue({ data: { data: [] } } as any)
    vi.mocked(customerApi.getActiveCustomers).mockResolvedValue({ data: { data: [] } } as any)
    vi.mocked(supplierApi.getActiveSuppliers).mockResolvedValue({ data: { data: [] } } as any)
    vi.mocked(personApi.getActivePersons).mockResolvedValue({ data: { data: [] } } as any)
  })

  it('加载列表时带上 allocationStatus 并排除转账', async () => {
    const wrapper = mountPage()
    useUserStore().setUser({
      id: 1,
      username: 'admin',
      email: 'a@b.c',
      fullName: 'Admin',
      role: 'Admin',
      isActive: true
    })
    await flushPromises()

    expect(transactionApi.getTransactions).toHaveBeenCalledWith(
      expect.objectContaining({
        allocationStatus: 'Unallocated,PartiallyAllocated',
        excludeTransfer: true
      })
    )
    expect(wrapper.text()).not.toContain('功能正在开发中')
    expect(wrapper.text()).toContain('待分配交易')
  })

  it('关联已有应收时调用 receivePayment', async () => {
    const wrapper = mountPage()
    useUserStore().setUser({
      id: 1,
      username: 'admin',
      email: 'a@b.c',
      fullName: 'Admin',
      role: 'Admin',
      isActive: true
    })
    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleProcess(incomeTx)
    await flushPromises()
    vm.selectedSettlementId = 88
    vm.allocationAmount = 800
    await vm.confirmProcess()
    await flushPromises()

    expect(receivableApi.receivePayment).toHaveBeenCalledWith(88, expect.objectContaining({
      transactionId: 1,
      amount: 800
    }))
  })
})
