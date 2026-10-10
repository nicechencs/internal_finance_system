const PERSON_TYPE_LABELS: Record<string, string> = {
  Employee: '员工',
  Contractor: '承包商'
}

/** 利润率已是百分数（80 表示 80%），不是 0–1 小数。 */
export function formatRate(value: number | null | undefined): string {
  const num = Number(value)
  if (!Number.isFinite(num)) return '0.00%'
  return `${num.toFixed(2)}%`
}

export function formatPersonType(personType?: string | null): string {
  if (!personType) return '-'
  return PERSON_TYPE_LABELS[personType] || personType
}

export function currentYearMonth(): { year: number; month: number } {
  const now = new Date()
  return { year: now.getFullYear(), month: now.getMonth() + 1 }
}

export function monthRange(year: number, month: number): [string, string] {
  const start = `${year}-${String(month).padStart(2, '0')}-01`
  const lastDay = new Date(year, month, 0).getDate()
  const end = `${year}-${String(month).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`
  return [start, end]
}

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

/**
 * 现金流接口的结束日期是开区间（TransactionDate < endDate）。
 * 把用户选中的结束日加一天，才能包含当天流水。
 */
export function toExclusiveEndDate(endDate: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(endDate)
  if (!match) return endDate
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
  date.setDate(date.getDate() + 1)
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
