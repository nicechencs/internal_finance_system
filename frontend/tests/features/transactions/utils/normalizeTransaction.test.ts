import { describe, expect, it } from 'vitest'
import {
  normalizeTransaction,
  normalizeTransactionType,
  normalizeTransferDirection,
  normalizeTransactionStatus,
  normalizeTransactions
} from '@/features/transactions/utils/normalizeTransaction'

describe('normalizeTransaction', () => {
  it('将小写 transactionType 归一化为首字母大写枚举值', () => {
    expect(normalizeTransactionType('income')).toBe('Income')
    expect(normalizeTransactionType('expense')).toBe('Expense')
    expect(normalizeTransactionType('transfer')).toBe('Transfer')
  })

  it('将小写 transferDirection 归一化为首字母大写枚举值', () => {
    expect(normalizeTransferDirection('none')).toBe('None')
    expect(normalizeTransferDirection('out')).toBe('Out')
    expect(normalizeTransferDirection('in')).toBe('In')
  })

  it('将小写 status 归一化为首字母大写枚举值', () => {
    expect(normalizeTransactionStatus('pending')).toBe('Pending')
    expect(normalizeTransactionStatus('confirmed')).toBe('Confirmed')
    expect(normalizeTransactionStatus('cancelled')).toBe('Cancelled')
  })

  it('归一化单条交易对象', () => {
    const transaction = normalizeTransaction({
      id: 1,
      transactionDate: '2026-03-28',
      transactionType: 'income',
      transferDirection: 'out',
      amount: 100,
      accountId: 1,
      accountName: '测试账户',
      status: 'confirmed',
      isAllocated: false,
      allocations: [],
      createdAt: '2026-03-28T08:00:00'
    } as any)

    expect(transaction.transactionType).toBe('Income')
    expect(transaction.transferDirection).toBe('Out')
    expect(transaction.status).toBe('Confirmed')
  })

  it('归一化交易数组', () => {
    const transactions = normalizeTransactions([
      {
        id: 1,
        transactionDate: '2026-03-28',
        transactionType: 'income',
        amount: 100,
        accountId: 1,
        accountName: '测试账户',
        status: 'confirmed',
        isAllocated: false,
        allocations: [],
        createdAt: '2026-03-28T08:00:00'
      },
      {
        id: 2,
        transactionDate: '2026-03-28',
        transactionType: 'expense',
        amount: 50,
        accountId: 1,
        accountName: '测试账户',
        status: 'confirmed',
        isAllocated: false,
        allocations: [],
        createdAt: '2026-03-28T09:00:00'
      }
    ] as any)

    expect(transactions.map(item => item.transactionType)).toEqual(['Income', 'Expense'])
  })
})
