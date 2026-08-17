export interface FixedDeposit {
  id: number
  accountId: number
  accountName: string
  principal: number
  depositDate: string
  maturityDate: string
  termMonths: number
  interestRate: number
  status: string
  withdrawalDate?: string
  actualInterest?: number
  isEarlyWithdrawal: boolean
  daysToMaturity: number
  expectedInterest: number
  notes?: string
  depositTransactionId: number
  createdAt: string
}

export interface CreateFixedDepositRequest {
  accountId: number
  principal: number
  termMonths: number
  interestRate: number
  depositDate?: string
  notes?: string
}

export interface UpdateFixedDepositRequest {
  accountId: number
  principal: number
  depositDate: string
  termMonths: number
  interestRate: number
  notes?: string
}

export interface WithdrawFixedDepositRequest {
  withdrawalDate?: string
  actualInterest?: number
  transactionId: number  // 必须关联的交易记录ID
}

export interface FixedDepositStatistics {
  totalCount: number
  activeCount: number
  withdrawnCount: number
  upcomingCount: number
  totalPrincipal: number
  activePrincipal: number
  expectedInterest: number
}
