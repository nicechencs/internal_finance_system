import { describe, expect, it } from 'vitest'
import {
  normalizeRecentTransaction,
  normalizeRecentTransactions
} from '@/features/dashboard/utils/normalizeRecentTransaction'

describe('normalizeRecentTransaction', () => {
  it('将后端 type 字段映射为 transactionType', () => {
    const result = normalizeRecentTransaction({
      id: 1,
      transactionDate: '2026-03-28',
      type: 'Income',
      amount: 100,
      accountName: '测试账户'
    })

    expect(result.transactionType).toBe('Income')
  })

  it('将小写 type 归一化为标准 transactionType', () => {
    const result = normalizeRecentTransaction({
      id: 1,
      transactionDate: '2026-03-28',
      type: 'expense',
      amount: 100,
      accountName: '测试账户'
    })

    expect(result.transactionType).toBe('Expense')
  })

  it('归一化最近交易数组', () => {
    const result = normalizeRecentTransactions([
      {
        id: 1,
        transactionDate: '2026-03-28',
        type: 'income',
        amount: 100,
        accountName: '测试账户'
      },
      {
        id: 2,
        transactionDate: '2026-03-28',
        type: 'transfer',
        amount: 50,
        accountName: '测试账户'
      }
    ])

    expect(result.map(item => item.transactionType)).toEqual(['Income', 'Transfer'])
  })
})
