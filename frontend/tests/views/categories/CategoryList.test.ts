import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import { ElMessage, ElMessageBox } from 'element-plus'
import CategoryList from '@/views/categories/CategoryList.vue'
import * as categoryApi from '@/api/category'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/category')

describe('CategoryList.vue', () => {
  const mockCategories = {
    items: [
      {
        id: 1,
        name: '销售收入',
        categoryType: 'Income',
        parentId: undefined,
        parentName: undefined,
        level: 1,
        description: '产品销售收入',
        isActive: true,
        createdAt: '2024-01-01T00:00:00Z'
      },
      {
        id: 2,
        name: '办公费用',
        categoryType: 'Expense',
        parentId: undefined,
        parentName: undefined,
        level: 1,
        description: '日常办公支出',
        isActive: true,
        createdAt: '2024-01-02T00:00:00Z'
      },
      {
        id: 3,
        name: '房租',
        categoryType: 'Expense',
        parentId: 2,
        parentName: '办公费用',
        level: 2,
        description: '办公室租金',
        isActive: false,
        createdAt: '2024-01-03T00:00:00Z'
      }
    ],
    total: 3
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(categoryApi.getCategories).mockResolvedValue(
      mockAxiosResponse({ data: mockCategories })
    )
    vi.mocked(categoryApi.getActiveCategories).mockResolvedValue(
      mockAxiosResponse({ data: [] })
    )
  })

  it('应该正确渲染页面标题和新增按钮', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    expect(wrapper.find('.page-title').text()).toBe('分类管理')
    expect(wrapper.find('.page-desc').text()).toBe('设置交易的收入和支出分类')
    expect(wrapper.text()).toContain('新增分类')
  })

  it('应该在挂载时加载分类列表数据', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    expect(categoryApi.getCategories).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 20 })
    )

    const rows = wrapper.findAll('.el-table__row')
    expect(rows.length).toBe(3)
  })

  it('应该正确显示分类类型标签（收入/支出）', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    const tags = wrapper.findAllComponents({ name: 'ElTag' })

    // 第一个分类是收入类型
    expect(tags[0].props('type')).toBe('success')
    expect(tags[0].text()).toBe('收入')

    // 第二个分类是支出类型
    expect(tags[2].props('type')).toBe('danger')
    expect(tags[2].text()).toBe('支出')
  })

  it('应该正确显示父分类名称', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    const table = wrapper.findComponent({ name: 'ElTable' })
    const tableData = table.props('data')

    expect(tableData[0].parentName).toBeUndefined()
    expect(tableData[2].parentName).toBe('办公费用')
  })

  it('应该正确显示分类状态（启用/禁用）', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    const tags = wrapper.findAllComponents({ name: 'ElTag' })

    // 第一个分类是启用状态
    expect(tags[1].props('type')).toBe('success')
    expect(tags[1].text()).toBe('启用')

    // 第三个分类是禁用状态
    expect(tags[5].props('type')).toBe('info')
    expect(tags[5].text()).toBe('禁用')
  })

  it('应该支持按分类类型筛选', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    vi.clearAllMocks()

    const select = wrapper.findComponent({ name: 'ElSelect' })
    await select.vm.$emit('update:modelValue', 'Income')
    await select.vm.$emit('change')
    await flushPromises()

    expect(categoryApi.getCategories).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 20,
        categoryType: 'Income'
      })
    )
  })

  it('应该支持重置筛选条件', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    const select = wrapper.findComponent({ name: 'ElSelect' })
    await select.vm.$emit('update:modelValue', 'Expense')

    vi.clearAllMocks()

    const resetButton = wrapper.findAll('.search-section button')[1]
    await resetButton.trigger('click')
    await flushPromises()

    expect(categoryApi.getCategories).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 20 })
    )
  })

  it('点击新增按钮应该打开表单对话框', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    const addButton = wrapper.findAll('.el-button--primary').find(b => b.text() === '新增分类')
    expect(addButton).toBeTruthy()
    await addButton!.trigger('click')
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.dialogVisible).toBe(true)
    expect(vm.currentCategory).toBeNull()
  })

  it('点击编辑按钮应该打开表单对话框并传入分类数据', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    // 表格中的编辑按钮是 link 类型的 primary 按钮，排除分类名称链接按钮
    const editButtons = wrapper.findAll('.el-table .el-button').filter(b => b.text() === '编辑')
    await editButtons[0].trigger('click')
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.dialogVisible).toBe(true)
    expect(vm.currentCategory).toEqual(mockCategories.items[0])
  })

  it('点击删除按钮应该弹出确认框并删除分类', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(categoryApi.deleteCategory).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )

    const wrapper = mountWithPlugins(CategoryList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    vi.clearAllMocks()

    const deleteButtons = wrapper.findAll('.el-button--danger')
    await deleteButtons[0].trigger('click')
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      '确定要删除该分类吗？',
      '提示',
      expect.objectContaining({
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      })
    )
    expect(categoryApi.deleteCategory).toHaveBeenCalledWith(1)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('取消删除操作不应该调用删除API', async () => {
    vi.mocked(ElMessageBox.confirm).mockRejectedValue('cancel')

    const wrapper = mountWithPlugins(CategoryList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    vi.clearAllMocks()

    const deleteButtons = wrapper.findAll('.el-button--danger')
    await deleteButtons[0].trigger('click')
    await flushPromises()

    expect(categoryApi.deleteCategory).not.toHaveBeenCalled()
  })

  it('应该支持分页功能', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    vi.clearAllMocks()

    const pagination = wrapper.findComponent({ name: 'ElPagination' })
    await pagination.vm.$emit('update:currentPage', 2)
    await pagination.vm.$emit('current-change')
    await flushPromises()

    expect(categoryApi.getCategories).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2, pageSize: 20 })
    )
  })

  it('应该支持修改每页显示数量', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    vi.clearAllMocks()

    const pagination = wrapper.findComponent({ name: 'ElPagination' })
    await pagination.vm.$emit('update:pageSize', 50)
    await pagination.vm.$emit('size-change')
    await flushPromises()

    expect(categoryApi.getCategories).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 50 })
    )
  })

  it('表单提交成功后应该刷新列表', async () => {
    const wrapper = mountWithPlugins(CategoryList)
    await flushPromises()

    vi.clearAllMocks()

    // 触发 CategoryForm 的 success 事件（即 handleQuery）
    const form = wrapper.findComponent({ name: 'CategoryForm' })
    await form.vm.$emit('success')
    await flushPromises()

    expect(categoryApi.getCategories).toHaveBeenCalled()
  })
})
