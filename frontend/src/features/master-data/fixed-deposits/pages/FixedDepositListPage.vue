<template>
  <div class="page-container">
    <div class="page-header panel-card">
      <div class="page-header-left">
        <div class="page-kicker">MASTER DATA / FIXED DEPOSIT LEDGER</div>
        <h2 class="page-title">定期存款台账</h2>
        <p class="page-desc">按定期账户集中查看存续、到期与支取情况，明确区分账户主档和定期记录台账。</p>
      </div>
      <div class="page-header-right">
        <el-button @click="handleRefresh">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">
          <el-icon><Plus /></el-icon>
          新增定期记录
        </el-button>
      </div>
    </div>

    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="CreditCard"
          :value="String(statistics.totalCount)"
          label="总笔数"
          :count="selectedAccountSummary"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Check"
          :value="String(statistics.activeCount)"
          label="存续中"
          :count="activeAmountText"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Clock"
          :value="String(statistics.upcomingCount)"
          label="即将到期"
          :count="upcomingHintText"
          theme="profit"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Wallet"
          :value="formatMoney(statistics.expectedInterest)"
          label="预期收益"
          :count="expectedPrincipalText"
          theme="balance"
        />
      </el-col>
    </el-row>

    <div v-if="maturingSoonDeposits.length > 0" class="maturity-banner panel-card">
      <el-icon class="maturity-banner-icon"><Warning /></el-icon>
      <div class="maturity-banner-text">
        <strong>{{ maturingSoonDeposits.length }} 笔定期存款将在 30 天内到期</strong>
        <span>，合计本金 {{ formatMoney(maturingSoonPrincipal) }}，请及时关注并安排支取。</span>
      </div>
    </div>

    <div class="toolbar-card panel-card">
      <el-form :inline="true" :model="filters" class="filter-form">
        <el-form-item label="定期账户">
          <SearchableSelect
            v-model="filters.accountId"
            :options="filterAccountOptions"
            entity-name="定期账户"
            label-field="displayName"
            placeholder="全部定期账户"
            width="240px"
            @change="handleFilter"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select
            v-model="filters.status"
            clearable
            placeholder="全部状态"
            style="width: 180px"
            @change="handleFilter"
          >
            <el-option label="存续中" value="Active" />
            <el-option label="已到期" value="Matured" />
            <el-option label="已支取" value="Withdrawn" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleFilter">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <div class="toolbar-summary">
        <span>共 {{ paginationTotal }} 条记录</span>
        <span>更新于 {{ lastUpdatedText }}</span>
      </div>
    </div>

    <div class="table-card panel-card">
      <el-table
        :data="pagedDeposits"
        v-loading="loading"
        row-key="id"
        border
        class="resizable-table fixed-deposit-table"
        allow-drag-last-column
        @header-dragend="handleHeaderDragend"
        @sort-change="handleSortChange"
      >
        <el-table-column prop="accountName" label="账户名" :min-width="getColumnMinWidth('accountName', TABLE_COLUMN_WIDTH.account)">
          <template #default="{ row }">
            <div class="account-cell">
              <el-link class="account-main" type="primary" underline="never" @click="router.push({ name: 'AccountDetail', params: { id: row.accountId } })">
                {{ row.accountName }}
              </el-link>
              <div class="account-sub">{{ getAccountMetaText(row.accountId) }}</div>
            </div>
          </template>
        </el-table-column>

        <el-table-column prop="principal" label="本金" :width="getColumnWidth('principal', TABLE_COLUMN_WIDTH.amount)" align="right" sortable="custom">
          <template #default="{ row }">
            <span class="amount-text">{{ formatMoney(row.principal) }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="depositDate" label="起息日" :width="getColumnWidth('depositDate', TABLE_COLUMN_WIDTH.date)" sortable="custom">
          <template #default="{ row }">{{ formatDate(row.depositDate) }}</template>
        </el-table-column>

        <el-table-column prop="maturityDate" label="到期日" :width="getColumnWidth('maturityDate', TABLE_COLUMN_WIDTH.date)" sortable="custom">
          <template #default="{ row }">
            <span :class="getMaturityDateClass(row)">{{ formatDate(row.maturityDate) }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="termMonths" label="期限" :width="getColumnWidth('termMonths', TABLE_COLUMN_WIDTH.shortText)" align="center">
          <template #default="{ row }">{{ row.termMonths }} 个月</template>
        </el-table-column>

        <el-table-column prop="interestRate" label="利率" :width="getColumnWidth('interestRate', TABLE_COLUMN_WIDTH.rate)" align="right" sortable="custom">
          <template #default="{ row }">{{ formatRate(row.interestRate) }}</template>
        </el-table-column>

        <el-table-column prop="expectedInterest" label="预期收益" :width="getColumnWidth('expectedInterest', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            <span class="amount-text amount-profit">{{ formatMoney(row.expectedInterest) }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="status" label="状态" :width="getColumnWidth('status', TABLE_COLUMN_WIDTH.status)" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(getNormalizedStatus(row))" effect="light">{{ getStatusText(getNormalizedStatus(row)) }}</el-tag>
          </template>
        </el-table-column>

        <el-table-column label="剩余天数" :width="getColumnWidth('daysToMaturity', TABLE_COLUMN_WIDTH.shortText)" align="center">
          <template #default="{ row }">
            <span :class="getRemainingDaysClass(row)">{{ getRemainingDaysText(row) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="操作" :width="getColumnWidth('actions', 240)" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="userStore.canEdit"
              link
              type="primary"
              size="small"
              :disabled="!canEdit(row)"
              @click="handleEdit(row)"
            >
              编辑
            </el-button>
            <el-button
              v-if="userStore.canEdit"
              link
              type="primary"
              size="small"
              :disabled="getNormalizedStatus(row) === 'Withdrawn'"
              @click="handleWithdraw(row)"
            >
              支取
            </el-button>
            <el-button link type="info" size="small" @click="handleView(row)">查看</el-button>
            <el-button
              v-if="userStore.isAdmin"
              link
              type="danger"
              size="small"
              :disabled="!canDelete(row)"
              @click="handleDelete(row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="!loading && paginationTotal === 0" class="empty-guide">
        <el-empty description="" :image-size="120">
          <template #description>
            <div class="empty-guide-content">
              <h3>暂无定期存款记录</h3>
              <p v-if="filters.accountId || filters.status" class="empty-filter-hint">
                当前筛选条件下没有匹配的记录，请尝试
                <el-link type="primary" @click="handleReset">重置筛选条件</el-link>
              </p>
              <template v-else>
                <p>定期存款记录可通过以下两种方式创建：</p>
                <div class="empty-guide-steps">
                  <div class="guide-step">
                    <div class="guide-step-number">1</div>
                    <div class="guide-step-body">
                      <strong>推荐：通过账户转账自动创建</strong>
                      <span>在交易页面发起"账户转账"，将资金从活期账户转入定期账户时，系统会自动创建定期存款记录。</span>
                    </div>
                  </div>
                  <div class="guide-step">
                    <div class="guide-step-number">2</div>
                    <div class="guide-step-body">
                      <strong>手动新增记录</strong>
                      <span>如果定期存款已存在但尚未录入系统，可点击上方"新增定期记录"手动补录。</span>
                    </div>
                  </div>
                </div>
                <div class="guide-prerequisite">
                  <el-icon><Warning /></el-icon>
                  <span>前提：需先在银行账户管理中创建类型为"定期存款"的账户</span>
                </div>
              </template>
            </div>
          </template>
        </el-empty>
      </div>

      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :total="paginationTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        class="pagination"
        @size-change="handlePageSizeChange"
        @current-change="handlePageChange"
      />
    </div>

    <FixedDepositFormDialog
      v-model="createDialogVisible"
      :accounts="availableFormAccounts"
      :deposit="editingDeposit"
      :default-account-id="defaultCreateAccountId"
      @success="handleDialogSuccess"
    />

    <FixedDepositWithdrawDialog
      v-model="withdrawDialogVisible"
      :deposit="currentDeposit"
      @success="handleDialogSuccess"
    />

    <el-drawer
      v-model="detailDrawerVisible"
      title="定期存款详情"
      size="440px"
    >
      <template v-if="currentDeposit">
        <div class="detail-grid">
          <div class="detail-item">
            <span class="detail-label">账户</span>
            <span class="detail-value">{{ currentDeposit.accountName }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">状态</span>
            <span class="detail-value">{{ getStatusText(getNormalizedStatus(currentDeposit)) }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">本金</span>
            <span class="detail-value">{{ formatMoney(currentDeposit.principal) }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">利率</span>
            <span class="detail-value">{{ formatRate(currentDeposit.interestRate) }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">起息日</span>
            <span class="detail-value">{{ formatDate(currentDeposit.depositDate) }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">到期日</span>
            <span class="detail-value">{{ formatDate(currentDeposit.maturityDate) }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">期限</span>
            <span class="detail-value">{{ currentDeposit.termMonths }} 个月</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">预期收益</span>
            <span class="detail-value">{{ formatMoney(currentDeposit.expectedInterest) }}</span>
          </div>
          <div class="detail-item" v-if="currentDeposit.withdrawalDate">
            <span class="detail-label">支取日期</span>
            <span class="detail-value">{{ formatDate(currentDeposit.withdrawalDate) }}</span>
          </div>
          <div class="detail-item" v-if="currentDeposit.actualInterest != null">
            <span class="detail-label">实际利息</span>
            <span class="detail-value">{{ formatMoney(currentDeposit.actualInterest) }}</span>
          </div>
          <div class="detail-item detail-item-full" v-if="currentDeposit.notes">
            <span class="detail-label">备注</span>
            <span class="detail-value">{{ currentDeposit.notes }}</span>
          </div>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, CreditCard, Check, Clock, Wallet, Warning } from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import { useUserStore } from '@/features/auth/stores/user'
import { getAccounts } from '@/features/master-data/accounts/api/account'
import type { Account } from '@/features/master-data/accounts/types/account'
import FixedDepositFormDialog from '@/features/master-data/fixed-deposits/components/FixedDepositFormDialog.vue'
import FixedDepositWithdrawDialog from '@/features/master-data/fixed-deposits/components/FixedDepositWithdrawDialog.vue'
import { getFixedDeposits, getFixedDepositStatistics, deleteFixedDeposit } from '@/features/master-data/fixed-deposits/api/fixedDeposit'
import type { FixedDeposit } from '@/features/master-data/fixed-deposits/types/fixedDeposit'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { useListPageStatistics } from '@/shared/composables/useListPageStatistics'
import { formatDateTime, formatMoney } from '@/shared/utils/formatters'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('fixed-deposit-list')

const loading = ref(false)
const accounts = ref<Account[]>([])
const deposits = ref<FixedDeposit[]>([])
const currentDeposit = ref<FixedDeposit | null>(null)
const editingDeposit = ref<FixedDeposit | null>(null)
const createDialogVisible = ref(false)
const withdrawDialogVisible = ref(false)
const detailDrawerVisible = ref(false)
const lastUpdatedAt = ref<string>('')
const defaultCreateAccountId = ref<number | undefined>()

const filters = reactive({
  accountId: undefined as number | undefined,
  status: '' as '' | 'Active' | 'Matured' | 'Withdrawn'
})

const pagination = reactive({
  page: 1,
  pageSize: 20
})

const fixedDepositAccounts = computed(() => {
  return accounts.value
    .filter(item => item.accountType === 'FixedDeposit')
    .sort((left, right) => left.name.localeCompare(right.name, 'zh-CN'))
})

const accountMap = computed(() => {
  return new Map(fixedDepositAccounts.value.map(item => [item.id, item]))
})

const filterAccountOptions = computed(() => {
  return fixedDepositAccounts.value.map(item => ({
    ...item,
    displayName: item.isActive ? item.name : `${item.name}（已停用）`
  }))
})

const availableFormAccounts = computed(() => {
  const editingAccountId = editingDeposit.value?.accountId
  return fixedDepositAccounts.value.filter(item => item.isActive || item.id === editingAccountId)
})

const sortState = reactive({
  prop: '' as string,
  order: '' as string
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.prop = prop || ''
  sortState.order = order || ''
}

const normalizedDeposits = computed(() => {
  let result = [...deposits.value]
  if (sortState.prop && sortState.order) {
    result.sort((a: any, b: any) => {
      let valA = a[sortState.prop]
      let valB = b[sortState.prop]
      // 日期字符串直接比较即可（YYYY-MM-DD 格式）
      if (typeof valA === 'string') {
        valA = valA || ''
        valB = valB || ''
        return sortState.order === 'ascending'
          ? valA.localeCompare(valB)
          : valB.localeCompare(valA)
      }
      // 数字比较
      valA = valA ?? 0
      valB = valB ?? 0
      return sortState.order === 'ascending' ? valA - valB : valB - valA
    })
  }
  return result
})

const pagedDeposits = computed(() => {
  const start = (pagination.page - 1) * pagination.pageSize
  return normalizedDeposits.value.slice(start, start + pagination.pageSize)
})

const paginationTotal = computed(() => normalizedDeposits.value.length)

const { statistics, statisticsLoading, loadStatistics } = useListPageStatistics({
  fetchStatistics: getFixedDepositStatistics,
  initialStatistics: {
    totalCount: 0,
    activeCount: 0,
    withdrawnCount: 0,
    upcomingCount: 0,
    totalPrincipal: 0,
    activePrincipal: 0,
    expectedInterest: 0
  },
  buildParams: () => {
    return {
      accountIds: filters.accountId ? [filters.accountId] : undefined,
      status: filters.status || undefined
    }
  },
  autoLoad: false
})

const totalPrincipal = computed(() => statistics.value.totalPrincipal)
const selectedAccountSummary = computed(() => {
  if (!filters.accountId) {
    const inactiveCount = fixedDepositAccounts.value.filter(item => !item.isActive).length
    return inactiveCount > 0
      ? `${fixedDepositAccounts.value.length} 个定期账户（含 ${inactiveCount} 个已停用）`
      : `${fixedDepositAccounts.value.length} 个定期账户`
  }
  const account = accountMap.value.get(filters.accountId)
  if (!account) return ''
  return account.isActive ? `当前账户：${account.name}` : `当前账户：${account.name}（已停用）`
})
const activeAmountText = computed(() => `本金 ${formatMoney(statistics.value.activePrincipal)}`)
const upcomingHintText = computed(() => statistics.value.upcomingCount > 0 ? '30 天内需重点跟进' : '暂无近期到期')
const expectedPrincipalText = computed(() => `记录本金 ${formatMoney(totalPrincipal.value)}`)

const maturingSoonDeposits = computed(() => {
  return deposits.value.filter(d => {
    if (d.status === 'Withdrawn') return false
    const days = dayjs(d.maturityDate).diff(dayjs(), 'day')
    return days >= 0 && days <= 30
  })
})

const maturingSoonPrincipal = computed(() => {
  return maturingSoonDeposits.value.reduce((sum, d) => sum + d.principal, 0)
})

const lastUpdatedText = computed(() => lastUpdatedAt.value ? formatDateTime(lastUpdatedAt.value, 'datetime') : '-')

const loadAccounts = async () => {
  const pageSize = 200
  let page = 1
  let total = 0
  const items: Account[] = []

  do {
    const { data } = await getAccounts({ page, pageSize })
    const pageItems = data.data.items as Account[]
    items.push(...pageItems)
    total = data.data.total

    if (items.length >= total || pageItems.length === 0) {
      break
    }

    page += 1
  } while (page <= 20)

  accounts.value = items
}

const loadDeposits = async () => {
  loading.value = true
  try {
    const { data } = await getFixedDeposits({
      accountIds: filters.accountId ? [filters.accountId] : undefined,
      status: filters.status || undefined
    })

    deposits.value = data.data
    lastUpdatedAt.value = new Date().toISOString()
  } catch (error) {
    console.error('加载定期存款失败:', error)
    ElMessage.error('加载定期存款失败')
  } finally {
    loading.value = false
  }
}

const loadPageData = async () => {
  try {
    await loadAccounts()
    await Promise.all([loadDeposits(), loadStatistics()])
  } catch (error) {
    console.error('加载页面数据失败:', error)
  }
}

const handleFilter = async () => {
  pagination.page = 1
  await loadDeposits()
  await loadStatistics()
}

const handleReset = async () => {
  filters.accountId = undefined
  filters.status = ''
  pagination.page = 1
  await loadDeposits()
  await loadStatistics()
}

const handleRefresh = async () => {
  await loadPageData()
  ElMessage.success('已刷新最新数据')
}

const handleCreate = () => {
  editingDeposit.value = null
  defaultCreateAccountId.value = filters.accountId
  createDialogVisible.value = true
}

const handleEdit = (row: FixedDeposit) => {
  editingDeposit.value = row
  createDialogVisible.value = true
}

const handleWithdraw = (row: FixedDeposit) => {
  currentDeposit.value = row
  withdrawDialogVisible.value = true
}

const handleView = (row: FixedDeposit) => {
  currentDeposit.value = row
  detailDrawerVisible.value = true
}

const handleDelete = async (row: FixedDeposit) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除定期存款记录吗？本金：${formatMoney(row.principal)}，账户：${row.accountName}`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    await deleteFixedDeposit(row.id)
    ElMessage.success('删除成功')
    await loadPageData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除定期存款失败:', error)
    }
  }
}

const handleDialogSuccess = async () => {
  createDialogVisible.value = false
  withdrawDialogVisible.value = false
  editingDeposit.value = null
  defaultCreateAccountId.value = undefined
  await loadPageData()
}

const handlePageChange = (page: number) => {
  pagination.page = page
}

const handlePageSizeChange = (pageSize: number) => {
  pagination.pageSize = pageSize
  pagination.page = 1
}

const getNormalizedStatus = (row: FixedDeposit) => {
  if (row.status === 'Withdrawn') return 'Withdrawn'
  if (dayjs(row.maturityDate).isBefore(dayjs(), 'day')) return 'Matured'
  return 'Active'
}

const getDaysToMaturity = (row: FixedDeposit) => {
  if (getNormalizedStatus(row) === 'Withdrawn') return 0
  return dayjs(row.maturityDate).diff(dayjs(), 'day')
}

const getStatusText = (status: string) => {
  if (status === 'Withdrawn') return '已支取'
  if (status === 'Matured') return '已到期'
  return '存续中'
}

const getStatusTagType = (status: string) => {
  if (status === 'Withdrawn') return 'info'
  if (status === 'Matured') return 'warning'
  return 'success'
}

const getRemainingDaysText = (row: FixedDeposit) => {
  if (getNormalizedStatus(row) === 'Withdrawn') return '已完成'
  const days = getDaysToMaturity(row)
  if (days < 0) return `逾期 ${Math.abs(days)} 天`
  if (days === 0) return '今日到期'
  return `${days} 天`
}

const getRemainingDaysClass = (row: FixedDeposit) => {
  if (getNormalizedStatus(row) === 'Withdrawn') return 'text-muted'
  const days = getDaysToMaturity(row)
  if (days < 0) return 'text-danger'
  if (days <= 30) return 'text-warning'
  return 'text-normal'
}

const getMaturityDateClass = (row: FixedDeposit) => {
  if (getNormalizedStatus(row) === 'Withdrawn') return 'text-muted'
  const days = getDaysToMaturity(row)
  if (days < 0) return 'text-danger'
  if (days <= 30) return 'text-warning'
  return ''
}

const getAccountMetaText = (accountId: number) => {
  const account = accountMap.value.get(accountId)
  if (!account) return '定期账户'

  const parts = []
  if (account.bankName) {
    parts.push(account.bankName)
  }
  parts.push(account.isActive ? '启用' : '已停用')

  return parts.join(' · ')
}

const canEdit = (row: FixedDeposit) => {
  // 仅 Admin 可编辑
  if (!userStore.isAdmin) return false
  // 已支取的不能编辑
  if (getNormalizedStatus(row) === 'Withdrawn') return false
  // 有关联交易的不能编辑
  if (row.depositTransactionId > 0) return false
  return true
}

const canDelete = (row: FixedDeposit) => {
  // 仅 Admin 可删除
  if (!userStore.isAdmin) return false
  // 已支取的不能删除
  if (getNormalizedStatus(row) === 'Withdrawn') return false
  // 有关联交易的不能删除
  if (row.depositTransactionId > 0) return false
  return true
}

const formatDate = (value?: string) => {
  if (!value) return '-'
  return formatDateTime(value, 'date')
}

const formatRate = (value?: number) => {
  if (value == null) return '-'
  return `${Number(value).toFixed(2)}%`
}

const parseAccountIdQuery = () => {
  const rawValue = Array.isArray(route.query.accountId) ? route.query.accountId[0] : route.query.accountId
  const accountId = Number(rawValue)
  return Number.isInteger(accountId) && accountId > 0 ? accountId : undefined
}

const clearRouteIntent = async () => {
  if (!route.query.accountId && !route.query.action) return

  const nextQuery = { ...route.query }
  delete nextQuery.accountId
  delete nextQuery.action

  await router.replace({ name: 'FixedDeposits', query: nextQuery })
}

const applyRouteIntent = async () => {
  const action = Array.isArray(route.query.action) ? route.query.action[0] : route.query.action
  const accountId = parseAccountIdQuery()

  if (accountId && fixedDepositAccounts.value.some(item => item.id === accountId) && filters.accountId !== accountId) {
    filters.accountId = accountId
    pagination.page = 1
    await Promise.all([loadDeposits(), loadStatistics()])
  }

  if (action === 'create' && userStore.canEdit) {
    editingDeposit.value = null
    defaultCreateAccountId.value = accountId ?? filters.accountId
    createDialogVisible.value = true
  }

  await clearRouteIntent()
}

watch(
  () => [fixedDepositAccounts.value.length, route.query.accountId, route.query.action] as const,
  async ([accountCount, accountIdQuery, actionQuery]) => {
    if (accountCount === 0 || (!accountIdQuery && !actionQuery)) return
    await applyRouteIntent()
  },
  { immediate: true }
)

onMounted(async () => {
  await loadPageData()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.panel-card {
  background: var(--bg-card);
  border: 1px solid var(--border-light);
  border-radius: 14px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04);
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 20px;
  padding: 20px 22px;
  position: relative;
  overflow: hidden;
}

.page-header::after {
  content: '';
  position: absolute;
  inset: auto -60px -60px auto;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(64, 158, 255, 0.12), rgba(64, 158, 255, 0));
  pointer-events: none;
}

.page-kicker {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.12em;
  color: var(--text-placeholder);
}

.page-title {
  margin: 8px 0 0;
  font-size: 22px;
  font-weight: 700;
  color: var(--text-primary);
}

.page-desc {
  margin: 8px 0 0;
  max-width: 720px;
  font-size: 13px;
  line-height: 1.7;
  color: var(--text-secondary);
}

.page-header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.stat-cards {
  margin-bottom: 20px;
}

.toolbar-card,
.table-card {
  padding: 18px 20px;
}

.toolbar-card {
  margin-bottom: 16px;
}

.filter-form {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  row-gap: 8px;
}

.toolbar-summary {
  margin-top: 6px;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 12px;
  color: var(--text-placeholder);
  border-top: 1px dashed var(--border-light);
  padding-top: 12px;
}

.fixed-deposit-table :deep(.el-table__header-wrapper th) {
  background: #fafcff;
}

.account-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.account-main {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
}

.account-sub {
  font-size: 12px;
  color: var(--text-placeholder);
}

.amount-text {
  font-variant-numeric: tabular-nums;
}

.amount-profit {
  color: var(--color-success);
  font-weight: 600;
}

.text-danger {
  color: var(--color-danger);
  font-weight: 600;
}

.text-warning {
  color: var(--color-warning);
  font-weight: 600;
}

.text-normal,
.text-muted {
  color: var(--text-placeholder);
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px 12px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 12px 14px;
  border: 1px solid var(--border-light);
  border-radius: 10px;
  background: var(--bg-page);
}

.detail-item-full {
  grid-column: 1 / -1;
}

.detail-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.detail-value {
  font-size: 14px;
  color: var(--text-primary);
  word-break: break-word;
}

.pagination {
  margin-top: 16px;
  justify-content: flex-end;
}

.maturity-banner {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 20px;
  margin-bottom: 20px;
  background: linear-gradient(135deg, #fff7ed, #fffbeb);
  border-color: #fed7aa;
}

.maturity-banner-icon {
  font-size: 22px;
  color: #ea580c;
  flex-shrink: 0;
}

.maturity-banner-text {
  font-size: 14px;
  color: #9a3412;
  line-height: 1.5;
}

.maturity-banner-text strong {
  color: #c2410c;
}

.empty-guide {
  padding: 40px 20px;
}

.empty-guide-content {
  text-align: left;
  max-width: 480px;
  margin: 0 auto;
}

.empty-guide-content h3 {
  text-align: center;
  font-size: 16px;
  color: var(--text-primary);
  margin-bottom: 12px;
}

.empty-guide-content > p {
  color: var(--text-secondary);
  font-size: 14px;
  margin-bottom: 16px;
  text-align: center;
}

.empty-filter-hint {
  text-align: center;
}

.empty-guide-steps {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 18px;
}

.guide-step {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  padding: 14px 16px;
  border-radius: 10px;
  background: var(--bg-page);
  border: 1px solid var(--border-light);
}

.guide-step-number {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: var(--el-color-primary);
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 2px;
}

.guide-step-body {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.guide-step-body strong {
  font-size: 14px;
  color: var(--text-primary);
}

.guide-step-body span {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.5;
}

.guide-prerequisite {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 14px;
  border-radius: 8px;
  background: #fef3c7;
  color: #92400e;
  font-size: 13px;
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
  }

  .page-header-right {
    width: 100%;
    justify-content: flex-end;
  }

  .toolbar-summary,
  .detail-grid {
    grid-template-columns: 1fr;
  }
}
</style>
