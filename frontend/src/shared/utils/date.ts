const DATE_ONLY_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/

const pad = (value: number) => String(value).padStart(2, '0')

export function isDateOnlyString(value: string): boolean {
  return DATE_ONLY_PATTERN.test(value.trim())
}

export function parseDateInput(value: string | Date): Date | null {
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value
  }

  if (!value) {
    return null
  }

  const match = value.match(DATE_ONLY_PATTERN)
  if (match) {
    const [, year, month, day] = match
    return new Date(Number(year), Number(month) - 1, Number(day))
  }

  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

export function formatLocalDate(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function toDateOnlyString(value: string | Date | null | undefined): string {
  if (!value) {
    return ''
  }

  if (typeof value === 'string' && isDateOnlyString(value)) {
    return value
  }

  const date = parseDateInput(value)
  return date ? formatLocalDate(date) : ''
}

export function getTodayDateString(reference = new Date()): string {
  return formatLocalDate(reference)
}

export function isDateBefore(value: string | Date, compareTo: string | Date): boolean {
  const left = toDateOnlyString(value)
  const right = toDateOnlyString(compareTo)

  if (!left || !right) {
    return false
  }

  return left < right
}

export function isDateBeforeToday(value: string | Date, reference = new Date()): boolean {
  return isDateBefore(value, reference)
}
