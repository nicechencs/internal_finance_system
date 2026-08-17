import { formatDateTime } from '@/shared/utils/formatters'

// 审计日志共享常量与工具函数
// 供 AuditLogListPage 和 AuditLogDetailDialog 复用

// ── 操作类型映射 ──
export const ACTION_LABEL_MAP: Record<string, string> = {
  Create: '创建',
  Update: '更新',
  Delete: '删除',
  Archive: '归档',
  Enable: '启用',
  Disable: '停用',
  Confirm: '确认导入',
  BatchLink: '批量关联',
  RuleRerun: '规则重跑',
  BatchLinkConfirm: '智能关联',
  ConvertToTransfer: '转为转账',
  Transfer: '转账',
  Withdraw: '支取',
  Login: '登录',
  Logout: '退出登录',
  LoginFailed: '登录失败',
  ChangePassword: '修改密码',
  UpdateProfile: '更新资料',
  SetPassword: '设置密码',
  Unlock: '解锁',
  AddBinding: '新增绑定',
  RemoveBinding: '移除绑定',
  SetBindings: '设置绑定',
}

export const ACTION_TAG_TYPE_MAP: Record<string, '' | 'success' | 'warning' | 'danger' | 'info'> = {
  Create: 'success',
  Update: 'warning',
  Delete: 'danger',
  Archive: 'warning',
  Enable: 'success',
  Disable: 'warning',
  Confirm: 'info',
  BatchLink: '',
  RuleRerun: 'info',
  BatchLinkConfirm: '',
  ConvertToTransfer: 'warning',
  Transfer: '',
  Withdraw: 'warning',
  Login: 'success',
  Logout: 'info',
  LoginFailed: 'danger',
  ChangePassword: 'warning',
  UpdateProfile: 'info',
  SetPassword: 'warning',
  Unlock: 'warning',
  AddBinding: 'success',
  RemoveBinding: 'warning',
  SetBindings: 'info',
}

// ── 实体类型映射 ──
export const ENTITY_TYPE_LABEL_MAP: Record<string, string> = {
  Transaction: '交易',
  Account: '账户',
  Category: '分类',
  Project: '项目',
  Customer: '客户',
  Supplier: '供应商',
  Person: '人员',
  Receivable: '应收账款',
  Payable: '应付账款',
  ClassificationRule: '分类规则',
  ImportBatch: '导入批次',
  FixedDeposit: '定期存款',
  Tag: '标签',
  TagBinding: '标签绑定',
  User: '用户',
}

// ── 属性名中文映射 ──
export const PROPERTY_LABEL_MAP: Record<string, string> = {
  id: 'ID',
  name: '名称',
  description: '描述',
  amount: '金额',
  transactionDate: '交易日期',
  transactionType: '交易类型',
  accountId: '账户ID',
  accountName: '账户名称',
  categoryId: '分类ID',
  categoryName: '分类名称',
  projectId: '项目ID',
  projectName: '项目名称',
  customerId: '客户ID',
  customerName: '客户名称',
  supplierId: '供应商ID',
  supplierName: '供应商名称',
  personId: '人员ID',
  personName: '人员名称',
  counterpartyType: '对方类型',
  remark: '备注',
  remarks: '备注',
  note: '备注',
  notes: '备注',
  status: '状态',
  type: '类型',
  isActive: '是否启用',
  isDeleted: '是否删除',
  createdAt: '创建时间',
  updatedAt: '更新时间',
  createdBy: '创建人',
  balance: '余额',
  initialBalance: '初始余额',
  currency: '币种',
  bankName: '银行名称',
  accountNumber: '账号',
  contactPerson: '联系人',
  contactPhone: '联系电话',
  phone: '电话',
  email: '邮箱',
  address: '地址',
  taxNumber: '税号',
  parentId: '父级ID',
  parentName: '父级名称',
  sortOrder: '排序',
  code: '编码',
  title: '标题',
  dueDate: '到期日期',
  settledAt: '结清时间',
  paidAmount: '已付金额',
  receivedAmount: '已收金额',
  remainingAmount: '剩余金额',
  totalAmount: '总金额',
  invoiceNumber: '发票号',
  counterpartyName: '对方名称',
  relatedTransactionId: '关联交易ID',
  pattern: '匹配模式',
  priority: '优先级',
  matchField: '匹配字段',
  matchValue: '匹配值',
  targetCategoryId: '目标分类ID',
  targetCategoryName: '目标分类名称',
  targetProjectId: '目标项目ID',
  targetProjectName: '目标项目名称',
  fileName: '文件名',
  totalRows: '总行数',
  successRows: '成功行数',
  failedRows: '失败行数',
  bankTransactionDescription: '银行摘要',
  maturityDate: '到期日期',
  interestRate: '利率',
  depositAmount: '存款金额',
  startDate: '开始日期',
  endDate: '结束日期',
  username: '用户名',
  role: '角色',
  isSplit: '是否分摊',
  splitGroup: '分摊组',
  isTransfer: '是否转账',
}

// ── 实体级字段标签覆盖（同名字段在不同实体下有不同含义时使用）──
const ENTITY_PROPERTY_LABEL_OVERRIDES: Record<string, Record<string, string>> = {
  Receivable: {
    totalAmount: '应收金额',
    remainingAmount: '未收金额',
  },
  Payable: {
    totalAmount: '应付金额',
    remainingAmount: '未付金额',
  },
}

// ── 需要隐藏的系统字段 ──
export const HIDDEN_FIELDS = new Set(['id', 'createdAt', 'updatedAt', 'createdBy', 'isDeleted'])

// ── 金额类字段 ──
export const AMOUNT_FIELDS = new Set([
  'amount', 'balance', 'initialBalance', 'paidAmount',
  'receivedAmount', 'totalAmount', 'depositAmount', 'remainingAmount',
])

// ── 值映射 ──
const TRANSACTION_TYPE_MAP: Record<string, string> = {
  Income: '收入',
  Expense: '支出',
}

const STATUS_MAP: Record<string, string> = {
  Pending: '待处理',
  Completed: '已完成',
  Cancelled: '已取消',
  Draft: '草稿',
  Confirmed: '已确认',
  Paid: '已付款',
  Received: '已收款',
  Overdue: '逾期',
  PartiallyPaid: '部分付款',
  PartiallyReceived: '部分收款',
}

const DATE_FIELDS = new Set(['transactionDate', 'dueDate', 'maturityDate', 'startDate', 'endDate'])
const TIMESTAMP_FIELDS = new Set(['createdAt', 'updatedAt', 'settledAt'])

// ── 工具函数 ──
export const getActionLabel = (action?: string) =>
  action ? (ACTION_LABEL_MAP[action] || action) : '-'

export const getActionTagType = (action?: string): '' | 'success' | 'warning' | 'danger' | 'info' =>
  action ? (ACTION_TAG_TYPE_MAP[action] ?? '') : ''

export const getEntityTypeLabel = (entityType?: string) =>
  entityType ? (ENTITY_TYPE_LABEL_MAP[entityType] || entityType) : '-'

export const getFieldLabel = (key: string, entityType?: string): string => {
  if (entityType) {
    const override = ENTITY_PROPERTY_LABEL_OVERRIDES[entityType]?.[key]
    if (override) return override
  }
  return PROPERTY_LABEL_MAP[key] || key
}

/** 将审计日志中的字段值格式化为友好的中文显示 */
export const formatFieldValue = (key: string, value: unknown): string => {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'boolean') return value ? '是' : '否'

  const str = String(value)

  if (key === 'transactionType' && TRANSACTION_TYPE_MAP[str]) return TRANSACTION_TYPE_MAP[str]
  if (key === 'status' && STATUS_MAP[str]) return STATUS_MAP[str]

  if (AMOUNT_FIELDS.has(key)) {
    const num = Number(value)
    if (!isNaN(num)) return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }

  if (key === 'interestRate') {
    const num = Number(value)
    if (!isNaN(num)) return `${num}%`
  }

  if (DATE_FIELDS.has(key)) {
    const formatted = formatDateTime(str, 'date')
    if (formatted !== '-') return formatted
  }

  if (TIMESTAMP_FIELDS.has(key)) {
    const formatted = formatDateTime(str)
    if (formatted !== '-') return formatted
  }

  return str
}

/** 格式化摘要中的值（截断长文本） */
export const formatSummaryValue = (key: string, value: unknown): string => {
  if (value === null || value === undefined) return '空'
  if (typeof value === 'boolean') return value ? '是' : '否'
  const str = String(value)
  if (AMOUNT_FIELDS.has(key)) {
    const num = Number(value)
    if (!isNaN(num)) return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }
  if (str.length > 20) return str.substring(0, 20) + '...'
  return str
}

/** 安全解析 JSON，返回 null 表示解析失败 */
export const safeJsonParse = (jsonStr: string | undefined | null): Record<string, unknown> | null => {
  if (!jsonStr) return null
  try {
    const obj = JSON.parse(jsonStr)
    if (typeof obj !== 'object' || obj === null || Array.isArray(obj)) return null
    return obj as Record<string, unknown>
  } catch {
    return null
  }
}

export interface FieldItem {
  key: string
  label: string
  display: string
  rawValue: unknown
}

/** 将 JSON 字符串解析为友好的字段列表 */
export const parseFieldList = (jsonStr: string | undefined | null, entityType?: string): FieldItem[] => {
  const obj = safeJsonParse(jsonStr)
  if (!obj) return []
  return Object.entries(obj)
    .filter(([key]) => !HIDDEN_FIELDS.has(key))
    .map(([key, value]) => ({
      key,
      label: getFieldLabel(key, entityType),
      display: formatFieldValue(key, value),
      rawValue: value,
    }))
}

export interface DiffField {
  key: string
  label: string
  oldDisplay: string
  newDisplay: string
}

/** 对比新旧 JSON，仅返回有变化的字段 */
export const buildDiffFields = (oldJson: string | undefined | null, newJson: string | undefined | null, entityType?: string): DiffField[] => {
  const oldObj = safeJsonParse(oldJson)
  const newObj = safeJsonParse(newJson)
  if (!oldObj || !newObj) return []

  const allKeys = new Set([...Object.keys(oldObj), ...Object.keys(newObj)])
  const result: DiffField[] = []

  for (const key of allKeys) {
    if (HIDDEN_FIELDS.has(key)) continue
    const oldVal = oldObj[key]
    const newVal = newObj[key]
    if (JSON.stringify(oldVal) !== JSON.stringify(newVal)) {
      result.push({
        key,
        label: getFieldLabel(key, entityType),
        oldDisplay: formatFieldValue(key, oldVal),
        newDisplay: formatFieldValue(key, newVal),
      })
    }
  }
  return result
}
