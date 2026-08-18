import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ElMessage } from 'element-plus'
import * as authApi from '@/api/auth'
import Login from '@/views/Login.vue'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import { useSiteBrandStore } from '@/features/system/stores/siteBrand'

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  logout: vi.fn(),
  getCurrentUser: vi.fn(),
  changePassword: vi.fn()
}))

vi.mock('@element-plus/icons-vue', () => ({
  DataAnalysis: { name: 'DataAnalysis', template: '<i class="icon-data-analysis"></i>' }
}))

describe('Login.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该正确渲染登录页面结构', () => {
    const wrapper = mountWithPlugins(Login)
    const text = wrapper.text()

    expect(text).toContain('财务管理系统')
    expect(text).toContain('欢迎登录')
    expect(text).toContain('请输入您的账号信息')
  })

  it('应该渲染品牌展示区信息', () => {
    const wrapper = mountWithPlugins(Login)
    const text = wrapper.text()

    expect(text).toContain('Finance Management System')
    expect(text).toContain('高效管理企业资金流、项目收支与日常财务协作')
  })

  it('应该渲染更新后的站点名称', async () => {
    const wrapper = mountWithPlugins(Login)
    const brandStore = useSiteBrandStore()
    brandStore.apply({ siteName: '演示财务平台', siteNameEn: 'Demo Finance' })
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('演示财务平台')
    expect(wrapper.text()).toContain('Demo Finance')
    expect(document.title).toBe('演示财务平台')
  })

  it('应该在表单验证通过后调用登录 API 并跳转首页', async () => {
    vi.mocked(authApi.login).mockResolvedValue(
      mockAxiosResponse({
        data: {
          user: {
            id: 1,
            username: 'admin',
            email: 'admin@example.com',
            fullName: '管理员',
            role: 'Admin',
            isActive: true
          },
          mustChangePassword: false
        }
      })
    )

    const wrapper = mountWithPlugins(Login)
    const routerPushSpy = vi.spyOn(wrapper.vm.$router, 'push')

    wrapper.vm.loginForm.username = 'admin'
    wrapper.vm.loginForm.password = 'password123'
    wrapper.vm.formRef = {
      validate: vi.fn().mockResolvedValue(true)
    } as any

    await wrapper.vm.handleLogin()
    await flushPromises()

    expect(authApi.login).toHaveBeenCalledWith({
      username: 'admin',
      password: 'password123'
    })
    expect(ElMessage.success).toHaveBeenCalledWith('登录成功')
    expect(routerPushSpy).toHaveBeenCalledWith('/')
  })

  it('应该在首次登录必须改密时跳转账号安全页', async () => {
    vi.mocked(authApi.login).mockResolvedValue(
      mockAxiosResponse({
        data: {
          user: {
            id: 1,
            username: 'admin',
            email: 'admin@example.com',
            fullName: '管理员',
            role: 'Admin',
            isActive: true
          },
          mustChangePassword: true
        }
      })
    )

    const wrapper = mountWithPlugins(Login)
    const routerPushSpy = vi.spyOn(wrapper.vm.$router, 'push')

    wrapper.vm.loginForm.username = 'admin'
    wrapper.vm.loginForm.password = 'password123'
    wrapper.vm.formRef = {
      validate: vi.fn().mockResolvedValue(true)
    } as any

    await wrapper.vm.handleLogin()
    await flushPromises()

    expect(ElMessage.success).toHaveBeenCalledWith('登录成功，请先修改密码')
    expect(routerPushSpy).toHaveBeenCalledWith('/account-security')
  })

  it('应该在登录失败时捕获错误', async () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.mocked(authApi.login).mockRejectedValue(new Error('用户名或密码错误'))

    const wrapper = mountWithPlugins(Login)
    wrapper.vm.loginForm.username = 'admin'
    wrapper.vm.loginForm.password = 'wrongpassword'
    wrapper.vm.formRef = {
      validate: vi.fn().mockResolvedValue(true)
    } as any

    await wrapper.vm.handleLogin()
    await flushPromises()

    expect(authApi.login).toHaveBeenCalled()
    expect(consoleErrorSpy).toHaveBeenCalled()

    consoleErrorSpy.mockRestore()
  })
})
