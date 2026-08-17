export enum TagScope {
  Transaction = 'transaction',
  Project = 'project',
  Person = 'person',
  Customer = 'customer',
  Supplier = 'supplier',
  Receivable = 'receivable',
  Payable = 'payable'
}

export enum TagMatchMode {
  Or = 'or',
  And = 'and'
}

export interface Tag {
  id: number
  scope: string
  name: string
  code?: string
  color?: string
  sortOrder: number
  description?: string
  isActive: boolean
  isSystem: boolean
  createdAt: string
  updatedAt: string
}

export interface TagBinding {
  id: number
  tagId: number
  tagName: string
  tagColor?: string
  tagIsDeleted: boolean
  ownerType: string
  ownerId: number
}

export interface CreateTagRequest {
  scope: string
  name: string
  code?: string
  color?: string
  sortOrder?: number
  description?: string
  isActive?: boolean
}

export interface UpdateTagRequest {
  name: string
  code?: string
  color?: string
  sortOrder: number
  description?: string
  isActive: boolean
}

export interface SetBindingsRequest {
  ownerType: string
  ownerId: number
  tagIds: number[]
}

export interface BatchSetBindingsRequest {
  ownerType: string
  ownerIds: number[]
  tagIds: number[]
}

export interface TagFilterGroup {
  scope: string
  tagIds: number[]
  matchMode?: string  // 'or' | 'and'
}

// ── 分析统计类型 ──

export interface TagSummaryItemDto {
  tagId: number
  tagName: string
  tagColor?: string
  transactionCount: number
  incomeAmount: number
  expenseAmount: number
  netAmount: number
  incomePercentage: number
  expensePercentage: number
}

export interface TagSummaryDto {
  scope: string
  dateFrom?: string
  dateTo?: string
  totalTransactionCount: number
  totalIncomeAmount: number
  totalExpenseAmount: number
  totalNetAmount: number
  items: TagSummaryItemDto[]
}

export interface TagCrossAnalysisCellDto {
  rowTagId: number
  colTagId: number
  transactionCount: number
  incomeAmount: number
  expenseAmount: number
  netAmount: number
}

export interface TagCrossAnalysisDto {
  rowScope: string
  colScope: string
  dateFrom?: string
  dateTo?: string
  rowTags: TagSummaryItemDto[]
  colTags: TagSummaryItemDto[]
  cells: TagCrossAnalysisCellDto[]
}
