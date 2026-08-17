import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mountWithPlugins, flushPromises, mockAxiosResponse } from '@tests/utils'
import { ElMessage } from 'element-plus'
import PayableDetail from '@/views/payables/PayableDetail.vue'
import * as payableApi from '@/api/payable'

vi.mock('@/api/payable')

const mockPayable = {
  id: 1,
  supplierId: 1,
  supplierName: '供应商A',
  projectId: 1,
  projectName: '项目A',
  totalAmount: 20000,
  paidAmount: 5000,
  remainingAmount: 15000,
  status: 'partial',
  dueDate: '2026-04-15',
  description: '测试应付款项',
  createdAt: '2026-03-01T00:00:00Z',
  updatedAt: '2026-03-01T00:00:00Z',
  settledAt: null,
  details: [
    {
      id: 1,
      payableId: 1,
      paymentDate: '2026-03-05',
      amount: 5000,
      paymentMethod: 'bank_transfer',
      description: '第一笔付款',
      createdAt: '2026-03-05T10:00:00Z'
    }
  ]
}

const mockSettledPayable = {
  ...mockPayable,
  id: 2,
  paidAmount: 20000,
  remainingAmount: 0,
  status: 'settled',
  settledAt: '2026-03-15T00:00:00Z',
  details: [
    {
      id: 1,
      payableId: 2,
      paymentDate: '2026-03-05',
      amount: 5000,
      paymentMethod: 'bank_transfer',
      description: '第一笔付款',
      createdAt: '2026-03-05T10:00:00Z'
    },
    {
      id: 2,
      payableId: 2,
      paymentDate: '2026-03-15',
      amount: 15000,
      paymentMethod: 'check',
      description: '尾款',
      createdAt: '2026-03-15T10:00:00Z'
    }
  ]
}

describe('PayableDetail', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该在 visible 为 true 时加载应付详情', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: {
        visible: false,
        payableId: 1
      }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(payableApi.getPayableById).toHaveBeenCalledWith(1)
  })

  it('应该在加载成功后更新 payable 数据', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.payable).toEqual(mockPayable)
    expect(vm.loading).toBe(false)
  })

  it('应该在加载失败时显示错误消息', async () => {
    vi.mocked(payableApi.getPayableById).mockRejectedValue(new Error('网络错误'))

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(ElMessage.error).toHaveBeenCalledWith('加载应付详情失败')
  })

  it('应该在关闭时触发 update:visible 事件', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: true, payableId: 1 }
    })
    await flushPromises()

    wrapper.vm.$emit('update:visible', false)

    expect(wrapper.emitted('update:visible')).toBeTruthy()
    expect(wrapper.emitted('update:visible')![0]).toEqual([false])
  })

  it('应该在部分付款状态下初始化付款金额为剩余金额', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.paymentForm.amount).toBe(15000)
  })

  it('应该在已结清状态下加载数据', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockSettledPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 2 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const vm = wrapper.vm as any
    expect(vm.payable.status).toBe('settled')
    expect(vm.payable.remainingAmount).toBe(0)
  })

  it('应该正确渲染格式化后的货币金额', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('¥20,000.00')
    expect(text).toContain('¥5,000.00')
    expect(text).toContain('¥15,000.00')
  })

  it('应该正确渲染状态文案和标签类型', async () => {
    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: mockPayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(wrapper.text()).toContain('部分付款')
    expect(wrapper.html()).toContain('el-tag--info')
  })

  it('应该在逾期时高亮到期日期', async () => {
    const overduePayable = {
      ...mockPayable,
      status: 'pending',
      dueDate: '2020-01-01'
    }

    vi.mocked(payableApi.getPayableById).mockResolvedValue(
      mockAxiosResponse({ data: overduePayable })
    )

    const wrapper = mountWithPlugins(PayableDetail, {
      props: { visible: false, payableId: 1 }
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(wrapper.text()).toContain('待付款')
    expect(wrapper.find('.overdue').exists()).toBe(true)
  })
})
