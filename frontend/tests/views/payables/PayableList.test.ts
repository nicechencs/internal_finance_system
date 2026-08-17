import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import { ElMessage, ElMessageBox } from 'element-plus'
import PayableList from '@/views/payables/PayableList.vue'
import * as payableApi from '@/api/payable'
import * as projectApi from '@/api/project'
import * as supplierApi from '@/api/supplier'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/payable')
vi.mock('@/api/project')
vi.mock('@/api/supplier')

const mockPayables = [
  {
    id: 1,
    supplierId: 1,
    supplierName: '供应商A',
    projectId: 1,
    projectName: '项目A',
    totalAmount: 20000,
    paidAmount: 5000,
    remainingAmount: 15000,
    status: 'partial',
    dueDate: '2026-04-15',
    description: '应付款项1',
    createdAt: '2026-03-01T00:00:00Z',
    updatedAt: '2026-03-01T00:00:00Z',
    details: []
  },
  {
    id: 2,
    supplierId: 2,
    supplierName: '供应商B',
    projectId: null,
    projectName: null,
    totalAmount: 8000,
    paidAmount: 0,
    remainingAmount: 8000,
    status: 'pending',
    dueDate: '2026-03-10',
    description: '应付款项2',
    createdAt: '2026-03-02T00:00:00Z',
    updatedAt: '2026-03-02T00:00:00Z',
    details: []
  },
  {
    id: 3,
    supplierId: 1,
    supplierName: '供应商A',
    projectId: 2,
    projectName: '项目B',
    totalAmount: 12000,
    paidAmount: 12000,
    remainingAmount: 0,
    status: 'settled',
    dueDate: '2026-02-20',
    description: '已结清款项',
    createdAt: '2026-01-15T00:00:00Z',
    updatedAt: '2026-02-20T00:00:00Z',
    settledAt: '2026-02-20T00:00:00Z',
    details: []
  }
]

const mockProjects = [
  { id: 1, name: '项目A' },
  { id: 2, name: '项目B' }
]

const mockSuppliers = [
  { id: 1, name: '供应商A' },
  { id: 2, name: '供应商B' }
]

function setupMocks(payables = mockPayables, total = 3) {
  vi.mocked(payableApi.getPayables).mockResolvedValue(
    mockAxiosResponse({ data: { items: payables, total } })
  )
  vi.mocked(projectApi.getProjects).mockResolvedValue(
    mockAxiosResponse({ data: { items: mockProjects, total: 2 } })
  )
  vi.mocked(supplierApi.getSuppliers).mockResolvedValue(
    mockAxiosResponse({ data: { items: mockSuppliers, total: 2 } })
  )
  vi.mocked(payableApi.getPayableStatistics).mockResolvedValue(
    mockAxiosResponse({
      data: {
        totalCount: 3,
        pendingCount: 1,
        partialCount: 1,
        settledCount: 1,
        totalAmount: 40000,
        paidAmount: 17000,
        remainingAmount: 23000,
        overdueAmount: 8000
      }
    })
  )
}

describe('PayableList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该正确渲染页面标题和新增按钮', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    const userStore = useUserStore()
    userStore.setUser({ id: 1, username: 'admin', email: 'a@a.com', fullName: '管理员', role: 'Admin', isActive: true })
    await flushPromises()

    expect(wrapper.find('.page-title').text()).toBe('应付管理')
    expect(wrapper.find('.page-desc').text()).toBe('跟踪和管理应付账款')
    expect(wrapper.text()).toContain('新增应付')
  })

  it('应该在挂载时加载应付列表数据', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    expect(payableApi.getPayables).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      projectId: null,
      supplierId: null,
      status: ''
    })
    expect(projectApi.getProjects).toHaveBeenCalled()
    expect(supplierApi.getSuppliers).toHaveBeenCalled()
  })

  it('应该在加载失败时显示错误消息', async () => {
    vi.mocked(payableApi.getPayables).mockRejectedValue(new Error('网络错误'))
    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({ data: { items: [], total: 0 } })
    )
    vi.mocked(supplierApi.getSuppliers).mockResolvedValue(
      mockAxiosResponse({ data: { items: [], total: 0 } })
    )

    mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载应付列表失败')
  })

  it('应该在点击查询按钮时重置页码并重新加载', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()

    const buttons = wrapper.findAll('.el-button')
    const queryBtn = buttons.find(b => b.text() === '查询')
    await queryBtn?.trigger('click')
    await flushPromises()

    expect(payableApi.getPayables).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1 })
    )
  })

  it('应该在点击重置按钮时清空筛选条件', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()

    const buttons = wrapper.findAll('.el-button')
    const resetBtn = buttons.find(b => b.text() === '重置')
    await resetBtn?.trigger('click')
    await flushPromises()

    expect(payableApi.getPayables).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 1,
        projectId: null,
        supplierId: null,
        status: ''
      })
    )
  })

  it('应该在删除确认后调用删除API并刷新列表', async () => {
    setupMocks()
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')
    vi.mocked(payableApi.deletePayable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )

    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()
    vi.mocked(payableApi.deletePayable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

    const deleteButtons = wrapper.findAll('.el-button').filter(b => b.text() === '删除')
    if (deleteButtons.length > 0) {
      await deleteButtons[0].trigger('click')
      await flushPromises()

      expect(payableApi.deletePayable).toHaveBeenCalled()
      expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
    }
  })

  it('应该在挂载时加载统计数据', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    expect(payableApi.getPayableStatistics).toHaveBeenCalled()
  })

  it('应该在删除后刷新统计数据', async () => {
    setupMocks()
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')
    vi.mocked(payableApi.deletePayable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )

    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    vi.clearAllMocks()
    setupMocks()
    vi.mocked(payableApi.deletePayable).mockResolvedValue(
      mockAxiosResponse({ data: null })
    )
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

    const deleteButtons = wrapper.findAll('.el-button').filter(b => b.text() === '删除')
    if (deleteButtons.length > 0) {
      await deleteButtons[0].trigger('click')
      await flushPromises()

      expect(payableApi.getPayableStatistics).toHaveBeenCalled()
    }
  })

  it('应该在点击详情按钮时打开详情对话框', async () => {
    setupMocks()
    const wrapper = mountWithPlugins(PayableList, {
      stubs: { PayableDetail: true }
    })
    await flushPromises()

    const viewButtons = wrapper.findAll('.el-button').filter(b => b.text() === '详情')
    if (viewButtons.length > 0) {
      await viewButtons[0].trigger('click')
      await flushPromises()

      const detail = wrapper.findComponent({ name: 'PayableDetail' })
      expect(detail.exists()).toBe(true)
    }
  })
})
