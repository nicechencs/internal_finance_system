import { describe, it, expect } from 'vitest'
import { TABLE_COLUMN_WIDTH } from '@/constants/table'

describe('TABLE_COLUMN_WIDTH', () => {
  it('应该导出 TABLE_COLUMN_WIDTH 常量', () => {
    expect(TABLE_COLUMN_WIDTH).toBeDefined()
    expect(typeof TABLE_COLUMN_WIDTH).toBe('object')
  })

  describe('基础列宽', () => {
    it('应该包含 selection 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.selection).toBe(54)
    })

    it('应该包含 index 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.index).toBe(72)
    })

    it('应该包含 type 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.type).toBe(96)
    })

    it('应该包含 status 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.status).toBe(108)
    })

    it('应该包含 shortText 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.shortText).toBe(120)
    })
  })

  describe('日期相关列宽', () => {
    it('应该包含 date 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.date).toBe(124)
    })

    it('应该包含 dateTime 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.dateTime).toBe(176)
    })

    it('dateTime 应该大于 date', () => {
      expect(TABLE_COLUMN_WIDTH.dateTime).toBeGreaterThan(TABLE_COLUMN_WIDTH.date)
    })
  })

  describe('金额相关列宽', () => {
    it('应该包含 amount 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.amount).toBe(140)
    })

    it('应该包含 amountCompact 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.amountCompact).toBe(124)
    })

    it('应该包含 rate 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.rate).toBe(96)
    })

    it('amountCompact 应该小于 amount', () => {
      expect(TABLE_COLUMN_WIDTH.amountCompact).toBeLessThan(TABLE_COLUMN_WIDTH.amount)
    })
  })

  describe('业务实体列宽', () => {
    it('应该包含 account 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.account).toBe(160)
    })

    it('应该包含 category 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.category).toBe(136)
    })

    it('应该包含 project 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.project).toBe(180)
    })

    it('应该包含 company 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.company).toBe(180)
    })

    it('应该包含 contact 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.contact).toBe(140)
    })
  })

  describe('联系方式列宽', () => {
    it('应该包含 phone 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.phone).toBe(148)
    })

    it('应该包含 email 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.email).toBe(220)
    })

    it('email 应该大于 phone（email 通常更长）', () => {
      expect(TABLE_COLUMN_WIDTH.email).toBeGreaterThan(TABLE_COLUMN_WIDTH.phone)
    })
  })

  describe('银行相关列宽', () => {
    it('应该包含 bank 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.bank).toBe(180)
    })

    it('应该包含 bankAccount 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.bankAccount).toBe(200)
    })

    it('bankAccount 应该大于 bank', () => {
      expect(TABLE_COLUMN_WIDTH.bankAccount).toBeGreaterThan(TABLE_COLUMN_WIDTH.bank)
    })
  })

  describe('通用列宽', () => {
    it('应该包含 code 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.code).toBe(140)
    })

    it('应该包含 name 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.name).toBe(180)
    })

    it('应该包含 description 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.description).toBe(240)
    })

    it('应该包含 detail 列宽', () => {
      expect(TABLE_COLUMN_WIDTH.detail).toBe(320)
    })

    it('detail > description > name > code 宽度递增', () => {
      expect(TABLE_COLUMN_WIDTH.detail).toBeGreaterThan(TABLE_COLUMN_WIDTH.description)
      expect(TABLE_COLUMN_WIDTH.description).toBeGreaterThan(TABLE_COLUMN_WIDTH.name)
      expect(TABLE_COLUMN_WIDTH.name).toBeGreaterThan(TABLE_COLUMN_WIDTH.code)
    })
  })

  describe('操作列宽', () => {
    it('应该包含 actionTwo 列宽（两个按钮）', () => {
      expect(TABLE_COLUMN_WIDTH.actionTwo).toBe(160)
    })

    it('应该包含 actionThree 列宽（三个按钮）', () => {
      expect(TABLE_COLUMN_WIDTH.actionThree).toBe(200)
    })

    it('应该包含 actionFour 列宽（四个按钮）', () => {
      expect(TABLE_COLUMN_WIDTH.actionFour).toBe(240)
    })

    it('操作列宽度应该随按钮数量递增', () => {
      expect(TABLE_COLUMN_WIDTH.actionThree).toBeGreaterThan(TABLE_COLUMN_WIDTH.actionTwo)
      expect(TABLE_COLUMN_WIDTH.actionFour).toBeGreaterThan(TABLE_COLUMN_WIDTH.actionThree)
    })
  })

  describe('类型安全', () => {
    it('所有值应该为正整数', () => {
      const values = Object.values(TABLE_COLUMN_WIDTH)
      for (const value of values) {
        expect(typeof value).toBe('number')
        expect(value).toBeGreaterThan(0)
        expect(Number.isInteger(value)).toBe(true)
      }
    })

    it('应该包含预期数量的属性', () => {
      const keys = Object.keys(TABLE_COLUMN_WIDTH)
      expect(keys.length).toBe(26)
    })
  })
})
