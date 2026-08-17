import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { PageRequest, PageResponse } from '@/shared/types/common'
import type { ImportBatch, ImportPreviewResponse, ImportConfirmRequest } from '@/features/import/types/import'

export const previewImport = (formData: FormData) => {
  return request<ApiResponse<ImportPreviewResponse>>({
    url: '/imports/preview',
    method: 'post',
    data: formData,
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })
}

export const confirmImport = (data: ImportConfirmRequest) => {
  return request<ApiResponse<ImportBatch>>({
    url: '/imports/confirm',
    method: 'post',
    data
  })
}

export interface ImportBatchQuery extends PageRequest {
  accountId?: number
  status?: string
  fileName?: string
  startDate?: string
  endDate?: string
}

export const getImportBatches = (params: ImportBatchQuery) => {
  return request<ApiResponse<PageResponse<ImportBatch>>>({
    url: '/imports/batches',
    method: 'get',
    params
  })
}

export const getImportBatchById = (id: number) => {
  return request<ApiResponse<ImportBatch>>({
    url: `/imports/batches/${id}`,
    method: 'get'
  })
}

export const deleteImportBatch = (id: number) => {
  return request<ApiResponse<null>>({
    url: `/imports/batches/${id}`,
    method: 'delete'
  })
}

export const getImportBatchPreview = (id: number) => {
  return request<ApiResponse<ImportPreviewResponse>>({
    url: `/imports/batches/${id}/preview`,
    method: 'get'
  })
}

// 批量导入结果类型
export interface BatchImportResult {
  totalCount: number
  successCount: number
  failedCount: number
  errors: Array<{
    index: number
    message: string
  }>
}

/**
 * 批量导入文件（通用接口）
 * @param moduleType 模块类型：customer | supplier | person | project
 * @param file 要上传的文件
 */
export const batchImportFile = (
  moduleType: 'customer' | 'supplier' | 'person' | 'project',
  file: File
) => {
  const formData = new FormData()
  formData.append('file', file)

  const urlMap = {
    customer: '/customers/batch-import',
    supplier: '/suppliers/batch-import',
    person: '/persons/batch-import',
    project: '/projects/batch-import'
  }

  return request<ApiResponse<BatchImportResult>>({
    url: urlMap[moduleType],
    method: 'post',
    data: formData,
    headers: {
      'Content-Type': 'multipart/form-data'
    },
    timeout: 60000 // 60秒超时
  })
}

