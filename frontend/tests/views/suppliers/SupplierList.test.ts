import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import SupplierList from '@/views/suppliers/SupplierList.vue'
import SupplierForm from '@/views/suppliers/SupplierForm.vue'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import * as supplierApi from '@/api/supplier'
import type { Supplier } from '@/types/supplier'

vi.mock('@/api/supplier')

describe('SupplierList.vue', () => {
  const mockSuppliers: Supplier[] = [
    {
      id: 1,
      name: '测试供应商A',
      shortName: '供应商A',
      contactPerson: '张三',
      contactPhone: '13800138000',
      contactEmail: 'zhangsan@test.com',
      address: '北京市朝阳区',
      taxNumber: '91110000000000001X',
      bankAccount: '6222000000000001',
      bankName: '中国银行',
      description: '测试供应商A描述',
      isActive: true,
      createdAt: '2024-01-01T00:00:00Z'
    },
    {
      id: 2,
      name: '测试供应商B',
      shortName: '供应商B',
      contactPerson: '李四',
      contactPhone: '13900139000',
      contactEmail: 'lisi@test.com',
      address: '上海市浦东新区',
      taxNumber: '91310000000000002Y',
      bankAccount: '6222000000000002',
      bankName: '工商银行',
      description: '测试供应商B描述',
      isActive: false,
      createdAt: '2024-01-02T00:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(supplierApi.getSuppliers).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          items: mockSuppliers,
          total: 2,
          page: 1,
          pageSize: 20
        }
      })
    )
  })

  it('应该正确渲染供应商列表', async () => {
    const wrapper = mountWithPlugins(SupplierList)
    await flushPromises()
    await nextTick()

    expect(supplierApi.getSuppliers).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20
    })
    expect(wrapper.text()).toContain('测试供应商A')
    expect(wrapper.text()).toContain('测试供应商B')
  })

  it('应该支持通过弹窗编辑供应商', async () => {
    vi.mocked(supplierApi.updateSupplier).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockSuppliers[0], name: '修改后的供应商名称' }
      })
    )

    const wrapper = mountWithPlugins(SupplierList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    vm.handleEdit(vm.tableData[0])
    await nextTick()

    const form = wrapper.findComponent(SupplierForm)
    expect(form.props('visible')).toBe(true)

    const formVm = form.vm as any
    formVm.formData.name = '修改后的供应商名称'

    await formVm.handleSubmit()
    await flushPromises()

    expect(supplierApi.updateSupplier).toHaveBeenCalled()
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
    expect(supplierApi.getSuppliers).toHaveBeenCalledTimes(2)
  })

  it('应该支持通过弹窗新增供应商', async () => {
    vi.mocked(supplierApi.createSupplier).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: { ...mockSuppliers[0], id: 3, name: '新供应商' }
      })
    )

    const wrapper = mountWithPlugins(SupplierList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    vm.handleCreate()
    await nextTick()

    const form = wrapper.findComponent(SupplierForm)
    expect(form.props('visible')).toBe(true)

    const formVm = form.vm as any
    formVm.formData.name = '新供应商'
    formVm.formData.contactPerson = '王五'

    await formVm.handleSubmit()
    await flushPromises()

    expect(supplierApi.createSupplier).toHaveBeenCalled()
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    expect(supplierApi.getSuppliers).toHaveBeenCalledTimes(2)
  })

  it('应该支持删除供应商', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(supplierApi.deleteSupplier).mockResolvedValue(
      mockAxiosResponse({ code: 200, message: 'success', data: null })
    )

    const wrapper = mountWithPlugins(SupplierList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    await vm.handleDelete({ id: 1, name: '测试供应商A' })
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      '确定要删除该供应商吗？',
      '提示',
      expect.any(Object)
    )
    expect(supplierApi.deleteSupplier).toHaveBeenCalledWith(1)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('应该支持重置搜索', async () => {
    const wrapper = mountWithPlugins(SupplierList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.searchForm.name = '测试'
    vm.searchForm.contactPerson = '张三'
    vm.searchForm.contactPhone = '13800138000'

    vm.handleReset()
    await flushPromises()

    expect(vm.searchForm.name).toBe('')
    expect(vm.searchForm.contactPerson).toBe('')
    expect(vm.searchForm.contactPhone).toBe('')
    expect(supplierApi.getSuppliers).toHaveBeenCalledTimes(2)
  })

  it('查询按钮应该传递筛选参数并重置页码', async () => {
    const wrapper = mountWithPlugins(SupplierList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.pagination.page = 3
    vm.searchForm.name = '华为'
    vm.searchForm.contactPerson = '张三'

    const searchButton = wrapper.findAll('.el-button').find(b => b.text() === '查询')
    await searchButton!.trigger('click')
    await flushPromises()

    expect(vm.pagination.page).toBe(1)
    expect(supplierApi.getSuppliers).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 20,
      name: '华为',
      contactPerson: '张三',
      contactPhone: undefined
    })
  })
})
