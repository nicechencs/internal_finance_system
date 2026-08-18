import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getPublicBrand } from '@/features/system/api/siteBrand'
import {
  DEFAULT_SITE_NAME,
  DEFAULT_SITE_NAME_EN,
  type SiteBrand
} from '@/features/system/types/siteBrand'

function normalizeBrand(brand?: Partial<SiteBrand> | null): SiteBrand {
  const siteName = brand?.siteName?.trim() || DEFAULT_SITE_NAME
  const siteNameEn = brand?.siteNameEn == null ? DEFAULT_SITE_NAME_EN : brand.siteNameEn.trim()
  return { siteName, siteNameEn }
}

function applyDocumentTitle(siteName: string) {
  if (typeof document !== 'undefined') {
    document.title = siteName
  }
}

export const useSiteBrandStore = defineStore('siteBrand', () => {
  const siteName = ref(DEFAULT_SITE_NAME)
  const siteNameEn = ref(DEFAULT_SITE_NAME_EN)
  const loaded = ref(false)

  const apply = (brand?: Partial<SiteBrand> | null) => {
    const normalized = normalizeBrand(brand)
    siteName.value = normalized.siteName
    siteNameEn.value = normalized.siteNameEn
    applyDocumentTitle(normalized.siteName)
    return normalized
  }

  const load = async () => {
    try {
      const res = await getPublicBrand()
      apply(res.data?.data)
    } catch {
      apply({
        siteName: DEFAULT_SITE_NAME,
        siteNameEn: DEFAULT_SITE_NAME_EN
      })
    } finally {
      loaded.value = true
    }
  }

  return {
    siteName,
    siteNameEn,
    loaded,
    apply,
    load
  }
})
