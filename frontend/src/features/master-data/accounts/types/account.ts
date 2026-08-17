export interface Account {
  id: number
  name: string
  accountType: string
  openingBalance: number
  currentBalance: number
  balance?: number
  currency: string
  bankName?: string
  accountNumber?: string
  description?: string
  isActive: boolean
  createdAt: string
  interestStartDate?: string
  maturityDate?: string
  interestRate?: number
  autoRenewal?: boolean
}

export interface CreateAccountRequest {
  name: string
  accountType: string
  initialBalance: number
  currency: string
  bankName?: string
  accountNumber?: string
}

export interface UpdateAccountRequest {
  name: string
  isActive: boolean
  bankName?: string
  accountNumber?: string
}

export interface AccountStatistics {
  totalCount: number
  activeCount: number
  totalBalance: number
  fixedDepositCount: number
}

export interface BalanceTrendItem {
  date: string
  balance: number
}

export interface BalanceTrendResponse {
  trends: BalanceTrendItem[]
  currentBalance: number
  openingBalance: number
}
