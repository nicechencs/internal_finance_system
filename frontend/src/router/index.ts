import { createRouter, createWebHistory } from 'vue-router'
import { ElMessage } from 'element-plus'
import MainLayout from '@/shared/layouts/MainLayout.vue'
import { useUserStore } from '@/features/auth/stores/user'
import { authRoutes, authProtectedRoutes } from '@/features/auth/routes'
import { dashboardRoutes } from '@/features/dashboard/routes'
import { masterDataRoutes } from '@/features/master-data/routes'
import { transactionRoutes } from '@/features/transactions/routes'
import { financeRoutes } from '@/features/finance/routes'
import { importRoutes } from '@/features/import/routes'
import { reconciliationRoutes } from '@/features/reconciliation/routes'
import { systemRoutes } from '@/features/system/routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    ...authRoutes,
    {
      path: '/',
      component: MainLayout,
      meta: { requiresAuth: true },
      children: [
        ...dashboardRoutes,
        ...transactionRoutes,
        ...financeRoutes,
        ...importRoutes,
        ...masterDataRoutes,
        ...reconciliationRoutes,
        ...systemRoutes,
        ...authProtectedRoutes
      ]
    }
  ]
})

router.beforeEach(async (to, from) => {
  const userStore = useUserStore()

  if (import.meta.env.DEV) {
    console.log('Route navigation:', {
      from: from.path,
      to: to.path,
      isLoggedIn: userStore.isLoggedIn(),
      userRole: userStore.user?.role,
      requiresAuth: to.meta.requiresAuth,
      allowedRoles: to.meta.roles
    })
  }

  if (!userStore.sessionInitialized) {
    await userStore.bootstrapSession()
  }

  if (to.meta.requiresAuth && !userStore.isLoggedIn()) {
    return '/login'
  }

  if (to.path === '/login' && userStore.isLoggedIn()) {
    return '/'
  }

  if (to.meta.roles && Array.isArray(to.meta.roles)) {
    const userRole = userStore.user?.role
    if (!userRole || !to.meta.roles.includes(userRole)) {
      ElMessage.error('无权访问此页面')
      return '/'
    }
  }
})

export default router
