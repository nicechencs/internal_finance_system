import { beforeEach, describe, expect, it, vi } from 'vitest'
import AuditLogListPage from '@/features/system/pages/AuditLogListPage.vue'
import * as auditLogApi from '@/features/system/api/auditLog'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'

vi.mock('@/features/system/api/auditLog')

describe('AuditLogListPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(auditLogApi.getAuditLogs).mockResolvedValue(
      mockAxiosResponse({
        code: 200,
        message: 'success',
        data: {
          items: [
            {
              id: 1,
              entityType: 'Account',
              entityId: 8,
              action: 'Update',
              oldValue: '{"bankName":"工商银行"}',
              newValue: '{"bankName":"招商银行"}',
              userId: 1,
              operatorName: 'admin',
              ipAddress: '127.0.0.1',
              createdAt: '2026-03-01T08:00:00Z'
            }
          ],
          total: 1,
          page: 1,
          pageSize: 20
        }
      })
    )
  })

  it('展示可排序列，并把表头排序传给已有查询参数', async () => {
    const wrapper = mountWithPlugins(AuditLogListPage)
    await flushPromises()

    expect(wrapper.text()).toContain('时间')
    expect(wrapper.text()).toContain('操作')
    expect(wrapper.text()).toContain('实体类型')
    expect(wrapper.text()).toContain('操作人')
    expect(wrapper.text()).toContain('开户行')
    expect(wrapper.text()).not.toContain('银行名称')

    const vm = wrapper.vm as any
    vm.handleSortChange({ prop: 'username', order: 'ascending' })
    await flushPromises()

    expect(auditLogApi.getAuditLogs).toHaveBeenLastCalledWith(expect.objectContaining({
      page: 1,
      pageSize: 20,
      sortBy: 'username',
      sortOrder: 'asc'
    }))
  })
})
