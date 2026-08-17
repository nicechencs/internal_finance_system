// 分类类型
export interface Category {
  id: number
  name: string
  categoryType: 'Income' | 'Expense'
  parentId?: number
  parentName?: string
  description?: string
  isActive: boolean
  createdAt: string
}

export interface CategoryStatistics {
  totalCount: number
  incomeCategoryCount: number
  expenseCategoryCount: number
  activeCount: number
}

export interface CreateCategoryRequest {
  name: string
  categoryType: 'Income' | 'Expense'
  parentId?: number
  description?: string
}

export interface UpdateCategoryRequest {
  name: string
  parentId?: number
  description?: string
  isActive: boolean
}
