import {
  TRANSACTION_TYPE_OPTIONS,
  getEnumLabel,
  getEnumTagType
} from '@/shared/constants/enums'
import type { Transaction } from '@/features/transactions/types/transaction'

export type TransactionTypeFilter = 'all' | 'Income' | 'Expense' | 'Transfer'

export function getTransactionTypeLabel(
  transactionType?: string,
  transferDirection?: string | null
): string {
  const type = (transactionType || '').toLowerCase()
  if (type === 'transfer') {
    const direction = (transferDirection || '').toLowerCase()
    if (direction === 'out') return '转出'
    if (direction === 'in') return '转入'
    return '转账'
  }
  return getEnumLabel(TRANSACTION_TYPE_OPTIONS, transactionType || '')
}

export function getTransactionTypeTagType(transactionType?: string) {
  return getEnumTagType(TRANSACTION_TYPE_OPTIONS, transactionType || '') || 'info'
}

export function filterTransactionsByType<T extends Pick<Transaction, 'transactionType'>>(
  transactions: T[],
  typeFilter: TransactionTypeFilter
): T[] {
  if (!typeFilter || typeFilter === 'all') return transactions
  return transactions.filter(item => item.transactionType === typeFilter)
}
