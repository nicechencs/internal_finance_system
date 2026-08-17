import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'

export interface CrudFactoryConfig {
  baseUrl: string
  features?: {
    getActive?: boolean
    batchCreate?: boolean
    batchImport?: boolean
  }
}

export function createCrudApi<T = any, C = any, U = any>(config: CrudFactoryConfig) {
  const { baseUrl, features } = config

  const api: Record<string, any> = {
    getList: (params?: any) =>
      request<ApiResponse<any>>({ url: baseUrl, method: 'get', params }),
    getById: (id: number) =>
      request<ApiResponse<T>>({ url: `${baseUrl}/${id}`, method: 'get' }),
    create: (data: C) =>
      request<ApiResponse<T>>({ url: baseUrl, method: 'post', data }),
    update: (id: number, data: U) =>
      request<ApiResponse<T>>({ url: `${baseUrl}/${id}`, method: 'put', data }),
    remove: (id: number) =>
      request<ApiResponse<any>>({ url: `${baseUrl}/${id}`, method: 'delete' }),
  }

  if (features?.getActive) {
    api.getActive = () =>
      request<ApiResponse<T[]>>({ url: `${baseUrl}/active`, method: 'get' })
  }

  if (features?.batchCreate) {
    api.batchCreate = (data: { items: C[] }) =>
      request<ApiResponse<any>>({ url: `${baseUrl}/batch`, method: 'post', data })
  }

  if (features?.batchImport) {
    api.batchImport = (file: FormData) =>
      request<ApiResponse<any>>({ url: `${baseUrl}/batch-import`, method: 'post', data: file, headers: { 'Content-Type': 'multipart/form-data' } })
  }

  return api
}
