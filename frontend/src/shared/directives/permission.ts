import type { Directive, DirectiveBinding } from 'vue'
import { useUserStore } from '@/features/auth/stores/user'

/**
 * 权限指令
 * 用法: v-permission="['Admin', 'Accountant']"
 */
export const permission: Directive = {
  mounted(el: HTMLElement, binding: DirectiveBinding) {
    const { value } = binding
    const userStore = useUserStore()

    if (value && Array.isArray(value) && value.length > 0) {
      const requiredRoles = value
      const userRole = userStore.user?.role

      if (!userRole || !requiredRoles.includes(userRole)) {
        // 隐藏元素
        el.style.display = 'none'
        // 或者直接移除元素（可选）
        // el.parentNode?.removeChild(el)
      }
    }
  },
  updated(el: HTMLElement, binding: DirectiveBinding) {
    const { value } = binding
    const userStore = useUserStore()

    if (value && Array.isArray(value) && value.length > 0) {
      const requiredRoles = value
      const userRole = userStore.user?.role

      if (!userRole || !requiredRoles.includes(userRole)) {
        el.style.display = 'none'
      } else {
        el.style.display = ''
      }
    }
  }
}
