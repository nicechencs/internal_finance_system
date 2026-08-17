import { describe, it, expect } from 'vitest'
import {
  formatCurrency,
  formatDateTime,
  formatPercent,
  formatFileSize,
  formatPhone,
  formatBankCard,
} from '@/shared/utils/formatters'

describe('formatters', () => {
  describe('formatCurrency', () => {
    it('应该正确格式化数字金额', () => {
      expect(formatCurrency(1234.56)).toBe('CNY 1,234.56')
      expect(formatCurrency(1000000)).toBe('CNY 1,000,000.00')
      expect(formatCurrency(0)).toBe('CNY 0.00')
    })

    it('应该正确格式化字符串金额', () => {
      expect(formatCurrency('1234.56')).toBe('CNY 1,234.56')
      expect(formatCurrency('1000000')).toBe('CNY 1,000,000.00')
    })

    it('应该支持不同货币', () => {
      expect(formatCurrency(1234.56, 'USD')).toBe('USD 1,234.56')
      expect(formatCurrency(1234.56, 'EUR')).toBe('EUR 1,234.56')
    })

    it('应该支持自定义小数位数', () => {
      expect(formatCurrency(1234.567, 'CNY', 3)).toBe('CNY 1,234.567')
      expect(formatCurrency(1234.5, 'CNY', 0)).toBe('CNY 1,235')
    })

    it('应该处理无效输入', () => {
      expect(formatCurrency('invalid')).toBe('CNY 0.00')
      expect(formatCurrency(NaN)).toBe('CNY 0.00')
    })
  })

  describe('formatDateTime', () => {
    it('应该正确格式化日期时间', () => {
      const date = new Date('2024-03-13T10:30:45')
      expect(formatDateTime(date)).toBe('2024-03-13 10:30:45')
    })

    it('应该正确格式化日期字符串', () => {
      expect(formatDateTime('2024-03-13T10:30:45')).toBe('2024-03-13 10:30:45')
    })

    it('应该按本地日期处理纯日期字符串', () => {
      expect(formatDateTime('2024-03-13', 'date')).toBe('2024-03-13')
    })

    it('应该支持仅日期格式', () => {
      const date = new Date('2024-03-13T10:30:45')
      expect(formatDateTime(date, 'date')).toBe('2024-03-13')
    })

    it('应该支持仅时间格式', () => {
      const date = new Date('2024-03-13T10:30:45')
      expect(formatDateTime(date, 'time')).toBe('10:30:45')
    })

    it('应该处理无效输入', () => {
      expect(formatDateTime('')).toBe('-')
      expect(formatDateTime('invalid')).toBe('-')
    })
  })

  describe('formatPercent', () => {
    it('应该正确格式化小数形式百分比', () => {
      expect(formatPercent(0.1234)).toBe('12.34%')
      expect(formatPercent(0.5)).toBe('50.00%')
      expect(formatPercent(1)).toBe('100.00%')
    })

    it('应该正确格式化整数形式百分比', () => {
      expect(formatPercent(12.34, 2, false)).toBe('12.34%')
      expect(formatPercent(50, 2, false)).toBe('50.00%')
    })

    it('应该支持自定义小数位数', () => {
      expect(formatPercent(0.1234, 1)).toBe('12.3%')
      expect(formatPercent(0.1234, 0)).toBe('12%')
    })

    it('应该处理无效输入', () => {
      expect(formatPercent(NaN)).toBe('0.00%')
    })
  })

  describe('formatFileSize', () => {
    it('应该正确格式化文件大小', () => {
      expect(formatFileSize(0)).toBe('0 Bytes')
      expect(formatFileSize(1024)).toBe('1 KB')
      expect(formatFileSize(1048576)).toBe('1 MB')
      expect(formatFileSize(1073741824)).toBe('1 GB')
    })

    it('应该支持自定义小数位数', () => {
      expect(formatFileSize(1536, 1)).toBe('1.5 KB')
      expect(formatFileSize(1536, 0)).toBe('2 KB')
    })
  })

  describe('formatPhone', () => {
    it('应该正确格式化手机号', () => {
      expect(formatPhone('13812345678')).toBe('138****5678')
      expect(formatPhone('18900001111')).toBe('189****1111')
    })

    it('应该处理无效输入', () => {
      expect(formatPhone('')).toBe('')
      expect(formatPhone('123')).toBe('123')
      expect(formatPhone('12345678901234')).toBe('12345678901234')
    })
  })

  describe('formatBankCard', () => {
    it('应该正确格式化银行卡号', () => {
      expect(formatBankCard('6222021234567890')).toBe('6222 0212 3456 7890')
      expect(formatBankCard('1234567890123456')).toBe('1234 5678 9012 3456')
    })

    it('应该支持隐藏中间部分', () => {
      expect(formatBankCard('6222021234567890', true)).toBe('6222 **** **** 7890')
    })

    it('应该处理带空格的输入', () => {
      expect(formatBankCard('6222 0212 3456 7890')).toBe('6222 0212 3456 7890')
    })

    it('应该处理空输入', () => {
      expect(formatBankCard('')).toBe('')
    })
  })
})

