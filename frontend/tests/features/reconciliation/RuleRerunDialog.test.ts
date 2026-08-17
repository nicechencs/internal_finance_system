import { defineComponent, h, nextTick, PropType, ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ElMessageBox } from 'element-plus'
import { mockAxiosResponse, mountWithPlugins } from '@tests/utils'
import RuleRerunDialog from '@/features/reconciliation/components/RuleRerunDialog.vue'
import { RuleRerunStrategy } from '@/features/transactions/types/link'
import * as linkApi from '@/features/transactions/api/link'

vi.mock('@/features/transactions/api/link')

const TableStub = defineComponent({
  name: 'ElTable',
  props: {
    data: {
      type: Array as PropType<Array<Record<string, unknown>>>,
      default: () => []
    }
  },
  emits: ['selection-change'],
  setup(props, { emit, slots, expose }) {
    const selectedRows = ref<Array<Record<string, unknown>>>([])

    const toggleRowSelection = async (row: Record<string, unknown>, selected: boolean) => {
      const dataSnapshot = props.data
      selectedRows.value = selected ? [row] : []
      emit('selection-change', [...selectedRows.value])

      await nextTick()

      if (props.data !== dataSnapshot && selectedRows.value.length > 0) {
        selectedRows.value = []
        emit('selection-change', [])
      }
    }

    expose({ toggleRowSelection })

    return () =>
      h('div', { class: 'el-table-stub' }, [
        props.data.length > 0
          ? h(
              'button',
              {
                class: 'manual-select',
                onClick: () => toggleRowSelection(props.data[0]!, true)
              },
              'select-first'
            )
          : null,
        slots.default?.()
      ])
  }
})

const ButtonStub = defineComponent({
  name: 'ElButton',
  props: {
    disabled: Boolean
  },
  emits: ['click'],
  setup(props, { emit, slots }) {
    return () =>
      h(
        'button',
        {
          disabled: props.disabled,
          onClick: () => emit('click')
        },
        slots.default?.()
      )
  }
})

const SlotStub = defineComponent({
  name: 'SlotStub',
  setup(_, { slots }) {
    return () => h('div', slots.default?.())
  }
})

describe('RuleRerunDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()

    vi.mocked(linkApi.previewRuleRerun).mockResolvedValue(
      mockAxiosResponse({
        data: {
          totalAffected: 1,
          wouldUpdate: 1,
          strategy: RuleRerunStrategy.Conservative,
          candidates: [
            {
              transactionId: 101,
              transactionDate: '2026-04-01',
              amount: 88.5,
              transactionType: 'Expense',
              counterparty: 'Vendor A',
              description: 'Office purchase',
              currentCategoryName: undefined,
              newCategoryName: 'Office Expense',
              newCategoryId: 9,
              willChange: true
            }
          ]
        }
      })
    )

    vi.mocked(linkApi.confirmRuleRerun).mockResolvedValue(
      mockAxiosResponse({
        data: {
          updatedCount: 1,
          skippedCount: 0,
          message: 'Rule rerun completed'
        }
      })
    )

    vi.mocked(ElMessageBox.confirm).mockResolvedValue('confirm' as never)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('keeps selected rows confirmable after preview selection', async () => {
    const wrapper = mountWithPlugins(RuleRerunDialog, {
      props: {
        modelValue: true
      },
      global: {
        stubs: {
          ElDialog: SlotStub,
          ElForm: SlotStub,
          ElFormItem: SlotStub,
          ElDatePicker: SlotStub,
          ElRadioGroup: SlotStub,
          ElRadio: SlotStub,
          ElTooltip: SlotStub,
          ElAlert: SlotStub,
          ElEmpty: SlotStub,
          ElIcon: SlotStub,
          ElTable: TableStub,
          ElTableColumn: true,
          ElTag: SlotStub,
          ElButton: ButtonStub
        }
      }
    })

    await (wrapper.vm as any).handlePreview()
    await nextTick()

    await wrapper.find('.manual-select').trigger('click')
    await nextTick()

    expect((wrapper.vm as any).selectedIds).toEqual([101])

    await (wrapper.vm as any).handleConfirm()
    await nextTick()

    expect(linkApi.confirmRuleRerun).toHaveBeenCalledWith({
      startDate: undefined,
      endDate: undefined,
      strategy: RuleRerunStrategy.Conservative,
      transactionIds: [101]
    })
  })
})
