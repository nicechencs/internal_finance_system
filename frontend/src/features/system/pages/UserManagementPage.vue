<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h2 class="page-title">用户管理</h2>
        <p class="page-desc">由管理员统一创建账号、设置密码、解锁和启停用用户。</p>
      </div>
      <el-button v-if="userStore.isAdmin" type="primary" @click="openCreateDialog">新建用户</el-button>
    </div>

    <div class="table-section">
      <div class="toolbar">
        <el-input
          v-model="keyword"
          clearable
          placeholder="搜索用户名、姓名或邮箱"
          class="search-input"
        />
        <el-button @click="loadUsers" :loading="loading">刷新</el-button>
      </div>

      <el-table :data="filteredUsers" v-loading="loading" class="resizable-table" border>
        <el-table-column prop="username" label="用户名" min-width="120" />
        <el-table-column prop="fullName" label="姓名" min-width="140" />
        <el-table-column prop="email" label="邮箱" min-width="180">
          <template #default="{ row }">
            {{ row.email || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="role" label="角色" width="120">
          <template #default="{ row }">
            <el-tag :type="roleTagType(row.role)" effect="light">{{ roleLabel(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="120">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'" effect="light">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="锁定" width="140">
          <template #default="{ row }">
            <el-tag v-if="row.isLocked" type="warning" effect="light">已锁定</el-tag>
            <span v-else>正常</span>
          </template>
        </el-table-column>
        <el-table-column label="最近登录" min-width="180">
          <template #default="{ row }">
            {{ row.lastLoginAt ? formatDateTime(row.lastLoginAt) : '-' }}
          </template>
        </el-table-column>
        <el-table-column v-if="userStore.isAdmin" label="操作" :min-width="TABLE_COLUMN_WIDTH.actionFour" fixed="right">
          <template #default="{ row }">
            <div class="action-group">
              <el-button link type="primary" size="small" @click="openEditDialog(row)">编辑</el-button>
              <el-button link type="primary" size="small" @click="openPasswordDialog(row)">设置密码</el-button>
              <el-button
                v-if="row.isLocked"
                link
                type="warning"
                size="small"
                @click="handleUnlock(row)"
              >
                解锁
              </el-button>
              <el-button
                link
                :type="row.isActive ? 'danger' : 'success'"
                size="small"
                @click="handleToggleStatus(row)"
              >
                {{ row.isActive ? '禁用' : '启用' }}
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog v-model="createDialogVisible" title="新建用户" width="560px">
      <el-form
        ref="createFormRef"
        :model="createForm"
        :rules="createRules"
        label-position="top"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="用户名" prop="username">
              <el-input v-model="createForm.username" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="姓名" prop="fullName">
              <el-input v-model="createForm.fullName" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="角色" prop="role">
              <el-select v-model="createForm.role" class="full-width">
                <el-option label="管理员" value="Admin" />
                <el-option label="会计" value="Accountant" />
                <el-option label="查看者" value="Viewer" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="邮箱">
              <el-input v-model="createForm.email" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="初始密码" prop="password">
          <el-input v-model="createForm.password" type="password" show-password />
        </el-form-item>

        <div class="switch-row">
          <el-switch v-model="createForm.isActive" active-text="创建后立即启用" />
          <el-switch v-model="createForm.mustChangePassword" active-text="首次登录必须改密" />
        </div>
      </el-form>

      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleCreateUser">创建</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="editDialogVisible" :title="`编辑用户 - ${selectedUser?.fullName || ''}`" width="560px">
      <el-form
        ref="editFormRef"
        :model="editForm"
        :rules="editRules"
        label-position="top"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="用户名">
              <el-input :model-value="selectedUser?.username" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="姓名" prop="fullName">
              <el-input v-model="editForm.fullName" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="角色" prop="role">
              <el-select v-model="editForm.role" class="full-width">
                <el-option label="管理员" value="Admin" />
                <el-option label="会计" value="Accountant" />
                <el-option label="查看者" value="Viewer" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="邮箱">
              <el-input v-model="editForm.email" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleUpdateUser">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="passwordDialogVisible" :title="`设置密码 - ${selectedUser?.fullName || ''}`" width="520px">
      <el-form
        ref="passwordFormRef"
        :model="passwordForm"
        :rules="passwordRules"
        label-position="top"
      >
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="passwordForm.newPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="确认新密码" prop="confirmPassword">
          <el-input v-model="passwordForm.confirmPassword" type="password" show-password />
        </el-form-item>
        <el-switch v-model="passwordForm.mustChangePassword" active-text="该用户下次登录必须改密" />
      </el-form>

      <template #footer>
        <el-button @click="passwordDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSetPassword">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  createUser,
  getUsers,
  setUserPassword,
  setUserStatus,
  unlockUser,
  updateUser
} from '@/features/system/api/users'
import type { UserAdmin } from '@/shared/types/common'
import { formatDateTime } from '@/shared/utils/formatters'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useUserStore } from '@/features/auth/stores/user'

const userStore = useUserStore()

const users = ref<UserAdmin[]>([])
const keyword = ref('')
const loading = ref(false)
const saving = ref(false)
const createDialogVisible = ref(false)
const editDialogVisible = ref(false)
const passwordDialogVisible = ref(false)
const selectedUser = ref<UserAdmin | null>(null)
const createFormRef = ref<FormInstance>()
const editFormRef = ref<FormInstance>()
const passwordFormRef = ref<FormInstance>()

const createForm = reactive({
  username: '',
  fullName: '',
  email: '',
  role: 'Viewer' as 'Admin' | 'Accountant' | 'Viewer',
  password: '',
  isActive: true,
  mustChangePassword: false
})

const editForm = reactive({
  fullName: '',
  email: '',
  role: 'Viewer' as 'Admin' | 'Accountant' | 'Viewer'
})

const passwordForm = reactive({
  newPassword: '',
  confirmPassword: '',
  mustChangePassword: false
})

const createRules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  fullName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
  password: [
    { required: true, message: '请输入初始密码', trigger: 'blur' },
    { min: 10, message: '密码至少 10 位', trigger: 'blur' }
  ]
}

const editRules: FormRules = {
  fullName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }]
}

const passwordRules: FormRules = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 10, message: '密码至少 10 位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('两次输入的新密码不一致'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ]
}

const filteredUsers = computed(() => {
  const search = keyword.value.trim().toLowerCase()
  if (!search) {
    return users.value
  }

  return users.value.filter((user) => {
    return [user.username, user.fullName, user.email]
      .filter(Boolean)
      .some((field) => field.toLowerCase().includes(search))
  })
})

const roleLabel = (role: string) => {
  if (role === 'Admin') return '管理员'
  if (role === 'Accountant') return '会计'
  return '查看者'
}

const roleTagType = (role: string) => {
  if (role === 'Admin') return 'danger'
  if (role === 'Accountant') return 'warning'
  return 'info'
}

const resetCreateForm = () => {
  createForm.username = ''
  createForm.fullName = ''
  createForm.email = ''
  createForm.role = 'Viewer'
  createForm.password = ''
  createForm.isActive = true
  createForm.mustChangePassword = false
}

const resetPasswordForm = () => {
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
  passwordForm.mustChangePassword = false
}

const loadUsers = async () => {
  loading.value = true
  try {
    const response = await getUsers()
    users.value = response.data.data
  } finally {
    loading.value = false
  }
}

const openCreateDialog = () => {
  resetCreateForm()
  createDialogVisible.value = true
}

const openEditDialog = (user: UserAdmin) => {
  selectedUser.value = user
  editForm.fullName = user.fullName
  editForm.email = user.email || ''
  editForm.role = user.role
  editDialogVisible.value = true
}

const openPasswordDialog = (user: UserAdmin) => {
  selectedUser.value = user
  resetPasswordForm()
  passwordDialogVisible.value = true
}

const handleCreateUser = async () => {
  if (!createFormRef.value) {
    return
  }

  await createFormRef.value.validate(async (valid) => {
    if (!valid) {
      return
    }

    saving.value = true
    try {
      await createUser({
        username: createForm.username,
        password: createForm.password,
        fullName: createForm.fullName,
        email: createForm.email || undefined,
        role: createForm.role,
        isActive: createForm.isActive,
        mustChangePassword: createForm.mustChangePassword
      })
      ElMessage.success('用户创建成功')
      createDialogVisible.value = false
      await loadUsers()
    } finally {
      saving.value = false
    }
  })
}

const handleUpdateUser = async () => {
  const user = selectedUser.value
  if (!editFormRef.value || !user) return

  await editFormRef.value.validate(async (valid) => {
    if (!valid) return

    saving.value = true
    try {
      await updateUser(user.id, {
        fullName: editForm.fullName,
        email: editForm.email || undefined,
        role: editForm.role
      })
      ElMessage.success('用户信息更新成功')
      editDialogVisible.value = false
      await loadUsers()
    } finally {
      saving.value = false
    }
  })
}

const handleSetPassword = async () => {
  const user = selectedUser.value
  if (!passwordFormRef.value || !user) {
    return
  }

  await passwordFormRef.value.validate(async (valid) => {
    if (!valid) {
      return
    }

    saving.value = true
    try {
      await setUserPassword(user.id, {
        newPassword: passwordForm.newPassword,
        mustChangePassword: passwordForm.mustChangePassword
      })
      ElMessage.success('密码设置成功')
      passwordDialogVisible.value = false
      await loadUsers()
    } finally {
      saving.value = false
    }
  })
}

const handleUnlock = async (user: UserAdmin) => {
  await unlockUser(user.id)
  ElMessage.success('用户已解锁')
  await loadUsers()
}

const handleToggleStatus = async (user: UserAdmin) => {
  const action = user.isActive ? '禁用' : '启用'
  await ElMessageBox.confirm(
    `确认要${action}用户 “${user.fullName}” 吗？`,
    `${action}用户`,
    {
      type: 'warning'
    }
  )

  await setUserStatus(user.id, {
    isActive: !user.isActive
  })

  ElMessage.success(`用户已${action}`)
  await loadUsers()
}

onMounted(() => {
  loadUsers()
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
  gap: 16px;
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

.table-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  overflow: hidden;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 20px;
}

.search-input {
  max-width: 320px;
}

.action-group {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.full-width {
  width: 100%;
}

.switch-row {
  display: flex;
  gap: 24px;
  flex-wrap: wrap;
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
  }

  .toolbar {
    flex-direction: column;
  }

  .search-input {
    max-width: none;
  }
}
</style>
