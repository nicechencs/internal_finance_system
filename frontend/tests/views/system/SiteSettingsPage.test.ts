import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import SiteSettingsPage from '@/features/system/pages/SiteSettingsPage.vue'
import * as siteBrandApi from '@/features/system/api/siteBrand'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import { useSiteBrandStore } from '@/features/system/stores/siteBrand'
import { DEFAULT_SITE_NAME } from '@/features/system/types/siteBrand'

vi.mock('@/features/system/api/siteBrand', () => ({
  getPublicBrand: vi.fn(),
  updateSiteBrand: vi.fn()
}))

describe('SiteSettingsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(siteBrandApi.getPublicBrand).mockResolvedValue(
      mockAxiosResponse({
        data: {
          siteName: DEFAULT_SITE_NAME,
          siteNameEn: 'Finance Management System'
        }
      })
    )
  })

  it('加载后展示当前站点名称', async () => {
    const wrapper = mountWithPlugins(SiteSettingsPage)
    await flushPromises()

    expect(wrapper.text()).toContain('站点设置')
    expect(wrapper.vm.form.siteName).toBe(DEFAULT_SITE_NAME)
  })

  it('保存成功后同步 store 与标题', async () => {
    vi.mocked(siteBrandApi.updateSiteBrand).mockResolvedValue(
      mockAxiosResponse({
        data: {
          siteName: '新站点',
          siteNameEn: 'New Brand'
        }
      })
    )

    const wrapper = mountWithPlugins(SiteSettingsPage)
    await flushPromises()

    wrapper.vm.form.siteName = '新站点'
    wrapper.vm.form.siteNameEn = 'New Brand'
    wrapper.vm.formRef = {
      validate: vi.fn().mockResolvedValue(true)
    } as any

    await wrapper.vm.handleSubmit()
    await flushPromises()

    expect(siteBrandApi.updateSiteBrand).toHaveBeenCalledWith({
      siteName: '新站点',
      siteNameEn: 'New Brand'
    })

    const store = useSiteBrandStore()
    expect(store.siteName).toBe('新站点')
    expect(document.title).toBe('新站点')
  })

  it('站点名称为空时阻止提交', async () => {
    const wrapper = mountWithPlugins(SiteSettingsPage)
    await flushPromises()

    wrapper.vm.form.siteName = '   '
    wrapper.vm.formRef = {
      validate: vi.fn().mockRejectedValue(new Error('invalid'))
    } as any

    await wrapper.vm.handleSubmit()
    await nextTick()

    expect(siteBrandApi.updateSiteBrand).not.toHaveBeenCalled()
  })

  it('包含 HTML 时校验失败', async () => {
    const wrapper = mountWithPlugins(SiteSettingsPage)
    await flushPromises()

    const rule = wrapper.vm.rules.siteName.find((item: any) => item.validator)
    let message = ''
    await new Promise<void>((resolve) => {
      rule.validator({}, '<script>', (error?: Error) => {
        message = error?.message || ''
        resolve()
      })
    })

    expect(message).toContain('HTML')
  })
})
