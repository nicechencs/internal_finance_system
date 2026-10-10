<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <el-button text @click="router.back()">
          <el-icon><ArrowLeft /></el-icon> 返回
        </el-button>
        <h2 class="page-title">人员详情</h2>
      </div>
      <el-button type="warning" @click="linkDialogVisible = true" v-if="person">
        <el-icon><Link /></el-icon> 一键关联
      </el-button>
    </div>

    <LinkDialog
      v-model="linkDialogVisible"
      :link-type="LinkType.Person"
      :entity-id="Number(route.params.id)"
      :entity-name="person?.name || ''"
      @success="handleLinkSuccess"
    />

    <div v-loading="loading">
      <div class="info-card">
        <el-descriptions :column="3" border>
          <el-descriptions-item label="姓名">{{ person?.name || '-' }}</el-descriptions-item>
          <el-descriptions-item label="人员类型">{{ person ? formatPersonType(person.personType) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="部门">{{ person?.department || '-' }}</el-descriptions-item>
          <el-descriptions-item label="职位">{{ person?.position || '-' }}</el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ person?.phone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="邮箱">{{ person?.email || '-' }}</el-descriptions-item>
          <el-descriptions-item label="银行账号">{{ person?.bankAccount || '-' }}</el-descriptions-item>
          <el-descriptions-item label="开户行">{{ person?.bankName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="入职日期">{{ person?.joinDate ? formatDate(person.joinDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="离职日期">{{ person?.leaveDate ? formatDate(person.leaveDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag v-if="person" :type="person.isActive ? 'success' : 'danger'">
              {{ person.isActive ? '在职' : '离职' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="创建时间" :span="3">
            {{ person ? formatDateTime(person.createdAt) : '-' }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="tab-section">
        <el-tabs v-model="activeTab" @tab-change="handleTabChange">
          <el-tab-pane label="交易记录" name="transactions">
            <div class="transactions-summary mb-4">
              <el-descriptions :column="4" border size="small">
                <el-descriptions-item label="收入总额">
                  <span class="amount-success">{{ formatCurrency(transactionSummary.income) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="支出总额">
                  <span class="amount-danger">{{ formatCurrency(transactionSummary.expense) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="转账总额">
                  <span>{{ formatCurrency(transactionSummary.transfer) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="净额">
                  <span :class="transactionSummary.net >= 0 ? 'amount-success' : 'amount-danger'">
                    {{ formatCurrency(transactionSummary.net) }}
                  </span>
                </el-descriptions-item>
              </el-descriptions>
            </div>
            <div class="type-filter-row">
              <TransactionTypeFilter v-model="typeFilter" />
            </div>
            <el-table
              :data="filteredTransactions"
              v-loading="transactionsLoading"
              class="resizable-table clickable-rows"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
              @row-click="handleViewTransaction"
            >
              <el-table-column prop="transactionDate" label="日期" :width="getColumnWidth('transactionDate', TABLE_COLUMN_WIDTH.date)">
                <template #default="{ row }">{{ formatDate(row.transactionDate) }}</template>
              </el-table-column>
              <el-table-column prop="transactionType" label="类型" :width="getColumnWidth('transactionType', TABLE_COLUMN_WIDTH.type)">
                <template #default="{ row }">
                  <TransactionTypeTag
                    :transaction-type="row.transactionType"
                    :transfer-direction="row.transferDirection"
                  />
                </template>
              </el-table-column>
              <el-table-column prop="amount" label="金额" :width="getColumnWidth('amount', TABLE_COLUMN_WIDTH.amount)" align="right">
                <template #default="{ row }">
                  <span :style="{ color: getTransactionAmountColor(row.transactionType) }">
                    {{ formatTransactionAmount(row.amount, row.transactionType) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="标签" :width="getColumnWidth('tags', 180)">
                <template #default="{ row }">
                  <TagDisplay :tags="row.tags || []" size="small" :max-display="2" />
                </template>
              </el-table-column>
              <el-table-column prop="accountName" label="账户" :width="getColumnWidth('accountName', TABLE_COLUMN_WIDTH.account)">
                <template #default="{ row }">
                  <el-link v-if="row.accountId" type="primary" @click.stop="router.push({ name: 'AccountDetail', params: { id: row.accountId } })">
                    {{ row.accountName }}
                  </el-link>
                  <span v-else>{{ row.accountName || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="categoryName" label="分类" :width="getColumnWidth('categoryName', TABLE_COLUMN_WIDTH.category)">
                <template #default="{ row }">
                  <el-link v-if="row.categoryId" type="primary" @click.stop="router.push({ name: 'Transactions', query: { categoryId: String(row.categoryId) } })">
                    {{ row.categoryName }}
                  </el-link>
                  <span v-else>{{ row.categoryName || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="projectName" label="项目" :width="getColumnWidth('projectName', TABLE_COLUMN_WIDTH.project)">
                <template #default="{ row }">
                  <el-link v-if="row.projectId" type="primary" @click.stop="router.push({ name: 'ProjectDetail', params: { id: row.projectId } })">
                    {{ row.projectName }}
                  </el-link>
                  <span v-else>{{ row.projectName || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="description" label="描述" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="应收记录" name="receivables">
            <ReceivableRecordsTable :records="receivables" :loading="receivablesLoading" />
          </el-tab-pane>
          <el-tab-pane label="应付记录" name="payables">
            <PayableRecordsTable :records="payables" :loading="payablesLoading" />
          </el-tab-pane>
          <el-tab-pane label="交付验收" name="deliveries">
            <div class="delivery-toolbar">
              <el-button v-if="userStore.canEdit" type="primary" @click="openDeliveryDialog()">
                新增交付
              </el-button>
            </div>
            <el-table :data="deliveries" v-loading="deliveriesLoading" border>
              <el-table-column prop="title" label="交付物" min-width="160" show-overflow-tooltip />
              <el-table-column prop="projectName" label="关联项目" min-width="140" show-overflow-tooltip>
                <template #default="{ row }">
                  <el-link
                    v-if="row.projectId"
                    type="primary"
                    @click.stop="router.push({ name: 'ProjectDetail', params: { id: row.projectId } })"
                  >
                    {{ row.projectName || '-' }}
                  </el-link>
                  <span v-else>-</span>
                </template>
              </el-table-column>
              <el-table-column prop="status" label="验收状态" width="120">
                <template #default="{ row }">
                  <el-tag :type="deliveryStatusTag(row.status)">{{ formatDeliveryStatus(row.status) }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="验收时间" width="170">
                <template #default="{ row }">{{ row.acceptedAt ? formatDateTime(row.acceptedAt) : '-' }}</template>
              </el-table-column>
              <el-table-column prop="acceptedBy" label="验收人" width="120">
                <template #default="{ row }">{{ row.acceptedBy || '-' }}</template>
              </el-table-column>
              <el-table-column prop="description" label="说明" min-width="160" show-overflow-tooltip />
              <el-table-column v-if="userStore.canEdit" label="操作" width="220" fixed="right">
                <template #default="{ row }">
                  <el-button
                    v-if="row.status !== 'Accepted'"
                    link
                    type="success"
                    size="small"
                    @click="changeDeliveryStatus(row, 'Accepted')"
                  >验收</el-button>
                  <el-button
                    v-if="row.status !== 'Rejected'"
                    link
                    type="warning"
                    size="small"
                    @click="changeDeliveryStatus(row, 'Rejected')"
                  >驳回</el-button>
                  <el-button link type="primary" size="small" @click="openDeliveryDialog(row)">编辑</el-button>
                  <el-button
                    v-if="userStore.canDelete"
                    link
                    type="danger"
                    size="small"
                    @click="handleDeleteDelivery(row)"
                  >删除</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
        </el-tabs>
      </div>

    </div>

    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />

    <PersonDeliveryDialog
      v-model="deliveryDialogVisible"
      :person-id="Number(route.params.id)"
      :delivery="editingDelivery"
      :projects="projects"
      @success="reloadDeliveries"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Link } from '@element-plus/icons-vue'
import { deletePersonDelivery, getPersonById, getPersonDeliveries, updatePersonDelivery } from '@/features/master-data/persons/api/person'
import PersonDeliveryDialog from '@/features/master-data/persons/components/PersonDeliveryDialog.vue'
import { getActiveProjects } from '@/features/master-data/projects/api/project'
import { useUserStore } from '@/features/auth/stores/user'
import { getReceivablesByPerson } from '@/features/finance/api/receivable'
import { getPayablesByPerson } from '@/features/finance/api/payable'
import ReceivableRecordsTable from '@/shared/ui/ReceivableRecordsTable.vue'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import TransactionTypeFilter from '@/shared/ui/TransactionTypeFilter.vue'
import { filterTransactionsByType, type TransactionTypeFilter as TypeFilter } from '@/shared/utils/transactionType'
import type { Receivable } from '@/features/finance/types/receivable'
import type { Payable } from '@/features/finance/types/payable'
import { getTransactionsByPerson } from '@/features/transactions/api/transaction'
import LinkDialog from '@/features/transactions/components/LinkDialog.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import type { Person, PersonDelivery, PersonDeliveryStatus } from '@/features/master-data/persons/types/person'
import type { Transaction } from '@/features/transactions/types/transaction'
import { LinkType } from '@/features/transactions/types/link'
import { formatDateTime, formatTransactionAmount, getTransactionAmountColor, formatCurrency } from '@/shared/utils/formatters'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('person-detail-transactions')

const loading = ref(false)
const person = ref<Person | null>(null)
const activeTab = ref('transactions')
const transactions = ref<Transaction[]>([])
const transactionsLoading = ref(false)
const transactionsLoaded = ref(false)
const typeFilter = ref<TypeFilter>('all')
const filteredTransactions = computed(() => filterTransactionsByType(transactions.value, typeFilter.value))
const linkDialogVisible = ref(false)
const receivables = ref<Receivable[]>([])
const receivablesLoading = ref(false)
const receivablesLoaded = ref(false)
const payables = ref<Payable[]>([])
const payablesLoading = ref(false)
const payablesLoaded = ref(false)
const detailVisible = ref(false)
const currentTransactionId = ref(0)
const deliveries = ref<PersonDelivery[]>([])
const deliveriesLoading = ref(false)
const deliveriesLoaded = ref(false)
const deliveryDialogVisible = ref(false)
const editingDelivery = ref<PersonDelivery | null>(null)
const projects = ref<{ id: number; name: string }[]>([])

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}

const transactionSummary = computed(() => {
  const income = transactions.value
    .filter(t => t.transactionType === 'Income')
    .reduce((sum, t) => sum + t.amount, 0)
  const expense = transactions.value
    .filter(t => t.transactionType === 'Expense')
    .reduce((sum, t) => sum + t.amount, 0)
  const transfer = transactions.value
    .filter(t => t.transactionType === 'Transfer')
    .reduce((sum, t) => sum + t.amount, 0)
  return { income, expense, transfer, net: income - expense }
})

const handleLinkSuccess = () => {
  transactionsLoaded.value = false
  receivablesLoaded.value = false
  payablesLoaded.value = false
  loadTransactions()
}

const formatPersonType = (type: string): string => {
  const typeMap: Record<string, string> = {
    Employee: '员工',
    Contractor: '承包商',
    Partner: '合伙人',
    Shareholder: '股东'
  }
  return typeMap[type] || type
}

const loadPerson = async () => {
  const id = Number(route.params.id)
  if (!id) return

  loading.value = true
  try {
    const { data } = await getPersonById(id)
    person.value = data.data
    await loadTransactions()
  } catch {
    ElMessage.error('加载人员详情失败')
  } finally {
    loading.value = false
  }
}

const loadTransactions = async () => {
  const id = Number(route.params.id)
  if (!id || transactionsLoaded.value) return

  transactionsLoading.value = true
  try {
    const { data } = await getTransactionsByPerson(id)
    transactions.value = data.data
    transactionsLoaded.value = true
  } catch {
    ElMessage.error('加载交易记录失败')
  } finally {
    transactionsLoading.value = false
  }
}

const loadReceivables = async () => {
  const id = Number(route.params.id)
  if (!id || receivablesLoaded.value) return
  receivablesLoading.value = true
  try {
    const { data } = await getReceivablesByPerson(id)
    receivables.value = data.data
    receivablesLoaded.value = true
  } catch {
    ElMessage.error('加载应收记录失败')
  } finally {
    receivablesLoading.value = false
  }
}

const loadPayables = async () => {
  const id = Number(route.params.id)
  if (!id || payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const { data } = await getPayablesByPerson(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch {
    ElMessage.error('加载应付记录失败')
  } finally {
    payablesLoading.value = false
  }
}

const formatDeliveryStatus = (status: string) => {
  const labels: Record<string, string> = {
    Pending: '待验收',
    Accepted: '已验收',
    Rejected: '已驳回'
  }
  return labels[status] || status
}

const deliveryStatusTag = (status: string) => {
  if (status === 'Accepted') return 'success'
  if (status === 'Rejected') return 'danger'
  return 'info'
}

const loadProjects = async () => {
  if (projects.value.length > 0) return
  try {
    const { data } = await getActiveProjects()
    projects.value = data.data || []
  } catch {
    projects.value = []
  }
}

const loadDeliveries = async () => {
  const id = Number(route.params.id)
  if (!id || deliveriesLoaded.value) return
  deliveriesLoading.value = true
  try {
    const { data } = await getPersonDeliveries(id)
    deliveries.value = data.data || []
    deliveriesLoaded.value = true
  } catch {
    ElMessage.error('加载交付记录失败')
  } finally {
    deliveriesLoading.value = false
  }
}

const reloadDeliveries = async () => {
  deliveriesLoaded.value = false
  await loadDeliveries()
}

const openDeliveryDialog = async (row?: PersonDelivery) => {
  editingDelivery.value = row ?? null
  await loadProjects()
  deliveryDialogVisible.value = true
}

const changeDeliveryStatus = async (row: PersonDelivery, status: PersonDeliveryStatus) => {
  try {
    await updatePersonDelivery(Number(route.params.id), row.id, {
      title: row.title,
      description: row.description || undefined,
      projectId: row.projectId ?? null,
      status,
      notes: row.notes || undefined,
      acceptedAt: status === 'Accepted' && row.status === 'Accepted' ? row.acceptedAt : undefined,
      acceptedBy: status === 'Accepted' && row.status === 'Accepted' ? row.acceptedBy : undefined
    })
    ElMessage.success(status === 'Accepted' ? '已验收' : '已驳回')
    await reloadDeliveries()
  } catch {
    ElMessage.error('更新验收状态失败')
  }
}

const handleDeleteDelivery = async (row: PersonDelivery) => {
  try {
    await deletePersonDelivery(Number(route.params.id), row.id)
    ElMessage.success('交付记录已删除')
    await reloadDeliveries()
  } catch {
    ElMessage.error('删除交付记录失败')
  }
}

const handleTabChange = (tab: string | number) => {
  if (tab === 'transactions' && !transactionsLoaded.value) loadTransactions()
  if (tab === 'receivables' && !receivablesLoaded.value) loadReceivables()
  if (tab === 'payables' && !payablesLoaded.value) loadPayables()
  if (tab === 'deliveries' && !deliveriesLoaded.value) loadDeliveries()
}

const formatDate = (date: string) => formatDateTime(date, 'date')

onMounted(async () => {
  await loadPerson()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.info-card {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.tab-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.tab-section :deep(.el-table) {
  font-size: 13px;
}

.transactions-summary {
  margin-bottom: 16px;
}

.type-filter-row {
  margin-bottom: 12px;
}

.amount-success {
  color: var(--color-success);
  font-weight: 600;
}

.amount-danger {
  color: var(--color-danger);
  font-weight: 600;
}

.mb-4 {
  margin-bottom: 16px;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}

.delivery-toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 12px;
}
</style>
