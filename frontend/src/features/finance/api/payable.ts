import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { PageResponse } from '@/shared/types/common'
import type {
  Payable,
  PayableSummary,
  PayableTrend,
  PayableAging,
  PayableStatistics,
  CreatePayableRequest,
  UpdatePayableRequest,
  PayPaymentRequest,
  PayableType,
  CreatePayableTypeRequest,
  UpdatePayableTypeRequest
} from '@/features/finance/types/payable'

const api = createCrudApi<Payable, CreatePayableRequest, UpdatePayableRequest>({
  baseUrl: '/payables',
})

// 向后兼容具名导出
export const getPayables = api.getList
export const getPayableById = api.getById
export const createPayable = api.create
export const updatePayable = api.update
export const deletePayable = api.remove

// 特殊业务方法
export const payPayment = (id: number, data: PayPaymentRequest) =>
  request<ApiResponse<Payable>>({ url: `/payables/${id}/pay`, method: 'post', data })

export const getAvailablePayablesForTransaction = (transactionId: number, keyword?: string) =>
  request<ApiResponse<Payable[]>>({
    url: '/payables/available-for-transaction',
    method: 'get',
    params: { transactionId, keyword }
  })

export const getPayableSummary = () =>
  request<ApiResponse<PayableSummary>>({ url: '/payables/summary', method: 'get' })

export const getPayableTrend = (params?: { startDate?: string; endDate?: string }) =>
  request<ApiResponse<PayableTrend>>({ url: '/payables/trend', method: 'get', params })

export const getPayableAging = () =>
  request<ApiResponse<PayableAging>>({ url: '/payables/aging', method: 'get' })

export const getPayablesByCustomer = (customerId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/customer/${customerId}`, method: 'get' })

export const getPayablesBySupplier = (supplierId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/supplier/${supplierId}`, method: 'get' })

export const getPayablesByPerson = (personId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/person/${personId}`, method: 'get' })

export const getPayablesByProject = (projectId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/project/${projectId}`, method: 'get' })

// 应付款业务类型管理（管理页面 - 分页接口）
export const getPayableTypesPaged = (params?: { page?: number; pageSize?: number; name?: string; isActive?: boolean }) =>
  request<ApiResponse<PageResponse<PayableType>>>({
    url: '/payable-types',
    method: 'get',
    params: params || { page: 1, pageSize: 200 }
  })

// 下拉选择 - 仅返回启用项（非分页）
export const getPayableTypes = () =>
  request<ApiResponse<PayableType[]>>({ url: '/payable-types/active', method: 'get' })

export const createPayableType = (data: CreatePayableTypeRequest) =>
  request<ApiResponse<PayableType>>({ url: '/payable-types', method: 'post', data })

export const updatePayableType = (id: number, data: UpdatePayableTypeRequest) =>
  request<ApiResponse<PayableType>>({ url: `/payable-types/${id}`, method: 'put', data })

export const deletePayableType = (id: number) =>
  request<ApiResponse<void>>({ url: `/payable-types/${id}`, method: 'delete' })

export const getPayableStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<PayableStatistics>>({
    url: '/payables/statistics',
    method: 'get',
    params
  })
