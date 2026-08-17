import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/features/auth/stores/user'
import { PermissionGroups } from '@/shared/constants/permissions'

export interface MenuItem {
  path: string
  title: string
  icon: string
}

export interface MenuGroup {
  name: string
  items: MenuItem[]
}

export function useAuth() {
  const userStore = useUserStore()
  const router = useRouter()

  /** 检查当前用户是否拥有指定角色之一 */
  const hasPermission = (roles?: string[]): boolean => {
    if (!roles || roles.length === 0) return true
    const userRole = userStore.user?.role
    return !!userRole && roles.includes(userRole)
  }

  const isAdmin = computed(() => userStore.user?.role === 'Admin')
  const canEdit = computed(() => hasPermission(PermissionGroups.EDIT))
  const canDelete = computed(() => hasPermission(PermissionGroups.ADMIN_ONLY))

  /** 从路由配置动态生成菜单（需路由 meta 包含 group/icon/order） */
  const menuGroups = computed<MenuGroup[]>(() => {
    const routes = router.getRoutes()
    const groupMap: Record<string, MenuItem[]> = {}

    routes
      .filter(r => r.meta?.group && !r.meta?.hidden && hasPermission(r.meta?.roles as string[]))
      .sort((a, b) => ((a.meta?.order as number) ?? 99) - ((b.meta?.order as number) ?? 99))
      .forEach(route => {
        const group = route.meta!.group as string
        if (!groupMap[group]) groupMap[group] = []
        groupMap[group].push({
          path: route.path.startsWith('/') ? route.path : `/${route.path}`,
          title: route.meta!.title as string,
          icon: route.meta!.icon as string
        })
      })

    return Object.entries(groupMap).map(([name, items]) => ({ name, items }))
  })

  return { hasPermission, isAdmin, canEdit, canDelete, menuGroups }
}
