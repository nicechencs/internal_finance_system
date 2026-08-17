import type { AxiosResponse } from 'axios'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { PageRequest, PageResponse } from '@/shared/types/common'
import type {
  Project,
  ProjectStatistics,
  ProjectProfitReport,
  CreateProjectRequest,
  UpdateProjectRequest,
  ProfitAnalysisResponse
} from '@/features/master-data/projects/types/project'
import {
  normalizeProject,
  normalizeProjects
} from '@/features/master-data/projects/utils/normalizeProject'

const normalizeProjectPageResponse = (
  response: AxiosResponse<ApiResponse<PageResponse<Project>>>
) => {
  response.data.data.items = normalizeProjects(response.data.data.items)
  return response
}

const normalizeSingleProjectResponse = (
  response: AxiosResponse<ApiResponse<Project>>
) => {
  response.data.data = normalizeProject(response.data.data)
  return response
}

const normalizeProjectListResponse = (
  response: AxiosResponse<ApiResponse<Project[]>>
) => {
  response.data.data = normalizeProjects(response.data.data)
  return response
}

export const getProjects = (params?: PageRequest | Record<string, any>) =>
  request<ApiResponse<PageResponse<Project>>>({
    url: '/projects',
    method: 'get',
    params
  }).then(normalizeProjectPageResponse)

export const getProjectById = (id: number) =>
  request<ApiResponse<Project>>({
    url: `/projects/${id}`,
    method: 'get'
  }).then(normalizeSingleProjectResponse)

export const createProject = (data: CreateProjectRequest) =>
  request<ApiResponse<Project>>({
    url: '/projects',
    method: 'post',
    data
  }).then(normalizeSingleProjectResponse)

export const updateProject = (id: number, data: UpdateProjectRequest) =>
  request<ApiResponse<Project>>({
    url: `/projects/${id}`,
    method: 'put',
    data
  }).then(normalizeSingleProjectResponse)

export const deleteProject = (id: number) =>
  request<ApiResponse<void>>({
    url: `/projects/${id}`,
    method: 'delete'
  })

export const getActiveProjects = () =>
  request<ApiResponse<Project[]>>({
    url: '/projects/active',
    method: 'get'
  }).then(normalizeProjectListResponse)

export const batchCreateProjects = (data: { items: CreateProjectRequest[] }) =>
  request<ApiResponse<any>>({
    url: '/projects/batch',
    method: 'post',
    data
  })

export const getProjectProfitReport = () =>
  request<ApiResponse<ProjectProfitReport[]>>({
    url: '/projects/profit-report',
    method: 'get'
  })

export const generateProjectCode = () =>
  request<ApiResponse<string>>({
    url: '/projects/generate-code',
    method: 'get'
  })

export const getProjectProfitAnalysis = (id: number, months: number = 12) =>
  request<ApiResponse<ProfitAnalysisResponse>>({
    url: `/projects/${id}/profit-analysis`,
    method: 'get',
    params: { months }
  })

export const getProjectStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<ProjectStatistics>>({
    url: '/projects/statistics',
    method: 'get',
    params
  })

export const initializeReceivables = (id: number, data: any) =>
  request<ApiResponse<void>>({
    url: `/projects/${id}/initialize-receivables`,
    method: 'post',
    data
  })
