export interface PageRequest {
  page: number
  pageSize: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
  startDate?: string
  endDate?: string
  transactionType?: string
  name?: string
  accountId?: number
  categoryId?: number
  projectId?: number
  customerId?: number
  status?: string
  tagFilters?: Array<{ scope: string; tagIds: number[]; matchMode?: string }>
  allocationStatus?: string
  minAmount?: number
  maxAmount?: number
  excludeTransfer?: boolean
}

export interface PageResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface User {
  id: number
  username: string
  email: string
  fullName: string
  role: 'Admin' | 'Accountant' | 'Viewer'
  isActive: boolean
}

export interface UserAdmin extends User {
  accessFailedCount: number
  isLocked: boolean
  lockoutEndAt?: string
  lastLoginAt?: string
  passwordChangedAt: string
  mustChangePassword: boolean
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  user: User
  mustChangePassword: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface CreateUserRequest {
  username: string
  password: string
  fullName: string
  email?: string
  role: 'Admin' | 'Accountant' | 'Viewer'
  isActive: boolean
  mustChangePassword: boolean
}

export interface SetUserPasswordRequest {
  newPassword: string
  mustChangePassword: boolean
}

export interface SetUserStatusRequest {
  isActive: boolean
}

export interface UpdateProfileRequest {
  fullName: string
  email?: string
}

export interface UpdateUserRequest {
  fullName: string
  email?: string
  role: 'Admin' | 'Accountant' | 'Viewer'
}
