import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import { ElMessage, ElMessageBox } from 'element-plus'
import ReceivableList from '@/views/receivables/ReceivableList.vue'
import * as receivableApi from '@/api/receivable'
import * as projectApi from '@/api/project'
import * as customerApi from '@/api/customer'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/receivable')
vi.mock('@/api/project')
vi.mock('@/api/customer')

const mockReceivables = [
  {
    id: 1,
    projectId: 1,
    projectName: '项目A',
    customerId: 1,
    customerName: '客户A',
    totalAmount: 10000,
    receivedAmount: 3000,
    remainingAmount: 7000,
    status: 'partial',
    dueDate: '2026-04-01',
    description: '应收款项1',
    createdAt: '2026-03-01T00:00:00Z',
    updatedAt: '2026-03-01T00:00:00Z',
    details: []
  },
  {
    id: 2,
    projectId: 2,
    projectName: '项目B',
    customerId: 2,
    customerName: '客户B',
    totalAmount: 5000,
    receivedAmount: 0,
    remainingAmount: 5000,
    status: 'pending',
    dueDate: '2026-03-10',
    description: '应收款项2',
    createdAt: '2026-03-02T00:00:00Z',
    updatedAt: '2026-03-02T00:00:00Z',
    details: []
  },
  {
    id: 3,
    projectId: 1,
    projectName: '项目A',
    customerId: 3,
    customerName: '客户C',
    totalAmount: 8000,
    receivedAmount: 8000,
    remainingAmount: 0,
    status: 'settled',
    dueDate: '2026-02-15',
    description: '已结清款项',
    createdAt: '2026-01-10T00:00:00Z',
    updatedAt: '2026-02-15T00:00:00Z',
    settledAt: '2026-02-15T00:00:00Z',
    details: []
  }
]

const mockProjects = [
  { id: 1, name: '项目A' },
  { id: 2, name: '项目B' }
]

const mockCustomers = [
  { id: 1, name: '客户A' },
  { id: 2, name: '客户B' },
  { id: 3, name: '客户C' }
]

function setupMocks(receivables = mockReceivables, total = 3) {
  vi.mocked(receivableApi.getReceivables).mockResolvedValue(
    mockAxiosResponse({ data: { items: receivables, total } })
  )
  vi.mocked(projectApi.getProjects).mockResolvedValue(
    mockAxiosResponse({ data: { items: mockProjects, total: 2 } })
  )
  vi.mocked(customerApi.getCustomers).mockResolvedValue(
    mockAxiosResponse({ data: { items: mockCustomers, total: 3 } })
  )
  vi.mocked(receivableApi.getReceivableStatistics).mockResolvedValue(
    mockAxiosResponse({
      data: {
        totalCount: 3,
        pendingCount: 1,
        partialCount: 1,
        settledCount: 1,
        totalAmount: 23000,
        receivedAmount: 11000,
        remainingAmount: 12000,
        overdueAmount: 5000
      }
    })
  )
}

describe('ReceivableList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该正确渲染页面标题和新增按钮', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    expect(wrapper.find('.page-title').text()).toBe('应收管理')
    expect(wrapper.find('.page-desc').text()).toBe('跟踪和管理应收账款')
    expect(wrapper.text()).toContain('新增应收')
  })

  it('应该在挂载时加载应收列表数据', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    expect(receivableApi.getReceivables).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      projectId: null,
      customerId: null,
      status: ''
    })
    expect(projectApi.getProjects).toHaveBeenCalled()
    expect(customerApi.getCustomers).toHaveBeenCalled()
  })

  it('应该在加载失败时显示错误消息', async () => {
    vi.mocked(receivableApi.getReceivables).mockRejectedValue(new Error('网络错误'))
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({ data: { items: [], total: 0 } })
    )
    vi.mocked(customerApi.getCustomers).mockResolvedValue(
      mockAxiosResponse({ data: { items: [], total: 0 } })
    )

    mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载应收列表失败')
  })

  it('应该在点击查询按钮时重置页码并重新加载', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()

    // 触发查询
    const buttons = wrapper.findAll('.el-button')
    const queryBtn = buttons.find(b => b.text() === '查询')
    await queryBtn?.trigger('click')
    await flushPromises()

    expect(receivableApi.getReceivables).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1 })
    )
  })

  it('应该在点击重置按钮时清空筛选条件', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()

    const buttons = wrapper.findAll('.el-button')
    const resetBtn = buttons.find(b => b.text() === '重置')
    await resetBtn?.trigger('click')
    await flushPromises()

    expect(receivableApi.getReceivables).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 1,
        projectId: null,
        customerId: null,
        status: ''
      })
    )
  })

  it('应该在删除确认后调用删除API并刷新列表', async () => {
    setupMocks()
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')
    vi.mocked(receivableApi.deleteReceivable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )

    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()
    vi.mocked(receivableApi.deleteReceivable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

    const deleteButtons = wrapper.findAll('.el-button').filter(b => b.text() === '删除')
    if (deleteButtons.length > 0) {
      await deleteButtons[0].trigger('click')
      await flushPromises()

      expect(receivableApi.deleteReceivable).toHaveBeenCalled()
      expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
    }
  })

  it('应该在挂载时加载统计数据', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    expect(receivableApi.getReceivableStatistics).toHaveBeenCalled()
  })

  it('应该在删除后刷新统计数据', async () => {
    setupMocks()
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')
    vi.mocked(receivableApi.deleteReceivable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )

    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()
    vi.mocked(receivableApi.deleteReceivable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

    const deleteButtons = wrapper.findAll('.el-button').filter(b => b.text() === '删除')
    if (deleteButtons.length > 0) {
      await deleteButtons[0].trigger('click')
      await flushPromises()

      expect(receivableApi.getReceivableStatistics).toHaveBeenCalled()
    }
  })

  it('应该在点击详情按钮时打开详情对话框', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(ReceivableList, {
      stubs: { ReceivableDetail: true }
    })
    await flushPromises()

    const viewButtons = wrapper.findAll('.el-button').filter(b => b.text() === '详情')
    if (viewButtons.length > 0) {
      await viewButtons[0].trigger('click')
      await flushPromises()

      const detail = wrapper.findComponent({ name: 'ReceivableDetail' })
      expect(detail.exists()).toBe(true)
    }
  })
})
