import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mountWithPlugins, mockAxiosResponse } from '@tests/utils'
import { ElMessage, ElMessageBox } from 'element-plus'
import ProjectList from '@/views/projects/ProjectList.vue'
import * as projectApi from '@/api/project'
import * as customerApi from '@/api/customer'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/project')
vi.mock('@/api/customer')

describe('ProjectList.vue', () => {
  const mockProjects = [
    {
      id: 1,
      projectCode: 'PRJ001',
      name: '项目A',
      customerId: 1,
      customerName: '客户A',
      contractAmount: 100000,
      status: 'active',
      startDate: '2026-01-01',
      endDate: '2026-12-31'
    },
    {
      id: 2,
      projectCode: 'PRJ002',
      name: '项目B',
      customerId: 2,
      customerName: '客户B',
      contractAmount: 200000,
      status: 'completed',
      startDate: '2025-06-01',
      endDate: '2025-12-31'
    }
  ]

  const mockCustomers = [
    { id: 1, name: '客户A', contactPerson: '张三', phone: '13800138000' },
    { id: 2, name: '客户B', contactPerson: '李四', phone: '13900139000' }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: mockProjects,
          total: 2
        }
      })
    )
    vi.mocked(customerApi.getActiveCustomers).mockResolvedValue(
      mockAxiosResponse({ data: mockCustomers })
    )
  })

  it('应该正确渲染项目列表', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    expect(projectApi.getProjects).toHaveBeenCalled()
    expect(customerApi.getActiveCustomers).toHaveBeenCalled()

    const table = wrapper.find('.el-table')
    expect(table.exists()).toBe(true)
  })

  it('应该显示正确的项目数据', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const tableText = wrapper.text()
    expect(tableText).toContain('PRJ001')
    expect(tableText).toContain('项目A')
    expect(tableText).toContain('客户A')
  })

  it('应该支持按项目名称搜索', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.filters.name = '项目A'

    const queryButton = wrapper.findAll('.el-button').find(btn => btn.text() === '查询')
    await queryButton?.trigger('click')
    await flushPromises()

    // mount 时调用1次，点击查询按钮调用1次 = 2次
    expect(projectApi.getProjects).toHaveBeenCalledTimes(2)
  })

  it('应该支持按状态筛选', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    // 模拟选择状态
    const vm = wrapper.vm as any
    vm.filters.status = 'active'
    await wrapper.vm.$nextTick()

    const queryButton = wrapper.findAll('.el-button').find(btn => btn.text() === '查询')
    await queryButton?.trigger('click')
    await flushPromises()

    expect(projectApi.getProjects).toHaveBeenCalledTimes(2)
  })

  it('应该支持重置搜索条件', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.filters.name = '项目A'
    vm.filters.status = 'active'
    await wrapper.vm.$nextTick()

    const resetButton = wrapper.findAll('.el-button').find(btn => btn.text() === '重置')
    await resetButton?.trigger('click')
    await flushPromises()

    expect(vm.filters.name).toBe('')
    expect(vm.filters.status).toBe('')
  })

  it('应该打开新增项目对话框', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    const createButton = wrapper.find('.page-header-actions .el-button--primary')
    await createButton.trigger('click')
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    expect(vm.formVisible).toBe(true)
    expect(vm.currentProject).toBeNull()
  })

  it('应该打开编辑项目对话框', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    const editButtons = wrapper.findAll('.el-button--primary[link]')
    if (editButtons.length > 0) {
      await editButtons[0].trigger('click')
      await wrapper.vm.$nextTick()

      expect(vm.formVisible).toBe(true)
      expect(vm.currentProject).toBeTruthy()
    }
  })

  it('已取消项目不应显示标签、编辑和删除按钮', async () => {
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: [
            {
              id: 3,
              projectCode: 'PRJ003',
              name: '已取消项目',
              customerId: 1,
              customerName: '客户A',
              contractAmount: 10000,
              receivedAmount: 0,
              tags: [],
              status: 'Cancelled'
            }
          ],
          total: 1
        }
      })
    )
    vi.mocked(projectApi.getProjectStatistics).mockResolvedValue(
      mockAxiosResponse({
        data: { totalCount: 1, totalContractAmount: 0, totalProfit: 0, totalReceivable: 0 }
      })
    )
    vi.mocked(projectApi.getActiveProjects).mockResolvedValue(mockAxiosResponse({ data: [] }))

    const wrapper = mountWithPlugins(ProjectList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    const buttonLabels = wrapper.findAll('button').map(btn => btn.text())
    expect(wrapper.text()).toContain('已取消')
    expect(buttonLabels).not.toContain('标签')
    expect(buttonLabels).not.toContain('编辑')
    expect(buttonLabels).not.toContain('删除')
  })

  it('进行中项目仍显示标签、编辑和删除按钮', async () => {
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: [
            {
              ...mockProjects[0],
              status: 'Active',
              receivedAmount: 0,
              tags: []
            }
          ],
          total: 1
        }
      })
    )
    vi.mocked(projectApi.getProjectStatistics).mockResolvedValue(
      mockAxiosResponse({
        data: { totalCount: 1, totalContractAmount: 0, totalProfit: 0, totalReceivable: 0 }
      })
    )
    vi.mocked(projectApi.getActiveProjects).mockResolvedValue(mockAxiosResponse({ data: [] }))

    const wrapper = mountWithPlugins(ProjectList)
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    const buttonLabels = wrapper.findAll('button').map(btn => btn.text())
    expect(buttonLabels).toContain('标签')
    expect(buttonLabels).toContain('编辑')
    expect(buttonLabels).toContain('删除')
  })

  it('已取消项目的写操作处理函数应直接返回', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    const cancelled = { id: 9, name: '已取消项目', status: 'Cancelled' }

    vm.handleEdit(cancelled)
    vm.handleManageTags(cancelled)
    await vm.handleDelete(cancelled)
    await flushPromises()

    expect(vm.formVisible).toBe(false)
    expect(vm.tagDialogVisible).toBe(false)
    expect(projectApi.deleteProject).not.toHaveBeenCalled()
  })

  it('应该处理删除项目操作', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(projectApi.deleteProject).mockResolvedValue(mockAxiosResponse({}))

    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleDelete(mockProjects[0])
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalled()
    expect(projectApi.deleteProject).toHaveBeenCalledWith(1)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('应该处理分页变化', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.pagination.page = 2
    await vm.handlePageChange()
    await flushPromises()

    expect(projectApi.getProjects).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2 })
    )
  })

  it('应该处理每页大小变化', async () => {
    const wrapper = mountWithPlugins(ProjectList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.pagination.pageSize = 50
    await vm.handleSizeChange()
    await flushPromises()

    expect(projectApi.getProjects).toHaveBeenCalledWith(
      expect.objectContaining({ pageSize: 50 })
    )
  })
})
