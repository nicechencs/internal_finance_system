<template>
  <el-dialog
    :model-value="visible"
    title="交易详情"
    width="700px"
    @close="handleClose"
  >
    <div v-loading="loading">
      <el-descriptions v-if="transaction" :column="2" border>
        <el-descriptions-item label="交易日期">
          {{ formatDate(transaction.transactionDate) }}
        </el-descriptions-item>
        <el-descriptions-item label="交易类型">
          <TransactionTypeTag
            :transaction-type="transaction.transactionType"
            :transfer-direction="transaction.transferDirection"
            size="default"
          />
        </el-descriptions-item>
        <el-descriptions-item label="金额">
          <span style="font-size: 18px; font-weight: bold; color: var(--color-primary)">
            {{ formatRMB(transaction.amount) }}
          </span>
          <span
            v-if="transaction.availableAmount !== undefined && transaction.availableAmount !== transaction.amount"
            style="margin-left: 8px; font-size: 12px; color: var(--text-secondary)"
          >
            （可用余额：{{ formatRMB(transaction.availableAmount) }}）
          </span>
        </el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getTransactionStatusType(transaction.status)">
            {{ getTransactionStatusText(transaction.status) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="账户">
          <el-link v-if="transaction.accountId" type="primary" @click="goToEntityDetail('AccountDetail', transaction.accountId)">
            {{ transaction.accountName }}
          </el-link>
          <span v-else>{{ transaction.accountName }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="分类">
          <el-link v-if="transaction.categoryId" type="primary" @click="goToCategoryDetail(transaction.categoryId)">
            {{ transaction.categoryName }}
          </el-link>
          <span v-else>{{ transaction.categoryName || '-' }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="项目">
          <el-link v-if="transaction.projectId" type="primary" @click="goToEntityDetail('ProjectDetail', transaction.projectId)">
            {{ transaction.projectName }}
          </el-link>
          <span v-else>{{ transaction.projectName || '-' }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="对方">
          <el-link v-if="transaction.customerId" type="primary" @click="goToEntityDetail('CustomerDetail', transaction.customerId)">
            {{ transaction.customerName }}
          </el-link>
          <el-link v-else-if="transaction.supplierId" type="primary" @click="goToEntityDetail('SupplierDetail', transaction.supplierId)">
            {{ transaction.supplierName }}
          </el-link>
          <el-link v-else-if="transaction.personId" type="primary" @click="goToEntityDetail('PersonDetail', transaction.personId)">
            {{ transaction.personName }}
          </el-link>
          <span v-else>{{ transaction.counterparty || '-' }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="交易时间">
          {{ formatTime(transaction.transactionTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="对方账户">
          {{ transaction.counterpartyAccount || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="对方银行">
          {{ transaction.counterpartyBank || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="流水号">
          {{ transaction.transactionNumber || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="描述" :span="2">
          {{ transaction.description || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="标签" :span="2">
          <TagDisplay :tags="transaction.tags || []" />
        </el-descriptions-item>
        <el-descriptions-item label="创建时间" :span="2">
          {{ formatDateTime(transaction.createdAt) }}
        </el-descriptions-item>
      </el-descriptions>

      <div v-if="transaction && transaction.isAllocated && transaction.allocations.length > 0" class="allocations">
        <el-divider>费用分摊明细</el-divider>
        <el-table :data="transaction.allocations" border class="resizable-table" allow-drag-last-column @header-dragend="handleHeaderDragend">
          <el-table-column prop="projectName" label="项目" :width="getColumnWidth('allocationProjectName', TABLE_COLUMN_WIDTH.project)">
            <template #default="{ row }">
              <el-link v-if="row.projectId" type="primary" @click="goToEntityDetail('ProjectDetail', row.projectId)">
                {{ row.projectName }}
              </el-link>
              <span v-else>{{ row.projectName || '-' }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="personName" label="人员" :width="getColumnWidth('personName', TABLE_COLUMN_WIDTH.contact)">
            <template #default="{ row }">
              <el-link v-if="row.personId" type="primary" @click="goToEntityDetail('PersonDetail', row.personId)">
                {{ row.personName }}
              </el-link>
              <span v-else>{{ row.personName || '-' }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="amount" label="分摊金额" :width="getColumnWidth('allocationAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
            <template #default="{ row }">
              {{ formatRMB(row.amount) }}
            </template>
          </el-table-column>
          <el-table-column prop="allocationRate" label="分摊比例" :width="getColumnWidth('allocationRate', TABLE_COLUMN_WIDTH.status)" align="right">
            <template #default="{ row }">
              {{ row.allocationRate ? `${row.allocationRate}%` : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="description" label="备注" :min-width="getColumnMinWidth('allocationDescription', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        </el-table>
      </div>

      <div v-if="relatedRecords && (relatedRecords.receivables.length > 0 || relatedRecords.payables.length > 0)" class="related-records">
        <el-divider>关联应收应付</el-divider>

        <div v-if="relatedRecords.receivables.length > 0" class="receivables-section">
          <h4>应收记录</h4>
          <el-table :data="relatedRecords.receivables" border class="resizable-table" allow-drag-last-column @header-dragend="handleHeaderDragend">
            <el-table-column prop="projectName" label="项目" :width="getColumnWidth('receivableProjectName', TABLE_COLUMN_WIDTH.project)">
              <template #default="{ row }">
                <el-link v-if="row.projectId" type="primary" @click="goToEntityDetail('ProjectDetail', row.projectId)">
                  {{ row.projectName }}
                </el-link>
                <span v-else>{{ row.projectName || '-' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="customerName" label="客户" :width="getColumnWidth('customerName', TABLE_COLUMN_WIDTH.company)">
              <template #default="{ row }">
                <el-link v-if="row.customerId" type="primary" @click="goToEntityDetail('CustomerDetail', row.customerId)">
                  {{ row.customerName }}
                </el-link>
                <span v-else>{{ row.customerName || '-' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="totalAmount" label="应收总额" :width="getColumnWidth('receivableTotalAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
              <template #default="{ row }">
                {{ formatRMB(row.totalAmount) }}
              </template>
            </el-table-column>
            <el-table-column prop="paymentAmount" label="本次收款" :width="getColumnWidth('receivablePaymentAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
              <template #default="{ row }">
                <span style="color: var(--color-success); font-weight: bold">{{ formatRMB(row.paymentAmount) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="remainingAmount" label="剩余金额" :width="getColumnWidth('receivableRemainingAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
              <template #default="{ row }">
                {{ formatRMB(row.remainingAmount) }}
              </template>
            </el-table-column>
            <el-table-column prop="status" label="状态" :width="getColumnWidth('receivableStatus', TABLE_COLUMN_WIDTH.status)" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusType(row.status)">{{ getStatusText(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column column-key="receivableActions" label="操作" :width="getColumnWidth('receivableActions', TABLE_COLUMN_WIDTH.status)" align="center">
              <template #default="{ row }">
                <el-button type="primary" link @click="goToReceivableDetail(row.id)">查看详情</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <div v-if="relatedRecords.payables.length > 0" class="payables-section">
          <h4>应付记录</h4>
          <el-table :data="relatedRecords.payables" border class="resizable-table" allow-drag-last-column @header-dragend="handleHeaderDragend">
            <el-table-column prop="supplierName" label="供应商" :width="getColumnWidth('supplierName', TABLE_COLUMN_WIDTH.company)">
              <template #default="{ row }">
                <el-link v-if="row.supplierId" type="primary" @click="goToEntityDetail('SupplierDetail', row.supplierId)">
                  {{ row.supplierName }}
                </el-link>
                <span v-else>{{ row.supplierName || '-' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="projectName" label="项目" :width="getColumnWidth('payableProjectName', TABLE_COLUMN_WIDTH.project)">
              <template #default="{ row }">
                <el-link v-if="row.projectId" type="primary" @click="goToEntityDetail('ProjectDetail', row.projectId)">
                  {{ row.projectName }}
                </el-link>
                <span v-else>{{ row.projectName || '-' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="totalAmount" label="应付总额" :width="getColumnWidth('payableTotalAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
              <template #default="{ row }">
                {{ formatRMB(row.totalAmount) }}
              </template>
            </el-table-column>
            <el-table-column prop="paymentAmount" label="本次付款" :width="getColumnWidth('payablePaymentAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
              <template #default="{ row }">
                <span style="color: var(--color-danger); font-weight: bold">{{ formatRMB(row.paymentAmount) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="remainingAmount" label="剩余金额" :width="getColumnWidth('payableRemainingAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
              <template #default="{ row }">
                {{ formatRMB(row.remainingAmount) }}
              </template>
            </el-table-column>
            <el-table-column prop="status" label="状态" :width="getColumnWidth('payableStatus', TABLE_COLUMN_WIDTH.status)" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusType(row.status)">{{ getStatusText(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column column-key="payableActions" label="操作" :width="getColumnWidth('payableActions', TABLE_COLUMN_WIDTH.status)" align="center">
              <template #default="{ row }">
                <el-button type="primary" link @click="goToPayableDetail(row.id)">查看详情</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">关闭</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { getTransactionById, getRelatedFinanceRecords } from '@/features/transactions/api/transaction'
import type { Transaction, RelatedFinanceRecord } from '@/features/transactions/types/transaction'
import { useRouter } from 'vue-router'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { formatRMB, formatDateTime } from '@/shared/utils/formatters'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import {
  TRANSACTION_STATUS_OPTIONS,
  getEnumLabel,
  getEnumTagType
} from '@/shared/constants/enums'

interface Props {
  visible: boolean
  transactionId: number
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible'])
const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('transaction-detail')

const loading = ref(false)
const transaction = ref<Transaction | null>(null)
const relatedRecords = ref<RelatedFinanceRecord | null>(null)

watch(() => props.visible, async (val) => {
  if (val && props.transactionId) {
    await loadTransaction()
    await loadRelatedRecords()
  }
})

const loadTransaction = async () => {
  loading.value = true
  try {
    const { data } = await getTransactionById(props.transactionId)
    transaction.value = data.data
  } catch (error) {
    ElMessage.error('加载交易详情失败')
  } finally {
    loading.value = false
  }
}

const loadRelatedRecords = async () => {
  try {
    const { data } = await getRelatedFinanceRecords(props.transactionId)
    relatedRecords.value = data.data
  } catch (error) {
    console.error('加载关联应收应付记录失败', error)
  }
}

const handleClose = () => {
  emit('update:visible', false)
}

const formatDate = (date: string) => formatDateTime(date, 'date')

const formatTime = (value?: string) => {
  if (!value) return '-'
  const parts = value.split(':')
  if (parts.length >= 2) return `${parts[0]}:${parts[1]}${parts[2] ? ':' + parts[2].split('.')[0] : ''}`
  return value
}

const getTransactionStatusType = (status: string) => getEnumTagType(TRANSACTION_STATUS_OPTIONS, status) || 'info'

const getTransactionStatusText = (status: string) => getEnumLabel(TRANSACTION_STATUS_OPTIONS, status)

const getStatusType = (status: string) => {
  const statusMap: Record<string, 'success' | 'warning' | 'danger' | 'info'> = {
    'pending': 'warning',
    'partial': 'info',
    'settled': 'success'
  }
  return statusMap[status] || 'info'
}

const getStatusText = (status: string) => {
  const statusTextMap: Record<string, string> = {
    'pending': '待处理',
    'partial': '部分结算',
    'settled': '已结清'
  }
  return statusTextMap[status] || status
}

const goToEntityDetail = (routeName: string, id: number) => {
  handleClose()
  router.push({ name: routeName, params: { id } })
}

const goToCategoryDetail = (id: number) => {
  handleClose()
  router.push({ name: 'Transactions', query: { categoryId: String(id) } })
}

const goToReceivableDetail = (id: number) => {
  handleClose()
  router.push(`/receivables/${id}`)
}

const goToPayableDetail = (id: number) => {
  handleClose()
  router.push(`/payables/${id}`)
}
</script>

<style scoped>
.allocations {
  margin-top: 20px;
}

.related-records {
  margin-top: 20px;
}

.receivables-section,
.payables-section {
  margin-top: 15px;
}

.receivables-section h4,
.payables-section h4 {
  margin: 10px 0;
  font-size: 14px;
  color: var(--text-regular);
}
</style>
