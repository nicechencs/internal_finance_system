<template>
  <div class="profile-page">
    <div class="page-header">
      <div>
        <h1>个人资料</h1>
        <p>查看和修改你的个人信息。用户名和角色无法自行修改。</p>
      </div>
    </div>

    <el-row :gutter="20">
      <el-col :xs="24" :lg="16">
        <el-card class="profile-card" shadow="never">
          <template #header>
            <div class="card-title">基本信息</div>
          </template>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-position="top"
            class="profile-form"
          >
            <el-form-item label="用户名">
              <el-input :model-value="userStore.user?.username" disabled />
            </el-form-item>

            <el-form-item label="角色">
              <el-tag :type="roleTagType" effect="light" size="large">{{ roleLabel }}</el-tag>
            </el-form-item>

            <el-form-item label="姓名" prop="fullName">
              <el-input v-model="form.fullName" placeholder="请输入姓名" />
            </el-form-item>

            <el-form-item label="邮箱" prop="email">
              <el-input v-model="form.email" placeholder="请输入邮箱（选填）" />
            </el-form-item>

            <el-button type="primary" :loading="saving" @click="handleSubmit">
              保存修改
            </el-button>
          </el-form>
        </el-card>
      </el-col>

      <el-col :xs="24" :lg="8">
        <el-card class="profile-card tips-card" shadow="never">
          <template #header>
            <div class="card-title">说明</div>
          </template>

          <ul class="tips-list">
            <li>用户名创建后不可修改。</li>
            <li>角色由管理员分配和调整。</li>
            <li>修改密码请前往「账号安全」页面。</li>
          </ul>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { updateProfile } from '@/features/auth/api/auth'
import { useUserStore } from '@/features/auth/stores/user'

const userStore = useUserStore()
const formRef = ref<FormInstance>()
const saving = ref(false)

const form = reactive({
  fullName: '',
  email: ''
})

const rules: FormRules = {
  fullName: [
    { required: true, message: '请输入姓名', trigger: 'blur' },
    { max: 100, message: '姓名不能超过 100 个字符', trigger: 'blur' }
  ]
}

const roleLabel = computed(() => {
  const role = userStore.user?.role
  if (role === 'Admin') return '管理员'
  if (role === 'Accountant') return '会计'
  return '查看者'
})

const roleTagType = computed(() => {
  const role = userStore.user?.role
  if (role === 'Admin') return 'danger'
  if (role === 'Accountant') return 'warning'
  return 'info'
})

const initForm = () => {
  form.fullName = userStore.user?.fullName || ''
  form.email = userStore.user?.email || ''
}

const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
  } catch {
    return
  }

  saving.value = true
  try {
    await updateProfile({
      fullName: form.fullName,
      email: form.email || undefined
    })
    await userStore.fetchCurrentUser()
    ElMessage.success('个人资料更新成功')
  } catch (error) {
    console.error('更新失败:', error)
    ElMessage.error('个人资料更新失败')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  initForm()
})
</script>

<style scoped>
.profile-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.page-header h1 {
  margin: 0 0 6px;
  font-size: 28px;
  color: var(--text-primary);
}

.page-header p {
  margin: 0;
  color: var(--text-secondary);
}

.profile-card {
  border-radius: 16px;
  border: 1px solid var(--border-base);
}

.card-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.profile-form {
  max-width: 560px;
}

.tips-card {
  background: linear-gradient(180deg, var(--bg-page) 0%, var(--primary-surface) 100%);
}

.tips-list {
  margin: 0;
  padding-left: 18px;
  color: var(--text-regular);
  line-height: 1.8;
}
</style>
