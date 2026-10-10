export interface Person {
  id: number
  name: string
  personType: string
  department?: string
  position?: string
  idNumber?: string
  phone?: string
  email?: string
  bankAccount?: string
  bankName?: string
  joinDate?: string
  leaveDate?: string
  isActive: boolean
  createdAt: string
}

export interface PersonStatistics {
  totalCount: number
  activeCount: number
  inactiveCount: number
  thisMonthNewCount: number
}

export interface CreatePersonRequest {
  name: string
  personType: string
  department?: string
  position?: string
  idNumber?: string
  phone?: string
  email?: string
  bankAccount?: string
  bankName?: string
  joinDate?: string
}

export interface UpdatePersonRequest {
  name: string
  personType: string
  department?: string
  position?: string
  idNumber?: string
  phone?: string
  email?: string
  bankAccount?: string
  bankName?: string
  joinDate?: string
  leaveDate?: string
  isActive: boolean
}

export type PersonDeliveryStatus = 'Pending' | 'Accepted' | 'Rejected'

export interface PersonDelivery {
  id: number
  personId: number
  personName: string
  projectId?: number | null
  projectName?: string | null
  title: string
  description?: string | null
  status: PersonDeliveryStatus | string
  acceptedAt?: string | null
  acceptedBy?: string | null
  notes?: string | null
  createdAt: string
  updatedAt: string
}

export interface PersonDeliveryRequest {
  title: string
  description?: string
  projectId?: number | null
  status: PersonDeliveryStatus
  acceptedAt?: string | null
  acceptedBy?: string | null
  notes?: string | null
}

export interface PersonFinanceSummary {
  totalCost: number
  directCost: number
  allocatedCost: number
  transactionCount: number
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
