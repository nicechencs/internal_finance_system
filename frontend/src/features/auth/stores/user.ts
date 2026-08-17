import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getCurrentUser } from '@/features/auth/api/auth'
import { optionsCache } from '@/shared/composables/useCache'
import type { User } from '@/shared/types/common'

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)
  const sessionInitialized = ref(false)

  const setUser = (newUser: User | null) => {
    user.value = newUser
  }

  const markSessionInitialized = () => {
    sessionInitialized.value = true
  }

  const fetchCurrentUser = async () => {
    const res = await getCurrentUser()
    if (res.data?.data) {
      setUser(res.data.data)
      markSessionInitialized()
      return res.data.data
    }

    throw new Error('获取当前用户失败')
  }

  const bootstrapSession = async () => {
    if (sessionInitialized.value) {
      return user.value
    }

    try {
      return await fetchCurrentUser()
    } catch {
      setUser(null)
      markSessionInitialized()
      return null
    }
  }

  const logout = () => {
    user.value = null
    sessionInitialized.value = true
    optionsCache.clear()
  }

  const resetSession = () => {
    user.value = null
    sessionInitialized.value = false
    optionsCache.clear()
  }

  const isLoggedIn = () => {
    return !!user.value
  }

  const hasRole = (role: string) => {
    return user.value?.role === role
  }

  const isAdmin = computed(() => user.value?.role === 'Admin')
  const isAccountant = computed(() => user.value?.role === 'Accountant')
  const isViewer = computed(() => user.value?.role === 'Viewer')
  const canEdit = computed(() => isAdmin.value || isAccountant.value)
  const canDelete = computed(() => isAdmin.value)

  return {
    user,
    sessionInitialized,
    setUser,
    fetchCurrentUser,
    bootstrapSession,
    markSessionInitialized,
    logout,
    resetSession,
    isLoggedIn,
    hasRole,
    isAdmin,
    isAccountant,
    isViewer,
    canEdit,
    canDelete
  }
})
