import { ref, type Ref } from 'vue'

interface CacheItem<T> {
  data: T
  timestamp: number
}

/**
 * 简单的内存缓存 composable
 * @param ttl 缓存过期时间（毫秒），默认 5 分钟
 */
export function useCache<T = any>(ttl: number = 5 * 60 * 1000) {
  const cache = new Map<string, CacheItem<T>>()

  /**
   * 获取缓存数据
   * @param key 缓存键
   * @returns 缓存的数据，如果不存在或已过期则返回 null
   */
  const get = (key: string): T | null => {
    const item = cache.get(key)
    if (!item) return null

    const now = Date.now()
    if (now - item.timestamp > ttl) {
      // 缓存已过期，删除
      cache.delete(key)
      return null
    }

    return item.data
  }

  /**
   * 设置缓存数据
   * @param key 缓存键
   * @param data 要缓存的数据
   */
  const set = (key: string, data: T): void => {
    cache.set(key, {
      data,
      timestamp: Date.now()
    })
  }

  /**
   * 删除缓存数据
   * @param key 缓存键
   */
  const remove = (key: string): void => {
    cache.delete(key)
  }

  /**
   * 清空所有缓存
   */
  const clear = (): void => {
    cache.clear()
  }

  /**
   * 检查缓存是否存在且未过期
   * @param key 缓存键
   */
  const has = (key: string): boolean => {
    return get(key) !== null
  }

  return {
    get,
    set,
    remove,
    clear,
    has
  }
}

/**
 * 全局共享的选项数据缓存
 * 用于缓存项目、客户、供应商、账户、分类等下拉选项数据
 */
export const optionsCache = useCache<any[]>(10 * 60 * 1000) // 10 分钟过期
