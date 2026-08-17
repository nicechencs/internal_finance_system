import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import ReceivableDetailPage from '@/features/finance/pages/ReceivableDetailPage.vue'
import * as receivableApi from '@/features/finance/api/receivable'
import { useUserStore } from '@/features/auth/stores/user'

vi.mock('@/features/finance/api/receivable')

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

const ReceivableDetailContentStub = defineComponent({
  name: 'ReceivableDetailContent',
  setup() {
    return () => h('div', 'ReceivableDetailContent')
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

const mockReceivable = {
  id: 1,
  projectId: 11,
  projectName: '项目A',
  customerId: 21,
  customerName: '客户A',
  totalAmount: 10000,
  receivedAmount: 3000,
  remainingAmount: 7000,
  status: 'partial',
  dueDate: '2026-04-10',
  description: '首付款',
  createdAt: '2026-04-01T00:00:00Z',
  updatedAt: '2026-04-01T00:00:00Z',
  details: []
}

const mountPage = () => mountWithPlugins(ReceivableDetailPage, {
  props: {
    visible: false,
    receivableId: 1
  },
  global: {
    stubs: {
      ElDialog: DialogStub,
      ElCard: CardStub,
      ElButton: ButtonStub,
      ElIcon: true,
      ArrowLeft: true,
      ReceivableDetailContent: ReceivableDetailContentStub,
      TransactionForm: TransactionFormStub
    }
  }
})

describe('ReceivableDetailPage quick create', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockReceivable
      })
    )
    vi.mocked(receivableApi.receivePayment).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockReceivable
      })
    )
  })

  it('opens transaction form with receivable draft when quick create is triggered', async () => {
    const wrapper = mountPage()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleCreateNewTransaction()
    await wrapper.vm.$nextTick()

    const form = wrapper.findComponent(TransactionFormStub)
    expect(form.exists()).toBe(true)
    expect(form.props('visible')).toBe(true)
    expect(form.props('draft')).toMatchObject({
      transactionType: 'Income',
      projectId: 11,
      customerId: 21,
      amount: 7000,
      transactionDate: vm.paymentForm.paymentDate
    })
  })

  it('binds the created transaction back to payment form after quick create succeeds', async () => {
    const wrapper = mountPage()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleCreateNewTransaction()
    await wrapper.vm.$nextTick()

    await vm.handleTransactionFormSuccess({
      id: 88,
      transactionDate: '2026-04-02T08:30:00Z',
      amount: 9000,
      availableAmount: 4500
    })
    await flushPromises()

    expect(vm.paymentForm.transactionId).toBe(88)
    expect(vm.paymentForm.paymentDate).toBe('2026-04-02')
    expect(vm.paymentForm.amount).toBe(4500)
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
    vi.mocked(receivableApi.receivePayment).mockImplementation(async (_id, data) => {
      submittedPayload = {
        paymentDate: data.paymentDate,
        amount: data.amount,
        transactionId: data.transactionId
      }
      return mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockReceivable
      }) as any
    })
    vm.setPaymentFormRef({
      validate: vi.fn().mockResolvedValue(true),
      resetFields
    })
    vm.paymentForm.transactionId = 66
    vm.paymentForm.amount = 3200
    vm.paymentForm.paymentDate = '2026-04-03'

    await vm.handleSubmitPayment()
    await flushPromises()

    expect(receivableApi.receivePayment).toHaveBeenCalledOnce()
    expect(submittedPayload).toMatchObject({
      transactionId: 66,
      amount: 3200,
      paymentDate: '2026-04-03'
    })
    expect(resetFields).toHaveBeenCalled()
  })
})
