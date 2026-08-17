<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">账户管理</h2>
        <p class="page-desc">管理公司银行账户、活期资金账户和定期账户主档</p>
      </div>
      <div class="page-header-right">
        <el-button v-if="userStore.canEdit" type="warning" @click="batchLinkVisible = true">
          <el-icon><Link /></el-icon> 批量智能关联
        </el-button>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">新增账户</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="CreditCard"
          :value="String(statistics.totalCount)"
          label="总账户数"
          :count="`${statistics.activeCount} 个活跃`"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Check"
          :value="String(statistics.activeCount)"
          label="活跃账户"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Wallet"
          :value="formatAmount(statistics.totalBalance)"
          label="总余额"
          theme="balance"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Clock"
          :value="String(statistics.fixedDepositCount)"
          label="定期账户"
          theme="profit"
        />
      </el-col>
    </el-row>

    <!-- 搜索区域 -->
    <div class="search-section">
      <el-form :inline="true" :model="searchForm" @submit.prevent="loadData">
        <el-form-item label="账户名称">
          <SearchableFilterInput
            v-model="searchForm.name"
            :fetch-options="getActiveAccounts"
            placeholder="请输入或选择账户名称"
            clearable
          />
        </el-form-item>
        <el-form-item label="账户类型">
          <el-select v-model="searchForm.accountType" placeholder="全部类型" clearable style="width: 150px">
            <el-option label="银行账户" value="Bank" />
            <el-option label="支付宝" value="Alipay" />
            <el-option label="定期账户" value="FixedDeposit" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.isActive" placeholder="全部状态" clearable style="width: 120px">
            <el-option label="启用" :value="true" />
            <el-option label="禁用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadData">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- 数据表格区域 -->
    <div class="table-section">
      <el-table :data="tableData" v-loading="loading" row-key="id" class="resizable-table" border allow-drag-last-column @header-dragend="handleHeaderDragend" @sort-change="handleSortChange">
        <el-table-column prop="name" sortable="custom" :min-width="getColumnMinWidth('name', TABLE_COLUMN_WIDTH.name)">
          <template #header><span>账户名称 <span style="color: var(--color-danger)">*</span></span></template>
          <template #default="{ row }">
            <el-button link type="primary" @click="router.push(`/accounts/${row.id}`)">{{ row.name }}</el-button>
          </template>
        </el-table-column>
        <el-table-column prop="accountType" sortable="custom" :width="getColumnWidth('accountType', TABLE_COLUMN_WIDTH.shortText)">
          <template #header><span>账户类型 <span style="color: var(--color-danger)">*</span></span></template>
          <template #default="{ row }">
            <el-tag :type="getAccountTypeTag(row.accountType)" size="small">
              {{ getAccountTypeLabel(row.accountType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="balance" label="余额 (CNY)" sortable="custom" :width="getColumnWidth('balance', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            <span>{{ formatAmount(getDisplayBalance(row)) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="currency" label="币种" :width="getColumnWidth('currency', TABLE_COLUMN_WIDTH.shortText)" v-if="false">
          <template #default="{ row }">
            <span>{{ row.currency || 'CNY' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="bankName" label="银行名称" :width="getColumnWidth('bankName', TABLE_COLUMN_WIDTH.bank)">
          <template #default="{ row }">
            <span>{{ row.bankName || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="accountNumber" label="账号" :width="getColumnWidth('accountNumber', TABLE_COLUMN_WIDTH.bankAccount)">
          <template #default="{ row }">
            <span>{{ row.accountNumber || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" :width="getColumnWidth('isActive', TABLE_COLUMN_WIDTH.status)">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionThree)" fixed="right">
          <template #default="{ row }">
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button v-if="userStore.canDelete" link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
        class="pagination"
      />
    </div>

    <AccountForm
      v-model:visible="formVisible"
      :account="currentAccount"
      @success="handleFormSuccess"
    />

    <BatchLinkDialog
      v-model="batchLinkVisible"
      @success="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CreditCard, Check, Wallet, Clock, Link } from '@element-plus/icons-vue'
import type { Account, AccountStatistics } from '@/features/master-data/accounts/types/account'
import { getAccounts, getActiveAccounts, deleteAccount, getAccountStatistics } from '@/features/master-data/accounts/api/account'
import { useUserStore } from '@/features/auth/stores/user'
import SearchableFilterInput from '@/shared/ui/SearchableFilterInput.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import AccountForm from '@/features/master-data/accounts/components/AccountForm.vue'
import BatchLinkDialog from '@/shared/ui/BatchLinkDialog.vue'
import { formatMoney } from '@/shared/utils/formatters'

const router = useRouter()
type CreatedAccountSuccess = {
  accountId: number
  accountType: string
  name: string
}
const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('account-list')
const loading = ref(false)
const tableData = ref<any[]>([])
const formVisible = ref(false)
const batchLinkVisible = ref(false)
const currentAccount = ref<Account | null>(null)

const statistics = ref<AccountStatistics>({
  totalCount: 0,
  activeCount: 0,
  totalBalance: 0,
  fixedDepositCount: 0
})

const searchForm = reactive({
  name: '',
  accountType: '' as string,
  isActive: '' as string | boolean
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

const sortState = reactive({
  sortBy: '',
  sortOrder: '' as '' | 'asc' | 'desc'
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadData()
}

const getAccountTypeLabel = (type: string) => {
  const map: Record<string, string> = {
    'Bank': '银行',
    'Alipay': '支付宝',
    'FixedDeposit': '定期账户'
  }
  return map[type] || type
}

const getAccountTypeTag = (type: string) => {
  const map: Record<string, string | undefined> = {
    'Bank': undefined,
    'Alipay': 'success',
    'FixedDeposit': 'warning'
  }
  return map[type]
}

const getDisplayBalance = (row: Partial<Account> & { balance?: number }) => {
  return row.currentBalance ?? row.balance ?? row.openingBalance ?? 0
}

const handleCreate = () => {
  currentAccount.value = null
  formVisible.value = true
}

const handleEdit = (row: Account) => {
  currentAccount.value = row
  formVisible.value = true
}

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      name: searchForm.name || undefined,
      accountType: searchForm.accountType || undefined,
      isActive: searchForm.isActive === '' ? undefined : searchForm.isActive
    }
    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }
    const response = await getAccounts(params)
    tableData.value = response.data.data.items
    pagination.total = response.data.data.total
  } catch (error) {
    console.error('加载数据失败:', error)
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

const handleDelete = async (row: Account) => {
  try {
    await ElMessageBox.confirm('确定要删除该账户吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await deleteAccount(row.id)
    ElMessage.success('删除成功')
    loadData()
    loadStatistics()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const handleSizeChange = () => {
  pagination.page = 1
  loadData()
}

const handlePageChange = () => {
  loadData()
}

const handleReset = () => {
  searchForm.name = ''
  searchForm.accountType = ''
  searchForm.isActive = ''
  pagination.page = 1
  loadData()
}

const handleFormSuccess = (payload?: CreatedAccountSuccess) => {
  formVisible.value = false
  currentAccount.value = null
  loadData()
  loadStatistics()
  if (payload?.accountType === 'FixedDeposit') {
    void promptFixedDepositOnboarding(payload)
  }
}

const navigateToFixedDepositCreation = async (accountId: number) => {
  const query = { accountId: String(accountId), action: 'create' }
  if (router.hasRoute('FixedDeposits')) {
    await router.push({ name: 'FixedDeposits', query })
    return
  }
  await router.push({ name: 'AccountDetail', params: { id: String(accountId) } })
}

const promptFixedDepositOnboarding = async (account: CreatedAccountSuccess) => {
  try {
    await ElMessageBox.confirm(
      `定期账户「${account.name}」创建成功，是否立即登记第一笔定期记录？`,
      '下一步建议',
      {
        confirmButtonText: '立即登记',
        cancelButtonText: '稍后再说',
        type: 'info'
      }
    )
    await navigateToFixedDepositCreation(account.accountId)
  } catch (error) {
    if (error !== 'cancel') {
      console.error('提示定期记录失败:', error)
    }
  }
}

const loadStatistics = async () => {
  try {
    const { data } = await getAccountStatistics()
    statistics.value = data.data
  } catch (error) {
    console.error('加载统计数据失败:', error)
  }
}

const formatAmount = (amount: number | null | undefined) => {
  return formatMoney(Number(amount ?? 0))
}

onMounted(() => {
  loadStatistics()
  loadData()
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

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.page-desc {
  font-size: 13px;
  color: var(--text-placeholder);
  margin: 4px 0 0 0;
}

.stat-cards {
  margin-bottom: 24px;
}

.search-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 20px 4px 20px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.table-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  overflow: hidden;
}

/* 表格自定义样式 */
.table-section :deep(.el-table) {
  font-size: 13px;
}

.table-section :deep(.el-table th.el-table__cell) {
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.table-section :deep(.el-table td.el-table__cell) {
  padding: 12px 0;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}

/* 新增按钮样式 */
.page-header :deep(.el-button--primary) {
  border-radius: 8px;
  padding: 10px 20px;
}

/* 搜索表单样式 */
.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section :deep(.el-input__wrapper),
.search-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}

/* 到期提醒样式 */
.maturity-warning {
  color: var(--color-warning);
  font-weight: 600;
}

.maturity-expired {
  color: var(--color-danger);
  font-weight: 600;
}
</style>
