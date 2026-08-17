import type { TagItem } from '@/features/master-data/projects/types/project'

export interface Receivable {
  id: number
  projectId: number
  projectName?: string
  customerId?: number
  customerName?: string
  supplierId?: number
  supplierName?: string
  personId?: number
  personName?: string
  receivableTypeId?: number
  receivableTypeName?: string
  totalAmount: number
  receivedAmount: number
  remainingAmount: number
  dueDate?: string
  status: 'pending' | 'partial' | 'settled'
  description?: string
  tags: TagItem[]
  createdAt: string
  updatedAt: string
  settledAt?: string
  details: ReceivableDetail[]
}

export interface ReceivableDetail {
  id: number
  receivableId: number
  transactionId: number
  paymentDate: string
  amount: number
  paymentMethod?: string
  description?: string
  createdAt: string
}

export interface ReceivableSummary {
  totalReceivable: number
  totalReceived: number
  totalRemaining: number
  pendingCount: number
  partialCount: number
  settledCount: number
  overdueCount: number
}

export interface ReceivableTrend {
  months: string[]
  amounts: number[]
}

export interface ReceivableAging {
  categories: string[]
  amounts: number[]
}

export interface CreateReceivableRequest {
  projectId: number
  customerId?: number
  supplierId?: number
  personId?: number
  receivableTypeId?: number
  totalAmount: number
  dueDate?: string
  description?: string
}

export interface UpdateReceivableRequest {
  projectId: number
  customerId?: number
  supplierId?: number
  personId?: number
  receivableTypeId?: number
  totalAmount: number
  dueDate?: string
  description?: string
}

export interface ReceivePaymentRequest {
  paymentDate: string
  amount: number
  paymentMethod?: string
  description?: string
  transactionId: number
}

export interface ReceivableStatistics {
  totalCount: number
  pendingCount: number
  partialCount: number
  settledCount: number
  totalAmount: number
  receivedAmount: number
  remainingAmount: number
  overdueAmount: number
}

export interface ReceivableType {
  id: number
  name: string
  code?: string
  description?: string
  isActive: boolean
  sortOrder: number
}

export interface CreateReceivableTypeRequest {
  name: string
  code?: string
  description?: string
  isActive?: boolean
  sortOrder?: number
}

export interface UpdateReceivableTypeRequest {
  name: string
  code?: string
  description?: string
  isActive?: boolean
  sortOrder?: number
}
