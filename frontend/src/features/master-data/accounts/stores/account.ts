import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Account } from '@/features/master-data/accounts/types/account'
import { getAccounts } from '@/features/master-data/accounts/api/account'

export const useAccountStore = defineStore('account', () => {
  const items = ref<Account[]>([])
  const loading = ref(false)
  const lastFetchTime = ref(0)

  /** 缓存有效期：10 分钟 */
  const CACHE_TTL = 10 * 60 * 1000

  /** 活跃账户 */
  const activeAccounts = computed(() =>
    items.value.filter(item => item.isActive !== false)
  )

  /**
   * 加载账户选项数据（带 TTL 缓存）
   * @param force 是否强制刷新
   */
  const loadOptions = async (force = false) => {
    const now = Date.now()
    if (!force && items.value.length > 0 && now - lastFetchTime.value < CACHE_TTL) {
      return
    }

    loading.value = true
    try {
      const { data } = await getAccounts({ page: 1, pageSize: 1000 })
      items.value = data.data.items
      lastFetchTime.value = Date.now()
    } catch (error) {
      console.error('加载账户数据失败:', error)
    } finally {
      loading.value = false
    }
  }

  /** 使缓存失效 */
  const invalidateCache = () => {
    lastFetchTime.value = 0
  }

  /** 按 ID 查找账户 */
  const getItemById = (id: number) => {
    return items.value.find(item => item.id === id)
  }

  return {
    items,
    loading,
    lastFetchTime,
    activeAccounts,
    loadOptions,
    invalidateCache,
    getItemById
  }
})
