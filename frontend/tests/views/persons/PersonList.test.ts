import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import PersonList from '@/views/persons/PersonList.vue'
import PersonForm from '@/views/persons/PersonForm.vue'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import * as personApi from '@/api/person'
import type { Person } from '@/types/person'

vi.mock('@/api/person')

describe('PersonList.vue', () => {
  const mockPersons: Person[] = [
    {
      id: 1,
      name: '张三',
      personType: 'Employee',
      phone: '13800138000',
      email: 'zhangsan@example.com',
      bankName: '工商银行',
      bankAccount: '6222021234567890',
      joinDate: '2023-01-15',
      isActive: true,
      createdAt: '2023-01-15T08:00:00Z'
    },
    {
      id: 2,
      name: '李四',
      personType: 'Contractor',
      phone: '13900139000',
      email: 'lisi@example.com',
      bankName: '建设银行',
      bankAccount: '6217001234567890',
      joinDate: '2023-03-20',
      isActive: false,
      createdAt: '2023-03-20T08:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(personApi.getPersons).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: mockPersons,
          total: 2,
          page: 1,
          pageSize: 20
        }
      })
    )
  })

  it('应该在挂载时加载并渲染人员列表', async () => {
    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()

    expect(personApi.getPersons).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20
    })
    expect(wrapper.text()).toContain('张三')
    expect(wrapper.text()).toContain('李四')
  })

  it('应该正确显示人员类型和状态', async () => {
    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()

    expect(wrapper.text()).toContain('员工')
    expect(wrapper.text()).toContain('承包商')
    expect(wrapper.text()).toContain('在职')
    expect(wrapper.text()).toContain('离职')
  })

  it('应该支持通过弹窗编辑人员', async () => {
    vi.mocked(personApi.updatePerson).mockResolvedValue(
      mockAxiosResponse({
        data: { ...mockPersons[0], name: '王五' }
      })
    )

    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    vm.handleEdit(vm.tableData[0])
    await nextTick()

    const form = wrapper.findComponent(PersonForm)
    expect(form.props('visible')).toBe(true)

    const formVm = form.vm as any
    formVm.formData.name = '王五'

    await formVm.handleSubmit()
    await flushPromises()

    expect(personApi.updatePerson).toHaveBeenCalled()
    expect(ElMessage.success).toHaveBeenCalledWith('更新成功')
    expect(personApi.getPersons).toHaveBeenCalledTimes(2)
  })

  it('应该支持通过弹窗新增人员', async () => {
    vi.mocked(personApi.createPerson).mockResolvedValue(
      mockAxiosResponse({
        data: { id: 3, name: '赵六', personType: 'Employee', isActive: true, createdAt: '' }
      })
    )

    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    vm.handleCreate()
    await nextTick()

    const form = wrapper.findComponent(PersonForm)
    expect(form.props('visible')).toBe(true)

    const formVm = form.vm as any
    formVm.formData.name = '赵六'
    formVm.formData.personType = 'Employee'

    await formVm.handleSubmit()
    await flushPromises()

    expect(personApi.createPerson).toHaveBeenCalled()
    expect(ElMessage.success).toHaveBeenCalledWith('创建成功')
    expect(personApi.getPersons).toHaveBeenCalledTimes(2)
  })

  it('应该支持删除人员', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as any)
    vi.mocked(personApi.deletePerson).mockResolvedValue(
      mockAxiosResponse(null)
    )

    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()
    await nextTick()

    const vm = wrapper.vm as any
    await vm.handleDelete({ id: 1, name: '张三' })
    await flushPromises()

    expect(ElMessageBox.confirm).toHaveBeenCalledWith(
      '确定要删除该人员吗？',
      '提示',
      expect.any(Object)
    )
    expect(personApi.deletePerson).toHaveBeenCalledWith(1)
    expect(ElMessage.success).toHaveBeenCalledWith('删除成功')
  })

  it('点击重置按钮应该清空搜索条件并重新加载', async () => {
    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.searchForm.name = '张三'
    vm.searchForm.personType = 'Employee'
    vm.searchForm.phone = '13800138000'

    vm.handleReset()
    await flushPromises()

    expect(vm.searchForm.name).toBe('')
    expect(vm.searchForm.personType).toBe('')
    expect(vm.searchForm.phone).toBe('')
    expect(personApi.getPersons).toHaveBeenCalledTimes(2)
  })

  it('查询按钮应该传递筛选参数并重置页码', async () => {
    const wrapper = mountWithPlugins(PersonList)
    await flushPromises()

    const vm = wrapper.vm as any
    vm.pagination.page = 2
    vm.searchForm.name = '张三'
    vm.searchForm.personType = 'Employee'
    vm.searchForm.phone = '138'

    const searchButton = wrapper.findAll('.el-button').find(b => b.text() === '查询')
    await searchButton!.trigger('click')
    await flushPromises()

    expect(vm.pagination.page).toBe(1)
    expect(personApi.getPersons).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 20,
      name: '张三',
      personType: 'Employee',
      phone: '138'
    })
  })
})
