import { ref } from 'vue'

/**
 * 防抖 composable
 * @param fn 要防抖的函数
 * @param delay 延迟时间（毫秒），默认 300ms
 * @returns 防抖后的函数
 */
export function useDebounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number = 300
): (...args: Parameters<T>) => void {
  const timeoutId = ref<ReturnType<typeof setTimeout> | null>(null)

  return (...args: Parameters<T>) => {
    if (timeoutId.value !== null) {
      clearTimeout(timeoutId.value)
    }

    timeoutId.value = setTimeout(() => {
      fn(...args)
      timeoutId.value = null
    }, delay)
  }
}
