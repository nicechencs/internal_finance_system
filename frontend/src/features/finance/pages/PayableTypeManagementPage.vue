<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h2 class="page-title">应付款业务类型管理</h2>
        <p class="page-desc">管理应付款的业务分类</p>
      </div>
      <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>
        新增类型
      </el-button>
    </div>

    <!-- 搜索区域 -->
    <div class="search-section">
      <el-form :inline="true" @submit.prevent="handleSearch">
        <el-form-item label="名称">
          <el-input v-model="searchForm.name" placeholder="搜索类型名称" clearable style="width: 200px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.isActive" placeholder="全部状态" clearable style="width: 120px">
            <el-option label="启用" :value="true" />
            <el-option label="禁用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <div class="table-section">
      <el-table :data="payableTypes" v-loading="loading" border>
        <el-table-column label="名称" prop="name" min-width="150" />
        <el-table-column label="编码" prop="code" min-width="150">
          <template #default="{ row }">
            <span>{{ row.code || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="说明" prop="description" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.description || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="排序" prop="sortOrder" width="100" align="center" />
        <el-table-column label="操作" width="180" fixed="right" align="center">
          <template #default="{ row }">
            <el-button v-if="userStore.canEdit" link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button v-if="userStore.canDelete" link type="danger" @click="handleDelete(row)">删除</el-button>
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

    <!-- 新增/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑业务类型' : '新增业务类型'"
      width="600px"
      @close="handleDialogClose"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="如：项目成本支出" maxlength="100" />
        </el-form-item>
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" placeholder="如：PROJECT_COST" maxlength="50" />
        </el-form-item>
        <el-form-item label="说明" prop="description">
          <el-input
            v-model="form.description"
            type="textarea"
            :rows="3"
            placeholder="业务类型说明"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="是否启用" prop="isActive">
          <el-switch v-model="form.isActive" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="form.sortOrder" :min="0" :max="9999" :controls="true" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import {
  getPayableTypesPaged,
  createPayableType,
  updatePayableType,
  deletePayableType
} from '@/features/finance/api/payable'
import type { PayableType } from '@/features/finance/types/payable'
import { useUserStore } from '@/features/auth/stores/user'

const userStore = useUserStore()
const loading = ref(false)
const payableTypes = ref<PayableType[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const formRef = ref<FormInstance>()

const searchForm = reactive({
  name: '',
  isActive: '' as string | boolean
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

const form = reactive({
  id: 0,
  name: '',
  code: '',
  description: '',
  isActive: true,
  sortOrder: 0
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }]
}

const loadPayableTypes = async () => {
  loading.value = true
  try {
    const params: any = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      name: searchForm.name || undefined,
      isActive: searchForm.isActive === '' ? undefined : searchForm.isActive
    }
    const response = await getPayableTypesPaged(params)
    payableTypes.value = response.data.data.items
    pagination.total = response.data.data.total
  } catch (error) {
    ElMessage.error('加载业务类型失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.page = 1
  loadPayableTypes()
}

const handleReset = () => {
  searchForm.name = ''
  searchForm.isActive = ''
  pagination.page = 1
  loadPayableTypes()
}

const handleSizeChange = () => {
  pagination.page = 1
  loadPayableTypes()
}

const handlePageChange = () => {
  loadPayableTypes()
}

const handleCreate = () => {
  isEdit.value = false
  Object.assign(form, {
    id: 0,
    name: '',
    code: '',
    description: '',
    isActive: true,
    sortOrder: 0
  })
  dialogVisible.value = true
}

const handleEdit = (row: PayableType) => {
  isEdit.value = true
  Object.assign(form, { ...row })
  dialogVisible.value = true
}

const handleDialogClose = () => {
  formRef.value?.resetFields()
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    submitting.value = true
    try {
      const data = {
        name: form.name,
        code: form.code || undefined,
        description: form.description || undefined,
        isActive: form.isActive,
        sortOrder: form.sortOrder
      }

      if (isEdit.value) {
        await updatePayableType(form.id, data)
        ElMessage.success('更新成功')
      } else {
        await createPayableType(data)
        ElMessage.success('创建成功')
      }

      dialogVisible.value = false
      loadPayableTypes()
    } catch (error) {
      ElMessage.error(isEdit.value ? '更新失败' : '创建失败')
    } finally {
      submitting.value = false
    }
  })
}

const handleDelete = async (row: PayableType) => {
  try {
    await ElMessageBox.confirm('确定要删除该业务类型吗？', '提示', {
      type: 'warning',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })

    await deletePayableType(row.id)
    ElMessage.success('删除成功')
    loadPayableTypes()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

onMounted(() => {
  loadPayableTypes()
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
  margin: 0 0 8px 0;
}

.page-desc {
  color: var(--text-placeholder);
  margin: 0;
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

.search-section :deep(.el-input__wrapper),
.search-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}

.table-section {
  background: var(--bg-card);
  padding: 0;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  overflow: hidden;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}
</style>
