import { describe, expect, it } from 'vitest'
import PageHeader from '@/shared/ui/PageHeader.vue'
import { mountWithPlugins } from '@tests/utils'

describe('PageHeader.vue', () => {
  it('应渲染标题、描述和桌面操作按钮', async () => {
    const wrapper = mountWithPlugins(PageHeader, {
      props: {
        title: '交易管理',
        description: '查看记录',
        actions: [{ label: '账户转账', command: 'transfer' }]
      },
      slots: {
        primary: '<button class="primary-slot">新增交易</button>'
      }
    })

    expect(wrapper.text()).toContain('交易管理')
    expect(wrapper.text()).toContain('查看记录')
    expect(wrapper.text()).toContain('账户转账')
    expect(wrapper.text()).toContain('新增交易')

    await wrapper.findAll('button').find((button) => button.text() === '账户转账')?.trigger('click')
    expect(wrapper.emitted('action')?.[0]).toEqual(['transfer'])
  })
})
