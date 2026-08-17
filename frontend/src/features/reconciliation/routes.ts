import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const reconciliationRoutes: RouteRecordRaw[] = [
  {
    path: 'rules',
    name: 'Rules',
    component: () => import('@/features/reconciliation/pages/RuleListPage.vue'),
    meta: { title: '分类规则', roles: PermissionGroups.ADMIN_ONLY, icon: 'Setting', group: '自动化', order: 11 }
  },
  {
    path: 'tag-rules',
    name: 'TagRules',
    component: () => import('@/features/reconciliation/pages/TagRuleListPage.vue'),
    meta: { title: '标签规则', roles: PermissionGroups.ALL, icon: 'PriceTag', group: '自动化', order: 12 }
  }
]
