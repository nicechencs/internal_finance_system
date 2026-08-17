import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const authRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/features/auth/pages/LoginPage.vue'),
    meta: { requiresAuth: false, title: '登录' }
  }
]

/** 需要登录后才能访问的账号相关路由（作为 MainLayout children 挂载） */
export const authProtectedRoutes: RouteRecordRaw[] = [
  {
    path: 'account-security',
    name: 'AccountSecurity',
    component: () => import('@/features/auth/pages/AccountSecurityPage.vue'),
    meta: { title: '账号安全', roles: PermissionGroups.ALL, hidden: true }
  },
  {
    path: 'account-profile',
    name: 'AccountProfile',
    component: () => import('@/features/auth/pages/AccountProfilePage.vue'),
    meta: { title: '个人资料', roles: PermissionGroups.ALL, hidden: true }
  }
]
