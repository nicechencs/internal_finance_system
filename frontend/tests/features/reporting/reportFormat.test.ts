import { describe, expect, it } from 'vitest'
import { formatPersonType, formatRate, monthRange, toExclusiveEndDate } from '@/features/reporting/utils/reportFormat'

describe('reportFormat', () => {
  it('利润率按百分数格式化，不乘 100', () => {
    expect(formatRate(46.666)).toBe('46.67%')
    expect(formatRate(0)).toBe('0.00%')
    expect(formatRate(Number.NaN)).toBe('0.00%')
  })

  it('人员类型使用中文标签', () => {
    expect(formatPersonType('Employee')).toBe('员工')
    expect(formatPersonType('Contractor')).toBe('承包商')
    expect(formatPersonType('Other')).toBe('Other')
    expect(formatPersonType('')).toBe('-')
  })

  it('月份区间覆盖整月，含闰年二月', () => {
    expect(monthRange(2026, 3)).toEqual(['2026-03-01', '2026-03-31'])
    expect(monthRange(2024, 2)).toEqual(['2024-02-01', '2024-02-29'])
  })

  it('现金流结束日加一天以匹配开区间', () => {
    expect(toExclusiveEndDate('2026-03-31')).toBe('2026-04-01')
    expect(toExclusiveEndDate('2024-02-29')).toBe('2024-03-01')
    expect(toExclusiveEndDate('bad')).toBe('bad')
  })
})
