import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Category } from '@/features/master-data/categories/types/category'
import { getCategories } from '@/features/master-data/categories/api/category'

export const useCategoryStore = defineStore('category', () => {
  const items = ref<Category[]>([])
  const loading = ref(false)
  const lastFetchTime = ref(0)

  /** 缓存有效期：10 分钟 */
  const CACHE_TTL = 10 * 60 * 1000

  /** 收入分类 */
  const incomeCategories = computed(() =>
    items.value.filter(item => item.categoryType === 'Income')
  )

  /** 支出分类 */
  const expenseCategories = computed(() =>
    items.value.filter(item => item.categoryType === 'Expense')
  )

  /**
   * 加载分类选项数据（带 TTL 缓存）
   * @param force 是否强制刷新
   */
  const loadOptions = async (force = false) => {
    const now = Date.now()
    if (!force && items.value.length > 0 && now - lastFetchTime.value < CACHE_TTL) {
      return
    }

    loading.value = true
    try {
      const { data } = await getCategories({ page: 1, pageSize: 1000 })
      items.value = data.data.items
      lastFetchTime.value = Date.now()
    } catch (error) {
      console.error('加载分类数据失败:', error)
    } finally {
      loading.value = false
    }
  }

  /** 使缓存失效 */
  const invalidateCache = () => {
    lastFetchTime.value = 0
  }

  /** 按 ID 查找分类 */
  const getItemById = (id: number) => {
    return items.value.find(item => item.id === id)
  }

  return {
    items,
    loading,
    lastFetchTime,
    incomeCategories,
    expenseCategories,
    loadOptions,
    invalidateCache,
    getItemById
  }
})
