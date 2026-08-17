import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import CustomerForm from '@/views/customers/CustomerForm.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as customerApi from '@/api/customer'
import type { Customer } from '@/types/customer'

vi.mock('@/api/customer')

describe('CustomerForm.vue', () => {
  const mockCustomer: Customer = {
    id: 1,
    name: '测试客户',
    shortName: '客户A',
    contactPerson: '张三',
    contactPhone: '13800138000',
    contactEmail: 'zhangsan@test.com',
    address: '北京市朝阳区',
    taxNumber: '91110000000000001X',
    description: '测试描述',
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z'
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该在新增模式下正确渲染表单', async () => {
    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: true,
        customer: null
      }
    })
    await nextTick()

    expect(wrapper.find('.el-dialog__title').text()).toBe('新增客户')
    expect(wrapper.find('.el-switch').exists()).toBe(false)
  })

  it('应该在编辑模式下正确渲染表单并填充数据', async () => {
    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: false,
        customer: mockCustomer
      }
    })
    await nextTick()

    // 触发 watch，打开对话框
    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(wrapper.find('.el-dialog__title').text()).toBe('编辑客户')

    // 通过 vm 检查数据填充（name 现在是 SearchableInput/el-select，不在普通 input 中）
    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('测试客户')
    expect(vm.formData.contactPhone).toBe('13800138000')

    expect(wrapper.find('.el-switch').exists()).toBe(true)
  })

  it('应该验证必填字段', async () => {
    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: true,
        customer: null
      }
    })
    await nextTick()

    // 通过 vm 实例获取 formRef 并直接调用 validate
    const vm = wrapper.vm as any
    const formRef = vm.formRef || vm.$refs?.formRef
    if (formRef) {
      try {
        await formRef.validate()
      } catch {
        // 验证失败是预期行为
      }
    }

    // 验证 rules 中 name 字段配置了 required
    expect(vm.rules.name).toBeDefined()
    expect(vm.rules.name[0].required).toBe(true)
  })

  it('应该验证邮箱格式', async () => {
    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: true,
        customer: null
      }
    })
    await nextTick()

    // 验证 rules 中 contactEmail 字段配置了 email 类型验证
    const vm = wrapper.vm as any
    expect(vm.rules.contactEmail).toBeDefined()
    expect(vm.rules.contactEmail[0].type).toBe('email')
  })

  it('应该成功创建客户', async () => {
    vi.mocked(customerApi.createCustomer).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockCustomer
      })
    )

    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: true,
        customer: null
      }
    })
    await nextTick()

    // name 字段现在是 SearchableInput (el-select)，通过 vm 直接设置
    const vm = wrapper.vm as any
    vm.formData.name = '测试客户'
    await nextTick()

    const shortNameInput = wrapper.find('input[placeholder="请输入简称"]')
    await shortNameInput.setValue('客户A')

    const contactPersonInput = wrapper.find('input[placeholder="请输入联系人"]')
    await contactPersonInput.setValue('张三')

    const phoneInput = wrapper.find('input[placeholder="请输入联系电话"]')
    await phoneInput.setValue('13800138000')

    const emailInput = wrapper.find('input[placeholder="请输入联系邮箱"]')
    await emailInput.setValue('zhangsan@test.com')

    const submitButton = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitButton!.trigger('click')
    await flushPromises()

    expect(customerApi.createCustomer).toHaveBeenCalledWith(
      expect.objectContaining({
        name: '测试客户',
        shortName: '客户A',
        contactPerson: '张三',
        contactPhone: '13800138000',
        contactEmail: 'zhangsan@test.com'
      })
    )
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
  })

  it('应该成功更新客户', async () => {
    vi.mocked(customerApi.updateCustomer).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockCustomer, name: '修改后的客户名' }
      })
    )

    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: false,
        customer: mockCustomer
      }
    })
    await nextTick()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm2 = wrapper.vm as any
    vm2.formData.name = '修改后的客户名'
    await nextTick()

    const submitButton = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitButton!.trigger('click')
    await flushPromises()

    expect(customerApi.updateCustomer).toHaveBeenCalledWith(
      1,
      expect.objectContaining({
        name: '修改后的客户名',
        isActive: true
      })
    )
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
  })

  it('应该在关闭时触发事件', async () => {
    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: true,
        customer: null
      }
    })
    await nextTick()

    const cancelButton = wrapper.findAll('.el-button').find(b => b.text() === '取消')
    await cancelButton!.trigger('click')

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false])
  })

  it('应该在成功后触发success事件', async () => {
    vi.mocked(customerApi.createCustomer).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockCustomer
      })
    )

    const wrapper = mountWithPlugins(CustomerForm, {
      props: {
        visible: true,
        customer: null
      }
    })
    await nextTick()

    const vm3 = wrapper.vm as any
    vm3.formData.name = '测试客户'
    await nextTick()

    const submitButton = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitButton!.trigger('click')
    await flushPromises()

    expect(wrapper.emitted('success')).toBeTruthy()
  })
})
