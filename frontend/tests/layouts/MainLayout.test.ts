import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import * as authApi from '@/api/auth'
import MainLayout from '@/layouts/MainLayout.vue'
import { flushPromises, mountWithPlugins } from '@tests/utils'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/auth', () => ({
  logout: vi.fn().mockResolvedValue({ data: { success: true } })
}))

vi.mock('@element-plus/icons-vue', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@element-plus/icons-vue')>()
  return {
    ...actual,
    Coin: { name: 'Coin', template: '<i class="icon-coin"></i>' },
    UserFilled: { name: 'UserFilled', template: '<i class="icon-userfilled"></i>' },
    ArrowDown: { name: 'ArrowDown', template: '<i class="icon-arrowdown"></i>' },
    Setting: { name: 'Setting', template: '<i class="icon-setting"></i>' },
    SwitchButton: { name: 'SwitchButton', template: '<i class="icon-switch"></i>' },
    User: { name: 'User', template: '<i class="icon-user"></i>' },
    Expand: { name: 'Expand', template: '<i class="icon-expand"></i>' },
    HomeFilled: { name: 'HomeFilled', template: '<i class="icon-homefilled"></i>' },
    Tickets: { name: 'Tickets', template: '<i class="icon-tickets"></i>' }
  }
})

const menuRoutes = [
  {
    path: '/',
    component: { template: '<div><router-view /></div>' },
    children: [
      { path: '', meta: { title: '仪表盘', group: '工作台', icon: 'HomeFilled', order: 1, roles: ['Admin', 'Accountant', 'Viewer'] }, component: { template: '<div />' } },
      { path: 'transactions', meta: { title: '交易记录', group: '财务管理', icon: 'Tickets', order: 2, roles: ['Admin', 'Accountant', 'Viewer'] }, component: { template: '<div />' } },
      { path: 'settings/users', meta: { title: '用户管理', group: '系统设置', icon: 'UserFilled', order: 12, roles: ['Admin'] }, component: { template: '<div />' } }
    ]
  },
  { path: '/login', component: { template: '<div>Login</div>' } },
  { path: '/account-security', component: { template: '<div>Security</div>' } }
]

describe('MainLayout.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('应该正确渲染布局结构', () => {
    const wrapper = mountWithPlugins(MainLayout)
    const html = wrapper.html()

    expect(html).toContain('main-layout')
    expect(html).toContain('sidebar')
    expect(html).toContain('header')
    expect(html).toContain('footer')
  })

  it('应该显示系统 Logo 和名称', () => {
    const wrapper = mountWithPlugins(MainLayout)
    expect(wrapper.text()).toContain('财务系统')
  })

  it('应该渲染有权限的菜单项', async () => {
    const wrapper = mountWithPlugins(MainLayout, { routes: menuRoutes })
    const userStore = useUserStore()

    userStore.setUser({
      id: 1,
      username: 'admin',
      email: 'admin@example.com',
      fullName: '管理员',
      role: 'Admin',
      isActive: true
    })

    await nextTick()
    const text = wrapper.text()

    expect(text).toContain('仪表盘')
    expect(text).toContain('交易记录')
    expect(text).toContain('用户管理')
  })

  it('应该显示用户全名', async () => {
    const wrapper = mountWithPlugins(MainLayout)
    const userStore = useUserStore()

    userStore.setUser({
      id: 1,
      username: 'testuser',
      email: 'test@example.com',
      fullName: '测试用户',
      role: 'Admin',
      isActive: true
    })

    await nextTick()
    expect(wrapper.text()).toContain('测试用户')
  })

  it('应该在用户未设置时显示默认名称', () => {
    const wrapper = mountWithPlugins(MainLayout)
    const userStore = useUserStore()
    userStore.setUser(null)

    expect(wrapper.text()).toContain('用户')
  })

  it('应该正确处理退出登录命令', async () => {
    const wrapper = mountWithPlugins(MainLayout, { routes: menuRoutes })
    const userStore = useUserStore()
    const routerPushSpy = vi.spyOn(wrapper.vm.$router, 'push')
    const logoutSpy = vi.spyOn(userStore, 'logout')

    userStore.setUser({
      id: 1,
      username: 'testuser',
      email: 'test@example.com',
      fullName: '测试用户',
      role: 'Admin',
      isActive: true
    })

    await wrapper.vm.handleCommand('logout')
    await flushPromises()

    expect(authApi.logout).toHaveBeenCalled()
    expect(logoutSpy).toHaveBeenCalled()
    expect(routerPushSpy).toHaveBeenCalledWith('/login')
  })

  it('应该在点击账号安全时跳转到账号安全页', async () => {
    const wrapper = mountWithPlugins(MainLayout, { routes: menuRoutes })
    const routerPushSpy = vi.spyOn(wrapper.vm.$router, 'push')

    await wrapper.vm.handleCommand('security')

    expect(routerPushSpy).toHaveBeenCalledWith('/account-security')
  })
})
