import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mountWithPlugins, mockAxiosResponse } from '@tests/utils'
import { ElMessage } from 'element-plus'
import TransactionDetail from '@/views/transactions/TransactionDetail.vue'
import * as transactionApi from '@/api/transaction'

vi.mock('@/api/transaction')

describe('TransactionDetail.vue', () => {
  const emptyRelatedRecords = {
    receivables: [],
    payables: []
  }

  const mockTransaction = {
    id: 1,
    transactionDate: '2026-03-14',
    transactionType: 'Expense' as const,
    amount: 10000,
    accountId: 1,
    accountName: '工商银行',
    categoryId: 2,
    categoryName: '办公费用',
    projectId: 1,
    projectName: '项目A',
    supplierId: 1,
    supplierName: '供应商A',
    description: '采购办公设备',
    status: 'Completed',
    isAllocated: true,
    allocations: [
      {
        id: 1,
        projectId: 1,
        projectName: '项目A',
        personId: 1,
        personName: '张三',
        amount: 6000,
        allocationRate: 60,
        description: '项目A分摊'
      },
      {
        id: 2,
        projectId: 2,
        projectName: '项目B',
        personId: 2,
        personName: '李四',
        amount: 4000,
        allocationRate: 40,
        description: '项目B分摊'
      }
    ],
    createdAt: '2026-03-14T10:00:00'
  }

  const mockTransactionNoAllocation = {
    ...mockTransaction,
    isAllocated: false,
    allocations: []
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(transactionApi.getRelatedFinanceRecords).mockResolvedValue(
      mockAxiosResponse({ data: emptyRelatedRecords })
    )
  })

  const mountDetail = (props: Record<string, any> = {}) => {
    return mountWithPlugins(TransactionDetail, {
      props: {
        visible: false,  // 初始为 false
        transactionId: 1,
        ...props
      }
    })
  }

  it('应该在 visible 为 true 时加载交易详情', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(transactionApi.getTransactionById).toHaveBeenCalledWith(1)
  })

  it('visible 为 false 时不应该加载数据', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    mountDetail({ visible: false })
    await flushPromises()

    expect(transactionApi.getTransactionById).not.toHaveBeenCalled()
  })

  it('应该正确存储加载的交易数据', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.transaction).toEqual(mockTransaction)
    expect(vm.loading).toBe(false)
  })

  it('加载失败时应该显示错误消息', async () => {
    vi.mocked(transactionApi.getTransactionById).mockRejectedValueOnce(new Error('网络错误'))

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载交易详情失败')
    const vm = wrapper.vm as any
    expect(vm.loading).toBe(false)
  })

  it('点击关闭应该触发 update:visible 事件', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleClose()

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')![0]).toEqual([false])
  })

  it('应该正确格式化日期', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    const formatted = vm.formatDate('2026-03-14')
    expect(formatted).toContain('2026')
  })

  it('应该正确渲染格式化后的金额', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(wrapper.text()).toContain('¥10,000.00')
  })

  it('应该正确格式化日期时间', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    const formatted = vm.formatDateTime('2026-03-14T10:00:00')
    expect(formatted).toContain('2026')
  })

  it('有分摊数据时 transaction.isAllocated 应为 true', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransaction })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.transaction.isAllocated).toBe(true)
    expect(vm.transaction.allocations).toHaveLength(2)
    expect(vm.transaction.allocations[0].projectName).toBe('项目A')
    expect(vm.transaction.allocations[1].personName).toBe('李四')
  })

  it('无分摊数据时 transaction.isAllocated 应为 false', async () => {
    vi.mocked(transactionApi.getTransactionById).mockResolvedValue(
      mockAxiosResponse({ data: mockTransactionNoAllocation })
    )

    const wrapper = mountDetail()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.transaction.isAllocated).toBe(false)
    expect(vm.transaction.allocations).toHaveLength(0)
  })
})
