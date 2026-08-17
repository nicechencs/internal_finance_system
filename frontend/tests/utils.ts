import { mount, VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory, Router } from 'vue-router'
import ElementPlus from 'element-plus'
import type { Component } from 'vue'

/**
 * 创建测试用的 Pinia 实例
 */
export function createTestPinia() {
  const pinia = createPinia()
  setActivePinia(pinia)
  return pinia
}

/**
 * 创建测试用的 Router 实例
 */
export function createTestRouter(routes: any[] = []): Router {
  return createRouter({
    history: createMemoryHistory(),
    routes: routes.length > 0 ? routes : [
      { path: '/', name: 'home', component: { template: '<div>Home</div>' } },
      { path: '/login', name: 'login', component: { template: '<div>Login</div>' } },
      { path: '/transactions', name: 'Transactions', component: { template: '<div>Transactions</div>' } },
      { path: '/accounts', name: 'AccountList', component: { template: '<div>Account List</div>' } },
      { path: '/projects', name: 'ProjectList', component: { template: '<div>Project List</div>' } },
      { path: '/accounts/:id?', name: 'AccountDetail', component: { template: '<div>Account Detail</div>' } },
      { path: '/fixed-deposits', name: 'FixedDeposits', component: { template: '<div>Fixed Deposits</div>' } },
      { path: '/projects/:id?', name: 'ProjectDetail', component: { template: '<div>Project Detail</div>' } },
      { path: '/customers', name: 'CustomerList', component: { template: '<div>Customer List</div>' } },
      { path: '/customers/:id?', name: 'CustomerDetail', component: { template: '<div>Customer Detail</div>' } },
      { path: '/suppliers', name: 'SupplierList', component: { template: '<div>Supplier List</div>' } },
      { path: '/suppliers/:id?', name: 'SupplierDetail', component: { template: '<div>Supplier Detail</div>' } },
      { path: '/persons', name: 'PersonList', component: { template: '<div>Person List</div>' } },
      { path: '/persons/:id?', name: 'PersonDetail', component: { template: '<div>Person Detail</div>' } },
      { path: '/receivables', name: 'ReceivableList', component: { template: '<div>Receivable List</div>' } },
      { path: '/receivables/:id?', name: 'ReceivableDetail', component: { template: '<div>Receivable Detail</div>' } },
      { path: '/payables', name: 'PayableList', component: { template: '<div>Payable List</div>' } },
      { path: '/payables/:id?', name: 'PayableDetail', component: { template: '<div>Payable Detail</div>' } },
      { path: '/account-profile', name: 'AccountProfile', component: { template: '<div>Account Profile</div>' } },
      { path: '/account-security', name: 'AccountSecurity', component: { template: '<div>Account Security</div>' } },
    ],
  })
}

/**
 * 挂载组件的辅助函数，自动配置 Pinia 和 Router
 */
export function mountWithPlugins(
  component: Component,
  options: any = {}
): VueWrapper {
  const pinia = createTestPinia()
  const {
    global: globalOptions = {},
    routes,
    stubs,
    mocks,
    ...mountOptions
  } = options
  const router = createTestRouter(routes)

  return mount(component, {
    ...mountOptions,
    global: {
      ...globalOptions,
      plugins: [pinia, router, ElementPlus, ...(globalOptions.plugins || [])],
      stubs: {
        ...(stubs || {}),
        ...(globalOptions.stubs || {}),
      },
      mocks: {
        ...(mocks || {}),
        ...(globalOptions.mocks || {}),
      },
    },
  })
}

/**
 * 等待异步操作完成
 */
export async function flushPromises(): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, 0)
  })
}

/**
 * Mock Axios 响应
 */
export function mockAxiosResponse(data: any, status = 200) {
  return {
    data,
    status,
    statusText: 'OK',
    headers: {},
    config: {} as any,
  }
}

/**
 * Mock Axios 错误
 */
export function mockAxiosError(message: string, status = 500) {
  return {
    response: {
      data: { message },
      status,
      statusText: 'Error',
      headers: {},
      config: {} as any,
    },
    message,
    isAxiosError: true,
  }
}
