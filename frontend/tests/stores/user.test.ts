import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useUserStore } from '@/stores/user'

vi.mock('@/api/auth', () => ({
  getCurrentUser: vi.fn()
}))

describe('useUserStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('初始状态下 user 应为 null 且 session 未初始化', () => {
    const store = useUserStore()

    expect(store.user).toBeNull()
    expect(store.sessionInitialized).toBe(false)
  })

  it('setUser 应设置用户信息', () => {
    const store = useUserStore()
    const user = {
      id: 1,
      username: 'admin',
      email: 'admin@example.com',
      fullName: '管理员',
      role: 'Admin' as const,
      isActive: true
    }

    store.setUser(user)

    expect(store.user).toEqual(user)
  })

  it('markSessionInitialized 应标记会话已初始化', () => {
    const store = useUserStore()

    store.markSessionInitialized()

    expect(store.sessionInitialized).toBe(true)
  })

  it('logout 应清空用户并标记会话已初始化', () => {
    const store = useUserStore()

    store.setUser({
      id: 1,
      username: 'admin',
      email: 'admin@example.com',
      fullName: '管理员',
      role: 'Admin',
      isActive: true
    })

    store.logout()

    expect(store.user).toBeNull()
    expect(store.sessionInitialized).toBe(true)
    expect(store.isLoggedIn()).toBe(false)
  })

  it('resetSession 应清空用户并重置初始化状态', () => {
    const store = useUserStore()

    store.setUser({
      id: 1,
      username: 'admin',
      email: 'admin@example.com',
      fullName: '管理员',
      role: 'Admin',
      isActive: true
    })
    store.markSessionInitialized()

    store.resetSession()

    expect(store.user).toBeNull()
    expect(store.sessionInitialized).toBe(false)
  })

  it('isLoggedIn 在有用户信息时应返回 true', () => {
    const store = useUserStore()
    store.setUser({
      id: 1,
      username: 'admin',
      email: 'admin@example.com',
      fullName: '管理员',
      role: 'Admin',
      isActive: true
    })

    expect(store.isLoggedIn()).toBe(true)
  })

  it('bootstrapSession 应在成功时恢复用户信息', async () => {
    const { getCurrentUser } = await import('@/api/auth')
    vi.mocked(getCurrentUser).mockResolvedValue({
      data: {
        success: true,
        data: {
          id: 1,
          username: 'admin',
          email: 'admin@example.com',
          fullName: '管理员',
          role: 'Admin',
          isActive: true
        },
        message: 'ok'
      }
    } as any)

    const store = useUserStore()
    const user = await store.bootstrapSession()

    expect(user?.username).toBe('admin')
    expect(store.user?.username).toBe('admin')
    expect(store.sessionInitialized).toBe(true)
  })

  it('bootstrapSession 在失败时应返回 null 并完成初始化', async () => {
    const { getCurrentUser } = await import('@/api/auth')
    vi.mocked(getCurrentUser).mockRejectedValue(new Error('401'))

    const store = useUserStore()
    const user = await store.bootstrapSession()

    expect(user).toBeNull()
    expect(store.user).toBeNull()
    expect(store.sessionInitialized).toBe(true)
  })
})
