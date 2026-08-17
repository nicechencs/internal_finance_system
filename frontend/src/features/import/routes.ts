import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const importRoutes: RouteRecordRaw[] = [
  {
    path: 'import',
    name: 'Import',
    component: () => import('@/features/import/pages/ImportPage.vue'),
    meta: { title: '流水导入', roles: PermissionGroups.EDIT, icon: 'Upload', group: '财务管理', order: 4 }
  }
]
