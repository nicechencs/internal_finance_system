import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const dashboardRoutes: RouteRecordRaw[] = [
  {
    path: '',
    name: 'Dashboard',
    component: () => import('@/features/dashboard/pages/DashboardPage.vue'),
    meta: { title: '仪表盘', roles: PermissionGroups.ALL, icon: 'HomeFilled', group: '工作台', order: 1 }
  }
]
