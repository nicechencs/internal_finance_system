import type { ApiResponse } from '@/shared/utils/request'
import type { PageRequest, PageResponse } from '@/shared/types/common'

// 从 common 重导出分页类型，方便使用
export type { PageRequest, PageResponse }

/**
 * CRUD 操作的基础实体类型
 */
export interface CrudEntity {
  id: number
  createdAt?: string
  updatedAt?: string
}

/**
 * 创建请求类型（排除 id 和时间戳）
 */
export type CreateRequest<T extends CrudEntity> = Omit<
  T,
  'id' | 'createdAt' | 'updatedAt'
>

/**
 * 更新请求类型（排除时间戳，所有字段可选）
 */
export type UpdateRequest<T extends CrudEntity> = Partial<
  Omit<T, 'createdAt' | 'updatedAt'>
>

/**
 * API 响应包装类型（基于 AxiosResponse）
 */
export type ApiResult<T> = Promise<import('axios').AxiosResponse<ApiResponse<T>>>
