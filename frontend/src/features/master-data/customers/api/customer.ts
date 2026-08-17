import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { Customer, CustomerStatistics, CustomerFinanceSummary, CreateCustomerRequest, UpdateCustomerRequest } from '@/features/master-data/customers/types/customer'

const api = createCrudApi<Customer, CreateCustomerRequest, UpdateCustomerRequest>({
  baseUrl: '/customers',
  features: { getActive: true, batchCreate: true }
})

// 向后兼容具名导出
export const getCustomers = api.getList
export const getCustomerById = api.getById
export const createCustomer = api.create
export const updateCustomer = api.update
export const deleteCustomer = api.remove
export const getActiveCustomers = api.getActive
export const batchCreateCustomers = api.batchCreate

export const getCustomerStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<CustomerStatistics>>({ url: '/customers/statistics', method: 'get', params })

export const getCustomerFinanceSummary = (id: number) =>
  request<ApiResponse<CustomerFinanceSummary>>({ url: `/customers/${id}/finance-summary`, method: 'get' })
