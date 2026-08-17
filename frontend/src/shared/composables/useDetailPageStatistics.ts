import { ref, watch } from 'vue'
import type { Ref } from 'vue'
import type { AxiosResponse } from 'axios'
import type { ApiResponse } from '@/shared/utils/request'

export interface UseDetailPageStatisticsOptions<T> {
  entityId: Ref<number | null>
  fetchStatistics: (id: number) => Promise<AxiosResponse<ApiResponse<T>>>
  initialStatistics: T
  onRefresh?: () => void
}

export function useDetailPageStatistics<T>(
  options: UseDetailPageStatisticsOptions<T>
) {
  const statistics = ref<T | null>(null) as Ref<T | null>
  const loading = ref(false)

  const loadStatistics = async () => {
    const id = options.entityId.value
    if (!id) return

    loading.value = true
    try {
      const { data } = await options.fetchStatistics(id)
      statistics.value = data.data
    } catch (error) {
      console.error('加载统计数据失败:', error)
      statistics.value = options.initialStatistics
    } finally {
      loading.value = false
    }
  }

  // 监听 entityId 变化自动加载
  watch(options.entityId, (newId) => {
    if (newId) {
      loadStatistics()
    }
  }, { immediate: true })

  const refreshStatistics = () => {
    loadStatistics()
    options.onRefresh?.()
  }

  return {
    statistics,
    statisticsLoading: loading,
    loadStatistics,
    refreshStatistics
  }
}
