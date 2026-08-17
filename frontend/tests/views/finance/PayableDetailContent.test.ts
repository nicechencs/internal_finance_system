import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, reactive } from 'vue'
import { flushPromises, mountWithPlugins } from '@tests/utils'
import { useUserStore } from '@/features/auth/stores/user'
import PayableDetailContent from '@/features/finance/components/PayableDetailContent.vue'
import * as transactionApi from '@/features/transactions/api/transaction'

vi.mock('@/features/transactions/api/transaction')

const payable = {
  id: 1,
  projectId: 31,
  projectName: '椤圭洰B',
  supplierId: 41,
  supplierName: '渚涘簲鍟咥',
  totalAmount: 20000,
  paidAmount: 6000,
  remainingAmount: 14000,
  status: 'partial' as const,
  createdAt: '2026-04-01T00:00:00Z',
  updatedAt: '2026-04-01T00:00:00Z',
  details: []
}

const createPaymentForm = () => reactive({
  paymentDate: '2026-04-03',
  amount: 0,
  paymentMethod: undefined as string | undefined,
  description: undefined as string | undefined,
  transactionId: 0
})

const AlertStub = defineComponent({
  name: 'ElAlert',
  props: {
    title: { type: String, default: '' }
  },
  setup(props, { slots }) {
    return () => h('div', [props.title, slots.default?.()])
  }
})

const mountContent = (paymentForm = createPaymentForm()) => {
  const wrapper = mountWithPlugins(PayableDetailContent, {
    props: {
      payable,
      loading: false,
      submitting: false,
      paymentForm,
      paymentRules: {},
      paymentFormRef: undefined
    },
    global: {
      stubs: {
        ElDescriptions: { template: '<div><slot /></div>' },
        ElDescriptionsItem: { template: '<div><slot /></div>' },
        ElLink: { template: '<a><slot /></a>' },
        ElDivider: { template: '<div><slot /></div>' },
        ElTable: { template: '<div><slot /></div>' },
        ElTableColumn: { template: '<div><slot /></div>' },
        ElForm: { template: '<form><slot /></form>' },
        ElFormItem: { template: '<div><slot /></div>' },
        ElSelect: { template: '<div><slot /></div>' },
        ElOption: { template: '<div><slot /></div>' },
        ElButton: { template: '<button><slot /></button>' },
        ElDatePicker: { template: '<div />' },
        ElInputNumber: { template: '<div />' },
        ElInput: { template: '<div />' },
        ElTag: { template: '<span><slot /></span>' },
        ElAlert: AlertStub,
        RouterLink: { template: '<a><slot /></a>' }
      }
    }
  })

  useUserStore().setUser({
    id: 1,
    username: 'accountant',
    email: 'accountant@example.com',
    fullName: 'Accountant',
    role: 'Accountant',
    isActive: true
  })

  return wrapper
}

describe('PayableDetailContent', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(transactionApi.getAvailableTransactionsForPayable).mockResolvedValue([
      {
        id: 77,
        transactionDate: '2026-04-03',
        transactionType: 'Expense',
        amount: 5200,
        accountId: 1,
        accountName: '璐︽埛B',
        supplierName: '供应商A',
        counterparty: '杭州供应商A',
        description: '采购货款',
        memo: '4月批次',
        status: 'Confirmed',
        isAllocated: false,
        allocations: [],
        tags: [],
        createdAt: '2026-04-03T00:00:00Z',
        availableAmount: 5200
      } as any
    ])
  })

  it('shows a project-link hint after selecting an expense transaction without project', async () => {
    const paymentForm = createPaymentForm()
    const wrapper = mountContent(paymentForm)

    await flushPromises()

    paymentForm.transactionId = 77
    const vm = wrapper.vm as any
    vm.onTransactionSelected()
    await wrapper.vm.$nextTick()

    expect(paymentForm.amount).toBe(5200)
    expect(paymentForm.paymentDate).toBe('2026-04-03')
    expect(vm.showProjectBindingHint).toBe(true)
    expect(wrapper.text()).toContain('当前交易未关联项目，保存登记后会补齐为当前项目。')
    expect(wrapper.text()).toContain('已选交易详情')
    expect(wrapper.text()).toContain('收款方：供应商A')
    expect(wrapper.text()).toContain('银行对方：杭州供应商A')
    expect(wrapper.text()).toContain('备注/摘要：采购货款 / 4月批次')
  })
})
