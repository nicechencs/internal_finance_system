import { watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { LocationQueryRaw } from 'vue-router'

export interface RouteFilterFieldMapping {
  queryParam: string      // 路由查询参数名
  filterField: string     // 筛选表单字段名
  type: 'number' | 'number[]' | 'string' | 'boolean'
  defaultValue?: any
}

export interface UseRouteFiltersOptions<T extends Record<string, any>> {
  filters: T
  fieldMappings: RouteFilterFieldMapping[]
  onFiltersApplied?: () => void
}

type WritableFilters<T> = {
  -readonly [K in keyof T]: T[K]
}

export function useRouteFilters<T extends Record<string, any>>(
  options: UseRouteFiltersOptions<T>
) {
  const route = useRoute()
  const router = useRouter()
  const filters = options.filters as WritableFilters<T>
  let lastAppliedQueryKey: string | null = null

  const getQueryKey = () => JSON.stringify(route.query)

  // 解析单个数字
  const parseNumberQuery = (value: unknown): number | null => {
    const raw = Array.isArray(value) ? value[0] : value
    const parsed = Number(raw)
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null
  }

  // 解析数字数组
  const parseNumberArrayQuery = (value: unknown): number[] => {
    if (value == null || value === '') return []
    const raw = Array.isArray(value) ? value : [value]
    return raw
      .flatMap(v => v == null ? [] : String(v).split(','))
      .map(v => Number(v.trim()))
      .filter(n => Number.isFinite(n) && n > 0)
  }

  // 应用路由筛选
  const applyRouteFilters = () => {
    const queryKey = getQueryKey()
    if (lastAppliedQueryKey === queryKey) {
      return
    }

    options.fieldMappings.forEach(mapping => {
      const queryValue = route.query[mapping.queryParam]

      switch (mapping.type) {
        case 'number': {
          const num = parseNumberQuery(queryValue)
          ;(filters as any)[mapping.filterField] = num ?? mapping.defaultValue ?? null
          break
        }
        case 'number[]': {
          const numArray = parseNumberArrayQuery(queryValue)
          ;(filters as any)[mapping.filterField] = numArray.length > 0
            ? numArray
            : (mapping.defaultValue ?? [])
          break
        }
        case 'string':
          ;(filters as any)[mapping.filterField] = queryValue
            ? String(queryValue)
            : (mapping.defaultValue ?? '')
          break
        case 'boolean':
          ;(filters as any)[mapping.filterField] = queryValue === 'true'
          break
      }
    })

    lastAppliedQueryKey = queryKey
    options.onFiltersApplied?.()
  }

  // 监听路由变化
  watch(() => route.query, applyRouteFilters, { deep: true })

  // 更新路由查询参数
  const updateRouteQuery = async (partialFilters?: Partial<T>) => {
    const query: LocationQueryRaw = {}

    options.fieldMappings.forEach(mapping => {
      const value = partialFilters?.[mapping.filterField] ?? filters[mapping.filterField]

      if (value != null && value !== '' &&
          (Array.isArray(value) ? value.length > 0 : true)) {
        query[mapping.queryParam] = Array.isArray(value)
          ? value.map((item: unknown) => String(item))
          : String(value)
      }
    })

    await router.replace({ query })
  }

  return {
    applyRouteFilters,
    updateRouteQuery
  }
}
