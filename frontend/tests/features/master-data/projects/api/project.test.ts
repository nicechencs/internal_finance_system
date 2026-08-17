import { beforeEach, describe, expect, it, vi } from 'vitest'
import request from '@/shared/utils/request'
import {
  createProject,
  getActiveProjects,
  getProjectById,
  getProjects,
  updateProject
} from '@/features/master-data/projects/api/project'

vi.mock('@/shared/utils/request', () => ({
  default: vi.fn()
}))

const createProjectApiModel = () => ({
  id: 1,
  name: '测试项目',
  projectCode: 'PRJ-2026-001',
  customer: {
    id: 2,
    name: '测试客户'
  },
  contractAmount: 100000,
  receivedAmount: 20000,
  receivableAmount: 80000,
  totalCost: 30000,
  profitAmount: -10000,
  profitRate: -10,
  startDate: '2026-03-01',
  endDate: '2026-12-31',
  status: 'Active',
  description: '项目描述',
  tags: [],
  createdAt: '2026-03-01T00:00:00Z',
  updatedAt: '2026-03-02T00:00:00Z'
})

describe('project api normalization', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('将分页列表中的嵌套 customer 归一化为 customerId 和 customerName', async () => {
    vi.mocked(request).mockResolvedValue({
      data: {
        success: true,
        data: {
          items: [createProjectApiModel()],
          total: 1,
          page: 1,
          pageSize: 20
        },
        message: ''
      }
    } as any)

    const response = await getProjects({ page: 1, pageSize: 20 })
    const project = response.data.data.items[0]

    expect(project.customerId).toBe(2)
    expect(project.customerName).toBe('测试客户')
  })

  it('将详情接口中的嵌套 customer 归一化为 customerId 和 customerName', async () => {
    vi.mocked(request).mockResolvedValue({
      data: {
        success: true,
        data: createProjectApiModel(),
        message: ''
      }
    } as any)

    const response = await getProjectById(1)

    expect(response.data.data.customerId).toBe(2)
    expect(response.data.data.customerName).toBe('测试客户')
  })

  it('将活跃项目列表中的嵌套 customer 归一化为 customerId 和 customerName', async () => {
    vi.mocked(request).mockResolvedValue({
      data: {
        success: true,
        data: [createProjectApiModel()],
        message: ''
      }
    } as any)

    const response = await getActiveProjects()

    expect(response.data.data[0].customerId).toBe(2)
    expect(response.data.data[0].customerName).toBe('测试客户')
  })

  it('创建项目后返回的数据也会归一化 customer 字段', async () => {
    vi.mocked(request).mockResolvedValue({
      data: {
        success: true,
        data: createProjectApiModel(),
        message: ''
      }
    } as any)

    const response = await createProject({
      name: '测试项目',
      customerId: 2,
      contractAmount: 100000,
      startDate: '2026-03-01'
    })

    expect(response.data.data.customerId).toBe(2)
    expect(response.data.data.customerName).toBe('测试客户')
  })

  it('更新项目后返回的数据也会归一化 customer 字段', async () => {
    vi.mocked(request).mockResolvedValue({
      data: {
        success: true,
        data: createProjectApiModel(),
        message: ''
      }
    } as any)

    const response = await updateProject(1, {
      name: '测试项目',
      customerId: 2,
      contractAmount: 100000,
      startDate: '2026-03-01',
      status: 'Active'
    })

    expect(response.data.data.customerId).toBe(2)
    expect(response.data.data.customerName).toBe('测试客户')
  })
})
