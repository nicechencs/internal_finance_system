import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { Category, CategoryStatistics, CreateCategoryRequest, UpdateCategoryRequest } from '@/features/master-data/categories/types/category'

const api = createCrudApi<Category, CreateCategoryRequest, UpdateCategoryRequest>({
  baseUrl: '/categories',
  features: { getActive: true }
})

// 向后兼容具名导出
export const getCategories = api.getList
export const getCategoryById = api.getById
export const createCategory = api.create
export const updateCategory = api.update
export const deleteCategory = api.remove
export const getActiveCategories = api.getActive

// 特殊业务方法
export const getCategoriesByType = (type: string) =>
  request<ApiResponse<Category[]>>({ url: `/categories/by-type/${type}`, method: 'get' })

export const getCategoryStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<CategoryStatistics>>({ url: '/categories/statistics', method: 'get', params })
