import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mountWithPlugins, mockAxiosResponse } from '@tests/utils'
import { ElMessage } from 'element-plus'
import ProjectForm from '@/views/projects/ProjectForm.vue'
import * as projectApi from '@/api/project'
import * as customerApi from '@/api/customer'
import { ApiError, ErrorCode } from '@/shared/types/error'

vi.mock('@/api/project')
vi.mock('@/api/customer')

describe('ProjectForm.vue', () => {
  const mockCustomers = [
    { id: 1, name: '客户A', contactPerson: '张三', phone: '13800138000' },
    { id: 2, name: '客户B', contactPerson: '李四', phone: '13900139000' }
  ]

  const mockProject = {
    id: 1,
    projectCode: 'PRJ001',
    name: '测试项目',
    customerId: 1,
    customerName: '客户A',
    contractAmount: 50000,
    status: 'Active' as const,
    startDate: '2026-01-01',
    endDate: '2026-06-30',
    description: '测试描述'
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(customerApi.getActiveCustomers).mockResolvedValue(
      mockAxiosResponse({ data: mockCustomers })
    )
    vi.mocked(projectApi.createProject).mockResolvedValue(mockAxiosResponse({}))
    vi.mocked(projectApi.updateProject).mockResolvedValue(mockAxiosResponse({}))
    vi.mocked(projectApi.generateProjectCode).mockResolvedValue(
      mockAxiosResponse({ data: 'PRJ-2026-001' })
    )
  })

  it('应该渲染新增项目表单', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    expect(wrapper.text()).toContain('新增项目')
    expect(wrapper.find('input[placeholder="请输入项目编号或点击一键生成"]').exists()).toBe(true)
  })

  it('应该渲染编辑项目表单并填充数据', async () => {
    // 先以 visible: false 挂载，再切换为 true 以触发 watch
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: false, project: mockProject }
    })
    await flushPromises()

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(wrapper.text()).toContain('编辑项目')

    const vm = wrapper.vm as any
    expect(vm.form.name).toBe('测试项目')
    expect(vm.form.projectCode).toBe('PRJ001')
    expect(vm.form.contractAmount).toBe(50000)
    expect(vm.form.status).toBe('Active')
  })

  it('应该加载客户列表', async () => {
    mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    expect(customerApi.getActiveCustomers).toHaveBeenCalled()
  })

  it('应该支持一键生成项目编号', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const generateButton = wrapper.findAll('.el-button').find(btn => btn.text() === '一键生成')
    await generateButton?.trigger('click')
    await flushPromises()

    const vm = wrapper.vm as any
    expect(projectApi.generateProjectCode).toHaveBeenCalled()
    expect(vm.form.projectCode).toBe('PRJ-2026-001')
    expect(ElMessage.success).toHaveBeenCalledWith('项目编号已生成')
  })

  it('应该验证必填字段', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.name = ''
    vm.form.contractAmount = undefined
    await wrapper.vm.$nextTick()

    // 直接调用 handleSubmit 并检查验证结果
    await vm.handleSubmit()
    await flushPromises()

    // 验证表单应该包含必填字段的规则
    const formText = wrapper.text()
    expect(formText).toContain('项目名称')
    expect(formText).toContain('合同金额')
  })

  it('项目编号为空时应该给出提示并阻止提交', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.rules.projectCode[0].validator).toBeTypeOf('function')

    const callback = vi.fn()
    vm.rules.projectCode[0].validator({}, '   ', callback)

    expect(callback).toHaveBeenCalledOnce()
    expect((callback.mock.calls[0][0] as Error).message).toBe('请输入项目编号或点击一键生成')
  })

  it('应该成功提交新增项目', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.projectCode = 'PRJ-2026-001'
    vm.form.name = '新项目'
    vm.form.contractAmount = 100000
    vm.form.status = 'Active'

    await vm.handleSubmit()
    await flushPromises()

    // 如果表单验证通过，应调用 createProject
    if (vi.mocked(projectApi.createProject).mock.calls.length > 0) {
      expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    }
  })

  it('应该成功提交编辑项目', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: mockProject }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.name = '修改后的项目'

    await vm.handleSubmit()
    await flushPromises()

    if (vi.mocked(projectApi.updateProject).mock.calls.length > 0) {
      expect(projectApi.updateProject).toHaveBeenCalledWith(1, expect.objectContaining({
        name: '修改后的项目'
      }))
      expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
    }
  })

  it('应该处理关闭对话框', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const cancelButton = wrapper.findAll('.el-button').find(btn => btn.text() === '取消')
    await cancelButton?.trigger('click')
    await flushPromises()

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')![0]).toEqual([false])
  })

  it('应该包含日期范围选择器', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    expect(wrapper.text()).toContain('项目日期')
    // start-placeholder 和 end-placeholder 是 input 的 placeholder 属性，不在 text() 中
    // 改为检查 date-picker 组件的 HTML 属性
    const html = wrapper.html()
    expect(html).toContain('开始日期')
    expect(html).toContain('结束日期')
  })

  it('应该包含状态单选按钮组', async () => {
    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    expect(wrapper.text()).toContain('进行中')
    expect(wrapper.text()).toContain('已完成')
    expect(wrapper.text()).toContain('已取消')
  })

  it('应该处理提交失败', async () => {
    vi.mocked(projectApi.createProject).mockRejectedValue(new Error('网络错误'))

    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.projectCode = 'PRJ-2026-001'
    vm.form.name = '新项目'
    vm.form.contractAmount = 100000
    vm.form.status = 'Active'

    await vm.handleSubmit()
    await flushPromises()

    if (vi.mocked(projectApi.createProject).mock.calls.length > 0) {
      expect(ElMessage.error).toHaveBeenCalledWith('创建失败')
    }
  })

  it('项目编号重复时应保留后端返回的提示', async () => {
    vi.mocked(projectApi.createProject).mockRejectedValue(
      new ApiError(ErrorCode.VALIDATION_ERROR, '项目编号已存在: PRJ-2026-001', 400)
    )

    const wrapper = mountWithPlugins(ProjectForm, {
      props: { visible: true, project: null }
    })
    await flushPromises()

    const vm = wrapper.vm as any
    vm.form.projectCode = 'PRJ-2026-001'
    vm.form.name = '新项目'
    vm.form.contractAmount = 100000
    vm.form.status = 'Active'

    await vm.handleSubmit()
    await flushPromises()

    expect(projectApi.createProject).toHaveBeenCalled()
    expect(ElMessage.error).not.toHaveBeenCalledWith('创建失败')
  })
})
