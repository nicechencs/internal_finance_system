import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { ElMessage } from 'element-plus'
import TransactionForm from '@/features/transactions/components/TransactionForm.vue'
import * as transactionApi from '@/features/transactions/api/transaction'
import * as accountApi from '@/features/master-data/accounts/api/account'
import * as categoryApi from '@/features/master-data/categories/api/category'
import * as projectApi from '@/features/master-data/projects/api/project'
import * as customerApi from '@/features/master-data/customers/api/customer'
import * as supplierApi from '@/features/master-data/suppliers/api/supplier'
import * as personApi from '@/features/master-data/persons/api/person'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'

vi.mock('@/features/transactions/api/transaction')
vi.mock('@/features/master-data/accounts/api/account')
vi.mock('@/features/master-data/categories/api/category')
vi.mock('@/features/master-data/projects/api/project')
vi.mock('@/features/master-data/customers/api/customer')
vi.mock('@/features/master-data/suppliers/api/supplier')
vi.mock('@/features/master-data/persons/api/person')

const activeAccounts = [
  {
    id: 1,
    name: '工商银行',
    accountType: 'Bank',
    openingBalance: 10000,
    currentBalance: 15000,
    currency: 'CNY',
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
    isActive: true,
    createdAt: '2026-03-01T00:00:00Z'
  },
  {
    id: 3,
    name: '支付宝',
    accountType: 'Alipay',
    openingBalance: 3000,
    currentBalance: 3200,
    currency: 'CNY',
    isActive: true,
    createdAt: '2026-03-01T00:00:00Z'
  }
]

const mockTransaction = {
  id: 100,
  transactionDate: '2026-03-20T00:00:00Z',
  transactionType: 'Expense' as const,
  amount: 5000,
  accountId: 2,
  accountName: '三个月定期',
  categoryId: 11,
  categoryName: '办公费用',
  projectId: 21,
  projectName: '项目A',
  description: '历史定期支出',
  status: 'Completed',
  isAllocated: false,
  allocations: [],
  tags: [],
  createdAt: '2026-03-20T00:00:00Z'
}

const mountForm = (props: Record<string, any> = {}) => {
  const SlotStub = defineComponent({
    setup(_, { slots }) {
      return () => h('div', slots.default?.())
    }
  })

  return mountWithPlugins(TransactionForm, {
    props: {
      visible: false,
      transaction: null,
      ...props
    },
    global: {
      stubs: {
        ElDialog: defineComponent({
          name: 'ElDialog',
          props: ['title'],
          setup(props, { slots }) {
            return () => h('section', [
              h('div', props.title as string),
              slots.default?.(),
              slots.footer?.()
            ])
          }
        }),
        ElForm: defineComponent({
          name: 'ElForm',
          setup(_, { slots, expose }) {
            expose({
              validate: vi.fn((callback: (valid: boolean) => void) => callback(true)),
              clearValidate: vi.fn()
            })

            return () => h('form', slots.default?.())
          }
        }),
        ElFormItem: SlotStub,
        ElDatePicker: SlotStub,
        ElRadioGroup: SlotStub,
        ElRadio: SlotStub,
        ElInputNumber: SlotStub,
        ElSelect: SlotStub,
        ElOption: SlotStub,
        ElInput: SlotStub,
        ElDivider: SlotStub,
        ElButton: SlotStub,
        ElText: SlotStub,
        ElCard: SlotStub,
        ElAlert: SlotStub,
        SearchableSelect: SlotStub
      }
    }
  })
}

describe('TransactionForm.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(accountApi.getActiveAccounts).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: activeAccounts
      })
    )
    vi.mocked(categoryApi.getActiveCategories).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: [
          { id: 11, name: '办公费用', categoryType: 'Expense' },
          { id: 12, name: '服务收入', categoryType: 'Income' }
        ]
      })
    )
    vi.mocked(projectApi.getActiveProjects).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [{ id: 21, name: '项目A' }] })
    )
    vi.mocked(customerApi.getActiveCustomers).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )
    vi.mocked(supplierApi.getActiveSuppliers).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )
    vi.mocked(personApi.getActivePersons).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: [] })
    )
  })

  it('新增交易时应过滤掉定期账户，并显示台账提示', async () => {
    const wrapper = mountForm()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(accountApi.getActiveAccounts).toHaveBeenCalledTimes(1)
    expect(vm.transactionAccounts.map((account: any) => account.id)).toEqual([1, 3])
    expect(wrapper.text()).toContain('仅展示可用于经营收支的账户，定期账户请前往定期台账。')
  })

  it('编辑历史交易时应保留当前定期账户作为可选项', async () => {
    const wrapper = mountForm({ transaction: mockTransaction })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.transactionAccounts.map((account: any) => account.id)).toEqual([1, 2, 3])
    expect(vm.form.accountId).toBe(2)
  })

  it('应根据交易类型过滤分类，并在切换类型时清空不匹配的分类', async () => {
    const wrapper = mountForm()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any

    vm.form.transactionType = 'Expense'
    await wrapper.vm.$nextTick()
    expect(vm.filteredCategories.map((category: any) => category.id)).toEqual([11])

    vm.form.categoryId = 11
    vm.form.transactionType = 'Income'
    await wrapper.vm.$nextTick()

    expect(vm.filteredCategories.map((category: any) => category.id)).toEqual([12])
    expect(vm.form.categoryId).toBeUndefined()
  })

  it('提交新增交易时应继续按筛选后的账户发起创建', async () => {
    vi.mocked(transactionApi.createTransaction).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockTransaction
      })
    )

    const wrapper = mountForm()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.transactionDate = '2026-03-20'
    vm.form.transactionType = 'Expense'
    vm.form.amount = 1200
    vm.form.accountId = 1
    vm.form.categoryId = 11
    vm.form.description = '办公采购'

    await vm.handleSubmit()
    await flushPromises()

    expect(transactionApi.createTransaction).toHaveBeenCalledWith(expect.objectContaining({
      transactionDate: '2026-03-20',
      transactionType: 'Expense',
      amount: 1200,
      accountId: 1,
      categoryId: 11,
      description: '办公采购'
    }))
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    expect(wrapper.emitted('success')).toBeTruthy()
  })
  it('supports prefilling a create draft', async () => {
    const wrapper = mountForm({
      draft: {
        transactionDate: '2026-04-03',
        transactionType: 'Income',
        amount: 3200,
        projectId: 21,
        customerId: 9,
        description: '蹇嵎鍒涘缓'
      }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.form.transactionDate).toBe('2026-04-03')
    expect(vm.form.transactionType).toBe('Income')
    expect(vm.form.amount).toBe(3200)
    expect(vm.form.projectId).toBe(21)
    expect(vm.form.counterpartyType).toBe('customer')
    expect(vm.form.customerId).toBe(9)
    expect(vm.form.description).toBe('蹇嵎鍒涘缓')
  })

  it('emits the created transaction payload after create succeeds', async () => {
    vi.mocked(transactionApi.createTransaction).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          ...mockTransaction,
          id: 321,
          transactionType: 'Income'
        }
      })
    )

    const wrapper = mountForm()
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.transactionDate = '2026-04-03'
    vm.form.transactionType = 'Income'
    vm.form.amount = 3200
    vm.form.accountId = 1

    await vm.handleSubmit()
    await flushPromises()

    expect(wrapper.emitted('success')?.[0]?.[0]).toMatchObject({
      id: 321,
      transactionType: 'Income'
    })
  })
})
