import { computed } from 'vue'
import { useMediaQuery } from '@vueuse/core'
import {
  DESKTOP_MEDIA,
  MOBILE_MEDIA,
  TABLET_MEDIA
} from '@/shared/constants/breakpoints'

/**
 * 全站唯一断点事实源。组件禁止自行监听 resize / innerWidth。
 */
export function useBreakpoint() {
  const isMobile = useMediaQuery(MOBILE_MEDIA)
  const isTablet = useMediaQuery(TABLET_MEDIA)
  const isDesktopQuery = useMediaQuery(DESKTOP_MEDIA)
  const isDesktop = computed(() => isDesktopQuery.value || (!isMobile.value && !isTablet.value))

  return { isMobile, isTablet, isDesktop }
}
