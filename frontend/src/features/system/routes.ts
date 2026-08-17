import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const systemRoutes: RouteRecordRaw[] = [
  {
    path: 'settings/users',
    name: 'UserManagement',
    component: () => import('@/features/system/pages/UserManagementPage.vue'),
    meta: { title: '用户管理', roles: PermissionGroups.ADMIN_ONLY, icon: 'UserFilled', group: '系统设置', order: 12 }
  },
  {
    path: 'audit-logs',
    name: 'AuditLogs',
    component: () => import('@/features/system/pages/AuditLogListPage.vue'),
    meta: { title: '审计日志', roles: PermissionGroups.EDIT, icon: 'Document', group: '系统设置', order: 13 }
  }
]
