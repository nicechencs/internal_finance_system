import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ElMessage } from 'element-plus'
import SupplierDetail from '@/features/master-data/suppliers/pages/SupplierDetailPage.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as supplierApi from '@/features/master-data/suppliers/api/supplier'
import * as transactionApi from '@/features/transactions/api/transaction'
import type { Supplier } from '@/features/master-data/suppliers/types/supplier'
import type { Transaction } from '@/features/transactions/types/transaction'

vi.mock('@/api/supplier')
vi.mock('@/api/transaction')

// Mock vue-router 的 useRoute 和 useRouter（与 PersonDetail 测试一致）
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

describe('SupplierDetail.vue', () => {
  const mockSupplier: Supplier = {
    id: 1,
    name: '测试供应商',
    shortName: '供应商简称',
    contactPerson: '张三',
    contactPhone: '13800138000',
    contactEmail: 'test@example.com',
    address: '北京市朝阳区',
    taxNumber: '91110000000000001X',
    bankAccount: '6222000000000001',
    bankName: '中国银行',
    description: '测试描述',
    isActive: true,
    createdAt: '2024-01-15T10:30:00Z'
  }

  const mockTransactions: Transaction[] = [
    {
      id: 101,
      transactionDate: '2024-02-01',
      transactionType: 'Expense',
      amount: 5000.00,
      accountId: 1,
      accountName: '主账户',
      categoryId: 1,
      categoryName: '采购费',
      projectId: 1,
      projectName: '项目A',
      customerId: null,
      customerName: null,
      supplierId: 1,
      supplierName: '测试供应商',
      personId: null,
      personName: null,
      description: '采购原材料',
      createdAt: '2024-02-01T00:00:00Z'
    } as any,
    {
      id: 102,
      transactionDate: '2024-02-15',
      transactionType: 'Income',
      amount: 1000.00,
      accountId: 1,
      accountName: '主账户',
      categoryId: 2,
      categoryName: '退款',
      projectId: 1,
      projectName: '项目B',
      customerId: null,
      customerName: null,
      supplierId: 1,
      supplierName: '测试供应商',
      personId: null,
      personName: null,
      description: '退货退款',
      createdAt: '2024-02-15T00:00:00Z'
    } as any,
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    mockRouteParams.id = '1'
    vi.spyOn(supplierApi, 'getSupplierById').mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockSupplier
      }) as any
    )
    vi.spyOn(transactionApi, 'getTransactionsBySupplier').mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockTransactions
      }) as any
    )
    vi.spyOn(transactionApi, 'getSupplierTransactionStatistics').mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          totalIncome: 1000,
          totalExpense: 5000,
          netProfit: -4000,
          totalTransfer: 0,
          incomeCount: 1,
          expenseCount: 1,
          transferCount: 0,
          totalCount: 2
        }
      }) as any
    )
  })

  it('应该正确加载并显示供应商详情', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    expect(supplierApi.getSupplierById).toHaveBeenCalledWith(1)
    expect(wrapper.text()).toContain('测试供应商')
    expect(wrapper.text()).toContain('供应商简称')
    expect(wrapper.text()).toContain('张三')
    expect(wrapper.text()).toContain('13800138000')
    expect(wrapper.text()).toContain('test@example.com')
  })

  it('应该显示供应商状态标签', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    const tags = wrapper.findAll('.el-tag')
    const tagTexts = tags.map(t => t.text())
    expect(tagTexts).toContain('启用')
  })

  it('应该加载并显示关联交易记录', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    expect(transactionApi.getTransactionsBySupplier).toHaveBeenCalledWith(1)

    const text = wrapper.text()
    expect(text).toContain('交易记录')
    expect(text).toContain('采购原材料')
    expect(text).toContain('退货退款')
    expect(text).toContain('5,000.00')
    expect(text).toContain('1,000.00')
  })

  it('应该正确显示交易类型标签', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    const tags = wrapper.findAll('.el-tag')
    const tagTexts = tags.map(t => t.text())
    expect(tagTexts).toContain('收入')
    expect(tagTexts).toContain('支出')
  })

  it('应该在加载失败时显示错误消息', async () => {
    vi.spyOn(supplierApi, 'getSupplierById').mockRejectedValue(new Error('加载失败'))

    mountWithPlugins(SupplierDetail)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载供应商详情失败')
  })

  it('应该在交易加载失败时显示错误消息', async () => {
    vi.spyOn(transactionApi, 'getTransactionsBySupplier').mockRejectedValue(
      new Error('加载失败')
    )

    mountWithPlugins(SupplierDetail)
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载交易记录失败')
  })

  it('应该支持返回按钮', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    const backButton = wrapper.find('.el-button')
    await backButton.trigger('click')

    expect(mockBack).toHaveBeenCalled()
  })

  it('应该显示所有描述信息字段', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('供应商名称')
    expect(text).toContain('简称')
    expect(text).toContain('联系人')
    expect(text).toContain('联系电话')
    expect(text).toContain('联系邮箱')
    expect(text).toContain('地址')
    expect(text).toContain('税号')
    expect(text).toContain('银行账号')
    expect(text).toContain('开户行')
    expect(text).toContain('状态')
    expect(text).toContain('创建时间')
  })

  it('应该渲染页面标题和返回按钮', async () => {
    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    expect(wrapper.text()).toContain('供应商详情')
    expect(wrapper.text()).toContain('返回')
  })

  it('供应商数据为空时应显示占位符', async () => {
    vi.spyOn(supplierApi, 'getSupplierById').mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          id: 1,
          name: '空数据供应商',
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z'
        }
      }) as any
    )

    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('空数据供应商')
    expect(text).toContain('-')
  })

  it('should display transaction tags in the record table', async () => {
    vi.spyOn(transactionApi, 'getTransactionsBySupplier').mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: [
          { ...mockTransactions[0], tags: [{ tagId: 41, tagName: '原材', tagColor: '#F56C6C' }] },
          { ...mockTransactions[1], tags: [{ tagId: 42, tagName: '退款', tagColor: '#67C23A' }] }
        ]
      }) as any
    )

    const wrapper = mountWithPlugins(SupplierDetail)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('原材')
    expect(text).toContain('退款')
  })
})
