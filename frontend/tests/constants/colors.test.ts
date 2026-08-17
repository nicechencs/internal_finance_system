import { describe, it, expect } from 'vitest'
import { BRAND_COLORS, CHART_COLORS, CHART_PALETTE, CHART_AXIS } from '@/constants/colors'

const HEX_COLOR_RE = /^#[0-9A-Fa-f]{6}$/

describe('colors 常量', () => {
  describe('CHART_PALETTE', () => {
    it('长度应为 5', () => {
      expect(CHART_PALETTE).toHaveLength(5)
    })

    it('每个颜色应为合法的 6 位 hex 格式', () => {
      for (const color of CHART_PALETTE) {
        expect(color).toMatch(HEX_COLOR_RE)
      }
    })

    it('调色板中不应有重复颜色', () => {
      const unique = new Set(CHART_PALETTE)
      expect(unique.size).toBe(CHART_PALETTE.length)
    })
  })

  describe('CHART_COLORS', () => {
    it('income 应有值', () => {
      expect(CHART_COLORS.income).toBeTruthy()
    })

    it('expense 应有值', () => {
      expect(CHART_COLORS.expense).toBeTruthy()
    })

    it('profit 应有值', () => {
      expect(CHART_COLORS.profit).toBeTruthy()
    })

    it('receivable 应有值', () => {
      expect(CHART_COLORS.receivable).toBeTruthy()
    })

    it('payable 应有值', () => {
      expect(CHART_COLORS.payable).toBeTruthy()
    })

    it('balance 应有值', () => {
      expect(CHART_COLORS.balance).toBeTruthy()
    })

    it('所有颜色应为合法的 6 位 hex 格式', () => {
      for (const color of Object.values(CHART_COLORS)) {
        expect(color).toMatch(HEX_COLOR_RE)
      }
    })
  })

  describe('BRAND_COLORS', () => {
    it('所有颜色应为合法的 6 位 hex 格式', () => {
      for (const color of Object.values(BRAND_COLORS)) {
        expect(color).toMatch(HEX_COLOR_RE)
      }
    })

    it('应包含全部当前品牌色键', () => {
      const keys = Object.keys(BRAND_COLORS)
      expect(keys).toContain('primary')
      expect(keys).toContain('success')
      expect(keys).toContain('danger')
      expect(keys).toContain('warning')
      expect(keys).toContain('balance')
      expect(keys).toContain('transfer')
      expect(keys).toContain('purple')
    })
  })

  describe('CHART_COLORS 与 BRAND_COLORS 的映射关系', () => {
    it('income 应等于 BRAND_COLORS.success', () => {
      expect(CHART_COLORS.income).toBe(BRAND_COLORS.success)
    })

    it('expense 应等于 BRAND_COLORS.danger', () => {
      expect(CHART_COLORS.expense).toBe(BRAND_COLORS.danger)
    })

    it('profit 应等于 BRAND_COLORS.primary', () => {
      expect(CHART_COLORS.profit).toBe(BRAND_COLORS.primary)
    })

    it('receivable 应等于 BRAND_COLORS.success', () => {
      expect(CHART_COLORS.receivable).toBe(BRAND_COLORS.success)
    })

    it('payable 应等于 BRAND_COLORS.danger', () => {
      expect(CHART_COLORS.payable).toBe(BRAND_COLORS.danger)
    })

    it('balance 应等于 BRAND_COLORS.balance', () => {
      expect(CHART_COLORS.balance).toBe(BRAND_COLORS.balance)
    })
  })

  describe('CHART_AXIS', () => {
    it('所有颜色应为合法的 6 位 hex 格式', () => {
      for (const color of Object.values(CHART_AXIS)) {
        expect(color).toMatch(HEX_COLOR_RE)
      }
    })

    it('应包含 axisLine、axisLabel、splitLine 三个键', () => {
      expect(CHART_AXIS).toHaveProperty('axisLine')
      expect(CHART_AXIS).toHaveProperty('axisLabel')
      expect(CHART_AXIS).toHaveProperty('splitLine')
    })
  })
})
