<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">流水导入</h2>
        <p class="page-desc">导入银行流水数据（支持华夏银行、企业支付宝、通用Excel格式）</p>
      </div>
    </div>

    <!-- Upload Section -->
    <div class="content-card upload-section">
      <div class="content-card-header">
        <span class="content-card-title">银行流水导入</span>
      </div>

      <el-form :model="uploadForm" label-width="100px">
        <el-form-item label="选择账户" required>
          <SearchableSelect
            v-model="uploadForm.accountId"
            :options="accounts"
            entity-name="账户"
            placeholder="输入关键字搜索目标账户"
            width="300px"
            :clearable="false"
          />
        </el-form-item>

        <el-form-item label="上传文件" required>
          <el-upload
            ref="uploadRef"
            :auto-upload="false"
            :limit="1"
            :on-change="handleFileChange"
            :on-exceed="handleExceed"
            accept=".xlsx"
            drag
          >
            <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
            <div class="el-upload__text">
              将文件拖到此处，或 <em>点击上传</em>
            </div>
            <template #tip>
              <div class="el-upload__tip">
                仅支持 .xlsx 格式（Excel 2007 及以上版本），如有 .xls 或 .xml 文件请先转换为 .xlsx，文件大小不超过 10MB
              </div>
              <div class="el-upload__tip">
                自动识别格式：华夏银行交易明细、企业支付宝账务查询、通用Excel（日期/金额/对方/摘要）
              </div>
            </template>
          </el-upload>
        </el-form-item>

        <el-form-item>
          <el-collapse style="width: 100%">
            <el-collapse-item title="查看 Excel 格式示例" name="example">
              <div class="example-table-container">
                <table class="example-table">
                  <thead>
                    <tr>
                      <th>A列：日期</th>
                      <th>B列：金额</th>
                      <th>C列：对方名称</th>
                      <th>D列：摘要</th>
                      <th>E列：对方账号（可选）</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>2024-01-15</td>
                      <td class="amount-in">5000.00</td>
                      <td>张三公司</td>
                      <td>项目款收入</td>
                      <td>6222021234567890</td>
                    </tr>
                    <tr>
                      <td>2024-01-16</td>
                      <td class="amount-out">-1200.50</td>
                      <td>李四供应商</td>
                      <td>采购办公用品</td>
                      <td>6228481234567890</td>
                    </tr>
                    <tr>
                      <td>2024-01-17</td>
                      <td class="amount-in">8000.00</td>
                      <td>王五客户</td>
                      <td>服务费收入</td>
                      <td></td>
                    </tr>
                  </tbody>
                </table>
                <div class="example-notes">
                  <p><strong>格式说明：</strong></p>
                  <ul>
                    <li>日期格式：YYYY-MM-DD（如 2024-01-15）或 Excel 日期格式</li>
                    <li>金额：正数表示收入，负数表示支出（如 5000.00 或 -1200.50）</li>
                    <li>对方名称：必填，交易对方的名称</li>
                    <li>摘要：必填，交易的简要说明</li>
                    <li>对方账号：可选，银行账号或其他标识</li>
                  </ul>
                </div>
              </div>
            </el-collapse-item>
          </el-collapse>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            @click="handlePreview"
            :loading="previewLoading"
            :disabled="!canPreview"
          >
            解析预览
          </el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- Preview Section -->
    <div v-if="previewData" class="content-card">
      <div class="content-card-header">
        <span class="content-card-title">解析预览 - {{ previewData.fileName }}</span>
        <div class="preview-stats">
          <el-tag :type="getFormatTagType(previewData.detectedFormat)">{{ getFormatLabel(previewData.detectedFormat) }}</el-tag>
          <el-tag type="info" style="margin-left: 8px">总计: {{ previewData.totalRows }} 条</el-tag>
          <el-tag type="success" style="margin-left: 8px">新数据: {{ previewData.newRows }} 条</el-tag>
          <el-tag type="warning" style="margin-left: 8px">数据库重复: {{ previewData.duplicateRows }} 条</el-tag>
          <el-tag v-if="previewData.fileConflictRows > 0" type="danger" style="margin-left: 8px">文件内冲突: {{ previewData.fileConflictRows }} 条</el-tag>
          <el-tag v-if="previewData.recoverableRows > 0" type="primary" style="margin-left: 8px">可恢复: {{ previewData.recoverableRows }} 条</el-tag>
        </div>
      </div>

      <el-table
        ref="tableRef"
        :data="previewData.previews"
        style="width: 100%"
        :row-class-name="getRowClassName" class="resizable-table" border allow-drag-last-column @header-dragend="previewTableColumns.handleHeaderDragend"
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" :width="previewTableColumns.getColumnWidth('selection', TABLE_COLUMN_WIDTH.selection)" :selectable="isRowSelectable" />
        <el-table-column prop="rowNumber" label="行号" :width="previewTableColumns.getColumnWidth('rowNumber', TABLE_COLUMN_WIDTH.index)" />
        <el-table-column prop="transactionDate" label="日期" :width="previewTableColumns.getColumnWidth('transactionDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            {{ formatDate(row.transactionDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="amount" label="金额" :width="previewTableColumns.getColumnWidth('amount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            <span :class="row.direction === 'in' ? 'amount-in' : 'amount-out'">
              {{ row.direction === 'in' ? '+' : '-' }}{{ formatAmount(row.amount) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="direction" label="方向" :width="previewTableColumns.getColumnWidth('direction', TABLE_COLUMN_WIDTH.type)" align="center">
          <template #default="{ row }">
            <el-tag :type="row.direction === 'in' ? 'success' : 'danger'" size="small">
              {{ row.direction === 'in' ? '收入' : '支出' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="counterpartyName" label="对方名称" :min-width="previewTableColumns.getColumnMinWidth('counterpartyName', TABLE_COLUMN_WIDTH.company)" show-overflow-tooltip />
        <el-table-column prop="description" label="交易描述" :min-width="previewTableColumns.getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        <el-table-column prop="memo" label="摘要" :min-width="previewTableColumns.getColumnMinWidth('memo', 140)" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.memo || '-' }}
          </template>
        </el-table-column>
        <el-table-column v-if="isExtendedFormat" prop="transactionTime" label="交易时间" :width="previewTableColumns.getColumnWidth('transactionTime', 100)">
          <template #default="{ row }">
            {{ row.transactionTime ? formatTime(row.transactionTime) : '-' }}
          </template>
        </el-table-column>
        <el-table-column v-if="isExtendedFormat" prop="balance" label="余额" :width="previewTableColumns.getColumnWidth('balance', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            {{ row.balance != null ? formatAmount(row.balance) : '-' }}
          </template>
        </el-table-column>
        <el-table-column v-if="isExtendedFormat" prop="counterpartyBank" label="对方银行" :width="previewTableColumns.getColumnWidth('counterpartyBank', 140)" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.counterpartyBank || '-' }}
          </template>
        </el-table-column>
        <el-table-column v-if="isExtendedFormat" prop="transactionNumber" label="流水号" :width="previewTableColumns.getColumnWidth('transactionNumber', 160)" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.transactionNumber || '-' }}
          </template>
        </el-table-column>
        <el-table-column column-key="matchedCategory" label="匹配分类" :width="previewTableColumns.getColumnWidth('matchedCategory', TABLE_COLUMN_WIDTH.project)">
          <template #default="{ row }">
            <el-link
              v-if="row.matchedCategoryId && row.matchedCategoryName"
              class="matched-category"
              type="primary"
              @click="router.push({ name: 'Transactions', query: { categoryId: String(row.matchedCategoryId) } })"
            >
              {{ row.matchedCategoryName }}
            </el-link>
            <span v-else-if="row.matchedCategoryName" class="matched-category">
              {{ row.matchedCategoryName }}
            </span>
            <span v-else class="no-category">未匹配</span>
          </template>
        </el-table-column>
        <el-table-column column-key="status" label="状态" :width="previewTableColumns.getColumnWidth('status', TABLE_COLUMN_WIDTH.status)" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isFileConflict" type="danger" size="small">文件内冲突</el-tag>
            <el-tag v-else-if="row.isDuplicate" type="warning" size="small">数据库重复</el-tag>
            <el-tag v-else-if="row.isRecoverable" type="primary" size="small">可恢复</el-tag>
            <el-tag v-else type="success" size="small">新数据</el-tag>
          </template>
        </el-table-column>
      </el-table>

      <div class="import-actions">
        <div>
          <span>已选择 <strong>{{ selectedRows.length }}</strong> 条数据</span>
          <el-button link type="primary" @click="selectAllNew" style="margin-left: 12px">全选新数据</el-button>
        </div>
        <div>
          <el-button @click="handleCancelPreview">取消</el-button>
          <el-button
            type="primary"
            @click="handleConfirmImport"
            :loading="confirmLoading"
            :disabled="selectedRows.length === 0"
          >
            确认导入 ({{ selectedRows.length }} 条)
          </el-button>
        </div>
      </div>
    </div>

    <!-- Import History Section -->
    <div class="content-card">
      <div class="content-card-header">
        <span class="content-card-title">导入历史</span>
        <el-button @click="handleSearch" :loading="batchLoading" size="small">刷新</el-button>
      </div>

      <!-- 筛选栏 -->
      <div class="search-section">
        <el-form :inline="true" :model="batchFilter" class="search-form" @submit.prevent="handleSearch">
          <el-form-item label="账户">
            <el-select
              v-model="batchFilter.accountId"
              placeholder="全部"
              clearable
              style="width: 160px"
            >
              <el-option
                v-for="item in accounts"
                :key="item.id"
                :label="item.name"
                :value="item.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="导入日期">
            <el-date-picker
              v-model="batchFilter.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              :shortcuts="dateRangeShortcuts"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select
              v-model="batchFilter.status"
              placeholder="全部"
              clearable
              style="width: 120px"
            >
              <el-option label="待处理" value="Pending" />
              <el-option label="处理中" value="Processing" />
              <el-option label="已完成" value="Completed" />
              <el-option label="部分完成" value="PartialCompleted" />
              <el-option label="失败" value="Failed" />
            </el-select>
          </el-form-item>
          <el-form-item label="文件名">
            <el-input
              v-model="batchFilter.fileName"
              placeholder="搜索文件名"
              clearable
              style="width: 180px"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
          </el-form-item>
          <el-form-item class="search-buttons">
            <el-button type="primary" native-type="submit">查询</el-button>
            <el-button @click="handleResetFilter">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table :data="batches" style="width: 100%" v-loading="batchLoading" class="resizable-table" border allow-drag-last-column @header-dragend="batchTableColumns.handleHeaderDragend">
        <el-table-column prop="id" label="批次ID" :width="batchTableColumns.getColumnWidth('id', TABLE_COLUMN_WIDTH.index)" />
        <el-table-column prop="fileName" label="文件名" :min-width="batchTableColumns.getColumnMinWidth('fileName', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        <el-table-column prop="accountName" label="目标账户" :width="batchTableColumns.getColumnWidth('accountName', TABLE_COLUMN_WIDTH.account)">
          <template #default="{ row }">
            <el-link v-if="row.accountId" type="primary" @click="router.push({ name: 'AccountDetail', params: { id: row.accountId } })">
              {{ row.accountName }}
            </el-link>
            <span v-else>{{ row.accountName || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalCount" label="总记录数" :width="batchTableColumns.getColumnWidth('totalCount', TABLE_COLUMN_WIDTH.status)" align="center" />
        <el-table-column prop="successCount" label="成功" :width="batchTableColumns.getColumnWidth('successCount', TABLE_COLUMN_WIDTH.type)" align="center">
          <template #default="{ row }">
            <span class="amount-in">{{ row.successCount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="duplicateCount" label="重复" :width="batchTableColumns.getColumnWidth('duplicateCount', TABLE_COLUMN_WIDTH.type)" align="center">
          <template #default="{ row }">
            <span style="color: var(--color-warning)">{{ row.duplicateCount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="errorCount" label="失败" :width="batchTableColumns.getColumnWidth('errorCount', TABLE_COLUMN_WIDTH.type)" align="center">
          <template #default="{ row }">
            <el-tooltip v-if="row.errorCount > 0 && row.errorMessage" :content="row.errorMessage" placement="top" :show-after="300">
              <span style="color: #EF4444; font-weight: 600; cursor: help; border-bottom: 1px dashed #EF4444">{{ row.errorCount }}</span>
            </el-tooltip>
            <span v-else-if="row.errorCount > 0" style="color: #EF4444; font-weight: 600">{{ row.errorCount }}</span>
            <span v-else>0</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" :width="batchTableColumns.getColumnWidth('status', TABLE_COLUMN_WIDTH.status)" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="导入时间" :width="batchTableColumns.getColumnWidth('createdAt', TABLE_COLUMN_WIDTH.dateTime)">
          <template #default="{ row }">
            {{ formatDateTime(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" :width="batchTableColumns.getColumnWidth('actions', 140)" align="center" fixed="right">
          <template #default="{ row }">
            <template v-if="row.status === 'Pending' || row.status === 'Failed'">
              <el-button
                v-if="row.status === 'Pending'"
                type="primary"
                link
                size="small"
                :loading="resumeLoadingId === row.id"
                @click="handleResumeBatch(row)"
              >
                继续处理
              </el-button>
              <el-button
                type="danger"
                link
                size="small"
                :loading="deleteLoadingId === row.id"
                @click="handleDeleteBatch(row)"
              >
                删除
              </el-button>
            </template>
            <span v-else class="no-action">-</span>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="batchPagination.page"
        v-model:page-size="batchPagination.pageSize"
        :total="batchPagination.total"
        :page-sizes="[10, 20, 50]"
        layout="total, sizes, prev, pager, next"
        @size-change="loadBatches"
        @current-change="loadBatches"
        class="pagination"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadFile, UploadInstance } from 'element-plus'
import { UploadFilled, Search } from '@element-plus/icons-vue'
import { previewImport, confirmImport, getImportBatches, deleteImportBatch, getImportBatchPreview } from '@/features/import/api/import'
import type { ImportBatchQuery } from '@/features/import/api/import'
import { getActiveAccounts } from '@/features/master-data/accounts/api/account'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import type { Account } from '@/features/master-data/accounts/types/account'
import type { ImportPreviewResponse, BankTransactionPreview, ImportBatch } from '@/features/import/types/import'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'
import { formatDateTime as formatDateTimeUtil } from '@/shared/utils/formatters'
import { ApiError } from '@/shared/types/error'

const router = useRouter()

// Upload form
const uploadForm = reactive({
  accountId: null as number | null,
  file: null as File | null
})

const uploadRef = ref<UploadInstance>()
const tableRef = ref()
const accounts = ref<Account[]>([])
const previewLoading = ref(false)
const confirmLoading = ref(false)
const previewData = ref<ImportPreviewResponse | null>(null)
const selectedRows = ref<BankTransactionPreview[]>([])
const previewTableColumns = useResizableTableColumns('import-preview')
const batchTableColumns = useResizableTableColumns('import-batches')
const resumeLoadingId = ref<number | null>(null)
const deleteLoadingId = ref<number | null>(null)

// Batch history
const batchLoading = ref(false)
const batches = ref<ImportBatch[]>([])
const batchPagination = reactive({
  page: 1,
  pageSize: 10,
  total: 0
})
const batchFilter = reactive({
  accountId: null as number | null,
  dateRange: null as [string, string] | null,
  status: '' as string,
  fileName: '' as string
})

const canPreview = computed(() => {
  return uploadForm.accountId !== null && uploadForm.file !== null
})

const isExtendedFormat = computed(() => {
  return previewData.value != null && previewData.value.detectedFormat !== 'Simple'
})

// Load active accounts
const loadAccounts = async () => {
  try {
    const response = await getActiveAccounts()
    accounts.value = response.data.data
  } catch (error) {
    console.error('Failed to load accounts:', error)
  }
}

// Handle file selection
const handleFileChange = (uploadFile: UploadFile) => {
  if (uploadFile.raw) {
    uploadForm.file = uploadFile.raw
  }
}

const handleExceed = () => {
  ElMessage.warning('只能上传一个文件，请先移除已选文件')
}

const getImportErrorMessage = (error: unknown, fallback: string) => {
  if (error === 'cancel') {
    return ''
  }

  if (error instanceof ApiError) {
    return error.errors?.[0] || error.message || fallback
  }

  if (error instanceof Error) {
    return error.message || fallback
  }

  if (typeof error === 'object' && error !== null) {
    const responseMessage = (error as {
      response?: {
        data?: {
          message?: string
        }
      }
    }).response?.data?.message

    if (responseMessage) {
      return responseMessage
    }
  }

  return fallback
}

// Preview upload
const handlePreview = async () => {
  if (!uploadForm.accountId) {
    ElMessage.warning('请选择目标账户')
    return
  }
  if (!uploadForm.file) {
    ElMessage.warning('请选择要上传的 Excel 文件')
    return
  }

  previewLoading.value = true
  try {
    const formData = new FormData()
    formData.append('file', uploadForm.file)
    formData.append('accountId', String(uploadForm.accountId))

    const response = await previewImport(formData)
    previewData.value = response.data.data

    // Auto-select all new (non-duplicate) rows after table renders
    await nextTick()
    selectAllNew()

    ElMessage.success(`解析完成，共 ${previewData.value.totalRows} 条记录`)
  } catch (error) {
    console.error('Preview failed:', error)
    ElMessage.error(getImportErrorMessage(error, '解析预览失败，请重试'))
  } finally {
    previewLoading.value = false
  }
}

// Selection helpers
const isRowSelectable = (row: BankTransactionPreview) => {
  // 文件内冲突和数据库重复不可选，可恢复和新数据可选
  return !row.isDuplicate && !row.isFileConflict
}

const handleSelectionChange = (selection: BankTransactionPreview[]) => {
  selectedRows.value = selection
}

const selectAllNew = () => {
  if (!previewData.value || !tableRef.value) return
  const selectableRows = previewData.value.previews.filter(r => !r.isDuplicate && !r.isFileConflict)
  selectableRows.forEach(row => {
    tableRef.value.toggleRowSelection(row, true)
  })
}

// Cancel preview
const handleCancelPreview = () => {
  previewData.value = null
  selectedRows.value = []
  uploadForm.file = null
  uploadRef.value?.clearFiles()
}

// Confirm import
const handleConfirmImport = async () => {
  if (!previewData.value) return

  // 阻断文件内冲突
  if (previewData.value.fileConflictRows > 0) {
    ElMessageBox.alert(
      `文件中存在 ${previewData.value.fileConflictRows} 条记录的 hash 冲突，请检查文件内容后重新上传。`,
      '文件内冲突',
      {
        confirmButtonText: '知道了',
        type: 'error'
      }
    )
    return
  }

  try {
    await ElMessageBox.confirm(
      `确定要导入选中的 ${selectedRows.value.length} 条数据吗？`,
      '确认导入',
      {
        confirmButtonText: '确定导入',
        cancelButtonText: '取消',
        type: 'info'
      }
    )

    confirmLoading.value = true
    const selectedRowNumbers = selectedRows.value.map(r => r.rowNumber)

    const response = await confirmImport({
      batchId: previewData.value.batchId,
      selectedRowNumbers
    })

    const result = response.data.data
    const successMsg = `导入完成！成功 ${result.successCount} 条，重复 ${result.duplicateCount} 条`
    const errorMsg = result.errorCount > 0 ? `，失败 ${result.errorCount} 条` : ''
    if (result.errorCount > 0) {
      ElMessage.warning(successMsg + errorMsg)
      // 显示详细错误信息
      if (result.errorMessage) {
        const escapeHtml = (str: string) => str
          .replace(/&/g, '&amp;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;')
          .replace(/"/g, '&quot;')
        ElMessageBox.alert('', `导入失败详情（${result.errorCount} 条）`, {
          confirmButtonText: '知道了',
          type: 'warning',
          dangerouslyUseHTMLString: true,
          message: `<pre style="white-space: pre-wrap; word-wrap: break-word; margin: 0; font-family: inherit; max-height: 400px; overflow-y: auto;">${escapeHtml(result.errorMessage)}</pre>`
        })
      }
    } else {
      ElMessage.success(successMsg)
    }

    // 先加载列表再清除预览，避免列表短暂消失
    await loadBatches()
    handleCancelPreview()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Import failed:', error)
      ElMessage.error(getImportErrorMessage(error, '导入失败，请重试'))
    }
  } finally {
    confirmLoading.value = false
  }
}

// Load import batches
const loadBatches = async () => {
  batchLoading.value = true
  try {
    const params: ImportBatchQuery = {
      page: batchPagination.page,
      pageSize: batchPagination.pageSize
    }
    if (batchFilter.accountId) {
      params.accountId = batchFilter.accountId
    }
    if (batchFilter.dateRange && batchFilter.dateRange.length === 2) {
      params.startDate = batchFilter.dateRange[0]
      params.endDate = batchFilter.dateRange[1]
    }
    if (batchFilter.status) {
      params.status = batchFilter.status
    }
    if (batchFilter.fileName) {
      params.fileName = batchFilter.fileName
    }
    const response = await getImportBatches(params)
    batches.value = response.data.data.items
    batchPagination.total = response.data.data.total
  } catch (error) {
    console.error('Failed to load batches:', error)
    ElMessage.error(getImportErrorMessage(error, '加载导入批次失败，请重试'))
  } finally {
    batchLoading.value = false
  }
}

// 筛选相关
const handleSearch = () => {
  batchPagination.page = 1
  loadBatches()
}

const handleResetFilter = () => {
  batchFilter.accountId = null
  batchFilter.dateRange = null
  batchFilter.status = ''
  batchFilter.fileName = ''
  handleSearch()
}

// Delete batch
const handleDeleteBatch = async (row: ImportBatch) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除批次 "${row.fileName}" 吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    deleteLoadingId.value = row.id
    await deleteImportBatch(row.id)
    ElMessage.success('删除成功')
    loadBatches()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Delete batch failed:', error)
      ElMessage.error('删除失败，请重试')
    }
  } finally {
    deleteLoadingId.value = null
  }
}

// Resume pending batch
const handleResumeBatch = async (row: ImportBatch) => {
  resumeLoadingId.value = row.id
  try {
    const response = await getImportBatchPreview(row.id)
    const data = response.data

    // 恢复预览界面
    previewData.value = data.data
    uploadForm.accountId = row.accountId

    await nextTick()
    selectAllNew()

    ElMessage.success(`已恢复预览，共 ${data.data.totalRows} 条记录`)
  } catch (error: any) {
    const msg = error?.response?.data?.message || ''
    if (msg.includes('过期')) {
      // 缓存已过期，提示删除
      ElMessageBox.confirm(
        '预览数据已过期，请删除此批次后重新上传文件。是否立即删除？',
        '缓存已过期',
        {
          confirmButtonText: '删除批次',
          cancelButtonText: '取消',
          type: 'warning'
        }
      ).then(async () => {
        await deleteImportBatch(row.id)
        ElMessage.success('已删除，请重新上传文件')
        loadBatches()
      }).catch(() => {})
    } else {
      ElMessage.error(msg || '恢复预览失败，请重试')
    }
  } finally {
    resumeLoadingId.value = null
  }
}

// Format helpers
const formatDate = (dateStr: string) => {
  return formatDateTimeUtil(dateStr, 'date')
}

const formatDateTime = (dateStr: string) => formatDateTimeUtil(dateStr)

const formatAmount = (amount: number) => {
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const getRowClassName = ({ row }: { row: BankTransactionPreview }) => {
  if (row.isFileConflict) return 'file-conflict-row'
  if (row.isDuplicate) return 'duplicate-row'
  if (row.isRecoverable) return 'recoverable-row'
  return ''
}

const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    Pending: 'info',
    Processing: 'warning',
    Completed: 'success',
    PartialCompleted: 'warning',
    Failed: 'danger'
  }
  return (types[status] || 'info') as 'info' | 'warning' | 'success' | 'danger'
}

const getStatusLabel = (status: string) => {
  const labels: Record<string, string> = {
    Pending: '待处理',
    Processing: '处理中',
    Completed: '已完成',
    PartialCompleted: '部分完成',
    Failed: '失败'
  }
  return labels[status] || status
}

const getFormatLabel = (format: string) => {
  const labels: Record<string, string> = {
    Simple: '通用格式',
    HuaxiaBank: '华夏银行',
    AlipayBusiness: '企业支付宝'
  }
  return labels[format] || format
}

const getFormatTagType = (format: string) => {
  const types: Record<string, 'info' | 'primary' | 'success' | 'warning' | 'danger'> = {
    Simple: 'info',
    HuaxiaBank: 'warning',
    AlipayBusiness: 'primary'
  }
  return types[format] ?? 'info'
}

const formatTime = (timeStr: string) => {
  // TimeSpan 从后端返回格式为 "HH:MM:SS" 或 "HH:MM:SS.xxx"
  if (!timeStr) return '-'
  const parts = timeStr.split(':')
  if (parts.length >= 2) return `${parts[0]}:${parts[1]}`
  return timeStr
}

onMounted(() => {
  loadAccounts()
  loadBatches()
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

.content-card {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 24px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.content-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.content-card-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.preview-stats {
  display: flex;
  align-items: center;
  gap: 8px;
}

.amount-in {
  color: var(--color-success);
  font-weight: 600;
}

.amount-out {
  color: var(--color-danger);
  font-weight: 600;
}

.matched-category {
  color: var(--color-primary);
}

.no-category {
  color: var(--text-placeholder);
  font-style: italic;
}

.no-action {
  color: var(--border-base);
}

:deep(.duplicate-row) {
  background-color: var(--color-warning-light-4) !important;
}

:deep(.duplicate-row td) {
  color: var(--text-placeholder);
}

:deep(.file-conflict-row) {
  background-color: var(--color-danger-light-5) !important;
  border-left: 3px solid var(--color-danger);
}

:deep(.file-conflict-row td) {
  color: var(--color-danger);
  font-weight: 500;
}

:deep(.recoverable-row) {
  background-color: var(--primary-surface) !important;
}

:deep(.recoverable-row td) {
  color: var(--color-primary);
}

.upload-section :deep(.el-upload-dragger) {
  width: 100%;
  border-radius: 8px;
}

.upload-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.upload-section :deep(.el-input__wrapper),
.upload-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}

.import-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid var(--bg-hover);
}

/* 表格样式 */
.content-card :deep(.el-table) {
  font-size: 13px;
}

.content-card :deep(.el-table th.el-table__cell) {
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.content-card :deep(.el-table td.el-table__cell) {
  padding: 12px 0;
}

.search-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 20px 4px 20px;
  margin-bottom: 16px;
}

.search-form {
  display: flex;
  flex-wrap: wrap;
  gap: 0;
}

.search-buttons {
  margin-left: auto !important;
}

.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section :deep(.el-input__wrapper),
.search-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}

.pagination {
  padding: 16px 0;
  justify-content: flex-end;
}

/* 示例表格样式 */
.example-table-container {
  padding: 16px;
  background: var(--bg-page);
  border-radius: 8px;
}

.example-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--bg-card);
  border-radius: 6px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  margin-bottom: 16px;
}

.example-table thead {
  background: var(--bg-hover);
}

.example-table th {
  padding: 12px 16px;
  text-align: left;
  font-size: 12px;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 2px solid var(--border-base);
}

.example-table td {
  padding: 12px 16px;
  font-size: 13px;
  color: var(--text-regular);
  border-bottom: 1px solid var(--bg-hover);
}

.example-table tbody tr:last-child td {
  border-bottom: none;
}

.example-table tbody tr:hover {
  background: var(--bg-page);
}

.example-notes {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.6;
}

.example-notes strong {
  color: var(--text-regular);
  font-weight: 600;
}

.example-notes ul {
  margin: 8px 0 0 0;
  padding-left: 20px;
}

.example-notes li {
  margin: 4px 0;
}

:deep(.el-collapse) {
  border: 1px solid var(--border-base);
  border-radius: 8px;
}

:deep(.el-collapse-item__header) {
  padding: 0 16px;
  font-size: 13px;
  color: var(--color-primary);
  font-weight: 500;
  background: var(--bg-page);
  border-radius: 8px;
}

:deep(.el-collapse-item__wrap) {
  border-top: 1px solid var(--border-base);
}

:deep(.el-collapse-item__content) {
  padding: 16px;
}

</style>
