import { describe, expect, it } from 'vitest'
import { getAmountTone } from '@/shared/utils/formatters'
import { getCounterpartyLabel, joinMeta } from '@/shared/utils/recordDisplay'

describe('recordDisplay', () => {
  it('应按客户 / 供应商 / 人员 / 对方名称优先级取值', () => {
    expect(getCounterpartyLabel({ customerName: '客户A', supplierName: '供应商B' })).toBe('客户A')
    expect(getCounterpartyLabel({ supplierName: '供应商B' })).toBe('供应商B')
    expect(getCounterpartyLabel({ personName: '张三' })).toBe('张三')
    expect(getCounterpartyLabel({ counterpartyName: '外部对方' })).toBe('外部对方')
    expect(getCounterpartyLabel({})).toBe('')
  })

  it('应拼接非空 meta 片段', () => {
    expect(joinMeta('2026-08-16', '工商银行', '-', undefined, '销售收入')).toBe('2026-08-16 · 工商银行 · 销售收入')
  })
})

describe('getAmountTone', () => {
  it('应映射交易类型到金额色调', () => {
    expect(getAmountTone('Income')).toBe('income')
    expect(getAmountTone('Expense')).toBe('expense')
    expect(getAmountTone('Transfer')).toBe('neutral')
  })
})
