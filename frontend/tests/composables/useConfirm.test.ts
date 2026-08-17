import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useConfirm } from '@/composables/useConfirm'
import { ElMessageBox } from 'element-plus'

// Mock Element Plus
vi.mock('element-plus', () => ({
  ElMessageBox: {
    confirm: vi.fn(),
  },
}))

describe('useConfirm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('confirm', () => {
    it('应该在用户确认时返回 true', async () => {
      vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

      const { confirm } = useConfirm()
      const result = await confirm('确定要执行此操作吗？')

      expect(result).toBe(true)
      expect(ElMessageBox.confirm).toHaveBeenCalledWith(
        '确定要执行此操作吗？',
        '提示',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning',
        }
      )
    })

    it('应该在用户取消时返回 false', async () => {
      vi.mocked(ElMessageBox.confirm).mockRejectedValue('cancel')

      const { confirm } = useConfirm()
      const result = await confirm('确定要执行此操作吗？')

      expect(result).toBe(false)
    })

    it('应该支持自定义配置', async () => {
      vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

      const { confirm } = useConfirm()
      await confirm('确定要删除吗？', {
        title: '警告',
        type: 'error',
        confirmButtonText: '删除',
        cancelButtonText: '放弃',
      })

      expect(ElMessageBox.confirm).toHaveBeenCalledWith(
        '确定要删除吗？',
        '警告',
        {
          confirmButtonText: '删除',
          cancelButtonText: '放弃',
          type: 'error',
        }
      )
    })
  })

  describe('confirmDelete', () => {
    it('应该使用默认删除消息', async () => {
      vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

      const { confirmDelete } = useConfirm()
      const result = await confirmDelete()

      expect(result).toBe(true)
      expect(ElMessageBox.confirm).toHaveBeenCalledWith(
        '确定要删除吗？',
        '删除确认',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning',
        }
      )
    })

    it('应该支持自定义删除消息', async () => {
      vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm')

      const { confirmDelete } = useConfirm()
      await confirmDelete('确定要删除该账户吗？')

      expect(ElMessageBox.confirm).toHaveBeenCalledWith(
        '确定要删除该账户吗？',
        '删除确认',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning',
        }
      )
    })
  })
})

