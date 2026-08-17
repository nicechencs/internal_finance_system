import { ref } from 'vue'
import { ElMessage } from 'element-plus'

interface InlineEditOptions<T, C, U> {
  createApi: (data: C) => Promise<any>
  updateApi: (id: number, data: U) => Promise<any>
  deleteApi: (id: number) => Promise<any>
  createEmptyRow: () => T & { _editing?: boolean; _isNew?: boolean }
  toCreateRequest: (row: T) => C
  toUpdateRequest: (row: T) => U
  onSuccess?: () => void
}

export function useInlineEdit<T extends { id: number }, C, U>(options: InlineEditOptions<T, C, U>) {
  const editingRowId = ref<number | null>(null)
  const editingRowBackup = ref<T | null>(null)
  const savingRowId = ref<number | null>(null)

  const startAdd = (tableData: any[]) => {
    // Cancel any current editing
    if (editingRowId.value !== null) {
      cancelEdit(tableData)
    }
    const newRow = options.createEmptyRow()
    newRow._editing = true
    newRow._isNew = true
    newRow.id = -Date.now() // temp negative id
    tableData.unshift(newRow)
    editingRowId.value = newRow.id
  }

  const startEdit = (row: any) => {
    editingRowBackup.value = { ...row }
    row._editing = true
    editingRowId.value = row.id
  }

  const cancelEdit = (tableData: any[]) => {
    if (editingRowId.value === null) return
    const index = tableData.findIndex((r: any) => r.id === editingRowId.value)
    if (index === -1) return
    const row = tableData[index]
    if (row._isNew) {
      tableData.splice(index, 1)
    } else if (editingRowBackup.value) {
      Object.assign(row, editingRowBackup.value)
      row._editing = false
    }
    editingRowId.value = null
    editingRowBackup.value = null
  }

  const saveEdit = async (row: any, tableData: any[]) => {
    savingRowId.value = row.id
    try {
      if (row._isNew) {
        await options.createApi(options.toCreateRequest(row))
        ElMessage.success('创建成功')
      } else {
        await options.updateApi(row.id, options.toUpdateRequest(row))
        ElMessage.success('更新成功')
      }
      row._editing = false
      row._isNew = false
      editingRowId.value = null
      editingRowBackup.value = null
      options.onSuccess?.()
    } catch (error) {
      // error already handled by request interceptor
    } finally {
      savingRowId.value = null
    }
  }

  return {
    editingRowId,
    savingRowId,
    startAdd,
    startEdit,
    cancelEdit,
    saveEdit
  }
}
