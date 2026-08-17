import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, reactive } from 'vue'
import { flushPromises, mountWithPlugins } from '@tests/utils'
import { useUserStore } from '@/features/auth/stores/user'
import ReceivableDetailContent from '@/features/finance/components/ReceivableDetailContent.vue'
import * as transactionApi from '@/features/transactions/api/transaction'

vi.mock('@/features/transactions/api/transaction')

const receivable = {
  id: 1,
  projectId: 11,
  projectName: '椤圭洰A',
  customerId: 21,
  customerName: '瀹㈡埛A',
  totalAmount: 10000,
  receivedAmount: 2000,
  remainingAmount: 8000,
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
  const wrapper = mountWithPlugins(ReceivableDetailContent, {
    props: {
      receivable,
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

describe('ReceivableDetailContent', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(transactionApi.getAvailableTransactionsForReceivable).mockResolvedValue([
      {
        id: 66,
        transactionDate: '2026-04-03',
        transactionType: 'Income',
        amount: 3500,
        accountId: 1,
        accountName: '璐︽埛A',
        customerName: '客户A',
        counterparty: '上海客户A',
        description: '合同首付款',
        memo: '银行来账备注',
        status: 'Confirmed',
        isAllocated: false,
        allocations: [],
        tags: [],
        createdAt: '2026-04-03T00:00:00Z',
        availableAmount: 3500
      } as any
    ])
  })

  it('shows a project-link hint after selecting an income transaction without project', async () => {
    const paymentForm = createPaymentForm()
    const wrapper = mountContent(paymentForm)

    await flushPromises()

    paymentForm.transactionId = 66
    const vm = wrapper.vm as any
    vm.onTransactionSelected()
    await wrapper.vm.$nextTick()

    expect(paymentForm.amount).toBe(3500)
    expect(paymentForm.paymentDate).toBe('2026-04-03')
    expect(vm.showProjectBindingHint).toBe(true)
    expect(wrapper.text()).toContain('当前交易未关联项目，保存登记后会补齐为当前项目。')
    expect(wrapper.text()).toContain('已选交易详情')
    expect(wrapper.text()).toContain('付款方：客户A')
    expect(wrapper.text()).toContain('银行对方：上海客户A')
    expect(wrapper.text()).toContain('备注/摘要：合同首付款 / 银行来账备注')
  })
})
