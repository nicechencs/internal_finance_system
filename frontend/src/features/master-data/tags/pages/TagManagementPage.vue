<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">标签管理</h2>
        <p class="page-desc">管理各业务场景的标签，用于对交易、项目、人员等进行分类标注</p>
      </div>
      <div class="page-header-right">
        <el-button
          v-if="userStore.canEdit"
          type="primary"
          @click="handleAdd"
        >
          新增标签
        </el-button>
      </div>
    </div>

    <!-- 搜索区域 -->
    <div class="search-section">
      <el-form :inline="true" @submit.prevent>
        <el-form-item label="标签名称">
          <el-input v-model="searchName" placeholder="搜索标签名称" clearable style="width: 200px" @input="handleFilterChange" />
        </el-form-item>
      </el-form>
    </div>

    <!-- Scope Tab 切换 -->
    <div class="tab-section">
      <el-tabs v-model="activeScope" @tab-change="handleScopeChange">
        <el-tab-pane
          v-for="tab in SCOPE_TABS"
          :key="tab.value"
          :label="tab.label"
          :name="tab.value"
        />
      </el-tabs>
    </div>

    <!-- 数据表格区域 -->
    <div class="table-section">
      <el-table
        :data="paginatedData"
        style="width: 100%"
        v-loading="loading"
        border
      >
        <!-- 标签名称（含颜色色块） -->
        <el-table-column label="标签名称" min-width="160">
          <template #default="{ row }">
            <div class="tag-name-cell">
              <span
                v-if="row.color"
                class="color-swatch"
                :style="{ backgroundColor: row.color }"
              />
              <el-link type="primary" @click="handleViewByTag(row)">
                {{ row.name }}
              </el-link>
            </div>
          </template>
        </el-table-column>

        <!-- 编码 -->
        <el-table-column prop="code" label="编码" width="140">
          <template #default="{ row }">
            <span class="code-text">{{ row.code || '-' }}</span>
          </template>
        </el-table-column>

        <!-- 排序 -->
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />

        <!-- 是否启用 -->
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>

        <!-- 是否系统标签 -->
        <el-table-column label="系统标签" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isSystem" type="warning" size="small">系统</el-tag>
            <span v-else class="text-placeholder">-</span>
          </template>
        </el-table-column>

        <!-- 描述 -->
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.description || '-' }}
          </template>
        </el-table-column>

        <!-- 操作 -->
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button
              link
              type="primary"
              @click="handleViewByTag(row)"
            >
              查看
            </el-button>
            <el-button
              v-if="userStore.canEdit"
              link
              type="primary"
              :disabled="row.isSystem"
              @click="handleEdit(row)"
            >
              编辑
            </el-button>
            <el-button
              v-if="userStore.isAdmin"
              link
              type="danger"
              :disabled="row.isSystem"
              @click="handleDeleteTag(row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 空状态 -->
      <div v-if="!loading && filteredData.length === 0" class="empty-tip">
        <el-empty description="暂无标签数据" />
      </div>

      <el-pagination
        v-if="filteredData.length > 0"
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :total="filteredData.length"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="currentPage = 1"
        @current-change="() => {}"
        class="pagination"
      />
    </div>

    <!-- 新增/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="currentTag ? '编辑标签' : '新增标签'"
      width="520px"
      @close="handleDialogClose"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="标签名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入标签名称" />
        </el-form-item>

        <el-form-item label="编码" prop="code">
          <el-input v-model="formData.code" placeholder="请输入编码（可选）" />
        </el-form-item>

        <el-form-item label="颜色" prop="color">
          <div class="color-picker-row">
            <el-color-picker v-model="formData.color" show-alpha />
            <el-button
              v-if="formData.color"
              link
              type="info"
              size="small"
              @click="formData.color = ''"
            >
              清除
            </el-button>
          </div>
        </el-form-item>

        <el-form-item label="排序" prop="sortOrder">
          <el-input-number
            v-model="formData.sortOrder"
            :min="0"
            :max="9999"
            controls-position="right"
            style="width: 160px"
          />
        </el-form-item>

        <el-form-item label="描述" prop="description">
          <el-input
            v-model="formData.description"
            type="textarea"
            :rows="3"
            placeholder="请输入描述（可选）"
          />
        </el-form-item>

        <el-form-item label="是否启用" prop="isActive">
          <el-switch v-model="formData.isActive" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="handleDialogClose">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useUserStore } from '@/features/auth/stores/user'
import { useTagStore } from '@/features/master-data/tags/stores/tagStore'
import { getTags, createTag, updateTag, deleteTag } from '@/features/master-data/tags/api/tag'
import type { Tag, CreateTagRequest, UpdateTagRequest } from '@/features/master-data/tags/types/tag'

// ── Scope 配置 ──
const SCOPE_TABS = [
  { value: 'transaction', label: '交易标签' },
  { value: 'project', label: '项目标签' },
  { value: 'person', label: '人员标签' },
  { value: 'customer', label: '客户标签' },
  { value: 'supplier', label: '供应商标签' },
  { value: 'receivable', label: '应收标签' },
  { value: 'payable', label: '应付标签' },
]

// ── Router ──
const router = useRouter()

// ── scope → 目标路由名称映射 ──
const SCOPE_ROUTE_MAP: Record<string, string> = {
  transaction: 'Transactions',
  project: 'ProjectList',
  customer: 'CustomerList',
  supplier: 'SupplierList',
  person: 'PersonList',
  receivable: 'ReceivableList',
  payable: 'PayableList',
}

// ── Store ──
const userStore = useUserStore()
const tagStore = useTagStore()

// ── 搜索与分页 ──
const searchName = ref('')
const currentPage = ref(1)
const pageSize = ref(20)

// ── 状态 ──
const activeScope = ref('transaction')
const loading = ref(false)
const tableData = ref<Tag[]>([])
const dialogVisible = ref(false)
const submitting = ref(false)
const currentTag = ref<Tag | null>(null)
const formRef = ref<FormInstance>()

// ── 表单数据 ──
const formData = reactive<CreateTagRequest & UpdateTagRequest>({
  scope: 'transaction',
  name: '',
  code: '',
  color: '',
  sortOrder: 0,
  description: '',
  isActive: true,
})

// ── 校验规则 ──
const formRules: FormRules = {
  name: [{ required: true, message: '请输入标签名称', trigger: 'blur' }],
}

// ── 客户端过滤与分页 ──
const filteredData = computed(() => {
  if (!searchName.value) return tableData.value
  const keyword = searchName.value.toLowerCase()
  return tableData.value.filter(tag => tag.name.toLowerCase().includes(keyword))
})

const paginatedData = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return filteredData.value.slice(start, start + pageSize.value)
})

const handleFilterChange = () => {
  currentPage.value = 1
}

// ── 数据加载 ──
const loadData = async () => {
  loading.value = true
  try {
    const res = await getTags({ scope: activeScope.value })
    tableData.value = res.data.data ?? []
  } catch (error) {
    console.error('加载标签列表失败:', error)
    tableData.value = []
  } finally {
    loading.value = false
  }
}

// ── 查看标签（跳转到对应列表页面） ──
const handleViewByTag = (row: Tag) => {
  const routeName = SCOPE_ROUTE_MAP[activeScope.value]
  if (routeName) {
    router.push({ name: routeName, query: { tagId: String(row.id) } })
  }
}

// ── Tab 切换 ──
const handleScopeChange = (scope: string) => {
  activeScope.value = scope
  searchName.value = ''
  currentPage.value = 1
  loadData()
}

// ── 新增 ──
const handleAdd = () => {
  currentTag.value = null
  Object.assign(formData, {
    scope: activeScope.value,
    name: '',
    code: '',
    color: '',
    sortOrder: 0,
    description: '',
    isActive: true,
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

// ── 编辑 ──
const handleEdit = (row: Tag) => {
  currentTag.value = row
  Object.assign(formData, {
    scope: row.scope,
    name: row.name,
    code: row.code ?? '',
    color: row.color ?? '',
    sortOrder: row.sortOrder,
    description: row.description ?? '',
    isActive: row.isActive,
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

// ── 删除 ──
const handleDeleteTag = async (row: Tag) => {
  try {
    await ElMessageBox.confirm(`确定要删除标签「${row.name}」吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    })
    await deleteTag(row.id)
    ElMessage.success('删除成功')
    tagStore.invalidateScope(activeScope.value)
    await loadData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

// ── 提交表单 ──
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      if (currentTag.value) {
        // 编辑
        const payload: UpdateTagRequest = {
          name: formData.name,
          code: formData.code || undefined,
          color: formData.color || undefined,
          sortOrder: formData.sortOrder ?? 0,
          description: formData.description || undefined,
          isActive: formData.isActive,
        }
        await updateTag(currentTag.value.id, payload)
        ElMessage.success('更新成功')
      } else {
        // 新增
        const payload: CreateTagRequest = {
          scope: activeScope.value,
          name: formData.name,
          code: formData.code || undefined,
          color: formData.color || undefined,
          sortOrder: formData.sortOrder ?? 0,
          description: formData.description || undefined,
          isActive: formData.isActive,
        }
        await createTag(payload)
        ElMessage.success('创建成功')
      }
      tagStore.invalidateScope(activeScope.value)
      dialogVisible.value = false
      await loadData()
    } catch (error) {
      console.error('提交失败:', error)
    } finally {
      submitting.value = false
    }
  })
}

// ── 关闭对话框 ──
const handleDialogClose = () => {
  dialogVisible.value = false
}

// ── 初始化 ──
onMounted(() => {
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

.search-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 20px 4px 20px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section :deep(.el-input__wrapper) {
  border-radius: 8px;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}

.tab-section {
  background: var(--bg-card);
  border-radius: 12px 12px 0 0;
  padding: 0 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  margin-bottom: 0;
}

.tab-section :deep(.el-tabs__header) {
  margin-bottom: 0;
}

.table-section {
  background: var(--bg-card);
  border-radius: 0 0 12px 12px;
  padding: 0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  overflow: hidden;
}

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

/* 标签名称单元格 */
.tag-name-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.color-swatch {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 3px;
  flex-shrink: 0;
  border: 1px solid rgba(0, 0, 0, 0.1);
}

.code-text {
  font-family: monospace;
  font-size: 12px;
  color: var(--text-secondary);
}

.text-placeholder {
  color: var(--text-placeholder);
}

.empty-tip {
  padding: 40px 0;
}

/* 颜色选择器行 */
.color-picker-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* 新增按钮 */
.page-header :deep(.el-button--primary) {
  border-radius: 8px;
  padding: 10px 20px;
}
</style>
