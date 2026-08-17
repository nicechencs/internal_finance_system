import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { PageResponse } from '@/shared/types/common'
import type { AuditLog, AuditLogQueryParams } from '@/features/system/types/auditLog'

// 获取审计日志列表（分页）
export const getAuditLogs = (params: AuditLogQueryParams) => {
  return request<ApiResponse<PageResponse<AuditLog>>>({
    url: '/audit-logs',
    method: 'get',
    params
  })
}

// 获取审计日志详情
export const getAuditLogById = (id: number) => {
  return request<ApiResponse<AuditLog>>({
    url: `/audit-logs/${id}`,
    method: 'get'
  })
}
