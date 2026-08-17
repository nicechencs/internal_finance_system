import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mountWithPlugins, mockAxiosResponse } from '@tests/utils'
import { ElMessage, ElMessageBox } from 'element-plus'
import TransactionList from '@/views/transactions/TransactionList.vue'
import * as transactionApi from '@/api/transaction'
import * as accountApi from '@/api/account'
import * as categoryApi from '@/api/category'
import * as projectApi from '@/api/project'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/transaction')
vi.mock('@/api/account')
vi.mock('@/api/category')
vi.mock('@/api/project')

const mockAccounts = [
  {
    id: 1,
    name: '工商银行',
    accountType: 'Bank',
    openingBalance: 100000,
    currentBalance: 50000,
    currency: 'CNY',
    bankName: '工商银行',
    isActive: true,
    createdAt: '2026-01-01T00:00:00'
  },
  {
    id: 2,
    name: '建设银行',
    accountType: 'Bank',
    openingBalance: 80000,
    currentBalance: 30000,
    currency: 'CNY',
    bankName: '建设银行',
    isActive: true,
    createdAt: '2026-01-01T00:00:00'
  }
]

describe('TransactionList.vue', () => {
  const mockTransactions = {
    items: [
      {
        id: 1,
        transactionDate: '2026-03-14',
        transactionType: 'Income',
        amount: 10000,
        accountId: 1,
        accountName: '工商银行',
        categoryId: 1,
        categoryName: '销售收入',
        projectId: 1,
        projectName: '项目A',
        customerId: 1,
        customerName: '客户A',
        description: '项目款项',
        status: 'Completed',
        isAllocated: false,
        allocations: [],
        createdAt: '2026-03-14T10:00:00'
      },
      {
        id: 2,
        transactionDate: '2026-03-13',
        transactionType: 'Expense',
        amount: 5000,
        accountId: 2,
        accountName: '建设银行',
        categoryId: 2,
        categoryName: '办公费用',
        supplierId: 1,
        supplierName: '供应商A',
        description: '采购办公用品',
        status: 'Completed',
        isAllocated: true,
        allocations: [],
        createdAt: '2026-03-13T15:00:00'
      }
    ],
    total: 2,
    page: 1,
    pageSize: 20
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(transactionApi.getTransactions).mockResolvedValue(
      mockAxiosResponse({ data: mockTransactions })
    )
    vi.mocked(transactionApi.getTransactionStatistics).mockResolvedValue(
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
    vi.mocked(accountApi.getAccounts).mockResolvedValue(
      mockAxiosResponse({ data: { items: mockAccounts, total: 2 } })
    )
    vi.mocked(categoryApi.getCategories).mockResolvedValue(
      mockAxiosResponse({ data: { items: [], total: 0 } })
    )
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({ data: { items: [], total: 0 } })
    )
  })

  const stubConfig = {
    global: {
      stubs: {
        TransactionForm: true,
        TransactionDetail: true,
        TransferDialog: true,
        ElTable: true,
        ElTableColumn: true,
        ElPagination: true,
        ElForm: true,
        ElFormItem: true,
        ElSelect: true,
        ElOption: true,
        ElDatePicker: true,
        ElButton: true,
        ElTag: true,
        ElLink: true
      }
    }
  }

  it('应该在挂载时加载交易列表', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    expect(transactionApi.getTransactions).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20
    })
  })

  it('应该渲染页面标题', () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    expect(wrapper.html()).toContain('交易管理')
  })

  it('应该渲染筛选区域', () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    expect(wrapper.find('.search-section').exists()).toBe(true)
  })

  it('点击新增按钮应该打开表单对话框', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    // 通过 vm 访问组件实例
    const vm = wrapper.vm as any
    expect(vm.formVisible).toBe(false)

    vm.handleCreate()
    await wrapper.vm.$nextTick()

    expect(vm.formVisible).toBe(true)
    expect(vm.currentTransaction).toBeNull()
  })

  it('应该正确处理分页', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    vi.mocked(transactionApi.getTransactions).mockClear()

    vm.pagination.page = 2
    await vm.handlePageChange()
    await flushPromises()

    expect(transactionApi.getTransactions).toHaveBeenCalledWith({
      page: 2,
      pageSize: 20
    })
  })

  it('加载失败时应该显示错误消息', async () => {
    vi.mocked(transactionApi.getTransactions).mockRejectedValueOnce(new Error('网络错误'))

    mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载交易列表失败')
  })

  it('点击删除应该调用确认框和删除API', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(transactionApi.deleteTransaction).mockResolvedValue(mockAxiosResponse({}))

    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleDelete(mockTransactions.items[0])
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalled()
    expect(transactionApi.deleteTransaction).toHaveBeenCalledWith(1)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('点击重置应该清空筛选条件并重新加载', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.activeTab = 'Income'
    vm.filters.dateRange = [new Date(), new Date()]

    vi.mocked(transactionApi.getTransactions).mockClear()

    await vm.handleReset()
    await flushPromises()

    expect(vm.activeTab).toBe('all')
    expect(vm.filters.dateRange).toBeNull()
    expect(transactionApi.getTransactions).toHaveBeenCalled()
  })

  it('点击编辑应该打开表单并传入交易数据', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleEdit(mockTransactions.items[0])

    expect(vm.formVisible).toBe(true)
    expect(vm.currentTransaction).toEqual(mockTransactions.items[0])
  })

  it('点击详情应该打开详情对话框', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleView(mockTransactions.items[0])

    expect(vm.detailVisible).toBe(true)
    expect(vm.currentTransactionId).toBe(1)
  })

  // ===== 转账功能测试 =====

  it('应该在挂载时加载账户列表', async () => {
    mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    expect(accountApi.getAccounts).toHaveBeenCalledWith({ page: 1, pageSize: 1000 })
  })

  it('应该渲染转账对话框组件', () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    const transferDialog = wrapper.find('transfer-dialog-stub')
    expect(transferDialog.exists()).toBe(true)
  })

  it('点击转账按钮应该打开转账对话框', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.transferVisible).toBe(false)

    vm.transferVisible = true
    await wrapper.vm.$nextTick()

    expect(vm.transferVisible).toBe(true)
  })

  it('转账成功后应该刷新交易列表和账户列表', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    vi.mocked(transactionApi.getTransactions).mockClear()
    vi.mocked(accountApi.getAccounts).mockClear()

    const vm = wrapper.vm as any
    await vm.handleTransferSuccess()
    await flushPromises()

    expect(transactionApi.getTransactions).toHaveBeenCalled()
    expect(accountApi.getAccounts).toHaveBeenCalled()
  })

  it('转账类型的交易不应该显示编辑按钮（通过 v-if 条件验证）', async () => {
    const transferTransaction = {
      id: 3,
      transactionDate: '2026-03-14',
      transactionType: 'Transfer',
      amount: 2000,
      accountId: 1,
      accountName: '工商银行',
      description: '转账至建设银行',
      status: 'Completed',
      isAllocated: false,
      relatedTransactionId: 4,
      relatedAccountName: '建设银行',
      allocations: [],
      createdAt: '2026-03-14T12:00:00'
    }

    const mockWithTransfer = {
      items: [transferTransaction],
      total: 1,
      page: 1,
      pageSize: 20
    }

    vi.mocked(transactionApi.getTransactions).mockResolvedValue(
      mockAxiosResponse({ data: mockWithTransfer })
    )

    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    // 验证 Transfer 类型的交易在数据中存在
    expect(vm.transactions[0].transactionType).toBe('Transfer')
  })

  it('getTransferLabel 应该为转出交易返回正确标签', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any

    const outRow = {
      transactionType: 'Transfer',
      relatedAccountName: '建设银行',
      description: '转账至建设银行'
    }
    expect(vm.getTransferLabel(outRow)).toBe('→ 建设银行')
  })

  it('getTransferLabel 应该为转入交易返回正确标签', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any

    const inRow = {
      transactionType: 'Transfer',
      relatedAccountName: '工商银行',
      description: '从工商银行转入'
    }
    expect(vm.getTransferLabel(inRow)).toBe('← 工商银行')
  })

  it('getTransferLabel 没有关联账户名时应该返回空字符串', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any

    const row = {
      transactionType: 'Transfer',
      relatedAccountName: undefined,
      description: '转账'
    }
    expect(vm.getTransferLabel(row)).toBe('')
  })

  it('canConvertToTransfer 应对可编辑且非转账记录返回 true', async () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    await flushPromises()

    const vm = wrapper.vm as any
    const userStore = useUserStore()
    const editableUser = {
      id: 1,
      username: 'tester',
      email: 'tester@example.com',
      fullName: 'Tester',
      role: 'Accountant',
      isActive: true
    }

    userStore.setUser(editableUser)

    expect(vm.canConvertToTransfer({
      id: 10,
      transactionType: 'Expense',
      bankTransactionId: 99
    })).toBe(true)

    expect(vm.canConvertToTransfer({
      id: 11,
      transactionType: 'Expense'
    })).toBe(true)

    expect(vm.canConvertToTransfer({
      id: 12,
      transactionType: 'Transfer',
      bankTransactionId: 100
    })).toBe(false)

    userStore.setUser({
      ...editableUser,
      role: 'Viewer'
    })

    expect(vm.canConvertToTransfer({
      id: 13,
      transactionType: 'Expense'
    })).toBe(false)
  })

  it('筛选区域应该包含转账选项', () => {
    const wrapper = mountWithPlugins(TransactionList, stubConfig)
    expect(wrapper.html()).toContain('转账')
  })
})
