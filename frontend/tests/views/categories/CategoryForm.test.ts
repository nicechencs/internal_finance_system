import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import { ElMessage } from 'element-plus'
import CategoryForm from '@/views/categories/CategoryForm.vue'
import * as categoryApi from '@/api/category'
import type { Category } from '@/types/category'

vi.mock('@/api/category')

describe('CategoryForm.vue', () => {
  const mockParentCategories = [
    {
      id: 1,
      name: '销售收入',
      categoryType: 'Income' as const,
      level: 1,
      isActive: true,
      createdAt: '2024-01-01T00:00:00Z'
    },
    {
      id: 2,
      name: '办公费用',
      categoryType: 'Expense' as const,
      level: 1,
      isActive: true,
      createdAt: '2024-01-02T00:00:00Z'
    }
  ]

  const mockCategory: Category = {
    id: 3,
    name: '房租',
    categoryType: 'Expense',
    parentId: 2,
    parentName: '办公费用',
    level: 2,
    description: '办公室租金',
    isActive: true,
    createdAt: '2024-01-03T00:00:00Z'
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(categoryApi.getActiveCategories).mockResolvedValue(
      mockAxiosResponse({ data: mockParentCategories })
    )
  })

  it('应该正确渲染新增模式的对话框标题', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const dialog = wrapper.findComponent({ name: 'ElDialog' })
    expect(dialog.props('title')).toBe('新增分类')
  })

  it('应该正确渲染编辑模式的对话框标题', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: mockCategory
      }
    })
    await flushPromises()

    const dialog = wrapper.findComponent({ name: 'ElDialog' })
    expect(dialog.props('title')).toBe('编辑分类')
  })

  it('新增模式应该显示分类类型选择器', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const formItems = wrapper.findAllComponents({ name: 'ElFormItem' })
    const typeFormItem = formItems.find(item => item.props('label') === '类型')

    expect(typeFormItem?.exists()).toBe(true)
    expect(typeFormItem?.props('prop')).toBe('categoryType')
  })

  it('编辑模式不应该显示分类类型选择器', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: mockCategory
      }
    })
    await flushPromises()

    const formItems = wrapper.findAllComponents({ name: 'ElFormItem' })
    const typeFormItem = formItems.find(item => item.props('label') === '类型')

    expect(typeFormItem).toBeUndefined()
  })

  it('编辑模式应该显示状态开关', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: mockCategory
      }
    })
    await flushPromises()

    const formItems = wrapper.findAllComponents({ name: 'ElFormItem' })
    const statusFormItem = formItems.find(item => item.props('label') === '状态')

    expect(statusFormItem?.exists()).toBe(true)
    expect(statusFormItem?.props('prop')).toBe('isActive')
  })

  it('新增模式不应该显示状态开关', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const formItems = wrapper.findAllComponents({ name: 'ElFormItem' })
    const statusFormItem = formItems.find(item => item.props('label') === '状态')

    expect(statusFormItem).toBeUndefined()
  })

  it('对话框打开时应该加载父分类列表', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: false,
        category: null
      }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(categoryApi.getActiveCategories).toHaveBeenCalled()
  })

  it('父分类列表应该排除当前编辑的分类', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: mockCategory
      }
    })
    await flushPromises()

    const parentSelect = wrapper.findAllComponents({ name: 'ElSelect' })[0]
    const options = parentSelect.findAllComponents({ name: 'ElOption' })

    const categoryIds = options.map(opt => opt.props('value'))
    expect(categoryIds).not.toContain(mockCategory.id)
  })

  it('编辑模式应该正确填充表单数据', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: false,
        category: mockCategory
      }
    })
    await flushPromises()

    // 通过 setProps 触发 watch
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const nameInput = wrapper.findAllComponents({ name: 'ElInput' })[0]
    expect(nameInput.props('modelValue')).toBe(mockCategory.name)

    const descInput = wrapper.findAllComponents({ name: 'ElInput' })[1]
    expect(descInput.props('modelValue')).toBe(mockCategory.description)

    const statusSwitch = wrapper.findComponent({ name: 'ElSwitch' })
    expect(statusSwitch.props('modelValue')).toBe(mockCategory.isActive)
  })

  it('新增模式应该使用默认表单数据', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const nameInput = wrapper.findAllComponents({ name: 'ElInput' })[0]
    expect(nameInput.props('modelValue')).toBe('')

    const typeSelect = wrapper.findAllComponents({ name: 'ElSelect' })[0]
    expect(typeSelect.props('modelValue')).toBe('Expense')
  })

  it('点击取消按钮应该关闭对话框', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const cancelButton = wrapper.findAll('.el-dialog__footer button')[0]
    await cancelButton.trigger('click')

    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false])
  })

  it('提交新增表单应该调用创建API', async () => {
    vi.mocked(categoryApi.createCategory).mockResolvedValue(
      mockAxiosResponse({ data: mockCategory })
    )

    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    // 填写表单
    const vm = wrapper.vm as any
    vm.formData.name = '新分类'
    vm.formData.categoryType = 'Income'
    vm.formData.description = '测试描述'

    const submitButton = wrapper.findAll('.el-dialog__footer button')[1]
    await submitButton.trigger('click')
    await flushPromises()

    expect(categoryApi.createCategory).toHaveBeenCalledWith({
      name: '新分类',
      categoryType: 'Income',
      parentId: undefined,
      description: '测试描述'
    })
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    expect(wrapper.emitted('success')).toBeTruthy()
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false])
  })

  it('提交编辑表单应该调用更新API', async () => {
    vi.mocked(categoryApi.updateCategory).mockResolvedValue(
      mockAxiosResponse({ data: mockCategory })
    )

    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: false,
        category: mockCategory
      }
    })
    await flushPromises()

    // 通过 setProps 触发 watch 填充表单数据
    await wrapper.setProps({ visible: true })
    await flushPromises()

    // 修改表单
    const vm = wrapper.vm as any
    vm.formData.name = '修改后的名称'
    vm.formData.description = '修改后的描述'
    vm.formData.isActive = false

    const submitButton = wrapper.findAll('.el-dialog__footer button')[1]
    await submitButton.trigger('click')
    await flushPromises()

    expect(categoryApi.updateCategory).toHaveBeenCalledWith(mockCategory.id, {
      name: '修改后的名称',
      parentId: mockCategory.parentId,
      description: '修改后的描述',
      isActive: false
    })
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
    expect(wrapper.emitted('success')).toBeTruthy()
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false])
  })

  it('表单验证失败时不应该提交', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: false,
        category: null
      }
    })
    await wrapper.setProps({ visible: true })
    await flushPromises()

    // 不填写必填字段
    const vm = wrapper.vm as any
    vm.formData.name = ''

    // Mock formRef.validate 模拟验证失败
    vm.formRef.validate = vi.fn(async (cb: any) => {
      if (cb) cb(false)
    })

    const submitButton = wrapper.findAll('.el-dialog__footer button')[1]
    await submitButton.trigger('click')
    await flushPromises()

    expect(categoryApi.createCategory).not.toHaveBeenCalled()
  })

  it('应该正确处理父分类选择', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.formData.parentId = 1

    expect(vm.formData.parentId).toBe(1)
  })

  it('父分类选择器应该支持清空', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: mockCategory
      }
    })
    await flushPromises()

    const parentSelect = wrapper.findAllComponents({ name: 'ElSelect' })[0]
    expect(parentSelect.props('clearable')).toBe(true)
  })

  it('提交时应该显示加载状态', async () => {
    let resolveCreate: any
    vi.mocked(categoryApi.createCategory).mockImplementation(
      () => new Promise(resolve => { resolveCreate = resolve })
    )

    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: false,
        category: null
      }
    })
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.formData.name = '新分类'

    // Mock formRef.validate 确保验证通过
    vm.formRef.validate = vi.fn(async (cb: any) => {
      if (cb) cb(true)
      return true
    })

    const submitButton = wrapper.findAll('.el-dialog__footer button')[1]
    await submitButton.trigger('click')
    // 等待 validate 回调执行
    await flushPromises()

    expect(vm.submitting).toBe(true)

    // 完成请求
    resolveCreate(mockAxiosResponse({ data: mockCategory }))
    await flushPromises()

    expect(vm.submitting).toBe(false)
  })

  it('关闭对话框时应该重置表单（新增模式）', async () => {
    const wrapper = mountWithPlugins(CategoryForm, {
      props: {
        visible: true,
        category: null
      }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.formData.name = '测试名称'
    vm.formData.description = '测试描述'

    await wrapper.setProps({ visible: false })
    await flushPromises()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(vm.formData.name).toBe('')
    expect(vm.formData.description).toBe('')
  })
})
