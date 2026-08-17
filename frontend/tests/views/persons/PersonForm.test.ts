import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import PersonForm from '@/views/persons/PersonForm.vue'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import * as personApi from '@/api/person'
import type { Person } from '@/types/person'

vi.mock('@/api/person')

describe('PersonForm.vue', () => {
  const mockPerson: Person = {
    id: 1,
    name: '张三',
    personType: 'Employee',
    idNumber: '110101199001011234',
    phone: '13800138000',
    email: 'zhangsan@example.com',
    bankName: '工商银行',
    bankAccount: '6222021234567890',
    joinDate: '2023-01-15',
    leaveDate: '',
    isActive: true,
    createdAt: '2023-01-15T08:00:00Z'
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(personApi.createPerson).mockResolvedValue(
      mockAxiosResponse({ data: mockPerson })
    )
    vi.mocked(personApi.updatePerson).mockResolvedValue(
      mockAxiosResponse({ data: mockPerson })
    )
  })

  it('新增模式应该显示"新增人员"标题', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: null }
    })
    await nextTick()

    expect(wrapper.text()).toContain('新增人员')
  })

  it('编辑模式应该显示"编辑人员"标题', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: mockPerson }
    })
    await nextTick()

    expect(wrapper.text()).toContain('编辑人员')
  })

  it('应该渲染所有基本表单字段', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: null }
    })
    await nextTick()

    expect(wrapper.text()).toContain('姓名')
    expect(wrapper.text()).toContain('人员类型')
    expect(wrapper.text()).toContain('证件号码')
    expect(wrapper.text()).toContain('电话')
    expect(wrapper.text()).toContain('邮箱')
    expect(wrapper.text()).toContain('开户行')
    expect(wrapper.text()).toContain('银行账号')
    expect(wrapper.text()).toContain('入职日期')
  })

  it('编辑模式应该额外显示离职日期和状态字段', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: mockPerson }
    })
    await nextTick()

    expect(wrapper.text()).toContain('离职日期')
    expect(wrapper.text()).toContain('状态')
  })

  it('新增模式不应该显示离职日期和状态字段', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: null }
    })
    await nextTick()

    // 新增模式下 v-if="isEdit" 为 false，不渲染离职日期和状态
    const formItems = wrapper.findAll('.el-form-item')
    const labels = formItems.map(item => item.text())
    const hasLeaveDate = labels.some(l => l.includes('离职日期'))
    expect(hasLeaveDate).toBe(false)
  })

  it('编辑模式应该填充现有人员数据到表单', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: false, person: mockPerson }
    })
    await nextTick()

    // 触发 watch，打开对话框
    await wrapper.setProps({ visible: true })
    await flushPromises()

    // name 现在是 SearchableInput (el-select)，通过 vm 检查
    const vm = wrapper.vm as any
    expect(vm.formData.name).toBe('张三')
    expect(vm.formData.phone).toBe('13800138000')
    expect(vm.formData.email).toBe('zhangsan@example.com')
  })

  it('点击确定按钮应该在新增模式下调用 createPerson', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: null }
    })
    await nextTick()

    // 填写姓名（必填字段）
    const nameInput = wrapper.find('.el-form-item input')
    await nameInput.setValue('王五')
    await nextTick()

    const submitBtn = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(personApi.createPerson).toHaveBeenCalled()
  })

  it('点击确定按钮应该在编辑模式下调用 updatePerson', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: false, person: mockPerson }
    })
    await nextTick()

    // 触发 watch 填充数据
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const submitBtn = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(personApi.updatePerson).toHaveBeenCalledWith(1, expect.objectContaining({
      name: '张三',
      personType: 'Employee'
    }))
  })

  it('点击取消按钮应该触发 update:visible 事件', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: null }
    })
    await nextTick()

    const cancelBtn = wrapper.findAll('.el-button').find(b => b.text() === '取消')
    await cancelBtn!.trigger('click')

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')![0]).toEqual([false])
  })

  it('创建成功后应该触发 success 事件并显示成功消息', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: true, person: null }
    })
    await nextTick()

    // 填写姓名
    const nameInput = wrapper.find('.el-form-item input')
    await nameInput.setValue('新人员')
    await nextTick()

    const submitBtn = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    expect(wrapper.emitted('success')).toBeTruthy()
  })

  it('更新成功后应该触发 success 事件并显示成功消息', async () => {
    const wrapper = mountWithPlugins(PersonForm, {
      props: { visible: false, person: mockPerson }
    })
    await nextTick()

    // 触发 watch 填充数据
    await wrapper.setProps({ visible: true })
    await flushPromises()

    const submitBtn = wrapper.findAll('.el-button').find(b => b.text() === '确定')
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
    expect(wrapper.emitted('success')).toBeTruthy()
  })
})
