import type { RecentTransaction } from '@/features/dashboard/types/dashboard'

interface RecentTransactionApiModel extends Omit<RecentTransaction, 'transactionType'> {
  type: string
}

const TRANSACTION_TYPE_MAP = {
  income: 'Income',
  expense: 'Expense',
  transfer: 'Transfer'
} as const

function normalizeTransactionType(value: string): RecentTransaction['transactionType'] {
  const normalized = TRANSACTION_TYPE_MAP[value.toLowerCase() as keyof typeof TRANSACTION_TYPE_MAP]
  return (normalized ?? value) as RecentTransaction['transactionType']
}

export function normalizeRecentTransaction(item: RecentTransactionApiModel): RecentTransaction {
  return {
    id: item.id,
    transactionDate: item.transactionDate,
    transactionType: normalizeTransactionType(item.type),
    amount: item.amount,
    accountName: item.accountName,
    categoryName: item.categoryName,
    counterpartyName: item.counterpartyName,
    description: item.description
  }
}

export function normalizeRecentTransactions(items: RecentTransactionApiModel[]): RecentTransaction[] {
  return items.map(normalizeRecentTransaction)
}
