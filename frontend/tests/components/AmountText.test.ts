import { describe, expect, it } from 'vitest'
import AmountText from '@/shared/ui/AmountText.vue'
import { mountWithPlugins } from '@tests/utils'

describe('AmountText.vue', () => {
  it('应按收入类型显示正号和成功色', () => {
    const wrapper = mountWithPlugins(AmountText, {
      props: { value: 1234.5, type: 'income' }
    })

    expect(wrapper.text()).toBe('+1,234.50')
    expect(wrapper.classes()).toContain('amount-text--income')
  })

  it('应按支出类型显示负号', () => {
    const wrapper = mountWithPlugins(AmountText, {
      props: { value: 88, type: 'expense' }
    })

    expect(wrapper.text()).toBe('-88.00')
    expect(wrapper.classes()).toContain('amount-text--expense')
  })

  it('中性金额可以不带正负号', () => {
    const wrapper = mountWithPlugins(AmountText, {
      props: { value: 10, type: 'neutral', signed: false }
    })

    expect(wrapper.text()).toBe('10.00')
  })
})
