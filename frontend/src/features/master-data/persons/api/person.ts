import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { Person, PersonStatistics, PersonFinanceSummary, CreatePersonRequest, UpdatePersonRequest } from '@/features/master-data/persons/types/person'

const api = createCrudApi<Person, CreatePersonRequest, UpdatePersonRequest>({
  baseUrl: '/persons',
  features: { getActive: true, batchCreate: true }
})

// 向后兼容具名导出
export const getPersons = api.getList
export const getPersonById = api.getById
export const createPerson = api.create
export const updatePerson = api.update
export const deletePerson = api.remove
export const getActivePersons = api.getActive
export const batchCreatePersons = api.batchCreate

// 特殊业务方法
export const getPersonCostSummary = (id: number) =>
  request<ApiResponse<any>>({ url: `/persons/${id}/cost-summary`, method: 'get' })

export const getPersonFinanceSummary = (id: number) =>
  request<ApiResponse<PersonFinanceSummary>>({ url: `/persons/${id}/finance-summary`, method: 'get' })

export const getPersonStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<PersonStatistics>>({ url: '/persons/statistics', method: 'get', params })
