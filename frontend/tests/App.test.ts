import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestPinia, createTestRouter } from '@tests/utils'
import App from '@/App.vue'

describe('App.vue', () => {
  const createWrapper = (routes?: any[]) => {
    const pinia = createTestPinia()
    const router = createTestRouter(routes)
    return mount(App, {
      global: {
        plugins: [pinia, router],
      },
    })
  }

  it('应该正确渲染组件', () => {
    const wrapper = createWrapper()
    expect(wrapper.exists()).toBe(true)
  })

  it('应该包含 RouterView 组件', () => {
    const wrapper = createWrapper()
    expect(wrapper.findComponent({ name: 'RouterView' }).exists()).toBe(true)
  })

  it('应该渲染默认路由的内容', async () => {
    const router = createTestRouter([
      { path: '/', component: { template: '<div class="home-page">首页</div>' } },
    ])
    await router.push('/')
    await router.isReady()

    const pinia = createTestPinia()
    const wrapper = mount(App, {
      global: { plugins: [pinia, router] },
    })

    expect(wrapper.find('.home-page').exists()).toBe(true)
    expect(wrapper.text()).toContain('首页')
  })

  it('应该在路由切换时渲染不同内容', async () => {
    const router = createTestRouter([
      { path: '/', component: { template: '<div class="page-home">首页</div>' } },
      { path: '/about', component: { template: '<div class="page-about">关于</div>' } },
    ])
    const pinia = createTestPinia()

    await router.push('/')
    await router.isReady()

    const wrapper = mount(App, {
      global: { plugins: [pinia, router] },
    })

    expect(wrapper.find('.page-home').exists()).toBe(true)

    await router.push('/about')
    await router.isReady()

    expect(wrapper.find('.page-about').exists()).toBe(true)
    expect(wrapper.text()).toContain('关于')
  })

  it('应该不包含业务逻辑组件，仅作为路由容器', () => {
    const wrapper = createWrapper()
    // App.vue 只是一个 RouterView 容器，不应包含侧边栏、头部等业务组件
    expect(wrapper.findComponent({ name: 'ElMenu' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'ElHeader' }).exists()).toBe(false)
  })
})
