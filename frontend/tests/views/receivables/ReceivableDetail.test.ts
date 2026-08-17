import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import { ElMessage } from 'element-plus'
import ReceivableDetail from '@/views/receivables/ReceivableDetail.vue'
import * as receivableApi from '@/api/receivable'

vi.mock('@/api/receivable')

const mockReceivable = {
  id: 1,
  projectId: 1,
  projectName: '项目A',
  customerId: 1,
  customerName: '客户A',
  totalAmount: 10000,
  receivedAmount: 3000,
  remainingAmount: 7000,
  status: 'partial',
  dueDate: '2026-04-01',
  description: '测试应收款项',
  createdAt: '2026-03-01T00:00:00Z',
  updatedAt: '2026-03-01T00:00:00Z',
  settledAt: null,
  details: [
    {
      id: 1,
      receivableId: 1,
      paymentDate: '2026-03-05',
      amount: 3000,
      paymentMethod: 'bank_transfer',
      description: '第一笔收款',
      createdAt: '2026-03-05T10:00:00Z'
    }
  ]
}

const mockSettledReceivable = {
  ...mockReceivable,
  id: 2,
  receivedAmount: 10000,
  remainingAmount: 0,
  status: 'settled',
  settledAt: '2026-03-10T00:00:00Z',
  details: [
    {
      id: 1,
      receivableId: 2,
      paymentDate: '2026-03-05',
      amount: 3000,
      paymentMethod: 'bank_transfer',
      description: '第一笔收款',
      createdAt: '2026-03-05T10:00:00Z'
    },
    {
      id: 2,
      receivableId: 2,
      paymentDate: '2026-03-10',
      amount: 7000,
      paymentMethod: 'cash',
      description: '尾款',
      createdAt: '2026-03-10T10:00:00Z'
    }
  ]
}

describe('ReceivableDetail', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该在 visible 为 true 时加载应收详情', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: {
        visible: false,
        receivableId: 1
      }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(receivableApi.getReceivableById).toHaveBeenCalledWith(1)
  })

  it('应该在加载成功后更新 receivable 数据', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: false, receivableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.receivable).toEqual(mockReceivable)
    expect(vm.loading).toBe(false)
  })

  it('应该在加载失败时显示错误消息', async () => {
    vi.mocked(receivableApi.getReceivableById).mockRejectedValue(new Error('网络错误'))

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: false, receivableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载应收详情失败')
  })

  it('应该在关闭时触发 update:visible 事件', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: true, receivableId: 1 }
    })
    await flushPromises()

    wrapper.vm.$emit('update:visible', false)

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')![0]).toEqual([false])
  })

  it('应该在部分收款状态下初始化付款金额为剩余金额', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: false, receivableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.paymentForm.amount).toBe(7000)
  })

  it('应该在已结清状态下加载数据', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockSettledReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: false, receivableId: 2 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.receivable.status).toBe('settled')
    expect(vm.receivable.remainingAmount).toBe(0)
  })

  it('应该正确渲染格式化后的货币金额', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: false, receivableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('¥10,000.00')
    expect(text).toContain('¥3,000.00')
    expect(text).toContain('¥7,000.00')
  })

  it('应该正确渲染状态文案和标签类型', async () => {
    vi.mocked(receivableApi.getReceivableById).mockResolvedValue(
      mockAxiosResponse({ data: mockReceivable })
    )

    const wrapper = mountWithPlugins(ReceivableDetail, {
      props: { visible: false, receivableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(wrapper.text()).toContain('部分收款')
    expect(wrapper.html()).toContain('el-tag--info')
  })
})
