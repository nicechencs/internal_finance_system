import { ComputedRef, computed, defineComponent, h, inject, PropType, provide } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import TransactionListPage from '@/features/transactions/pages/TransactionListPage.vue'
import * as accountApi from '@/features/master-data/accounts/api/account'
import * as categoryApi from '@/features/master-data/categories/api/category'
import * as projectApi from '@/features/master-data/projects/api/project'
import * as transactionApi from '@/features/transactions/api/transaction'

vi.mock('@/features/transactions/api/transaction')
vi.mock('@/features/master-data/accounts/api/account')
vi.mock('@/features/master-data/categories/api/category')
vi.mock('@/features/master-data/projects/api/project')

const tableRowsKey = Symbol('tableRows')

const TableStub = defineComponent({
  name: 'ElTable',
  props: {
    data: {
      type: Array as PropType<Array<Record<string, unknown>>>,
      default: () => []
    }
  },
  setup(props, { slots }) {
    provide(tableRowsKey, computed(() => props.data))
    return () => h('div', { class: 'el-table-stub' }, slots.default?.())
  }
})

const TableColumnStub = defineComponent({
  name: 'ElTableColumn',
  props: {
    label: String,
    prop: String
  },
  setup(props, { slots }) {
    const rows = inject<ComputedRef<Array<Record<string, unknown>>>>(tableRowsKey, computed(() => []))

    return () =>
      h('section', { class: 'el-table-column-stub' }, [
        props.label ? h('div', { class: 'column-label' }, props.label) : null,
        ...rows.value.map((row, index) =>
          h(
            'div',
            { key: `${props.label ?? props.prop ?? 'column'}-${index}`, class: 'column-cell' },
            slots.default
              ? slots.default({ row })
              : String((props.prop ? row[props.prop] : undefined) ?? '-')
          )
        )
      ])
  }
})

const SlotStub = defineComponent({
  name: 'SlotStub',
  setup(_, { slots }) {
    return () => h('div', slots.default?.())
  }
})

describe('TransactionListPage summary column', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    vi.mocked(transactionApi.getTransactions).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: [
            {
              id: 1,
              transactionDate: '2026-04-01',
              transactionTime: '09:30:00',
              transactionType: 'Expense',
              amount: 188.8,
              accountId: 1,
              accountName: 'Main Account',
              description: 'Office supplies purchase',
              memo: 'Bank summary text',
              counterparty: 'Vendor A',
              status: 'Confirmed',
              isAllocated: false,
              allocations: [],
              tags: [],
              createdAt: '2026-04-01T09:30:00'
            }
          ],
          total: 1,
          page: 1,
          pageSize: 20
        }
      })
    )

    vi.mocked(transactionApi.getTransactionStatistics).mockResolvedValue(
      mockAxiosResponse({
        data: {
          totalIncome: 0,
          totalExpense: 188.8,
          netProfit: -188.8,
          totalTransfer: 0,
          incomeCount: 0,
          expenseCount: 1,
          transferCount: 0,
          totalCount: 1
        }
      })
    )

    vi.mocked(accountApi.getAccounts).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: [
            {
              id: 1,
              name: 'Main Account',
              accountType: 'Bank',
              currentBalance: 1000,
              openingBalance: 1000,
              currency: 'CNY',
              isActive: true,
              createdAt: '2026-04-01T00:00:00'
            }
          ],
          total: 1
        }
      })
    )

    vi.mocked(categoryApi.getCategories).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: [],
          total: 0
        }
      })
    )

    vi.mocked(projectApi.getProjects).mockResolvedValue(
      mockAxiosResponse({
        data: {
          items: [],
          total: 0
        }
      })
    )
  })

  it('renders the transaction summary column using memo data', async () => {
    const wrapper = mountWithPlugins(TransactionListPage, {
      global: {
        stubs: {
          ElRow: SlotStub,
          ElCol: SlotStub,
          ElForm: SlotStub,
          ElFormItem: SlotStub,
          ElDatePicker: SlotStub,
          ElTabs: SlotStub,
          ElTabPane: SlotStub,
          ElPagination: SlotStub,
          ElButton: SlotStub,
          ElLink: SlotStub,
          ElTag: SlotStub,
          ElIcon: SlotStub,
          ElTable: TableStub,
          ElTableColumn: TableColumnStub,
          SearchableSelect: true,
          TagSelector: true,
          ActiveTagFilters: true,
          StatCard: true,
          TagDisplay: true,
          TransactionForm: true,
          TransactionDetail: true,
          TransferDialog: true,
          ConvertTransactionToTransferDialog: true,
          BatchLinkDialog: true,
          TagEditorDialog: true
        }
      }
    })

    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('摘要')
    expect(wrapper.text()).toContain('Bank summary text')
  })
})
