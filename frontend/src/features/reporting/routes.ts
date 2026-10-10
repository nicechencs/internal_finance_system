import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const reportingRoutes: RouteRecordRaw[] = [
  {
    path: 'reports',
    name: 'Reports',
    component: () => import('@/features/reporting/pages/ReportsPage.vue'),
    meta: {
      title: '经营报表',
      roles: PermissionGroups.ALL,
      icon: 'DataAnalysis',
      group: '财务管理',
      order: 4.5
    }
  }
]
