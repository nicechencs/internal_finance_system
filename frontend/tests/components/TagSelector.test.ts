// @vitest-environment happy-dom
import { defineComponent, reactive } from 'vue'
import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import TagSelector from '@/components/tags/TagSelector.vue'
import { flushPromises } from '../utils'

const loadTagsByScope = vi.fn()
const loadingByScope = reactive<Record<string, boolean>>({})

type TagOption = {
  id: number
  name: string
  color: string
}

vi.mock('@/features/master-data/tags/stores/tagStore', () => ({
  useTagStore: () => ({
    loading: loadingByScope,
    loadTagsByScope
  })
}))

const ElSelectStub = defineComponent({
  name: 'ElSelect',
  props: {
    modelValue: {
      type: null,
      default: undefined
    },
    multiple: {
      type: Boolean,
      default: true
    },
    placeholder: {
      type: String,
      default: ''
    },
    disabled: {
      type: Boolean,
      default: false
    },
    loading: {
      type: Boolean,
      default: false
    }
  },
  emits: ['change'],
  template: `
    <div
      class="el-select"
      :data-loading="String(loading)"
      :data-multiple="String(multiple)"
    >
      <button class="emit-array" @click="$emit('change', [11, 22])">emit-array</button>
      <button class="emit-single" @click="$emit('change', 11)">emit-single</button>
      <button class="emit-null" @click="$emit('change', null)">emit-null</button>
      <button class="emit-undefined" @click="$emit('change', undefined)">emit-undefined</button>
      <slot />
    </div>
  `
})

const ElOptionStub = defineComponent({
  name: 'ElOption',
  props: {
    label: {
      type: String,
      default: ''
    },
    value: {
      type: Number,
      default: 0
    }
  },
  template: '<div class="el-option">{{ label }}</div>'
})

describe('TagSelector', () => {
  const tagOptionsByScope: Record<string, TagOption[]> = {
    transaction: [
      { id: 11, name: 'Transaction A', color: '#111111' },
      { id: 22, name: 'Transaction B', color: '#222222' }
    ],
    customer: [
      { id: 31, name: 'Customer A', color: '#333333' }
    ]
  }

  const mountComponent = (props: Record<string, unknown> = {}) => {
    return mount(TagSelector, {
      props: {
        modelValue: [],
        scope: 'transaction',
        ...props
      },
      global: {
        stubs: {
          'el-select': ElSelectStub,
          'el-option': ElOptionStub
        }
      }
    })
  }

  beforeEach(() => {
    loadTagsByScope.mockReset()

    for (const scope of Object.keys(loadingByScope)) {
      delete loadingByScope[scope]
    }

    loadTagsByScope.mockImplementation(async (scope: string) => {
      return tagOptionsByScope[scope] ?? []
    })
  })

  it('emits both model updates and change events on selection changes', async () => {
    const wrapper = mountComponent()

    await wrapper.find('.emit-array').trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[[11, 22]]])
    expect(wrapper.emitted('change')).toEqual([[[11, 22]]])
  })

  it('normalizes single-select changes into an array payload when multiple is false', async () => {
    const wrapper = mountComponent({
      multiple: false
    })

    await wrapper.find('.emit-single').trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[[11]]])
    expect(wrapper.emitted('change')).toEqual([[[11]]])
  })

  it.each([
    ['null', '.emit-null'],
    ['undefined', '.emit-undefined']
  ])('normalizes %s clears into an empty array payload', async (_caseName, triggerSelector) => {
    const wrapper = mountComponent({
      modelValue: [11]
    })

    await wrapper.find(triggerSelector).trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[[]]])
    expect(wrapper.emitted('change')).toEqual([[[]]])
  })

  it('reloads options when scope changes', async () => {
    const wrapper = mountComponent()
    await flushPromises()

    expect(loadTagsByScope).toHaveBeenNthCalledWith(1, 'transaction')
    expect(wrapper.text()).toContain('Transaction A')

    await wrapper.setProps({
      scope: 'customer'
    })
    await flushPromises()

    expect(loadTagsByScope).toHaveBeenNthCalledWith(2, 'customer')
    expect(wrapper.text()).toContain('Customer A')
    expect(wrapper.text()).not.toContain('Transaction A')
  })

  it('passes through the loading state for the current scope', async () => {
    loadingByScope.transaction = true

    const wrapper = mountComponent()
    const select = wrapper.findComponent(ElSelectStub)

    expect(select.props('loading')).toBe(true)

    loadingByScope.transaction = false
    await wrapper.vm.$nextTick()
    expect(select.props('loading')).toBe(false)

    loadingByScope.customer = true
    await wrapper.setProps({
      scope: 'customer'
    })

    expect(select.props('loading')).toBe(true)
  })

  it('ignores stale async results when scope changes during concurrent loads', async () => {
    let resolveTransaction!: (value: TagOption[]) => void
    let resolveCustomer!: (value: TagOption[]) => void

    const transactionPromise = new Promise<TagOption[]>((resolve) => {
      resolveTransaction = resolve
    })
    const customerPromise = new Promise<TagOption[]>((resolve) => {
      resolveCustomer = resolve
    })

    loadTagsByScope.mockImplementation((scope: string) => {
      return scope === 'transaction' ? transactionPromise : customerPromise
    })

    const wrapper = mountComponent()

    expect(loadTagsByScope).toHaveBeenNthCalledWith(1, 'transaction')

    await wrapper.setProps({
      scope: 'customer'
    })

    expect(loadTagsByScope).toHaveBeenNthCalledWith(2, 'customer')

    resolveCustomer(tagOptionsByScope.customer)
    await flushPromises()

    expect(wrapper.text()).toContain('Customer A')

    resolveTransaction(tagOptionsByScope.transaction)
    await flushPromises()

    expect(wrapper.text()).toContain('Customer A')
    expect(wrapper.text()).not.toContain('Transaction A')
  })
})
