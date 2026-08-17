import { ElMessageBox } from 'element-plus'
import type { ElMessageBoxOptions } from 'element-plus'

/**
 * 确认对话框配置选项
 */
export interface UseConfirmOptions {
  /** 确认框标题，默认 '提示' */
  title?: string
  /** 确认框类型，默认 'warning' */
  type?: 'success' | 'warning' | 'info' | 'error'
  /** 确认按钮文字，默认 '确定' */
  confirmButtonText?: string
  /** 取消按钮文字，默认 '取消' */
  cancelButtonText?: string
}

/**
 * 确认对话框封装 Composable
 *
 * @example
 * ```ts
 * const { confirm } = useConfirm()
 *
 * // 删除确认
 * const handleDelete = async (row: Account) => {
 *   const confirmed = await confirm('确定要删除该账户吗？')
 *   if (confirmed) {
 *     await deleteAccount(row.id)
 *     ElMessage.success('删除成功')
 *     loadData()
 *   }
 * }
 *
 * // 自定义配置
 * const confirmed = await confirm('确定要执行此操作吗？', {
 *   title: '警告',
 *   type: 'error',
 *   confirmButtonText: '执行',
 *   cancelButtonText: '放弃'
 * })
 * ```
 */
export function useConfirm() {
  /**
   * 显示确认对话框
   * @param message - 确认消息
   * @param options - 配置选项
   * @returns 用户点击确定返回 true，取消返回 false
   */
  const confirm = async (
    message: string,
    options: UseConfirmOptions = {}
  ): Promise<boolean> => {
    const {
      title = '提示',
      type = 'warning',
      confirmButtonText = '确定',
      cancelButtonText = '取消'
    } = options

    try {
      await ElMessageBox.confirm(message, title, {
        confirmButtonText,
        cancelButtonText,
        type
      } as ElMessageBoxOptions)
      return true
    } catch (error) {
      // 用户点击取消或关闭对话框
      return false
    }
  }

  /**
   * 删除确认对话框（预设配置）
   * @param message - 确认消息，默认 '确定要删除吗？'
   * @returns 用户点击确定返回 true，取消返回 false
   */
  const confirmDelete = async (message = '确定要删除吗？'): Promise<boolean> => {
    return confirm(message, {
      title: '删除确认',
      type: 'warning'
    })
  }

  return {
    confirm,
    confirmDelete
  }
}
