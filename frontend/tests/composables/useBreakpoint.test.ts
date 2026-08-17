import { describe, expect, it, vi } from 'vitest'
import { computed, ref } from 'vue'
import { DESKTOP_MEDIA, MOBILE_MAX, MOBILE_MEDIA, TABLET_MAX, TABLET_MEDIA } from '@/shared/constants/breakpoints'

vi.mock('@vueuse/core', () => ({
  useMediaQuery: vi.fn((query: string) => ref(query === '(max-width: 768px)'))
}))

describe('breakpoints', () => {
  it('应导出与 CSS 一致的断点常量', () => {
    expect(MOBILE_MAX).toBe(768)
    expect(TABLET_MAX).toBe(1024)
    expect(MOBILE_MEDIA).toBe('(max-width: 768px)')
    expect(TABLET_MEDIA).toBe('(min-width: 769px) and (max-width: 1024px)')
    expect(DESKTOP_MEDIA).toBe('(min-width: 1025px)')
  })
})

describe('useBreakpoint', () => {
  it('应按 matchMedia 结果返回 isMobile', async () => {
    const { useBreakpoint } = await import('@/shared/composables/useBreakpoint')
    const { isMobile, isTablet, isDesktop } = useBreakpoint()

    expect(isMobile.value).toBe(true)
    expect(isTablet.value).toBe(false)
    expect(isDesktop.value).toBe(false)
    expect(computed(() => isDesktop.value).value).toBe(false)
  })
})
