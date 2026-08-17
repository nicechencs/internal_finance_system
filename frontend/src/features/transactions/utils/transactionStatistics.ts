import type { Transaction, TransactionStatistics } from '@/features/transactions/types/transaction'

export const buildTransactionStatistics = (transactions: Transaction[]): TransactionStatistics => {
  return transactions.reduce<TransactionStatistics>((summary, transaction) => {
    summary.totalCount += 1
    const type = transaction.transactionType.toLowerCase()

    if (type === 'income') {
      summary.totalIncome += transaction.amount
      summary.incomeCount += 1
      summary.netProfit += transaction.amount
      return summary
    }

    if (type === 'expense') {
      summary.totalExpense += transaction.amount
      summary.expenseCount += 1
      summary.netProfit -= transaction.amount
      return summary
    }

    if (type === 'transfer') {
      summary.totalTransfer += transaction.amount
      summary.transferCount += 1
    }

    return summary
  }, {
    totalIncome: 0,
    totalExpense: 0,
    netProfit: 0,
    totalTransfer: 0,
    incomeCount: 0,
    expenseCount: 0,
    transferCount: 0,
    totalCount: 0
  })
}
