import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { Rule, CreateRuleRequest, UpdateRuleRequest } from '@/features/reconciliation/types/rule'

const api = createCrudApi<Rule, CreateRuleRequest, UpdateRuleRequest>({
  baseUrl: '/rules',
  features: { getActive: true }
})

// 向后兼容具名导出
export const getRules = api.getList
export const getRuleById = api.getById
export const createRule = api.create
export const updateRule = api.update
export const deleteRule = api.remove
export const getActiveRules = api.getActive

// 特殊业务方法
export const matchTransaction = (data: any) =>
  request<ApiResponse<any>>({ url: '/rules/match', method: 'post', data })
