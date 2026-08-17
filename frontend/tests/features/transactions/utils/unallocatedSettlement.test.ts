import { describe, expect, it } from 'vitest'
import type { Transaction } from '@/features/transactions/types/transaction'
import {
  buildCreateDescription,
  evaluateBatchCreate,
  isPreferredMatch,
  pickSingleCounterparty,
  toDateOnly
} from '@/features/transactions/utils/unallocatedSettlement'

const baseTransaction = (overrides: Partial<Transaction> = {}): Transaction => ({
  id: 1,
  transactionDate: '2026-08-16T10:00:00Z',
  transactionType: 'Income',
  amount: 1000,
  accountId: 1,
  accountName: '基本户',
  projectId: 11,
  projectName: '项目A',
  customerId: 21,
  customerName: '客户A',
  description: '到账',
  status: 'Confirmed',
  isAllocated: false,
  availableAmount: 1000,
  allocations: [],
  tags: [],
  createdAt: '2026-08-16T10:00:00Z',
  ...overrides
})

describe('unallocatedSettlement', () => {
  it('evaluateBatchCreate 跳过无项目的收入', () => {
    const result = evaluateBatchCreate(baseTransaction({ projectId: undefined }), 'receivable')
    expect(result.ok).toBe(false)
    if (!result.ok) expect(result.reason).toContain('项目')
  })

  it('evaluateBatchCreate 跳过无对方的支出', () => {
    const result = evaluateBatchCreate(baseTransaction({
      transactionType: 'Expense',
      customerId: undefined,
      supplierId: undefined,
      personId: undefined
    }), 'payable')
    expect(result.ok).toBe(false)
    if (!result.ok) expect(result.reason).toContain('对方')
  })

  it('evaluateBatchCreate 接受完整收入交易', () => {
    expect(evaluateBatchCreate(baseTransaction(), 'receivable')).toEqual({ ok: true })
  })

  it('pickSingleCounterparty 只保留一种对方', () => {
    expect(pickSingleCounterparty(baseTransaction({
      customerId: 21,
      supplierId: 31,
      personId: 41
    }))).toEqual({ customerId: 21 })
  })

  it('isPreferredMatch 识别同项目同客户', () => {
    expect(isPreferredMatch(baseTransaction(), {
      id: 9,
      projectId: 11,
      customerId: 21,
      remainingAmount: 200
    })).toBe(true)
    expect(isPreferredMatch(baseTransaction(), {
      id: 10,
      projectId: 99,
      customerId: 21,
      remainingAmount: 200
    })).toBe(false)
  })

  it('buildCreateDescription 带上日期和账户', () => {
    expect(buildCreateDescription(baseTransaction())).toContain('待分配补录')
    expect(buildCreateDescription(baseTransaction())).toContain('基本户')
    expect(toDateOnly('2026-08-16T10:00:00Z')).toBe('2026-08-16')
  })
})
