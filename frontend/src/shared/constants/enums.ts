/**
 * 枚举常量定义
 * 与后端 FinanceApp.Domain.Enums 保持一致
 */

/** 标签选项接口 */
export interface LabelOption<V = string> {
  label: string
  value: V
  /** Element Plus tag 类型 */
  tagType?: '' | 'success' | 'warning' | 'danger' | 'info'
}

// ============================
// 账户类型
// ============================
export const ACCOUNT_TYPE = {
  BANK: 'Bank',
  ALIPAY: 'Alipay'
} as const

export const ACCOUNT_TYPE_OPTIONS: LabelOption[] = [
  { label: '银行账户', value: 'Bank' },
  { label: '支付宝', value: 'Alipay' }
]

// ============================
// 交易类型
// ============================
export const TRANSACTION_TYPE = {
  INCOME: 'Income',
  EXPENSE: 'Expense',
  TRANSFER: 'Transfer'
} as const

export const TRANSACTION_TYPE_OPTIONS: LabelOption[] = [
  { label: '收入', value: 'Income', tagType: 'success' },
  { label: '支出', value: 'Expense', tagType: 'danger' },
  { label: '转账', value: 'Transfer', tagType: 'info' }
]

// ============================
// 交易状态
// ============================
export const TRANSACTION_STATUS = {
  PENDING: 'Pending',
  CONFIRMED: 'Confirmed',
  CANCELLED: 'Cancelled'
} as const

export const TRANSACTION_STATUS_OPTIONS: LabelOption[] = [
  { label: '待确认', value: 'Pending', tagType: 'warning' },
  { label: '已确认', value: 'Confirmed', tagType: 'success' },
  { label: '已取消', value: 'Cancelled', tagType: 'info' }
]

// ============================
// 项目状态
// ============================
export const PROJECT_STATUS = {
  ACTIVE: 'Active',
  COMPLETED: 'Completed',
  CANCELLED: 'Cancelled'
} as const

export const PROJECT_STATUS_OPTIONS: LabelOption[] = [
  { label: '进行中', value: 'Active', tagType: 'success' },
  { label: '已完成', value: 'Completed', tagType: '' },
  { label: '已取消', value: 'Cancelled', tagType: 'info' }
]

// ============================
// 分类类型
// ============================
export const CATEGORY_TYPE = {
  INCOME: 'Income',
  EXPENSE: 'Expense'
} as const

export const CATEGORY_TYPE_OPTIONS: LabelOption[] = [
  { label: '收入', value: 'Income', tagType: 'success' },
  { label: '支出', value: 'Expense', tagType: 'danger' }
]

// ============================
// 人员类型
// ============================
export const PERSON_TYPE = {
  EMPLOYEE: 'Employee',
  PARTNER: 'Partner',
  SHAREHOLDER: 'Shareholder',
  CONTRACTOR: 'Contractor'
} as const

export const PERSON_TYPE_OPTIONS: LabelOption[] = [
  { label: '员工', value: 'Employee' },
  { label: '合伙人', value: 'Partner', tagType: 'success' },
  { label: '股东', value: 'Shareholder', tagType: 'warning' },
  { label: '外包人员', value: 'Contractor', tagType: 'info' }
]

// ============================
// 应收应付状态
// ============================
export const SETTLEMENT_STATUS = {
  PENDING: 'pending',
  PARTIAL: 'partial',
  SETTLED: 'settled'
} as const

export const SETTLEMENT_STATUS_OPTIONS: LabelOption[] = [
  { label: '待结算', value: 'pending', tagType: 'danger' },
  { label: '部分结算', value: 'partial', tagType: 'warning' },
  { label: '已结算', value: 'settled', tagType: 'success' }
]

// ============================
// 导入批次状态
// ============================
export const IMPORT_BATCH_STATUS = {
  PENDING: 'Pending',
  PROCESSING: 'Processing',
  COMPLETED: 'Completed',
  FAILED: 'Failed'
} as const

export const IMPORT_BATCH_STATUS_OPTIONS: LabelOption[] = [
  { label: '待处理', value: 'Pending', tagType: 'info' },
  { label: '处理中', value: 'Processing', tagType: 'warning' },
  { label: '已完成', value: 'Completed', tagType: 'success' },
  { label: '失败', value: 'Failed', tagType: 'danger' }
]

/**
 * 根据枚举值获取中文标签
 * @param options - 选项数组
 * @param value - 枚举值
 * @returns 中文标签，找不到返回原值
 */
export function getEnumLabel(options: LabelOption[], value: string): string {
  return options.find((o) => o.value === value)?.label ?? value
}

/**
 * 根据枚举值获取 tag 类型
 * @param options - 选项数组
 * @param value - 枚举值
 * @returns Element Plus tag 类型
 */
export function getEnumTagType(
  options: LabelOption[],
  value: string
): LabelOption['tagType'] {
  return options.find((o) => o.value === value)?.tagType ?? ''
}
