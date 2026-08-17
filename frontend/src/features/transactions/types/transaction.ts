export interface Transaction {
  id: number
  bankTransactionId?: number
  transactionDate: string
  transactionType: 'Income' | 'Expense' | 'Transfer'
  transferDirection?: 'None' | 'Out' | 'In'
  amount: number
  accountId: number
  accountName: string
  categoryId?: number
  categoryName?: string
  projectId?: number
  projectName?: string
  customerId?: number
  customerName?: string
  supplierId?: number
  supplierName?: string
  personId?: number
  personName?: string
  counterparty?: string
  transactionTime?: string
  counterpartyAccount?: string
  counterpartyBank?: string
  transactionNumber?: string
  description?: string
  memo?: string
  status: string
  isAllocated: boolean
  availableAmount?: number
  allocationStatus?: 'Unallocated' | 'PartiallyAllocated' | 'FullyAllocated'
  relatedTransactionId?: number
  relatedAccountName?: string
  allocations: TransactionAllocation[]
  tags: TagItem[]
  createdAt: string
}

export interface TagItem {
  tagId: number
  tagName: string
  tagColor?: string
}

export interface TransactionAllocation {
  id: number
  projectId?: number
  projectName?: string
  personId?: number
  personName?: string
  amount: number
  allocationRate?: number
  description?: string
}

export interface CreateTransactionRequest {
  transactionDate: string
  transactionType: 'Income' | 'Expense'
  amount: number
  accountId: number
  categoryId?: number
  projectId?: number
  customerId?: number
  supplierId?: number
  personId?: number
  description?: string
  allocations?: CreateAllocationRequest[]
}

export interface CreateAllocationRequest {
  projectId?: number
  personId?: number
  amount?: number
  allocationRate?: number
  description?: string
}

export interface UpdateTransactionRequest {
  transactionDate: string
  transactionType: 'Income' | 'Expense'
  amount: number
  accountId: number
  categoryId?: number
  projectId?: number
  customerId?: number
  supplierId?: number
  personId?: number
  description?: string
  allocations?: CreateAllocationRequest[]
}

export interface CreateTransferRequest {
  fromAccountId: number
  toAccountId: number
  amount: number
  transactionDate: string
  description?: string
  // 定期存款参数（转入定期账户时使用）
  termMonths?: number
  interestRate?: number
}

export interface ConvertTransactionToTransferRequest {
  targetAccountId: number
  matchedTransactionId?: number
  description?: string
}

export interface TransferResult {
  outTransaction: Transaction
  inTransaction: Transaction
  fixedDepositLinkage?: {
    action: string
    fixedDepositId: number
    message?: string
  }
}

/**
 * 交易统计数据
 * 用于全局、账户级别、客户级别的交易统计
 */
export interface TransactionStatistics {
  /** 总收入金额 */
  totalIncome: number
  /** 总支出金额 */
  totalExpense: number
  /** 净利润（收入 - 支出） */
  netProfit: number
  /** 转账总额 */
  totalTransfer: number
  /** 收入交易数量 */
  incomeCount: number
  /** 支出交易数量 */
  expenseCount: number
  /** 转账交易数量 */
  transferCount: number
  /** 总交易数量 */
  totalCount: number
}

export interface RelatedFinanceRecord {
  receivables: RelatedReceivable[]
  payables: RelatedPayable[]
}

export interface RelatedReceivable {
  id: number
  projectId: number
  projectName: string
  customerId: number
  customerName: string
  totalAmount: number
  receivedAmount: number
  remainingAmount: number
  dueDate?: string
  status: string
  description?: string
  paymentAmount: number
  paymentDate: string
}

export interface RelatedPayable {
  id: number
  supplierId: number
  supplierName: string
  projectId?: number
  projectName?: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  dueDate?: string
  status: string
  description?: string
  paymentAmount: number
  paymentDate: string
}

