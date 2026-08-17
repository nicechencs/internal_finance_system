<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">待分配交易</h2>
        <p class="page-desc">处理尚未完全核销到应收 / 应付的收支记录</p>
      </div>
    </div>

    <div class="search-section">
      <el-form :inline="true" :model="filters" class="search-form" @submit.prevent="handleFilter">
        <el-form-item label="类型">
          <el-select v-model="filters.transactionType" placeholder="全部" style="width: 120px">
            <el-option label="全部" value="" />
            <el-option label="收入" value="Income" />
            <el-option label="支出" value="Expense" />
          </el-select>
        </el-form-item>
        <el-form-item label="日期范围">
          <el-date-picker
            v-model="filters.dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            :shortcuts="dateRangeShortcuts"
          />
        </el-form-item>
        <el-form-item label="交易金额范围">
          <el-input-number
            v-model="filters.minAmount"
            :controls="false"
            placeholder="最小金额"
            style="width: 120px"
          />
          <span class="mx-2">至</span>
          <el-input-number
            v-model="filters.maxAmount"
            :controls="false"
            placeholder="最大金额"
            style="width: 120px"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleFilter">查询</el-button>
          <el-button @click="resetFilters">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <div class="table-section">
      <el-table
        :data="transactions"
        v-loading="loading"
        class="clickable-rows"
        @selection-change="handleSelectionChange"
        @sort-change="handleSortChange"
        @row-click="handleViewTransaction"
      >
        <el-table-column v-if="canEdit" type="selection" width="55" />
        <el-table-column label="日期" prop="transactionDate" width="120" sortable="custom">
          <template #default="{ row }">
            {{ formatDateTime(row.transactionDate, 'date') }}
          </template>
        </el-table-column>
        <el-table-column label="类型" width="80">
          <template #default="{ row }">
            <TransactionTypeTag
              :transaction-type="row.transactionType"
              :transfer-direction="row.transferDirection"
            />
          </template>
        </el-table-column>
        <el-table-column label="金额" width="140" align="right" sortable="custom" prop="amount">
          <template #default="{ row }">
            <span :class="row.transactionType === 'Income' ? 'text-income' : 'text-expense'">
              {{ formatCurrency(row.amount) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="可用余额" width="140" align="right" sortable="custom" prop="availableAmount">
          <template #default="{ row }">
            <span style="font-weight: 600">{{ formatCurrency(row.availableAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="账户" prop="accountName" width="150" />
        <el-table-column label="项目" prop="projectName" width="150">
          <template #default="{ row }">
            {{ row.projectName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="对方" width="150">
          <template #default="{ row }">
            {{ row.customerName || row.supplierName || row.personName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="描述" prop="description" min-width="200" show-overflow-tooltip />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click.stop="handleViewTransaction(row)">
              详情
            </el-button>
            <el-button v-if="canEdit" link type="primary" @click.stop="handleProcess(row)">
              处理
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="canEdit" class="batch-actions">
        <el-button
          :disabled="selectedTransactions.length === 0 || processing"
          @click="openBatchPreview('receivable')"
        >
          批量创建应收并核销
        </el-button>
        <el-button
          :disabled="selectedTransactions.length === 0 || processing"
          @click="openBatchPreview('payable')"
        >
          批量创建应付并核销
        </el-button>
      </div>
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next, jumper"
        class="pagination"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>

    <el-dialog v-model="processDialogVisible" title="处理待分配交易" width="640px">
      <div v-if="currentTransaction">
        <div class="dialog-info-box">
          <p class="dialog-info-label">交易信息</p>
          <p style="font-weight: 600">
            {{ formatDateTime(currentTransaction.transactionDate, 'date') }}
            <TransactionTypeTag
              :transaction-type="currentTransaction.transactionType"
              :transfer-direction="currentTransaction.transferDirection"
              style="margin-left: 8px"
            />
          </p>
          <p class="dialog-info-amount">
            金额: {{ formatCurrency(currentTransaction.amount) }}
          </p>
          <p class="dialog-info-label" style="margin-top: 4px">
            可用余额: {{ formatCurrency(currentTransaction.availableAmount || 0) }}
          </p>
        </div>

        <el-radio-group v-model="processMode" class="dialog-radio-group">
          <el-radio label="link">
            关联已有{{ settlementLabel }}
          </el-radio>
          <el-radio label="create">
            创建新{{ settlementLabel }}并绑定
          </el-radio>
          <el-radio label="skip">暂不处理</el-radio>
        </el-radio-group>

        <div v-if="processMode === 'link'" style="margin-top: 16px">
          <el-select
            v-model="selectedSettlementId"
            filterable
            remote
            :remote-method="searchSettlements"
            placeholder="搜索并选择未结清单据..."
            style="width: 100%; margin-bottom: 12px"
            :loading="loadingSettlements"
            @change="handleSettlementChange"
          >
            <el-option-group v-if="preferredSettlements.length" label="推荐（同项目 / 同对方）">
              <el-option
                v-for="item in preferredSettlements"
                :key="item.id"
                :label="formatSettlementOptionLabel(item, formatCurrency, currentTransaction)"
                :value="item.id"
              />
            </el-option-group>
            <el-option-group v-if="showAllSettlements && otherSettlements.length" label="其他可核销单据">
              <el-option
                v-for="item in otherSettlements"
                :key="item.id"
                :label="formatSettlementOptionLabel(item, formatCurrency, currentTransaction)"
                :value="item.id"
              />
            </el-option-group>
          </el-select>
          <el-checkbox v-model="showAllSettlements">显示全部可核销单据（含跨项目）</el-checkbox>
          <p v-if="!loadingSettlements && availableSettlements.length === 0" class="hint-text">
            没有可关联的未结清{{ settlementLabel }}。可改为创建新单，或先到应收/应付详情处理。
          </p>
          <el-form-item label="核销金额" style="margin-top: 12px">
            <el-input-number
              v-model="allocationAmount"
              :max="getMaxAllocationAmount()"
              :min="0"
              :precision="2"
              style="width: 100%"
              placeholder="输入核销金额"
            />
          </el-form-item>
          <el-alert
            v-if="showPartialAmountWarning"
            type="warning"
            :closable="false"
            show-icon
            title="同一笔交易对同一张单据只能核销一次。降低金额后，剩余部分无法再核销到这张单据。"
          />
        </div>

        <div v-if="processMode === 'create'" style="margin-top: 16px">
          <el-form label-width="90px">
            <el-form-item v-if="isIncome" label="项目" required>
              <SearchableSelect
                v-model="createForm.projectId"
                :options="projects"
                entity-name="项目"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item v-else label="项目">
              <SearchableSelect
                v-model="createForm.projectId"
                :options="projects"
                entity-name="项目"
                placeholder="可选"
              />
            </el-form-item>
            <el-form-item label="对方类型">
              <el-radio-group v-model="createForm.counterpartyType" @change="handleCreateCounterpartyTypeChange">
                <el-radio v-if="isIncome" label="customer">客户</el-radio>
                <el-radio label="supplier">供应商</el-radio>
                <el-radio v-if="!isIncome" label="customer">客户</el-radio>
                <el-radio label="person">人员</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item v-if="createForm.counterpartyType === 'customer'" label="客户" required>
              <SearchableSelect
                v-model="createForm.customerId"
                :options="customers"
                entity-name="客户"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item v-if="createForm.counterpartyType === 'supplier'" label="供应商" required>
              <SearchableSelect
                v-model="createForm.supplierId"
                :options="suppliers"
                entity-name="供应商"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item v-if="createForm.counterpartyType === 'person'" label="人员" required>
              <SearchableSelect
                v-model="createForm.personId"
                :options="persons"
                entity-name="人员"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item :label="isIncome ? '应收金额' : '应付金额'">
              <el-input-number
                v-model="createForm.totalAmount"
                :min="0.01"
                :max="currentTransaction.availableAmount || currentTransaction.amount"
                :precision="2"
                :controls="false"
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="到期日">
              <el-date-picker
                v-model="createForm.dueDate"
                type="date"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="描述">
              <el-input v-model="createForm.description" type="textarea" :rows="2" maxlength="500" />
            </el-form-item>
          </el-form>
          <el-alert
            v-if="createForm.totalAmount < (currentTransaction.availableAmount || 0)"
            type="info"
            :closable="false"
            show-icon
            title="创建金额低于可用余额时，差额会继续留在待分配列表。"
          />
        </div>
      </div>

      <template #footer>
        <el-button @click="processDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmProcess" :loading="processing">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="batchDialogVisible" :title="batchKind === 'receivable' ? '批量创建应收并核销' : '批量创建应付并核销'" width="720px">
      <p class="hint-text">将为每笔符合条件的交易各创建一张单据，并按可用余额全额核销。不会合并到同一张单据。</p>
      <div v-if="batchReady.length">
        <p class="dialog-info-label">将处理 {{ batchReady.length }} 笔</p>
        <el-table :data="batchReady" max-height="240" size="small">
          <el-table-column label="日期" width="110">
            <template #default="{ row }">{{ formatDateTime(row.transactionDate, 'date') }}</template>
          </el-table-column>
          <el-table-column label="金额" width="120" align="right">
            <template #default="{ row }">{{ formatCurrency(row.availableAmount || 0) }}</template>
          </el-table-column>
          <el-table-column label="项目" prop="projectName" />
          <el-table-column label="对方">
            <template #default="{ row }">
              {{ row.customerName || row.supplierName || row.personName || '-' }}
            </template>
          </el-table-column>
        </el-table>
      </div>
      <div v-if="batchSkipped.length" style="margin-top: 16px">
        <p class="dialog-info-label">将跳过 {{ batchSkipped.length }} 笔</p>
        <el-table :data="batchSkipped" max-height="200" size="small">
          <el-table-column label="日期" width="110">
            <template #default="{ row }">{{ formatDateTime(row.transaction.transactionDate, 'date') }}</template>
          </el-table-column>
          <el-table-column label="原因" prop="reason" />
        </el-table>
      </div>
      <template #footer>
        <el-button @click="batchDialogVisible = false" :disabled="processing">取消</el-button>
        <el-button type="primary" :disabled="batchReady.length === 0" :loading="processing" @click="confirmBatch">
          确认执行
        </el-button>
      </template>
    </el-dialog>

    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { getTransactions } from '@/features/transactions/api/transaction'
import type { Transaction } from '@/features/transactions/types/transaction'
import {
  createReceivable,
  getAvailableReceivablesForTransaction,
  receivePayment
} from '@/features/finance/api/receivable'
import {
  createPayable,
  getAvailablePayablesForTransaction,
  payPayment
} from '@/features/finance/api/payable'
import { getActiveProjects } from '@/features/master-data/projects/api/project'
import { getActiveCustomers } from '@/features/master-data/customers/api/customer'
import { getActiveSuppliers } from '@/features/master-data/suppliers/api/supplier'
import { getActivePersons } from '@/features/master-data/persons/api/person'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Customer } from '@/features/master-data/customers/types/customer'
import type { Supplier } from '@/features/master-data/suppliers/types/supplier'
import type { Person } from '@/features/master-data/persons/types/person'
import { formatCurrency, formatDateTime } from '@/shared/utils/formatters'
import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'
import { useUserStore } from '@/features/auth/stores/user'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import {
  buildCreateDescription,
  evaluateBatchCreate,
  formatSettlementOptionLabel,
  isPreferredMatch,
  pickSingleCounterparty,
  toDateOnly,
  type SettlementCandidate
} from '@/features/transactions/utils/unallocatedSettlement'

const userStore = useUserStore()
const canEdit = computed(() => userStore.canEdit)

const loading = ref(false)
const transactions = ref<Transaction[]>([])
const selectedTransactions = ref<Transaction[]>([])
const processDialogVisible = ref(false)
const currentTransaction = ref<Transaction | null>(null)
const processMode = ref<'link' | 'create' | 'skip'>('link')
const loadingSettlements = ref(false)
const availableSettlements = ref<SettlementCandidate[]>([])
const selectedSettlementId = ref<number | null>(null)
const allocationAmount = ref(0)
const defaultAllocationAmount = ref(0)
const processing = ref(false)
const showAllSettlements = ref(false)
const settlementKeyword = ref('')

const detailVisible = ref(false)
const currentTransactionId = ref(0)

const batchDialogVisible = ref(false)
const batchKind = ref<'receivable' | 'payable'>('receivable')
const batchReady = ref<Transaction[]>([])
const batchSkipped = ref<Array<{ transaction: Transaction; reason: string }>>([])

const projects = ref<Project[]>([])
const customers = ref<Customer[]>([])
const suppliers = ref<Supplier[]>([])
const persons = ref<Person[]>([])
const createForm = reactive({
  projectId: undefined as number | undefined,
  counterpartyType: 'customer' as 'customer' | 'supplier' | 'person',
  customerId: undefined as number | undefined,
  supplierId: undefined as number | undefined,
  personId: undefined as number | undefined,
  totalAmount: 0,
  dueDate: '',
  description: ''
})

const isIncome = computed(() => currentTransaction.value?.transactionType === 'Income')
const settlementLabel = computed(() => (isIncome.value ? '应收款' : '应付款'))
const preferredSettlements = computed(() => {
  if (!currentTransaction.value) return []
  return availableSettlements.value.filter(item => isPreferredMatch(currentTransaction.value!, item))
})
const otherSettlements = computed(() => {
  if (!currentTransaction.value) return []
  return availableSettlements.value.filter(item => !isPreferredMatch(currentTransaction.value!, item))
})
const showPartialAmountWarning = computed(() => {
  return processMode.value === 'link'
    && defaultAllocationAmount.value > 0
    && allocationAmount.value > 0
    && allocationAmount.value < defaultAllocationAmount.value
})

const filters = ref({
  transactionType: '',
  dateRange: [] as [Date, Date] | [],
  minAmount: undefined as number | undefined,
  maxAmount: undefined as number | undefined
})

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

const sortState = reactive({
  sortBy: '',
  sortOrder: '' as '' | 'asc' | 'desc'
})

const handleViewTransaction = (row: Transaction, column?: { type?: string }) => {
  if (column?.type === 'selection') return
  currentTransactionId.value = row.id
  detailVisible.value = true
}

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.value.page = 1
  loadTransactions()
}

const handleSizeChange = () => {
  pagination.value.page = 1
  loadTransactions()
}

const handlePageChange = () => {
  loadTransactions()
}

const loadTransactions = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
      allocationStatus: 'Unallocated,PartiallyAllocated',
      excludeTransfer: true
    }

    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }

    if (filters.value.transactionType) {
      params.transactionType = filters.value.transactionType
    }

    if (filters.value.dateRange && filters.value.dateRange.length === 2) {
      params.startDate = filters.value.dateRange[0].toISOString().split('T')[0]
      params.endDate = filters.value.dateRange[1].toISOString().split('T')[0]
    }

    if (filters.value.minAmount !== undefined && filters.value.minAmount !== null) {
      params.minAmount = filters.value.minAmount
    }

    if (filters.value.maxAmount !== undefined && filters.value.maxAmount !== null) {
      params.maxAmount = filters.value.maxAmount
    }

    const response = await getTransactions(params)
    transactions.value = response.data.data.items
    pagination.value.total = response.data.data.total
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const handleFilter = () => {
  pagination.value.page = 1
  loadTransactions()
}

const resetFilters = () => {
  filters.value = {
    transactionType: '',
    dateRange: [],
    minAmount: undefined,
    maxAmount: undefined
  }
  handleFilter()
}

const handleSelectionChange = (selection: Transaction[]) => {
  selectedTransactions.value = selection
}

const resetCreateForm = (row: Transaction) => {
  createForm.projectId = row.projectId
  createForm.customerId = row.customerId
  createForm.supplierId = row.supplierId
  createForm.personId = row.personId
  createForm.totalAmount = row.availableAmount || 0
  createForm.dueDate = toDateOnly(row.transactionDate)
  createForm.description = buildCreateDescription(row)
  if (row.customerId) createForm.counterpartyType = 'customer'
  else if (row.supplierId) createForm.counterpartyType = 'supplier'
  else if (row.personId) createForm.counterpartyType = 'person'
  else createForm.counterpartyType = row.transactionType === 'Income' ? 'customer' : 'supplier'
}

const handleProcess = async (row: Transaction) => {
  currentTransaction.value = row
  processMode.value = 'link'
  selectedSettlementId.value = null
  allocationAmount.value = row.availableAmount || 0
  defaultAllocationAmount.value = row.availableAmount || 0
  showAllSettlements.value = false
  settlementKeyword.value = ''
  resetCreateForm(row)
  processDialogVisible.value = true
  await Promise.all([loadAvailableSettlements(), loadCreateOptions()])
}

const loadCreateOptions = async () => {
  const results = await Promise.allSettled([
    getActiveProjects(),
    getActiveCustomers(),
    getActiveSuppliers(),
    getActivePersons()
  ])
  if (results[0].status === 'fulfilled') projects.value = results[0].value.data.data
  if (results[1].status === 'fulfilled') customers.value = results[1].value.data.data
  if (results[2].status === 'fulfilled') suppliers.value = results[2].value.data.data
  if (results[3].status === 'fulfilled') persons.value = results[3].value.data.data
}

const loadAvailableSettlements = async (keyword = '') => {
  if (!currentTransaction.value) return
  loadingSettlements.value = true
  try {
    const tx = currentTransaction.value
    const response = tx.transactionType === 'Income'
      ? await getAvailableReceivablesForTransaction(tx.id, keyword || undefined)
      : await getAvailablePayablesForTransaction(tx.id, keyword || undefined)
    availableSettlements.value = response.data.data
  } catch (error) {
    console.error(error)
    availableSettlements.value = []
  } finally {
    loadingSettlements.value = false
  }
}

const searchSettlements = (query: string) => {
  settlementKeyword.value = query
  loadAvailableSettlements(query)
}

const handleSettlementChange = () => {
  const max = getMaxAllocationAmount()
  allocationAmount.value = max
  defaultAllocationAmount.value = max
}

const getMaxAllocationAmount = () => {
  if (!currentTransaction.value) return 0
  const transactionAvailable = currentTransaction.value.availableAmount || 0
  if (!selectedSettlementId.value) return transactionAvailable
  const settlement = availableSettlements.value.find(s => s.id === selectedSettlementId.value)
  return Math.min(transactionAvailable, settlement?.remainingAmount || 0)
}

const handleCreateCounterpartyTypeChange = () => {
  createForm.customerId = undefined
  createForm.supplierId = undefined
  createForm.personId = undefined
}

const confirmProcess = async () => {
  if (!currentTransaction.value) return

  if (processMode.value === 'skip') {
    processDialogVisible.value = false
    return
  }

  if (processMode.value === 'create') {
    await confirmCreateAndBind()
    return
  }

  if (!selectedSettlementId.value) {
    ElMessage.warning(`请选择要关联的${settlementLabel.value}`)
    return
  }
  if (!allocationAmount.value || allocationAmount.value <= 0) {
    ElMessage.warning('请输入有效的核销金额')
    return
  }

  processing.value = true
  try {
    await bindExisting(currentTransaction.value, selectedSettlementId.value, allocationAmount.value)
    ElMessage.success('核销成功')
    processDialogVisible.value = false
    await loadTransactions()
  } catch (error) {
    console.error(error)
    await loadTransactions()
  } finally {
    processing.value = false
  }
}

const confirmCreateAndBind = async () => {
  const tx = currentTransaction.value
  if (!tx) return

  if (isIncome.value && !createForm.projectId) {
    ElMessage.warning('创建应收必须选择项目')
    return
  }
  if (createForm.counterpartyType === 'customer' && !createForm.customerId) {
    ElMessage.warning('请选择客户')
    return
  }
  if (createForm.counterpartyType === 'supplier' && !createForm.supplierId) {
    ElMessage.warning('请选择供应商')
    return
  }
  if (createForm.counterpartyType === 'person' && !createForm.personId) {
    ElMessage.warning('请选择人员')
    return
  }
  if (!createForm.totalAmount || createForm.totalAmount <= 0) {
    ElMessage.warning('请输入有效金额')
    return
  }

  processing.value = true
  try {
    const created = await createSettlementFromForm(tx)
    try {
      await bindExisting(tx, created.id, createForm.totalAmount)
      ElMessage.success('已创建并核销')
      processDialogVisible.value = false
    } catch (bindError) {
      console.error(bindError)
      ElMessage.warning(`单据已创建但核销失败，请到${settlementLabel.value}详情继续登记（编号 ${created.id}，金额 ${formatCurrency(createForm.totalAmount)}）`)
      processDialogVisible.value = false
    }
    await loadTransactions()
  } catch (error) {
    console.error(error)
  } finally {
    processing.value = false
  }
}

const createSettlementFromForm = async (tx: Transaction) => {
  if (tx.transactionType === 'Income') {
    const response = await createReceivable({
      projectId: createForm.projectId!,
      customerId: createForm.counterpartyType === 'customer' ? createForm.customerId : undefined,
      supplierId: createForm.counterpartyType === 'supplier' ? createForm.supplierId : undefined,
      personId: createForm.counterpartyType === 'person' ? createForm.personId : undefined,
      totalAmount: createForm.totalAmount,
      dueDate: createForm.dueDate || undefined,
      description: createForm.description
    })
    return response.data.data
  }

  const response = await createPayable({
    projectId: createForm.projectId,
    customerId: createForm.counterpartyType === 'customer' ? createForm.customerId : undefined,
    supplierId: createForm.counterpartyType === 'supplier' ? createForm.supplierId : undefined,
    personId: createForm.counterpartyType === 'person' ? createForm.personId : undefined,
    totalAmount: createForm.totalAmount,
    dueDate: createForm.dueDate || undefined,
    description: createForm.description
  })
  return response.data.data
}

const bindExisting = async (tx: Transaction, settlementId: number, amount: number) => {
  const payload = {
    paymentDate: toDateOnly(tx.transactionDate),
    amount,
    transactionId: tx.id,
    description: tx.description
  }
  if (tx.transactionType === 'Income') {
    await receivePayment(settlementId, payload)
    return
  }
  await payPayment(settlementId, payload)
}

const openBatchPreview = (kind: 'receivable' | 'payable') => {
  batchKind.value = kind
  const ready: Transaction[] = []
  const skipped: Array<{ transaction: Transaction; reason: string }> = []
  for (const tx of selectedTransactions.value) {
    const result = evaluateBatchCreate(tx, kind)
    if (result.ok) ready.push(tx)
    else skipped.push({ transaction: tx, reason: result.reason })
  }
  batchReady.value = ready
  batchSkipped.value = skipped
  batchDialogVisible.value = true
}

const confirmBatch = async () => {
  if (batchReady.value.length === 0) return
  processing.value = true
  let success = 0
  let failed = 0
  try {
    for (const tx of batchReady.value) {
      try {
        const counterpart = pickSingleCounterparty(tx)
        const created = tx.transactionType === 'Income'
          ? (await createReceivable({
              projectId: tx.projectId!,
              ...counterpart,
              totalAmount: tx.availableAmount || tx.amount,
              dueDate: toDateOnly(tx.transactionDate),
              description: buildCreateDescription(tx)
            })).data.data
          : (await createPayable({
              projectId: tx.projectId,
              ...counterpart,
              totalAmount: tx.availableAmount || tx.amount,
              dueDate: toDateOnly(tx.transactionDate),
              description: buildCreateDescription(tx)
            })).data.data
        await bindExisting(tx, created.id, tx.availableAmount || tx.amount)
        success += 1
      } catch (error) {
        console.error(error)
        failed += 1
      }
    }
    ElMessage.success(`批量完成：成功 ${success}，跳过 ${batchSkipped.value.length}，失败 ${failed}`)
    batchDialogVisible.value = false
    await loadTransactions()
  } finally {
    processing.value = false
  }
}

watch(processMode, (mode) => {
  if (mode === 'create' && currentTransaction.value) {
    resetCreateForm(currentTransaction.value)
  }
})

onMounted(() => {
  loadTransactions()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 8px 0;
  color: var(--text-primary);
}

.page-desc {
  color: var(--text-placeholder);
  margin: 0;
}

.search-section {
  background: var(--bg-page);
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 16px;
}

.search-form {
  margin: 0;
}

.mx-2 {
  margin: 0 8px;
}

.batch-actions {
  padding: 12px 20px 0;
}

.table-section {
  background: var(--bg-card);
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  overflow: hidden;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}

.dialog-info-box {
  margin-bottom: 16px;
  padding: 16px;
  background: var(--bg-page);
  border-radius: 8px;
}

.dialog-info-label {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.dialog-info-amount {
  font-size: 16px;
  font-weight: 700;
  color: var(--color-primary);
  margin-top: 8px;
}

.dialog-radio-group {
  margin-bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.hint-text {
  color: var(--text-secondary);
  font-size: 13px;
  margin: 8px 0;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}
</style>
