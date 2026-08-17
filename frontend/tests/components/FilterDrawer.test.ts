import { describe, expect, it } from 'vitest'
import FilterDrawer from '@/shared/ui/FilterDrawer.vue'
import { mountWithPlugins } from '@tests/utils'

describe('FilterDrawer.vue', () => {
  it('桌面端应直接渲染筛选插槽', () => {
    const wrapper = mountWithPlugins(FilterDrawer, {
      props: { activeCount: 2 },
      slots: {
        default: '<form class="filter-slot">账户筛选</form>'
      }
    })

    expect(wrapper.text()).toContain('账户筛选')
    expect(wrapper.find('.filter-slot').exists()).toBe(true)
    expect(wrapper.find('.filter-toolbar').exists()).toBe(false)
  })
})
