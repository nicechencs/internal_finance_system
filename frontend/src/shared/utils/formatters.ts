import { parseDateInput } from './date'

/**
 * 格式化货币金额
 * @param amount - 金额
 * @param currency - 货币代码，默认 'CNY'
 * @param decimals - 小数位数，默认 2
 * @returns 格式化后的货币字符串
 *
 * @example
 * formatCurrency(1234.56) // 'CNY 1,234.56'
 * formatCurrency(1234.56, 'USD') // 'USD 1,234.56'
 * formatCurrency(1234.567, 'CNY', 3) // 'CNY 1,234.567'
 */
export function formatCurrency(
  amount: number | string,
  currency = 'CNY',
  decimals = 2
): string {
  const num = typeof amount === 'string' ? parseFloat(amount) : amount
  if (isNaN(num)) return `${currency} 0.00`

  const formatted = num.toLocaleString('zh-CN', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  })

  return `${currency} ${formatted}`
}

/**
 * 格式化日期时间
 * @param date - 日期字符串或 Date 对象
 * @param format - 格式类型
 * @returns 格式化后的日期字符串
 *
 * @example
 * formatDateTime('2024-03-13T10:30:00') // '2024-03-13 10:30:00'
 * formatDateTime('2024-03-13T10:30:00', 'date') // '2024-03-13'
 * formatDateTime('2024-03-13T10:30:00', 'time') // '10:30:00'
 */
export function formatDateTime(
  date: string | Date,
  format: 'datetime' | 'date' | 'time' = 'datetime'
): string {
  if (!date) return '-'

  const d = parseDateInput(date)
  if (!d) return '-'

  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hours = String(d.getHours()).padStart(2, '0')
  const minutes = String(d.getMinutes()).padStart(2, '0')
  const seconds = String(d.getSeconds()).padStart(2, '0')

  switch (format) {
    case 'date':
      return `${year}-${month}-${day}`
    case 'time':
      return `${hours}:${minutes}:${seconds}`
    case 'datetime':
    default:
      return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
  }
}

/**
 * 格式化百分比
 * @param value - 数值（0-1 或 0-100）
 * @param decimals - 小数位数，默认 2
 * @param isDecimal - 输入值是否为小数形式（0-1），默认 true
 * @returns 格式化后的百分比字符串
 *
 * @example
 * formatPercent(0.1234) // '12.34%'
 * formatPercent(12.34, 2, false) // '12.34%'
 * formatPercent(0.1234, 1) // '12.3%'
 */
export function formatPercent(
  value: number,
  decimals = 2,
  isDecimal = true
): string {
  if (isNaN(value)) return '0.00%'

  const percent = isDecimal ? value * 100 : value
  return `${percent.toFixed(decimals)}%`
}

/**
 * 格式化文件大小
 * @param bytes - 字节数
 * @param decimals - 小数位数，默认 2
 * @returns 格式化后的文件大小字符串
 *
 * @example
 * formatFileSize(1024) // '1.00 KB'
 * formatFileSize(1048576) // '1.00 MB'
 * formatFileSize(1073741824) // '1.00 GB'
 */
export function formatFileSize(bytes: number, decimals = 2): string {
  if (bytes === 0) return '0 Bytes'

  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(decimals))} ${sizes[i]}`
}

/**
 * 格式化手机号（隐藏中间四位）
 * @param phone - 手机号
 * @returns 格式化后的手机号
 *
 * @example
 * formatPhone('13812345678') // '138****5678'
 */
export function formatPhone(phone: string): string {
  if (!phone || phone.length !== 11) return phone
  return phone.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2')
}

/**
 * 格式化银行卡号（每四位一组）
 * @param cardNumber - 银行卡号
 * @param hideMiddle - 是否隐藏中间部分，默认 false
 * @returns 格式化后的银行卡号
 *
 * @example
 * formatBankCard('6222021234567890') // '6222 0212 3456 7890'
 * formatBankCard('6222021234567890', true) // '6222 **** **** 7890'
 */
export function formatBankCard(cardNumber: string, hideMiddle = false): string {
  if (!cardNumber) return ''

  const cleaned = cardNumber.replace(/\s/g, '')

  if (hideMiddle && cleaned.length >= 8) {
    const first = cleaned.slice(0, 4)
    const last = cleaned.slice(-4)
    const middle = '*'.repeat(Math.min(cleaned.length - 8, 8))
    return `${first} ${middle.match(/.{1,4}/g)?.join(' ')} ${last}`
  }

  return cleaned.match(/.{1,4}/g)?.join(' ') || cleaned
}

/**
 * 格式化金额（不带货币符号）
 * @param amount - 金额
 * @param decimals - 小数位数，默认 2
 * @returns 格式化后的金额字符串
 *
 * @example
 * formatMoney(1234.56) // '1,234.56'
 * formatMoney(1234.567, 3) // '1,234.567'
 */
export function formatMoney(amount: number | string, decimals = 2): string {
  const num = typeof amount === 'string' ? parseFloat(amount) : amount
  if (isNaN(num)) return '0.00'

  return num.toLocaleString('zh-CN', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  })
}

/**
 * 格式化人民币金额（带 ¥ 符号和千分位）
 * @param amount - 金额
 * @param decimals - 小数位数，默认 2
 * @returns 格式化后的金额字符串
 *
 * @example
 * formatRMB(1234.56) // '¥1,234.56'
 * formatRMB(1234.567, 3) // '¥1,234.567'
 */
export function formatRMB(amount: number | string, decimals = 2): string {
  return `¥${formatMoney(amount, decimals)}`
}

/**
 * 格式化交易金额（带符号）
 * @param amount - 金额
 * @param transactionType - 交易类型
 * @param showPositiveSign - 是否显示正号，默认 false
 * @returns 格式化后的金额字符串
 *
 * @example
 * formatTransactionAmount(1234.56, 'Expense') // '-1,234.56'
 * formatTransactionAmount(1234.56, 'Income') // '1,234.56'
 * formatTransactionAmount(1234.56, 'Income', true) // '+1,234.56'
 */
export function formatTransactionAmount(
  amount: number,
  transactionType: string,
  showPositiveSign = false
): string {
  const formatted = formatMoney(amount)
  const type = transactionType.toLowerCase()
  if (type === 'expense') {
    return '-' + formatted
  }
  if (type === 'income' && showPositiveSign) {
    return '+' + formatted
  }
  return formatted
}

/**
 * 获取交易类型对应的颜色 CSS 变量
 * @param transactionType - 交易类型
 * @returns CSS 颜色变量
 *
 * @example
 * getTransactionAmountColor('Income') // 'var(--color-success-dark-1)'
 * getTransactionAmountColor('Expense') // 'var(--color-danger-dark-1)'
 */
export function getTransactionAmountColor(transactionType: string): string {
  const type = transactionType.toLowerCase()
  if (type === 'income') return 'var(--color-success-dark-1)'
  if (type === 'expense') return 'var(--color-danger-dark-1)'
  return 'var(--text-primary)'
}

export type AmountTone = 'income' | 'expense' | 'neutral'

export function getAmountTone(transactionType?: string): AmountTone {
  const type = (transactionType || '').toLowerCase()
  if (type === 'income') return 'income'
  if (type === 'expense') return 'expense'
  return 'neutral'
}
