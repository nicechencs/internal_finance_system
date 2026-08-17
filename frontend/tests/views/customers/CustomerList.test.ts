import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import CustomerList from '@/views/customers/CustomerList.vue'
import CustomerForm from '@/views/customers/CustomerForm.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as customerApi from '@/api/customer'
import type { Customer } from '@/types/customer'

vi.mock('@/api/customer')

describe('CustomerList.vue', () => {
  const mockCustomers: Customer[] = [
    {
      id: 1,
      name: '测试客户A',
      shortName: '客户A',
      contactPerson: '张三',
      contactPhone: '13800138000',
      contactEmail: 'zhangsan@test.com',
      address: '北京市朝阳区',
      taxNumber: '91110000000000001X',
      description: '测试客户A描述',
      isActive: true,
      createdAt: '2024-01-01T00:00:00Z'
    },
    {
      id: 2,
      name: '测试客户B',
      shortName: '客户B',
      contactPerson: '李四',
      contactPhone: '13900139000',
      contactEmail: 'lisi@test.com',
      address: '上海市浦东新区',
      taxNumber: '91310000000000002Y',
      description: '测试客户B描述',
      isActive: false,
      createdAt: '2024-01-02T00:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(customerApi.getCustomers).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          items: JSON.parse(JSON.stringify(mockCustomers)), // 深拷贝避免测试间状态泄漏
          total: 2,
          page: 1,
          pageSize: 20
        }
      })
    )
  })

  it('应该正确渲染客户列表', async () => {
    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()

    expect(customerApi.getCustomers).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20
    })

    await nextTick()
    const rows = wrapper.findAll('tbody tr')
    expect(rows.length).toBe(2)
    expect(wrapper.text()).toContain('测试客户A')
    expect(wrapper.text()).toContain('测试客户B')
  })

  it('应该支持搜索功能', async () => {
    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.searchForm.name = '测试客户A'
    vm.searchForm.contactPerson = '张三'

    const searchButton = wrapper.findAll('.el-button').find(b => b.text() === '查询')
    await searchButton!.trigger('click')
    await flushPromises()

    expect(customerApi.getCustomers).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 20,
      name: '测试客户A',
      contactPerson: '张三',
      contactPhone: undefined
    })
  })

  it('应该支持通过弹窗编辑客户', async () => {
    vi.mocked(customerApi.updateCustomer).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockCustomers[0], name: '修改后的客户名' }
      })
    )

    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    vm.handleEdit(vm.tableData[0])
    await nextTick()

    const form = wrapper.findComponent(CustomerForm)
    expect(form.props('visible')).toBe(true)

    const formVm = form.vm as any
    formVm.formData.name = '修改后的客户名'

    await formVm.handleSubmit()
    await flushPromises()

    expect(customerApi.updateCustomer).toHaveBeenCalled()
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
    expect(customerApi.getCustomers).toHaveBeenCalledTimes(2)
  })

  it('应该支持通过弹窗新增客户', async () => {
    vi.mocked(customerApi.createCustomer).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockCustomers[0], id: 3, name: '新客户' }
      })
    )

    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    vm.handleCreate()
    await nextTick()

    const form = wrapper.findComponent(CustomerForm)
    expect(form.props('visible')).toBe(true)

    const formVm = form.vm as any
    formVm.formData.name = '新客户'

    await formVm.handleSubmit()
    await flushPromises()

    expect(customerApi.createCustomer).toHaveBeenCalled()
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    expect(customerApi.getCustomers).toHaveBeenCalledTimes(2)
  })

  it('应该支持删除客户', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(customerApi.deleteCustomer).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: null })
    )

    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    // 直接调用 handleDelete 方法
    vm.handleDelete(vm.tableData[0])
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      '确定要删除该客户吗？',
      '提示',
      expect.any(Object)
    )
    expect(customerApi.deleteCustomer).toHaveBeenCalledWith(1)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('应该支持启用/停用客户', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(customerApi.updateCustomer).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockCustomers[0], isActive: false }
      })
    )

    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    // 直接调用 handleToggleStatus 方法
    vm.handleToggleStatus(vm.tableData[0])
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      '确定要停用该客户吗？',
      '提示',
      expect.any(Object)
    )
    expect(customerApi.updateCustomer).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ isActive: false })
    )
    expect(ElMessage.success).toHaveBeenCalledWith('停用成功')
  })

  it('应该支持重置搜索', async () => {
    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.searchForm.name = '测试'

    const resetButton = wrapper.findAll('.el-button').find(b => b.text() === '重置')
    await resetButton!.trigger('click')
    await flushPromises()

    expect(vm.searchForm.name).toBe('')
    expect(customerApi.getCustomers).toHaveBeenCalledTimes(2)
  })

  it('查询按钮应该重置页码到第1页', async () => {
    const wrapper = mountWithPlugins(CustomerList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.pagination.page = 3
    vm.searchForm.name = '测试'

    const searchButton = wrapper.findAll('.el-button').find(b => b.text() === '查询')
    await searchButton!.trigger('click')
    await flushPromises()

    expect(vm.pagination.page).toBe(1)
    expect(customerApi.getCustomers).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 1, name: '测试' })
    )
  })
})
