import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { Supplier, SupplierStatistics, SupplierFinanceSummary, CreateSupplierRequest, UpdateSupplierRequest } from '@/features/master-data/suppliers/types/supplier'

const api = createCrudApi<Supplier, CreateSupplierRequest, UpdateSupplierRequest>({
  baseUrl: '/suppliers',
  features: { getActive: true, batchCreate: true }
})

// 向后兼容具名导出
export const getSuppliers = api.getList
export const getSupplierById = api.getById
export const createSupplier = api.create
export const updateSupplier = api.update
export const deleteSupplier = api.remove
export const getActiveSuppliers = api.getActive
export const batchCreateSuppliers = api.batchCreate

export const getSupplierStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<SupplierStatistics>>({ url: '/suppliers/statistics', method: 'get', params })

export const getSupplierFinanceSummary = (id: number) =>
  request<ApiResponse<SupplierFinanceSummary>>({ url: `/suppliers/${id}/finance-summary`, method: 'get' })
