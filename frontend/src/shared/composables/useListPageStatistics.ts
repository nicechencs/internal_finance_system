import { ref, onMounted } from 'vue'
import type { Ref } from 'vue'
import type { AxiosResponse } from 'axios'
import type { ApiResponse } from '@/shared/utils/request'

export interface UseListPageStatisticsOptions<T, P = any> {
  fetchStatistics: (params?: P) => Promise<AxiosResponse<ApiResponse<T>>>
  initialStatistics: T
  buildParams?: () => P
  autoLoad?: boolean
}

export function useListPageStatistics<T, P = any>(
  options: UseListPageStatisticsOptions<T, P>
) {
  const statistics = ref<T>(options.initialStatistics) as Ref<T>
  const loading = ref(false)

  const loadStatistics = async () => {
    loading.value = true
    try {
      const params = options.buildParams?.()
      const { data } = await options.fetchStatistics(params)
      statistics.value = data.data
    } catch (error) {
      console.error('加载统计数据失败:', error)
      // 失败时保持当前值，不重置
    } finally {
      loading.value = false
    }
  }

  if (options.autoLoad !== false) {
    onMounted(() => {
      loadStatistics()
    })
  }

  return {
    statistics,
    statisticsLoading: loading,
    loadStatistics
  }
}
