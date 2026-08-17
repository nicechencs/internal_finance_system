import type { Transaction } from '@/features/transactions/types/transaction'

const TRANSACTION_TYPE_MAP = {
  income: 'Income',
  expense: 'Expense',
  transfer: 'Transfer'
} as const

const TRANSFER_DIRECTION_MAP = {
  none: 'None',
  out: 'Out',
  in: 'In'
} as const

const TRANSACTION_STATUS_MAP = {
  pending: 'Pending',
  confirmed: 'Confirmed',
  cancelled: 'Cancelled'
} as const

export function normalizeTransactionType(value: string): Transaction['transactionType'] {
  const normalized = TRANSACTION_TYPE_MAP[value.toLowerCase() as keyof typeof TRANSACTION_TYPE_MAP]
  return (normalized ?? value) as Transaction['transactionType']
}

export function normalizeTransferDirection(value?: string | null): Transaction['transferDirection'] {
  if (!value) {
    return value as Transaction['transferDirection']
  }

  const normalized = TRANSFER_DIRECTION_MAP[value.toLowerCase() as keyof typeof TRANSFER_DIRECTION_MAP]
  return (normalized ?? value) as Transaction['transferDirection']
}

export function normalizeTransactionStatus(value: string): Transaction['status'] {
  const normalized = TRANSACTION_STATUS_MAP[value.toLowerCase() as keyof typeof TRANSACTION_STATUS_MAP]
  return (normalized ?? value) as Transaction['status']
}

export function normalizeTransaction<T extends Transaction>(transaction: T): T {
  return {
    ...transaction,
    transactionType: normalizeTransactionType(transaction.transactionType),
    transferDirection: normalizeTransferDirection(transaction.transferDirection),
    status: normalizeTransactionStatus(transaction.status)
  }
}

export function normalizeTransactions<T extends Transaction>(transactions: T[]): T[] {
  return transactions.map(normalizeTransaction)
}
