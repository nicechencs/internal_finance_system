import { effectScope, nextTick, reactive } from 'vue'
import type { EffectScope } from 'vue'
import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { useRouteFilters } from '@/shared/composables/useRouteFilters'

const route = reactive<{ query: Record<string, unknown> }>({
  query: {}
})

const replace = vi.fn()

vi.mock('vue-router', () => ({
  useRoute: () => route,
  useRouter: () => ({
    replace
  })
}))

describe('useRouteFilters', () => {
  let scope: EffectScope

  const setup = (filters: { tagIds: number[] }, onFiltersApplied?: () => void) => {
    return scope.run(() => useRouteFilters({
      filters,
      fieldMappings: [
        { queryParam: 'tagId', filterField: 'tagIds', type: 'number[]' }
      ],
      onFiltersApplied
    }))!
  }

  beforeEach(() => {
    scope = effectScope()
    route.query = {}
    replace.mockReset()
  })

  afterEach(() => {
    scope.stop()
  })

  it('parses comma-separated number arrays from the route query', async () => {
    const filters = reactive({
      tagIds: [] as number[]
    })
    const onFiltersApplied = vi.fn()

    setup(filters, onFiltersApplied)

    route.query = {
      tagId: '1, 2,3'
    }
    await nextTick()

    expect(filters.tagIds).toEqual([1, 2, 3])
    expect(onFiltersApplied).toHaveBeenCalledTimes(1)
  })

  it('parses repeated query params and drops invalid values', () => {
    const filters = reactive({
      tagIds: [] as number[]
    })

    const { applyRouteFilters } = setup(filters)
    route.query = {
      tagId: ['4', '0', 'x', '7']
    }

    applyRouteFilters()

    expect(filters.tagIds).toEqual([4, 7])
  })

  it('writes number arrays as repeated query params and round-trips them', async () => {
    const filters = reactive({
      tagIds: [8, 13, 21]
    })

    const { applyRouteFilters, updateRouteQuery } = setup(filters)

    await updateRouteQuery()

    expect(replace).toHaveBeenCalledWith({
      query: {
        tagId: ['8', '13', '21']
      }
    })

    filters.tagIds = []
    route.query = replace.mock.calls[0][0].query

    applyRouteFilters()

    expect(filters.tagIds).toEqual([8, 13, 21])
  })
})
