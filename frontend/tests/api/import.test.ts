import { describe, it, expect, vi, beforeEach } from 'vitest'
import request from '@/utils/request'
import {
  previewImport,
  confirmImport,
  getImportBatches,
  getImportBatchById,
  batchImportFile,
} from '@/api/import'

// Mock request 模块
vi.mock('@/utils/request', () => ({
  default: vi.fn(),
}))

describe('import API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('previewImport', () => {
    it('应该使用 POST 方法和 multipart/form-data 头', async () => {
      const mockFormData = new FormData()
      mockFormData.append('file', new File(['test'], 'test.xlsx'))
      mockFormData.append('accountId', '1')

      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await previewImport(mockFormData)

      expect(request).toHaveBeenCalledWith({
        url: '/imports/preview',
        method: 'post',
        data: mockFormData,
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      })
    })

    it('应该传递 FormData 参数', async () => {
      const mockFormData = new FormData()
      mockFormData.append('file', new File(['content'], 'bank.xlsx'))
      mockFormData.append('accountId', '42')

      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await previewImport(mockFormData)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.data).toBe(mockFormData)
    })
  })

  describe('confirmImport', () => {
    it('应该使用 POST 方法发送确认请求', async () => {
      const confirmData = {
        batchId: 123,
        selectedRowNumbers: [1, 2, 3],
      }

      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await confirmImport(confirmData)

      expect(request).toHaveBeenCalledWith({
        url: '/imports/confirm',
        method: 'post',
        data: confirmData,
      })
    })

    it('应该传递正确的行号列表', async () => {
      const confirmData = {
        batchId: 1,
        selectedRowNumbers: [1, 3, 5, 7],
      }

      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await confirmImport(confirmData)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.data.selectedRowNumbers).toEqual([1, 3, 5, 7])
    })
  })

  describe('getImportBatches', () => {
    it('应该使用 GET 方法查询批次列表', async () => {
      const params = { page: 1, pageSize: 10 }

      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await getImportBatches(params)

      expect(request).toHaveBeenCalledWith({
        url: '/imports/batches',
        method: 'get',
        params,
      })
    })

    it('应该传递筛选参数', async () => {
      const params = {
        page: 2,
        pageSize: 20,
        status: 'Completed',
        fileName: 'huaxia',
      }

      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await getImportBatches(params)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.params).toEqual(params)
    })
  })

  describe('getImportBatchById', () => {
    it('应该使用正确的 URL 路径', async () => {
      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await getImportBatchById(42)

      expect(request).toHaveBeenCalledWith({
        url: '/imports/batches/42',
        method: 'get',
      })
    })

    it('应该将 id 拼接到 URL 中', async () => {
      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      await getImportBatchById(999)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.url).toBe('/imports/batches/999')
    })
  })

  describe('batchImportFile', () => {
    it('应该使用正确的模块 URL', async () => {
      const testCases = [
        { moduleType: 'customer' as const, expectedUrl: '/customers/batch-import' },
        { moduleType: 'supplier' as const, expectedUrl: '/suppliers/batch-import' },
        { moduleType: 'person' as const, expectedUrl: '/persons/batch-import' },
        { moduleType: 'project' as const, expectedUrl: '/projects/batch-import' },
      ]

      for (const tc of testCases) {
        vi.clearAllMocks()
        vi.mocked(request).mockResolvedValue({ data: { data: {} } })

        const file = new File(['data'], 'test.xlsx')
        await batchImportFile(tc.moduleType, file)

        const callArg = vi.mocked(request).mock.calls[0][0]
        expect(callArg.url).toBe(tc.expectedUrl)
      }
    })

    it('应该使用 multipart/form-data 头', async () => {
      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      const file = new File(['data'], 'test.xlsx')
      await batchImportFile('customer', file)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.headers['Content-Type']).toBe('multipart/form-data')
    })

    it('应该设置 60 秒超时', async () => {
      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      const file = new File(['data'], 'test.xlsx')
      await batchImportFile('customer', file)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.timeout).toBe(60000)
    })

    it('应该将文件添加到 FormData', async () => {
      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      const file = new File(['test content'], 'customers.xlsx')
      await batchImportFile('supplier', file)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.data).toBeInstanceOf(FormData)
      expect(callArg.data.get('file')).toBe(file)
    })

    it('应该使用 POST 方法', async () => {
      vi.mocked(request).mockResolvedValue({ data: { data: {} } })

      const file = new File(['data'], 'test.xlsx')
      await batchImportFile('person', file)

      const callArg = vi.mocked(request).mock.calls[0][0]
      expect(callArg.method).toBe('post')
    })
  })
})
