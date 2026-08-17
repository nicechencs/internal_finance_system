import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getTags } from '../api/tag'
import type { Tag } from '../types/tag'

/** 缓存有效期：5 分钟 */
const CACHE_TTL = 5 * 60 * 1000

export const useTagStore = defineStore('tag', () => {
  const tagsByScope = ref<Record<string, Tag[]>>({})
  const lastFetchTime = ref<Record<string, number>>({})
  const loading = ref<Record<string, boolean>>({})

  /**
   * 按 scope 加载标签列表（带 TTL 缓存）
   * @param scope 标签作用域
   * @param force 是否强制刷新
   */
  const loadTagsByScope = async (scope: string, force = false): Promise<Tag[]> => {
    const now = Date.now()
    if (
      !force &&
      tagsByScope.value[scope] &&
      (now - (lastFetchTime.value[scope] ?? 0)) < CACHE_TTL
    ) {
      return tagsByScope.value[scope]
    }

    loading.value[scope] = true
    try {
      const res = await getTags({ scope, isActive: true })
      tagsByScope.value[scope] = res.data.data ?? []
      lastFetchTime.value[scope] = Date.now()
    } catch (error) {
      console.error(`加载标签数据失败（scope: ${scope}）:`, error)
      tagsByScope.value[scope] = tagsByScope.value[scope] ?? []
    } finally {
      loading.value[scope] = false
    }

    return tagsByScope.value[scope]
  }

  /** 使指定 scope 的缓存失效 */
  const invalidateScope = (scope: string) => {
    delete tagsByScope.value[scope]
    delete lastFetchTime.value[scope]
  }

  /** 使全部 scope 缓存失效 */
  const invalidateAll = () => {
    tagsByScope.value = {}
    lastFetchTime.value = {}
  }

  /** 按 ID 查找标签（在已缓存的 scope 中搜索） */
  const getTagById = (id: number): Tag | undefined => {
    for (const tags of Object.values(tagsByScope.value)) {
      const found = tags.find(t => t.id === id)
      if (found) return found
    }
    return undefined
  }

  return {
    tagsByScope,
    lastFetchTime,
    loading,
    loadTagsByScope,
    invalidateScope,
    invalidateAll,
    getTagById
  }
})
