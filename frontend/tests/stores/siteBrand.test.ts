import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useSiteBrandStore } from '@/features/system/stores/siteBrand'
import * as siteBrandApi from '@/features/system/api/siteBrand'
import { DEFAULT_SITE_NAME, DEFAULT_SITE_NAME_EN } from '@/features/system/types/siteBrand'
import { mockAxiosResponse } from '@tests/utils'

vi.mock('@/features/system/api/siteBrand', () => ({
  getPublicBrand: vi.fn(),
  updateSiteBrand: vi.fn()
}))

describe('siteBrand store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    document.title = DEFAULT_SITE_NAME
  })

  it('默认使用当前站点名称', () => {
    const store = useSiteBrandStore()
    expect(store.siteName).toBe(DEFAULT_SITE_NAME)
    expect(store.siteNameEn).toBe(DEFAULT_SITE_NAME_EN)
  })

  it('加载公开品牌后更新名称和 document.title', async () => {
    vi.mocked(siteBrandApi.getPublicBrand).mockResolvedValue(
      mockAxiosResponse({
        data: {
          siteName: '自定义站点',
          siteNameEn: 'Custom Brand'
        }
      })
    )

    const store = useSiteBrandStore()
    await store.load()

    expect(store.siteName).toBe('自定义站点')
    expect(store.siteNameEn).toBe('Custom Brand')
    expect(document.title).toBe('自定义站点')
  })

  it('接口失败时回退到默认名称', async () => {
    vi.mocked(siteBrandApi.getPublicBrand).mockRejectedValue(new Error('network'))

    const store = useSiteBrandStore()
    store.apply({ siteName: '临时名称', siteNameEn: 'Temp' })
    await store.load()

    expect(store.siteName).toBe(DEFAULT_SITE_NAME)
    expect(store.siteNameEn).toBe(DEFAULT_SITE_NAME_EN)
    expect(document.title).toBe(DEFAULT_SITE_NAME)
  })

  it('空站点名称回退到默认值', () => {
    const store = useSiteBrandStore()
    store.apply({ siteName: '   ', siteNameEn: '' })

    expect(store.siteName).toBe(DEFAULT_SITE_NAME)
    expect(store.siteNameEn).toBe('')
    expect(document.title).toBe(DEFAULT_SITE_NAME)
  })
})
