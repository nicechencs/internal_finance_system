import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  Tag,
  TagBinding,
  CreateTagRequest,
  UpdateTagRequest,
  SetBindingsRequest,
  BatchSetBindingsRequest,
  TagSummaryDto,
  TagCrossAnalysisDto
} from '../types/tag'

// 标签 CRUD
export const getTags = (params?: { scope?: string; isActive?: boolean }) =>
  request<ApiResponse<Tag[]>>({ url: '/tags', method: 'get', params })

export const getTagById = (id: number) =>
  request<ApiResponse<Tag>>({ url: `/tags/${id}`, method: 'get' })

export const createTag = (data: CreateTagRequest) =>
  request<ApiResponse<Tag>>({ url: '/tags', method: 'post', data })

export const updateTag = (id: number, data: UpdateTagRequest) =>
  request<ApiResponse<Tag>>({ url: `/tags/${id}`, method: 'put', data })

export const deleteTag = (id: number) =>
  request<ApiResponse<void>>({ url: `/tags/${id}`, method: 'delete' })

// 标签绑定
export const getTagBindings = (params: { ownerType: string; ownerId: number }) =>
  request<ApiResponse<TagBinding[]>>({ url: '/tags/bindings', method: 'get', params })

export const setTagBindings = (data: SetBindingsRequest) =>
  request<ApiResponse<void>>({ url: '/tags/bindings/set', method: 'put', data })

export const addTagBinding = (data: { ownerType: string; ownerId: number; tagId: number }) =>
  request<ApiResponse<void>>({ url: '/tags/bindings', method: 'post', data })

export const removeTagBinding = (data: { ownerType: string; ownerId: number; tagId: number }) =>
  request<ApiResponse<void>>({ url: '/tags/bindings', method: 'delete', data })

export const batchSetTagBindings = (data: BatchSetBindingsRequest) =>
  request<ApiResponse<void>>({ url: '/tags/bindings/batch', method: 'put', data })

// ── 分析统计 API ──

export const getTagSummary = (
  scope: string,
  params?: { dateFrom?: string; dateTo?: string }
) =>
  request<ApiResponse<TagSummaryDto>>({
    url: '/tags/analytics/summary',
    method: 'get',
    params: { scope, ...params }
  })

export const getTagCrossAnalysis = (
  rowScope: string,
  colScope: string,
  params?: { dateFrom?: string; dateTo?: string }
) =>
  request<ApiResponse<TagCrossAnalysisDto>>({
    url: '/tags/analytics/cross',
    method: 'get',
    params: { rowScope, colScope, ...params }
  })
