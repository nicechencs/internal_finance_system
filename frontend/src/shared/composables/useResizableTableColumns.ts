import { useStorage } from '@vueuse/core'
import type { TableColumnCtx } from 'element-plus'

type ColumnWidthMap = Record<string, number>

const STORAGE_PREFIX = 'finance-table-widths'

const normalizeWidth = (width: number) => {
  return Math.max(Math.round(width), 60)
}

export function useResizableTableColumns(storageKey: string) {
  const widthState = useStorage<ColumnWidthMap>(`${STORAGE_PREFIX}:${storageKey}`, {})

  const resolveColumnKey = (columnKey: string, fallback: number) => {
    return widthState.value[columnKey] ?? fallback
  }

  const getColumnWidth = (columnKey: string, fallback: number) => {
    return resolveColumnKey(columnKey, fallback)
  }

  const getColumnMinWidth = (columnKey: string, fallback: number) => {
    return resolveColumnKey(columnKey, fallback)
  }

  const handleHeaderDragend = (
    newWidth: number,
    _oldWidth: number,
    column: TableColumnCtx<any>
  ) => {
    const columnKey = column.columnKey || column.property || column.label || column.id
    if (!columnKey) {
      return
    }

    widthState.value = {
      ...widthState.value,
      [columnKey]: normalizeWidth(newWidth)
    }
  }

  const resetColumnWidths = () => {
    widthState.value = {}
  }

  return {
    getColumnWidth,
    getColumnMinWidth,
    handleHeaderDragend,
    resetColumnWidths
  }
}
