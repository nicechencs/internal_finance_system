import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import ImportDialog from '@/components/ImportDialog.vue'
import * as importApi from '@/api/import'
import { ElMessage } from 'element-plus'

// Mock API 模块
vi.mock('@/api/import', () => ({
  batchImportFile: vi.fn(),
}))

describe('ImportDialog.vue', () => {
  const defaultProps = {
    modelValue: true,
    moduleType: 'customer' as const,
  }

  const mockSuccessResult = {
    data: {
      totalCount: 10,
      successCount: 10,
      failedCount: 0,
      errors: [],
    },
  }

  const mockPartialResult = {
    data: {
      totalCount: 10,
      successCount: 7,
      failedCount: 3,
      errors: [
        { index: 2, message: '客户名称不能为空' },
        { index: 5, message: '重复的客户名称' },
        { index: 8, message: '格式错误' },
      ],
    },
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(importApi.batchImportFile).mockResolvedValue(
      mockAxiosResponse(mockSuccessResult)
    )
  })

  it('应该正确渲染对话框标题', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    expect(wrapper.html()).toContain('批量导入客户')
  })

  it('不同模块类型应显示不同标题', async () => {
    const modules = [
      { type: 'customer', label: '客户' },
      { type: 'supplier', label: '供应商' },
      { type: 'person', label: '人员' },
      { type: 'project', label: '项目' },
    ] as const

    for (const mod of modules) {
      const wrapper = mountWithPlugins(ImportDialog, {
        props: { ...defaultProps, moduleType: mod.type },
      })
      await flushPromises()

      expect(wrapper.html()).toContain(`批量导入${mod.label}`)
      wrapper.unmount()
    }
  })

  it('应该显示步骤指示器', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    expect(wrapper.html()).toContain('选择文件')
    expect(wrapper.html()).toContain('上传解析')
    expect(wrapper.html()).toContain('完成')
  })

  it('应该显示文件上传区域', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    expect(wrapper.find('.el-upload').exists()).toBe(true)
    expect(wrapper.html()).toContain('将文件拖到此处')
  })

  it('未选择文件时开始导入按钮应该禁用', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const importButton = wrapper.findAll('.el-button').find(
      btn => btn.text().includes('开始导入')
    )
    expect(importButton).toBeTruthy()
    expect(importButton!.attributes('disabled')).toBeDefined()
  })

  it('应该处理文件选择', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })

    expect(vm.selectedFile).toEqual(mockFile)
  })

  it('文件选择后开始导入按钮应该启用', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })
    await wrapper.vm.$nextTick()

    const importButton = wrapper.findAll('.el-button').find(
      btn => btn.text().includes('开始导入')
    )
    expect(importButton).toBeTruthy()
    // 文件选择后按钮应不再 disabled
    expect(vm.selectedFile).not.toBeNull()
  })

  it('应该正确调用 batchImportFile API', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })
    await vm.handleUpload()
    await flushPromises()

    expect(importApi.batchImportFile).toHaveBeenCalledWith('customer', mockFile)
  })

  it('supplier 类型应该使用正确的模块参数', async () => {
    const wrapper = mountWithPlugins(ImportDialog, {
      props: { ...defaultProps, moduleType: 'supplier' as const },
    })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'suppliers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'suppliers.xlsx' })
    await vm.handleUpload()
    await flushPromises()

    expect(importApi.batchImportFile).toHaveBeenCalledWith('supplier', mockFile)
  })

  it('导入成功后应该显示成功结果', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })
    await vm.handleUpload()
    await flushPromises()

    // 等待 setTimeout
    await new Promise(resolve => setTimeout(resolve, 600))

    expect(vm.currentStep).toBe(2)
    expect(vm.result.success).toBe(true)
    expect(vm.result.title).toBe('导入成功')
  })

  it('导入成功后应该触发 success 事件', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })
    await vm.handleUpload()
    await flushPromises()

    // 等待 setTimeout
    await new Promise(resolve => setTimeout(resolve, 600))

    expect(wrapper.emitted('success')).toBeTruthy()
  })

  it('部分导入成功时应该显示警告结果', async () => {
    vi.mocked(importApi.batchImportFile).mockResolvedValue(
      mockAxiosResponse(mockPartialResult)
    )

    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })
    await vm.handleUpload()
    await flushPromises()

    // 等待 setTimeout
    await new Promise(resolve => setTimeout(resolve, 600))

    expect(vm.result.success).toBe(false)
    expect(vm.result.title).toBe('部分导入成功')
    expect(vm.result.errors).toHaveLength(3)
  })

  it('导入失败时应该显示错误结果', async () => {
    vi.mocked(importApi.batchImportFile).mockRejectedValue({
      response: { data: { message: '文件格式错误' } },
    })

    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['bad data'], 'bad.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'bad.xlsx' })
    await vm.handleUpload()
    await flushPromises()

    expect(vm.currentStep).toBe(2)
    expect(vm.result.success).toBe(false)
    expect(vm.result.title).toBe('导入失败')
    expect(vm.result.subTitle).toBe('文件格式错误')
  })

  it('没有选择文件时上传应显示警告', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    await vm.handleUpload()

    expect(ElMessage.warning).toHaveBeenCalledWith('请选择要上传的文件')
    expect(importApi.batchImportFile).not.toHaveBeenCalled()
  })

  it('关闭对话框应该触发 update:modelValue', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleClose()

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')![0]).toEqual([false])
  })

  it('上传中不允许关闭对话框', async () => {
    // 使用一个永不 resolve 的 Promise 来模拟上传中状态
    vi.mocked(importApi.batchImportFile).mockImplementation(
      () => new Promise(() => {})
    )

    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    const mockFile = new File(['test data'], 'customers.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    vm.handleFileChange({ raw: mockFile, name: 'customers.xlsx' })
    vm.handleUpload() // 不 await，模拟上传进行中
    await flushPromises()

    // 尝试关闭
    vm.handleClose()

    expect(ElMessage.warning).toHaveBeenCalledWith('正在导入中，请稍候...')
    // modelValue 不应该被更新为 false
    const emitted = wrapper.emitted('update:modelValue') || []
    const hasFalse = emitted.some((args: any) => args[0] === false)
    expect(hasFalse).toBe(false)
  })

  it('关闭后应该重置所有状态', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any

    // 设置一些状态
    vm.handleFileChange({
      raw: new File(['test'], 'test.xlsx'),
      name: 'test.xlsx',
    })
    vm.currentStep = 2
    vm.uploadProgress = 100
    vm.result = { success: true, title: '成功', subTitle: '', errors: [] }

    // 关闭对话框
    vm.handleClose()

    // 等待 setTimeout 重置
    await new Promise(resolve => setTimeout(resolve, 400))

    expect(vm.currentStep).toBe(0)
    expect(vm.selectedFile).toBeNull()
    expect(vm.uploadProgress).toBe(0)
  })

  it('应该显示正确的文件格式提示', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    // 客户类型应该提示客户名称必填
    expect(wrapper.html()).toContain('客户名称')
  })

  it('超过文件数量限制时应该显示警告', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleExceed()

    expect(ElMessage.warning).toHaveBeenCalledWith('只能上传一个文件')
  })

  it('应该支持删除已选文件', async () => {
    const wrapper = mountWithPlugins(ImportDialog, { props: defaultProps })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.handleFileChange({
      raw: new File(['test'], 'test.xlsx'),
      name: 'test.xlsx',
    })

    expect(vm.selectedFile).not.toBeNull()

    vm.removeFile()

    expect(vm.selectedFile).toBeNull()
  })

  it('modelValue 为 false 时对话框不可见', async () => {
    const wrapper = mountWithPlugins(ImportDialog, {
      props: { ...defaultProps, modelValue: false },
    })
    await flushPromises()

    // 对话框应该不渲染内容
    expect(wrapper.find('.import-container').exists()).toBe(false)
  })
})
