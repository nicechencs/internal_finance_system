import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Supplier } from '@/features/master-data/suppliers/types/supplier'
import { getSuppliers } from '@/features/master-data/suppliers/api/supplier'

export const useSupplierStore = defineStore('supplier', () => {
  const items = ref<Supplier[]>([])
  const loading = ref(false)
  const lastFetchTime = ref(0)

  /** 缓存有效期：10 分钟 */
  const CACHE_TTL = 10 * 60 * 1000

  /** 活跃供应商 */
  const activeSuppliers = computed(() =>
    items.value.filter(item => item.isActive !== false)
  )

  /**
   * 加载供应商选项数据（带 TTL 缓存）
   * @param force 是否强制刷新
   */
  const loadOptions = async (force = false) => {
    const now = Date.now()
    if (!force && items.value.length > 0 && now - lastFetchTime.value < CACHE_TTL) {
      return
    }

    loading.value = true
    try {
      const { data } = await getSuppliers({ page: 1, pageSize: 1000 })
      items.value = data.data.items
      lastFetchTime.value = Date.now()
    } catch (error) {
      console.error('加载供应商数据失败:', error)
    } finally {
      loading.value = false
    }
  }

  /** 使缓存失效 */
  const invalidateCache = () => {
    lastFetchTime.value = 0
  }

  /** 按 ID 查找供应商 */
  const getItemById = (id: number) => {
    return items.value.find(item => item.id === id)
  }

  return {
    items,
    loading,
    lastFetchTime,
    activeSuppliers,
    loadOptions,
    invalidateCache,
    getItemById
  }
})
