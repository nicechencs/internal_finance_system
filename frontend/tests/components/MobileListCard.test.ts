import { describe, expect, it } from 'vitest'
import MobileListCard from '@/shared/ui/MobileListCard.vue'
import { mountWithPlugins } from '@tests/utils'

describe('MobileListCard.vue', () => {
  it('应展示标题、金额并在点击时抛出事件', async () => {
    const wrapper = mountWithPlugins(MobileListCard, {
      props: {
        title: '客户A',
        amount: 2000,
        amountType: 'income'
      },
      slots: {
        meta: '2026-08-16 · 工商银行',
        tag: '<span class="tag-slot">收入</span>'
      }
    })

    expect(wrapper.text()).toContain('客户A')
    expect(wrapper.text()).toContain('+2,000.00')
    expect(wrapper.text()).toContain('2026-08-16 · 工商银行')
    expect(wrapper.text()).toContain('收入')

    await wrapper.trigger('click')
    expect(wrapper.emitted('click')).toHaveLength(1)
  })
})
