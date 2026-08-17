import { describe, expect, it } from 'vitest'
import {
  filterTransactionsByType,
  getTransactionTypeLabel,
  getTransactionTypeTagType
} from '@/shared/utils/transactionType'
import type { Transaction } from '@/features/transactions/types/transaction'

const createTransaction = (transactionType: Transaction['transactionType']): Pick<Transaction, 'transactionType'> => ({
  transactionType
})

describe('transactionType', () => {
  describe('getTransactionTypeLabel', () => {
    it('将收入和支出映射为中文标签', () => {
      expect(getTransactionTypeLabel('Income')).toBe('收入')
      expect(getTransactionTypeLabel('Expense')).toBe('支出')
    })

    it('转账按方向显示转入、转出或转账', () => {
      expect(getTransactionTypeLabel('Transfer', 'Out')).toBe('转出')
      expect(getTransactionTypeLabel('Transfer', 'In')).toBe('转入')
      expect(getTransactionTypeLabel('Transfer')).toBe('转账')
      expect(getTransactionTypeLabel('Transfer', 'None')).toBe('转账')
    })

    it('不会把转账误标成支出', () => {
      expect(getTransactionTypeLabel('Transfer')).not.toBe('支出')
    })
  })

  describe('getTransactionTypeTagType', () => {
    it('为三类交易返回不同标签色', () => {
      expect(getTransactionTypeTagType('Income')).toBe('success')
      expect(getTransactionTypeTagType('Expense')).toBe('danger')
      expect(getTransactionTypeTagType('Transfer')).toBe('info')
    })
  })

  describe('filterTransactionsByType', () => {
    const rows = [
      createTransaction('Income'),
      createTransaction('Expense'),
      createTransaction('Transfer')
    ]

    it('全部时不过滤', () => {
      expect(filterTransactionsByType(rows, 'all')).toHaveLength(3)
    })

    it('可按收入、支出、转账分开查看', () => {
      expect(filterTransactionsByType(rows, 'Income').map(item => item.transactionType)).toEqual(['Income'])
      expect(filterTransactionsByType(rows, 'Expense').map(item => item.transactionType)).toEqual(['Expense'])
      expect(filterTransactionsByType(rows, 'Transfer').map(item => item.transactionType)).toEqual(['Transfer'])
    })
  })
})
