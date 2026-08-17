import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

export const transactionRoutes: RouteRecordRaw[] = [
  {
    path: 'transactions',
    name: 'Transactions',
    component: () => import('@/features/transactions/pages/TransactionListPage.vue'),
    meta: { title: '交易记录', roles: PermissionGroups.ALL, icon: 'Tickets', group: '财务管理', order: 2 }
  },
  {
    path: 'unallocated-transactions',
    name: 'UnallocatedTransactions',
    component: () => import('@/features/transactions/pages/UnallocatedTransactionsPage.vue'),
    meta: { title: '待分配交易', roles: PermissionGroups.ALL, icon: 'Warning', group: '财务管理', order: 2.5 }
  }
]

