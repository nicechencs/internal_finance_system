import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { useResizableTableColumns } from '@/composables/useResizableTableColumns'

// Mock @vueuse/core useStorage
const mockStorageValue = ref<Record<string, number>>({})

vi.mock('@vueuse/core', () => ({
  useStorage: vi.fn(() => mockStorageValue),
}))

describe('useResizableTableColumns', () => {
  beforeEach(() => {
    mockStorageValue.value = {}
  })

  describe('getColumnWidth', () => {
    it('应该在没有保存值时返回默认宽度', () => {
      const { getColumnWidth } = useResizableTableColumns('test-table')

      expect(getColumnWidth('name', 180)).toBe(180)
      expect(getColumnWidth('amount', 140)).toBe(140)
    })

    it('应该在有保存值时返回保存的宽度', () => {
      mockStorageValue.value = { name: 220 }
      const { getColumnWidth } = useResizableTableColumns('test-table')

      expect(getColumnWidth('name', 180)).toBe(220)
    })

    it('应该对不同列独立返回各自的值', () => {
      mockStorageValue.value = { name: 220, amount: 160 }
      const { getColumnWidth } = useResizableTableColumns('test-table')

      expect(getColumnWidth('name', 180)).toBe(220)
      expect(getColumnWidth('amount', 140)).toBe(160)
      expect(getColumnWidth('status', 100)).toBe(100) // 未保存的列使用默认值
    })
  })

  describe('getColumnMinWidth', () => {
    it('应该在没有保存值时返回默认宽度', () => {
      const { getColumnMinWidth } = useResizableTableColumns('test-table')

      expect(getColumnMinWidth('description', 240)).toBe(240)
    })

    it('应该在有保存值时返回保存的宽度', () => {
      mockStorageValue.value = { description: 300 }
      const { getColumnMinWidth } = useResizableTableColumns('test-table')

      expect(getColumnMinWidth('description', 240)).toBe(300)
    })
  })

  describe('handleHeaderDragend', () => {
    it('应该保存拖拽后的列宽', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(250, 180, {
        property: 'name',
      } as any)

      expect(getColumnWidth('name', 180)).toBe(250)
    })

    it('应该优先使用 columnKey 作为标识', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(200, 180, {
        columnKey: 'customKey',
        property: 'name',
        label: '名称',
        id: 'col-1',
      } as any)

      expect(getColumnWidth('customKey', 180)).toBe(200)
      expect(getColumnWidth('name', 180)).toBe(180) // property 不应被使用
    })

    it('应该按优先级回退：columnKey > property > label > id', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      // 只有 property
      handleHeaderDragend(210, 180, {
        property: 'amount',
        label: '金额',
        id: 'col-2',
      } as any)
      expect(getColumnWidth('amount', 140)).toBe(210)

      // 只有 label
      handleHeaderDragend(190, 180, {
        label: '状态',
        id: 'col-3',
      } as any)
      expect(getColumnWidth('状态', 100)).toBe(190)

      // 只有 id
      handleHeaderDragend(170, 180, {
        id: 'col-4',
      } as any)
      expect(getColumnWidth('col-4', 100)).toBe(170)
    })

    it('应该在没有可用标识时不保存宽度', () => {
      const { handleHeaderDragend } = useResizableTableColumns('test-table')

      handleHeaderDragend(200, 180, {} as any)

      expect(mockStorageValue.value).toEqual({})
    })

    it('应该能更新已保存的列宽', () => {
      mockStorageValue.value = { name: 200 }
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      expect(getColumnWidth('name', 180)).toBe(200)

      handleHeaderDragend(300, 200, {
        property: 'name',
      } as any)

      expect(getColumnWidth('name', 180)).toBe(300)
    })
  })

  describe('normalizeWidth（通过 handleHeaderDragend 间接测试）', () => {
    it('应该将宽度四舍五入为整数', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(150.7, 100, {
        property: 'col1',
      } as any)

      expect(getColumnWidth('col1', 100)).toBe(151)
    })

    it('应该将小于 60 的宽度限制为最小值 60', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(30, 100, {
        property: 'col2',
      } as any)

      expect(getColumnWidth('col2', 100)).toBe(60)
    })

    it('应该将 0 宽度限制为最小值 60', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(0, 100, {
        property: 'col3',
      } as any)

      expect(getColumnWidth('col3', 100)).toBe(60)
    })

    it('应该将负数宽度限制为最小值 60', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(-10, 100, {
        property: 'col4',
      } as any)

      expect(getColumnWidth('col4', 100)).toBe(60)
    })

    it('应该保留恰好为 60 的宽度', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(60, 100, {
        property: 'col5',
      } as any)

      expect(getColumnWidth('col5', 100)).toBe(60)
    })

    it('应该保留大于 60 的宽度', () => {
      const { handleHeaderDragend, getColumnWidth } = useResizableTableColumns('test-table')

      handleHeaderDragend(61, 100, {
        property: 'col6',
      } as any)

      expect(getColumnWidth('col6', 100)).toBe(61)
    })
  })

  describe('resetColumnWidths', () => {
    it('应该清除所有保存的列宽', () => {
      mockStorageValue.value = { name: 220, amount: 160, status: 120 }
      const { resetColumnWidths, getColumnWidth } = useResizableTableColumns('test-table')

      expect(getColumnWidth('name', 180)).toBe(220)
      expect(getColumnWidth('amount', 140)).toBe(160)

      resetColumnWidths()

      expect(getColumnWidth('name', 180)).toBe(180)
      expect(getColumnWidth('amount', 140)).toBe(140)
      expect(getColumnWidth('status', 100)).toBe(100)
    })

    it('应该在没有保存数据时也能正常调用', () => {
      const { resetColumnWidths } = useResizableTableColumns('test-table')

      expect(() => resetColumnWidths()).not.toThrow()
      expect(mockStorageValue.value).toEqual({})
    })
  })

  describe('返回值结构', () => {
    it('应该返回所有必要的方法', () => {
      const result = useResizableTableColumns('test-table')

      expect(result).toHaveProperty('getColumnWidth')
      expect(result).toHaveProperty('getColumnMinWidth')
      expect(result).toHaveProperty('handleHeaderDragend')
      expect(result).toHaveProperty('resetColumnWidths')
      expect(typeof result.getColumnWidth).toBe('function')
      expect(typeof result.getColumnMinWidth).toBe('function')
      expect(typeof result.handleHeaderDragend).toBe('function')
      expect(typeof result.resetColumnWidths).toBe('function')
    })
  })
})
