import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import ImportPage from '@/views/import/ImportPage.vue'
import * as importApi from '@/api/import'
import * as accountApi from '@/api/account'
import { ElMessage, ElMessageBox } from 'element-plus'

// Mock API 模块
vi.mock('@/api/import', () => ({
  previewImport: vi.fn(),
  confirmImport: vi.fn(),
  getImportBatches: vi.fn(),
}))

vi.mock('@/api/account', () => ({
  getActiveAccounts: vi.fn(),
}))

describe('ImportPage.vue', () => {
  const mockAccounts = [
    { id: 1, name: '工商银行', balance: 100000 },
    { id: 2, name: '招商银行', balance: 50000 },
  ]

  const mockPreviewData = {
    batchId: 123,
    fileName: 'bank_statement.xlsx',
    totalRows: 10,
    newRows: 8,
    duplicateRows: 2,
    fileConflictRows: 0,
    recoverableRows: 0,
    detectedFormat: 'Simple',
    previews: [
      {
        rowNumber: 1,
        transactionDate: '2024-03-14',
        amount: 5000.00,
        direction: 'in',
        counterpartyName: '客户A',
        description: '项目款',
        memo: '首次合同款',
        matchedCategoryName: '项目收入',
        uniqueHash: 'hash-1',
        isDuplicate: false,
        isFileConflict: false,
        isRecoverable: false,
      },
      {
        rowNumber: 2,
        transactionDate: '2024-03-13',
        amount: 1200.00,
        direction: 'out',
        counterpartyName: '供应商B',
        description: '采购办公用品',
        memo: '3月办公用品',
        matchedCategoryName: '办公费用',
        uniqueHash: 'hash-2',
        isDuplicate: false,
        isFileConflict: false,
        isRecoverable: false,
      },
      {
        rowNumber: 3,
        transactionDate: '2024-03-12',
        amount: 3000.00,
        direction: 'in',
        counterpartyName: '客户C',
        description: '重复数据',
        memo: '重复明细',
        matchedCategoryName: null,
        uniqueHash: 'hash-3',
        isDuplicate: true,
        isFileConflict: false,
        isRecoverable: false,
      },
    ],
  }

  const mockBatches = {
    items: [
      {
        id: 1,
        fileName: 'statement_202403.xlsx',
        accountId: 1,
        accountName: '工商银行',
        totalCount: 50,
        successCount: 45,
        duplicateCount: 5,
        errorCount: 0,
        status: 'Completed',
        createdAt: '2024-03-14T10:00:00Z',
      },
      {
        id: 2,
        fileName: 'statement_202402.xlsx',
        accountId: 2,
        accountName: '招商银行',
        totalCount: 30,
        successCount: 28,
        duplicateCount: 2,
        errorCount: 0,
        status: 'Completed',
        createdAt: '2024-02-15T09:00:00Z',
      },
    ],
    total: 2,
  }

  beforeEach(() => {
    vi.clearAllMocks()

    // 设置默认 mock 返回值
    vi.mocked(accountApi.getActiveAccounts).mockResolvedValue(
      mockAxiosResponse({ data: mockAccounts })
    )
    vi.mocked(importApi.getImportBatches).mockResolvedValue(
      mockAxiosResponse({ data: mockBatches })
    )
    vi.mocked(importApi.previewImport).mockResolvedValue(
      mockAxiosResponse({ data: mockPreviewData })
    )
    vi.mocked(importApi.confirmImport).mockResolvedValue(
      mockAxiosResponse({ data: { successCount: 8, duplicateCount: 0 } })
    )
  })

  it('应该正确渲染页面标题和上传表单', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    expect(wrapper.find('.page-title').text()).toBe('流水导入')
    expect(wrapper.find('.page-desc').text()).toContain('导入银行流水数据')
    expect(wrapper.find('.el-upload').exists()).toBe(true)
  })

  it('应该加载账户列表和导入历史', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    // 验证 API 调用
    expect(accountApi.getActiveAccounts).toHaveBeenCalledOnce()
    expect(importApi.getImportBatches).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
    })

    // 验证账户选择器渲染
    expect(wrapper.find('.el-select').exists()).toBe(true)

    // 验证导入历史表格
    const tables = wrapper.findAll('.el-table')
    expect(tables.length).toBeGreaterThanOrEqual(1)
  })

  it('应该处理文件选择', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const file = new File(['test content'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    const uploadFile = {
      name: 'test.xlsx',
      size: 1024,
      raw: file,
      uid: 123,
    }

    // 模拟文件选择
    const vm = wrapper.vm as any
    vm.handleFileChange(uploadFile)

    expect(vm.uploadForm.file).toStrictEqual(file)
  })

  it('应该在未选择账户或文件时禁用预览按钮', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    // Element Plus 渲染的按钮使用 class 而非 type 属性
    const previewButton = wrapper.findAll('.el-button').find(btn => btn.text().includes('解析预览'))
    expect(previewButton).toBeTruthy()
    expect(previewButton!.attributes('disabled')).toBeDefined()
  })

  it('应该成功预览导入数据', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any

    // 设置表单数据
    vm.uploadForm.accountId = 1
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await wrapper.vm.$nextTick()

    // 触发预览
    await vm.handlePreview()
    await flushPromises()

    // 验证 API 调用
    expect(importApi.previewImport).toHaveBeenCalled()
    const callArg = vi.mocked(importApi.previewImport).mock.calls[0][0]
    expect(callArg).toBeInstanceOf(FormData)

    // 验证成功消息
    expect(ElMessage.success).toHaveBeenCalledWith(
      expect.stringContaining('解析完成')
    )

    // 验证预览数据显示
    expect(vm.previewData).toEqual(mockPreviewData)
  })

  it('应该在预览表格中正确标记重复行', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.uploadForm.accountId = 1
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await vm.handlePreview()
    await flushPromises()
    await wrapper.vm.$nextTick()

    // 验证预览统计
    expect(wrapper.html()).toContain('总计: 10 条')
    expect(wrapper.html()).toContain('新数据: 8 条')
    expect(wrapper.html()).toContain('重复: 2 条')
  })

  it('应该成功确认导入', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)

    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any

    // 设置预览数据
    vm.previewData = mockPreviewData
    vm.selectedRows = mockPreviewData.previews.filter(p => !p.isDuplicate)

    await wrapper.vm.$nextTick()

    // 触发确认导入
    await vm.handleConfirmImport()
    await flushPromises()

    // 验证确认对话框
    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      expect.stringContaining('确定要导入选中的 2 条数据吗'),
      '确认导入',
      expect.any(Object)
    )

    // 验证 API 调用
    expect(importApi.confirmImport).toHaveBeenCalledWith({
      batchId: 123,
      selectedRowNumbers: [1, 2],
    })

    // 验证成功消息
    expect(ElMessage.success).toHaveBeenCalledWith(
      expect.stringContaining('导入完成')
    )
  })

  it('应该处理导入取消', async () => {
    vi.mocked(ElMessageBox.confirm).mockRejectedValue('cancel')

    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.previewData = mockPreviewData
    vm.selectedRows = mockPreviewData.previews.filter(p => !p.isDuplicate)

    await vm.handleConfirmImport()
    await flushPromises()

    // 验证 API 未被调用
    expect(importApi.confirmImport).not.toHaveBeenCalled()
  })

  it('应该处理文件上传错误', async () => {
    vi.mocked(importApi.previewImport).mockRejectedValue(
      new Error('File parse error')
    )

    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.uploadForm.accountId = 1
    vm.uploadForm.file = new File(['invalid'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await vm.handlePreview()
    await flushPromises()

    // 验证错误被捕获
    expect(consoleErrorSpy).toHaveBeenCalled()

    consoleErrorSpy.mockRestore()
  })

  it('应该在未选择账户时显示警告', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.uploadForm.accountId = null
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await vm.handlePreview()

    expect(ElMessage.warning).toHaveBeenCalledWith('请选择目标账户')
  })

  it('应该正确取消预览', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.previewData = mockPreviewData
    vm.selectedRows = [mockPreviewData.previews[0]]
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleCancelPreview()

    expect(vm.previewData).toBeNull()
    expect(vm.selectedRows).toEqual([])
    expect(vm.uploadForm.file).toBeNull()
  })

  it('应该显示导入历史记录', async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any

    expect(vm.batches).toHaveLength(2)
    expect(vm.batches.map((batch: any) => batch.fileName)).toEqual([
      'statement_202403.xlsx',
      'statement_202402.xlsx',
    ])
    expect(vm.batches.map((batch: any) => batch.accountName)).toEqual([
      '工商银行',
      '招商银行',
    ])
  })

  it('应该在预览中显示检测到的文件格式标签', async () => {
    const huaxiaPreviewData = {
      ...mockPreviewData,
      detectedFormat: 'HuaxiaBank',
    }
    vi.mocked(importApi.previewImport).mockResolvedValue(
      mockAxiosResponse({ data: huaxiaPreviewData })
    )

    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.uploadForm.accountId = 1
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await vm.handlePreview()
    await flushPromises()
    await wrapper.vm.$nextTick()

    expect(['primary', 'success', 'info', 'warning', 'danger']).toContain(
      vm.getFormatTagType('HuaxiaBank')
    )
    expect(wrapper.text()).toContain('华夏银行')
  })

  it('应该在非Simple格式预览中显示额外列', async () => {
    const extendedPreviewData = {
      ...mockPreviewData,
      detectedFormat: 'HuaxiaBank',
      previews: [
        {
          rowNumber: 1,
          transactionDate: '2024-03-14',
          amount: 5000.00,
          direction: 'in',
          counterpartyName: '客户A',
          description: '项目款',
          memo: '首次合同款',
          matchedCategoryName: '项目收入',
          uniqueHash: 'hash-extended-1',
          isDuplicate: false,
          isFileConflict: false,
          isRecoverable: false,
          transactionTime: '14:30:00',
          balance: 100000.00,
          counterpartyBank: '工商银行北京支行',
          transactionNumber: 'TXN20240314001',
        },
      ],
    }
    vi.mocked(importApi.previewImport).mockResolvedValue(
      mockAxiosResponse({ data: extendedPreviewData })
    )

    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.uploadForm.accountId = 1
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await vm.handlePreview()
    await flushPromises()
    await wrapper.vm.$nextTick()

    // 验证 isExtendedFormat 为 true
    expect(vm.isExtendedFormat).toBe(true)

    // 验证额外列显示
    const text = wrapper.text()
    expect(text).toContain('交易时间')
    expect(text).toContain('余额')
    expect(text).toContain('对方银行')
    expect(text).toContain('流水号')
  })

  it('Simple格式预览不应显示额外列', async () => {
    const simplePreviewData = {
      ...mockPreviewData,
      detectedFormat: 'Simple',
    }
    vi.mocked(importApi.previewImport).mockResolvedValue(
      mockAxiosResponse({ data: simplePreviewData })
    )

    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.uploadForm.accountId = 1
    vm.uploadForm.file = new File(['test'], 'test.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await vm.handlePreview()
    await flushPromises()
    await wrapper.vm.$nextTick()

    // 验证 isExtendedFormat 为 false
    expect(vm.isExtendedFormat).toBe(false)

    // 验证额外列不显示
    const text = wrapper.text()
    expect(text).not.toContain('交易时间')
    expect(text).not.toContain('对方银行')
    expect(text).not.toContain('流水号')
  })

  it('应该正确映射格式标签', { timeout: 10000 }, async () => {
    const wrapper = mountWithPlugins(ImportPage)
    await flushPromises()

    const vm = wrapper.vm as any

    // 验证 getFormatLabel 函数对各格式返回正确中文标签
    expect(vm.getFormatLabel('Simple')).toBe('通用格式')
    expect(vm.getFormatLabel('HuaxiaBank')).toBe('华夏银行')
    expect(vm.getFormatLabel('AlipayBusiness')).toBe('企业支付宝')
    expect(['primary', 'success', 'info', 'warning', 'danger']).toContain(
      vm.getFormatTagType('HuaxiaBank')
    )
  })
})
