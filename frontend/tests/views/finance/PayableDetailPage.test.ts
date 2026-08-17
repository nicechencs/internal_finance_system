import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import PayableDetailPage from '@/features/finance/pages/PayableDetailPage.vue'
import * as payableApi from '@/features/finance/api/payable'
import { useUserStore } from '@/features/auth/stores/user'

vi.mock('@/features/finance/api/payable')

const DialogStub = defineComponent({
  name: 'ElDialog',
  setup(_, { slots }) {
    return () => h('section', slots.default?.())
  }
})

const CardStub = defineComponent({
  name: 'ElCard',
  setup(_, { slots }) {
    return () => h('section', slots.default?.())
  }
})

const ButtonStub = defineComponent({
  name: 'ElButton',
  setup(_, { slots }) {
    return () => h('button', slots.default?.())
  }
})

const PayableDetailContentStub = defineComponent({
  name: 'PayableDetailContent',
  setup() {
    return () => h('div', 'PayableDetailContent')
  }
})

const TransactionFormStub = defineComponent({
  name: 'TransactionForm',
  props: {
    visible: { type: Boolean, default: false },
    draft: { type: Object, default: null }
  },
  setup(props) {
    return () => h('div', {
      'data-testid': 'transaction-form',
      'data-visible': String(props.visible)
    })
  }
})

const mockPayable = {
  id: 1,
  projectId: 31,
  projectName: '项目B',
  supplierId: 41,
  supplierName: '供应商A',
  totalAmount: 20000,
  paidAmount: 5000,
  remainingAmount: 15000,
  status: 'partial',
  dueDate: '2026-04-12',
  description: '采购款',
  createdAt: '2026-04-01T00:00:00Z',
  updatedAt: '2026-04-01T00:00:00Z',
  details: []
}

const mountPage = () => mountWithPlugins(PayableDetailPage, {
  props: {
    visible: false,
    payableId: 1
  },
  global: {
    stubs: {
      ElDialog: DialogStub,
      ElCard: CardStub,
      ElButton: ButtonStub,
      ElIcon: true,
      ArrowLeft: true,
      PayableDetailContent: PayableDetailContentStub,
      TransactionForm: TransactionFormStub
    }
  }
})

describe('PayableDetailPage quick create', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockPayable
      })
    )
    vi.mocked(payableApi.payPayment).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockPayable
      })
    )
  })

  it('opens transaction form with payable draft when quick create is triggered', async () => {
    const wrapper = mountPage()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleCreateTransaction()
    await wrapper.vm.$nextTick()

    const form = wrapper.findComponent(TransactionFormStub)
    expect(form.exists()).toBe(true)
    expect(form.props('visible')).toBe(true)
    expect(form.props('draft')).toMatchObject({
      transactionType: 'Expense',
      projectId: 31,
      supplierId: 41,
      amount: 15000,
      transactionDate: vm.paymentForm.paymentDate
    })
  })

  it('binds the created transaction back to payment form after quick create succeeds', async () => {
    const wrapper = mountPage()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleCreateTransaction()
    await wrapper.vm.$nextTick()

    await vm.handleTransactionFormSuccess({
      id: 98,
      transactionDate: '2026-04-05T09:15:00Z',
      amount: 16000,
      availableAmount: 3200
    })
    await flushPromises()

    expect(vm.paymentForm.transactionId).toBe(98)
    expect(vm.paymentForm.paymentDate).toBe('2026-04-05')
    expect(vm.paymentForm.amount).toBe(3200)
  })

  it('submits payment after form ref is initialized', async () => {
    const wrapper = mountPage()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    useUserStore().setUser({
      id: 1,
      username: 'accountant',
      email: 'accountant@example.com',
      fullName: 'Accountant',
      role: 'Accountant',
      isActive: true
    })

    const vm = wrapper.vm as any
    const resetFields = vi.fn()
    let submittedPayload: any = null
    vi.mocked(payableApi.payPayment).mockImplementation(async (_id, data) => {
      submittedPayload = {
        paymentDate: data.paymentDate,
        amount: data.amount,
        transactionId: data.transactionId
      }
      return mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockPayable
      }) as any
    })
    vm.setPaymentFormRef({
      validate: vi.fn().mockResolvedValue(true),
      resetFields
    })
    vm.paymentForm.transactionId = 77
    vm.paymentForm.amount = 2800
    vm.paymentForm.paymentDate = '2026-04-04'

    await vm.handleSubmitPayment()
    await flushPromises()

    expect(payableApi.payPayment).toHaveBeenCalledOnce()
    expect(submittedPayload).toMatchObject({
      transactionId: 77,
      amount: 2800,
      paymentDate: '2026-04-04'
    })
    expect(resetFields).toHaveBeenCalled()
  })
})
