import type { TagItem } from '@/features/master-data/projects/types/project'

export interface Payable {
  id: number
  supplierId?: number
  supplierName?: string
  customerId?: number
  customerName?: string
  personId?: number
  personName?: string
  projectId?: number
  projectName?: string
  payableTypeId?: number
  payableTypeName?: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  dueDate?: string
  status: 'pending' | 'partial' | 'settled'
  description?: string
  tags: TagItem[]
  createdAt: string
  updatedAt: string
  settledAt?: string
  details: PayableDetail[]
}

export interface PayableDetail {
  id: number
  payableId: number
  transactionId: number
  paymentDate: string
  amount: number
  paymentMethod?: string
  description?: string
  createdAt: string
}

export interface PayableSummary {
  totalPayable: number
  totalPaid: number
  totalRemaining: number
  pendingCount: number
  partialCount: number
  settledCount: number
  overdueCount: number
}

export interface PayableTrend {
  months: string[]
  amounts: number[]
}

export interface PayableAging {
  categories: string[]
  amounts: number[]
}

export interface CreatePayableRequest {
  supplierId?: number
  customerId?: number
  personId?: number
  projectId?: number
  payableTypeId?: number
  totalAmount: number
  dueDate?: string
  description?: string
}

export interface UpdatePayableRequest {
  supplierId?: number
  customerId?: number
  personId?: number
  projectId?: number
  payableTypeId?: number
  totalAmount: number
  dueDate?: string
  description?: string
}

export interface PayPaymentRequest {
  paymentDate: string
  amount: number
  paymentMethod?: string
  description?: string
  transactionId: number
}

export interface PayableStatistics {
  totalCount: number
  pendingCount: number
  partialCount: number
  settledCount: number
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  overdueAmount: number
}

export interface PayableType {
  id: number
  name: string
  code?: string
  description?: string
  isActive: boolean
  sortOrder: number
}

export interface CreatePayableTypeRequest {
  name: string
  code?: string
  description?: string
  isActive: boolean
  sortOrder: number
}

export interface UpdatePayableTypeRequest {
  name: string
  code?: string
  description?: string
  isActive: boolean
  sortOrder: number
}
