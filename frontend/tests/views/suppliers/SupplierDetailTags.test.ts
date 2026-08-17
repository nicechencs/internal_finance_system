import { describe, expect, it, vi, beforeEach } from 'vitest'
import SupplierDetailPage from '@/features/master-data/suppliers/pages/SupplierDetailPage.vue'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import * as supplierApi from '@/features/master-data/suppliers/api/supplier'
import * as transactionApi from '@/features/transactions/api/transaction'

vi.mock('@/features/master-data/suppliers/api/supplier')
vi.mock('@/features/transactions/api/transaction')

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

describe('SupplierDetailPage transaction tags', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockRouteParams.id = '1'
    vi.mocked(supplierApi.getSupplierById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          id: 1,
          name: '测试供应商',
          shortName: '供应商A',
          contactPerson: '张三',
          contactPhone: '13800138000',
          contactEmail: 'supplier@example.com',
          address: '北京市朝阳区',
          taxNumber: '91110000000000001X',
          bankAccount: '6222000000000001',
          bankName: '中国银行',
          isActive: true,
          createdAt: '2024-01-15T10:30:00Z'
        }
      }) as any
    )
    vi.mocked(transactionApi.getSupplierTransactionStatistics).mockResolvedValue(
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
    vi.mocked(transactionApi.getTransactionsBySupplier).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: [
          {
            id: 101,
            transactionDate: '2024-02-01',
            transactionType: 'Expense',
            amount: 5000,
            accountId: 1,
            accountName: '主账户',
            categoryId: 1,
            categoryName: '采购费',
            projectId: 1,
            projectName: '项目A',
            supplierId: 1,
            supplierName: '测试供应商',
            description: '采购原材料',
            status: 'Completed',
            isAllocated: false,
            allocations: [],
            tags: [
              { tagId: 61, tagName: '月结', tagColor: '#F56C6C' }
            ],
            createdAt: '2024-02-01T00:00:00Z'
          },
          {
            id: 102,
            transactionDate: '2024-02-15',
            transactionType: 'Income',
            amount: 1000,
            accountId: 1,
            accountName: '主账户',
            categoryId: 2,
            categoryName: '退款',
            projectId: 1,
            projectName: '项目B',
            supplierId: 1,
            supplierName: '测试供应商',
            description: '退货返款',
            status: 'Completed',
            isAllocated: false,
            allocations: [],
            tags: [
              { tagId: 62, tagName: '白名单', tagColor: '#67C23A' }
            ],
            createdAt: '2024-02-15T00:00:00Z'
          }
        ]
      }) as any
    )
  })

  it('should display transaction tags in the record table', async () => {
    const wrapper = mountWithPlugins(SupplierDetailPage)
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('月结')
    expect(text).toContain('白名单')
  })
})
