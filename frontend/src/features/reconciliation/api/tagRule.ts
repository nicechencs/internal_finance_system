import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  TagRule,
  CreateTagRuleRequest,
  UpdateTagRuleRequest,
  RunTagRulesRequest,
  RunTagRulesResult,
  RerunPreviewRequest,
  RerunPreviewResponse,
  RerunConfirmRequest,
  RerunConfirmResponse
} from '@/features/reconciliation/types/tagRule'

const api = createCrudApi<TagRule, CreateTagRuleRequest, UpdateTagRuleRequest>({
  baseUrl: '/tag-rules'
})

export const getTagRules = api.getList
export const getTagRuleById = api.getById
export const createTagRule = api.create
export const updateTagRule = api.update
export const deleteTagRule = api.remove

export const runTagRules = (data: RunTagRulesRequest) =>
  request<ApiResponse<RunTagRulesResult>>({ url: '/tag-rules/run', method: 'post', data })

export const previewTagRulesRerun = (data: RerunPreviewRequest) =>
  request<ApiResponse<RerunPreviewResponse>>({ url: '/tag-rules/rerun/preview', method: 'post', data })

export const confirmTagRulesRerun = (data: RerunConfirmRequest) =>
  request<ApiResponse<RerunConfirmResponse>>({ url: '/tag-rules/rerun/confirm', method: 'post', data })
