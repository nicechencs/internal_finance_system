import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  LinkPreviewRequest,
  LinkPreviewResponse,
  LinkConfirmRequest,
  LinkConfirmResponse,
  RuleRerunPreviewRequest,
  RuleRerunPreviewResponse,
  RuleRerunConfirmRequest,
  RuleRerunConfirmResponse,
  BatchLinkPreviewResponse,
  BatchLinkConfirmRequest,
  BatchLinkConfirmResponse
} from '@/features/transactions/types/link'

export const previewLink = (data: LinkPreviewRequest) => {
  return request<ApiResponse<LinkPreviewResponse>>({
    url: '/links/preview',
    method: 'post',
    data
  })
}

export const confirmLink = (data: LinkConfirmRequest) => {
  return request<ApiResponse<LinkConfirmResponse>>({
    url: '/links/confirm',
    method: 'post',
    data
  })
}

export const previewRuleRerun = (data: RuleRerunPreviewRequest) => {
  return request<ApiResponse<RuleRerunPreviewResponse>>({
    url: '/links/rule-rerun/preview',
    method: 'post',
    data
  })
}

export const confirmRuleRerun = (data: RuleRerunConfirmRequest) => {
  return request<ApiResponse<RuleRerunConfirmResponse>>({
    url: '/links/rule-rerun/confirm',
    method: 'post',
    data
  })
}

export const previewBatchLink = () => {
  return request<ApiResponse<BatchLinkPreviewResponse>>({
    url: '/links/batch-preview',
    method: 'post'
  })
}

export const confirmBatchLink = (data: BatchLinkConfirmRequest) => {
  return request<ApiResponse<BatchLinkConfirmResponse>>({
    url: '/links/batch-confirm',
    method: 'post',
    data
  })
}
