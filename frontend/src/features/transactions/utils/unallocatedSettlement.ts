import type { Transaction } from '@/features/transactions/types/transaction'

export interface SettlementCandidate {
  id: number
  projectId?: number
  projectName?: string
  customerId?: number
  customerName?: string
  supplierId?: number
  supplierName?: string
  personId?: number
  personName?: string
  remainingAmount: number
  dueDate?: string
  description?: string
}

export function toDateOnly(value?: string): string {
  if (!value) return ''
  return value.split('T')[0]
}

export function getCounterpartyName(item: {
  customerName?: string
  supplierName?: string
  personName?: string
}): string {
  return item.customerName || item.supplierName || item.personName || '无对方'
}

export function isPreferredMatch(transaction: Transaction, item: SettlementCandidate): boolean {
  const projectOk = !transaction.projectId || transaction.projectId === item.projectId
  const hasTxCounterpart = !!(transaction.customerId || transaction.supplierId || transaction.personId)
  const counterpartOk =
    (transaction.customerId && transaction.customerId === item.customerId) ||
    (transaction.supplierId && transaction.supplierId === item.supplierId) ||
    (transaction.personId && transaction.personId === item.personId) ||
    !hasTxCounterpart

  return projectOk && counterpartOk
}

export function isProjectMismatch(transaction: Transaction, item: SettlementCandidate): boolean {
  return !!transaction.projectId && !!item.projectId && transaction.projectId !== item.projectId
}

export function evaluateBatchCreate(
  transaction: Transaction,
  kind: 'receivable' | 'payable'
): { ok: true } | { ok: false; reason: string } {
  if (kind === 'receivable' && transaction.transactionType !== 'Income') {
    return { ok: false, reason: '不是收入交易' }
  }
  if (kind === 'payable' && transaction.transactionType !== 'Expense') {
    return { ok: false, reason: '不是支出交易' }
  }
  if ((transaction.availableAmount ?? 0) <= 0) {
    return { ok: false, reason: '没有可核销余额' }
  }
  if (kind === 'receivable' && !transaction.projectId) {
    return { ok: false, reason: '收入交易缺少项目，无法自动创建应收' }
  }
  if (!transaction.customerId && !transaction.supplierId && !transaction.personId) {
    return { ok: false, reason: '缺少对方（客户/供应商/人员）' }
  }
  return { ok: true }
}

export function pickSingleCounterparty(transaction: Transaction): {
  customerId?: number
  supplierId?: number
  personId?: number
} {
  if (transaction.customerId) return { customerId: transaction.customerId }
  if (transaction.supplierId) return { supplierId: transaction.supplierId }
  if (transaction.personId) return { personId: transaction.personId }
  return {}
}

export function buildCreateDescription(transaction: Transaction): string {
  const date = toDateOnly(transaction.transactionDate)
  const parts = ['待分配补录', date, transaction.accountName, transaction.description]
    .filter(Boolean)
  return parts.join(' ')
}

export function formatSettlementOptionLabel(
  item: SettlementCandidate,
  formatCurrency: (value: number) => string,
  transaction?: Transaction
): string {
  const project = item.projectName || '无项目'
  const mismatch = transaction && isProjectMismatch(transaction, item) ? '（项目不一致）' : ''
  return `${project}${mismatch} - ${getCounterpartyName(item)} - 剩余: ${formatCurrency(item.remainingAmount)}`
}
