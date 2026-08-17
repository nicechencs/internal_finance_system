export interface Supplier {
  id: number
  name: string
  shortName?: string
  contactPerson?: string
  contactPhone?: string
  contactEmail?: string
  address?: string
  taxNumber?: string
  bankAccount?: string
  bankName?: string
  description?: string
  isActive: boolean
  createdAt: string
}

export interface SupplierStatistics {
  totalCount: number
  activeCount: number
  thisMonthNewCount: number
  inactiveCount: number
}

export interface CreateSupplierRequest {
  name: string
  shortName?: string
  contactPerson?: string
  contactPhone?: string
  contactEmail?: string
  address?: string
  taxNumber?: string
  bankAccount?: string
  bankName?: string
  description?: string
}

export interface UpdateSupplierRequest {
  name: string
  shortName?: string
  contactPerson?: string
  contactPhone?: string
  contactEmail?: string
  address?: string
  taxNumber?: string
  bankAccount?: string
  bankName?: string
  description?: string
  isActive: boolean
}

export interface SupplierFinanceSummary {
  totalReceivable: number
  totalReceived: number
  receivableRemaining: number
  receivableOverdueCount: number
  receivableOverdueAmount: number
  totalPayable: number
  totalPaid: number
  payableRemaining: number
  payableOverdueCount: number
  payableOverdueAmount: number
  projectCount: number
}
