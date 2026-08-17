import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import AccountForm from '@/features/master-data/accounts/components/AccountForm.vue'
import * as accountApi from '@/features/master-data/accounts/api/account'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'

vi.mock('@/features/master-data/accounts/api/account')

const mockAccount = {
  id: 1,
  name: '工商银行账户',
  accountType: 'Bank',
  openingBalance: 10000,
  currentBalance: 15000,
  currency: 'CNY',
  bankName: '工商银行',
  accountNumber: '6222021234567890',
  isActive: true,
  createdAt: '2026-03-01T00:00:00Z'
}

const FormStub = defineComponent({
  name: 'ElForm',
  setup(_, { slots, expose }) {
    expose({
      clearValidate: vi.fn(),
      validate: vi.fn().mockResolvedValue(true)
    })

    return () => h('form', slots.default?.())
  }
})

const DialogStub = defineComponent({
  name: 'ElDialog',
  props: ['title'],
  setup(props, { slots }) {
    return () => h('section', [
      h('div', props.title as string),
      slots.default?.(),
      slots.footer?.()
    ])
  }
})

const SlotStub = defineComponent({
  setup(_, { slots }) {
    return () => h('div', slots.default?.())
  }
})

const SelectStub = defineComponent({
  name: 'ElSelect',
  setup(_, { slots }) {
    return () => h('div', slots.default?.())
  }
})

const OptionStub = defineComponent({
  name: 'ElOption',
  props: ['label'],
  setup(props) {
    return () => h('div', props.label as string)
  }
})

const mountForm = (props: Record<string, any>) => {
  return mountWithPlugins(AccountForm, {
    props,
    global: {
      stubs: {
        ElDialog: DialogStub,
        ElForm: FormStub,
        ElFormItem: SlotStub,
        ElSelect: SelectStub,
        ElOption: OptionStub,
        ElInput: SlotStub,
        ElInputNumber: SlotStub,
        ElSwitch: SlotStub,
        ElButton: SlotStub,
        SearchableInput: SlotStub
      }
    }
  })
}

describe('AccountForm.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('新增模式应展示“定期账户”选项文案', async () => {
    const wrapper = mountForm({
      visible: true,
      account: null
    })

    await nextTick()

    expect(wrapper.text()).toContain('新增账户')
    expect(wrapper.text()).toContain('定期账户')
  })

  it('创建定期账户成功后应通过 success 事件回传账户信息', async () => {
    vi.mocked(accountApi.createAccount).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          id: 3,
          name: '三个月定期',
          accountType: 'FixedDeposit'
        }
      })
    )

    const wrapper = mountForm({
      visible: true,
      account: null
    })

    await nextTick()

    const vm = wrapper.vm as any
    vm.formData.name = '三个月定期'
    vm.formData.accountType = 'FixedDeposit'
    vm.formData.initialBalance = 200000
    vm.formData.currency = 'CNY'
    vm.formData.bankName = '招商银行'
    vm.formData.accountNumber = '6222000000000001'

    await vm.handleSubmit()
    await flushPromises()

    expect(accountApi.createAccount).toHaveBeenCalledWith({
      name: '三个月定期',
      accountType: 'FixedDeposit',
      initialBalance: 200000,
      currency: 'CNY',
      bankName: '招商银行',
      accountNumber: '6222000000000001'
    })
    expect(wrapper.emitted('success')?.[0]).toEqual([{
      accountId: 3,
      accountType: 'FixedDeposit',
      name: '三个月定期'
    }])
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
  })

  it('编辑模式应回填账户信息并允许更新状态', async () => {
    vi.mocked(accountApi.updateAccount).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          ...mockAccount,
          name: '工商银行主账户',
          isActive: false
        }
      })
    )

    const wrapper = mountForm({
      visible: false,
      account: mockAccount
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('工商银行账户')
    expect(vm.formData.bankName).toBe('工商银行')
    expect(vm.formData.accountNumber).toBe('6222021234567890')

    vm.formData.name = '工商银行主账户'
    vm.formData.isActive = false

    await vm.handleSubmit()
    await flushPromises()

    expect(accountApi.updateAccount).toHaveBeenCalledWith(1, {
      name: '工商银行主账户',
      isActive: false,
      bankName: '工商银行',
      accountNumber: '6222021234567890'
    })
    expect(wrapper.emitted('success')?.[0]).toEqual([undefined])
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
  })
})
