import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Customer } from '@/features/master-data/customers/types/customer'
import { getCustomers } from '@/features/master-data/customers/api/customer'

export const useCustomerStore = defineStore('customer', () => {
  const items = ref<Customer[]>([])
  const loading = ref(false)
  const lastFetchTime = ref(0)

  /** 缓存有效期：10 分钟 */
  const CACHE_TTL = 10 * 60 * 1000

  /** 活跃客户 */
  const activeCustomers = computed(() =>
    items.value.filter(item => item.isActive !== false)
  )

  /**
   * 加载客户选项数据（带 TTL 缓存）
   * @param force 是否强制刷新
   */
  const loadOptions = async (force = false) => {
    const now = Date.now()
    if (!force && items.value.length > 0 && now - lastFetchTime.value < CACHE_TTL) {
      return
    }

    loading.value = true
    try {
      const { data } = await getCustomers({ page: 1, pageSize: 1000 })
      items.value = data.data.items
      lastFetchTime.value = Date.now()
    } catch (error) {
      console.error('加载客户数据失败:', error)
    } finally {
      loading.value = false
    }
  }

  /** 使缓存失效 */
  const invalidateCache = () => {
    lastFetchTime.value = 0
  }

  /** 按 ID 查找客户 */
  const getItemById = (id: number) => {
    return items.value.find(item => item.id === id)
  }

  return {
    items,
    loading,
    lastFetchTime,
    activeCustomers,
    loadOptions,
    invalidateCache,
    getItemById
  }
})
