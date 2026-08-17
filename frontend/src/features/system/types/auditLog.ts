// 审计日志
export interface AuditLog {
  id: number
  entityType: string
  entityId: number | null
  action: string
  oldValue?: string
  newValue?: string
  userId: number | null
  operatorName: string
  ipAddress: string
  createdAt: string
}

// 审计日志查询参数
export interface AuditLogQueryParams {
  page: number
  pageSize: number
  userId?: number
  username?: string
  action?: string
  entityType?: string
  startDate?: string
  endDate?: string
  sortBy?: string
  sortDesc?: boolean
}
