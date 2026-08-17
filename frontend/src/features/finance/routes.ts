import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const financeRoutes: RouteRecordRaw[] = [
  {
    path: 'finance',
    name: 'FinanceManagement',
    component: () => import('@/features/finance/pages/FinanceManagementPage.vue'),
    meta: { title: '应收应付', roles: PermissionGroups.ALL, icon: 'Money', group: '财务管理', order: 3 }
  },
  {
    path: 'receivables',
    name: 'ReceivableList',
    component: () => import('@/features/finance/pages/ReceivableListPage.vue'),
    meta: { title: '应收账款列表', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
  },
  {
    path: 'receivables/:id',
    name: 'ReceivableDetail',
    component: () => import('@/features/finance/pages/ReceivableDetailPage.vue'),
    meta: { title: '应收账款详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
  },
  {
    path: 'payables',
    name: 'PayableList',
    component: () => import('@/features/finance/pages/PayableListPage.vue'),
    meta: { title: '应付账款列表', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
  },
  {
    path: 'payables/:id',
    name: 'PayableDetail',
    component: () => import('@/features/finance/pages/PayableDetailPage.vue'),
    meta: { title: '应付账款详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
  },
  {
    path: 'payable-types',
    name: 'PayableTypeManagement',
    component: () => import('@/features/finance/pages/PayableTypeManagementPage.vue'),
    meta: { title: '应付款业务类型管理', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
  }
]
