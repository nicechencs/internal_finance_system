import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import SupplierForm from '@/views/suppliers/SupplierForm.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as supplierApi from '@/api/supplier'
import type { Supplier } from '@/types/supplier'

vi.mock('@/api/supplier')

describe('SupplierForm.vue', () => {
  const mockSupplier: Supplier = {
    id: 1,
    name: '测试供应商',
    shortName: '供应商',
    contactPerson: '张三',
    contactPhone: '13800138000',
    contactEmail: 'test@example.com',
    address: '北京市朝阳区',
    taxNumber: '91110000000000001X',
    bankAccount: '6222000000000001',
    bankName: '中国银行',
    description: '测试描述',
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z'
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该在新增模式下正确渲染表单', async () => {
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    expect(wrapper.find('.el-dialog__title').text()).toBe('新增供应商')
    expect(wrapper.find('.el-switch').exists()).toBe(false)
  })

  it('应该在编辑模式下正确渲染表单', async () => {
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: mockSupplier
      }
    })
    await nextTick()
    await flushPromises()

    expect(wrapper.find('.el-dialog__title').text()).toBe('编辑供应商')
    expect(wrapper.find('.el-switch').exists()).toBe(true)
  })

  it('应该验证必填字段规则', async () => {
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.rules.name).toBeDefined()
    expect(vm.rules.name[0].required).toBe(true)
  })

  it('应该验证邮箱格式规则', async () => {
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.rules.contactEmail).toBeDefined()
    expect(vm.rules.contactEmail[0].type).toBe('email')
  })

  it('应该成功创建供应商', async () => {
    vi.mocked(supplierApi.createSupplier).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockSupplier
      })
    )

    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    const vm = wrapper.vm as any
    vm.formData.name = '测试供应商'
    vm.formData.shortName = '供应商'
    vm.formData.contactPerson = '张三'
    vm.formData.contactPhone = '13800138000'
    vm.formData.contactEmail = 'test@example.com'
    await nextTick()

    await vm.handleSubmit()
    await flushPromises()

    expect(supplierApi.createSupplier).toHaveBeenCalledWith(
      expect.objectContaining({
        name: '测试供应商',
        shortName: '供应商',
        contactPerson: '张三'
      })
    )
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
  })

  it('应该成功更新供应商', async () => {
    vi.mocked(supplierApi.updateSupplier).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockSupplier, name: '修改后的供应商名' }
      })
    )

    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: mockSupplier
      }
    })
    await nextTick()

    const vm = wrapper.vm as any
    vm.formData.name = '修改后的供应商名'
    await nextTick()

    await vm.handleSubmit()
    await flushPromises()

    expect(supplierApi.updateSupplier).toHaveBeenCalledWith(
      1,
      expect.objectContaining({
        name: '修改后的供应商名',
        isActive: true
      })
    )
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
  })

  it('应该在关闭时触发事件', async () => {
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    const cancelButton = wrapper.findAll('.el-button')[0]
    await cancelButton.trigger('click')

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false])
  })

  it('应该在创建成功后触发success事件', async () => {
    vi.mocked(supplierApi.createSupplier).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: mockSupplier
      })
    )

    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    const vm = wrapper.vm as any
    vm.formData.name = '测试供应商'
    await nextTick()

    await vm.handleSubmit()
    await flushPromises()

    expect(wrapper.emitted('success')).toBeTruthy()
  })

  it('编辑模式应填充供应商数据', async () => {
    // 先以 visible=false 挂载，再设置 visible=true 触发 watch
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: false,
        supplier: mockSupplier
      }
    })
    await nextTick()

    await wrapper.setProps({ visible: true })
    await nextTick()
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('测试供应商')
    expect(vm.formData.contactPerson).toBe('张三')
    expect(vm.formData.contactPhone).toBe('13800138000')
    expect(vm.formData.contactEmail).toBe('test@example.com')
    expect(vm.formData.isActive).toBe(true)
  })

  it('新增模式应初始化空表单', async () => {
    const wrapper = mountWithPlugins(SupplierForm, {
      props: {
        visible: true,
        supplier: null
      }
    })
    await nextTick()

    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('')
    expect(vm.formData.contactPerson).toBe('')
    expect(vm.formData.contactPhone).toBe('')
    expect(vm.formData.isActive).toBe(true)
    expect(vm.isEdit).toBe(false)
  })
})
