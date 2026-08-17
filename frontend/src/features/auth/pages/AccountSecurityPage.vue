<template>
  <div class="security-page">
    <div class="page-header">
      <div>
        <h1>账号安全</h1>
        <p>在这里修改自己的登录密码。修改成功后，其他旧会话会自动失效。</p>
      </div>
    </div>

    <el-row :gutter="20">
      <el-col :xs="24" :lg="16">
        <el-card class="security-card" shadow="never">
          <template #header>
            <div class="card-title">修改密码</div>
          </template>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-position="top"
            class="security-form"
          >
            <el-form-item label="当前密码" prop="currentPassword">
              <el-input
                v-model="form.currentPassword"
                type="password"
                show-password
                placeholder="请输入当前密码"
              />
            </el-form-item>

            <el-form-item label="新密码" prop="newPassword">
              <el-input
                v-model="form.newPassword"
                type="password"
                show-password
                placeholder="至少 10 位，建议使用长密码短语"
              />
            </el-form-item>

            <el-form-item label="确认新密码" prop="confirmPassword">
              <el-input
                v-model="form.confirmPassword"
                type="password"
                show-password
                placeholder="请再次输入新密码"
              />
            </el-form-item>

            <el-button type="primary" :loading="saving" @click="handleSubmit">
              更新密码
            </el-button>
          </el-form>
        </el-card>
      </el-col>

      <el-col :xs="24" :lg="8">
        <el-card class="security-card tips-card" shadow="never">
          <template #header>
            <div class="card-title">安全建议</div>
          </template>

          <ul class="tips-list">
            <li>密码至少 10 位，优先使用长密码短语。</li>
            <li>不要与其他网站复用同一个密码。</li>
            <li>管理员重置密码后，建议尽快自行再改一次。</li>
            <li>如果你怀疑账号被他人使用，改密后请重新检查登录设备。</li>
          </ul>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { changePassword } from '@/features/auth/api/auth'

const formRef = ref<FormInstance>()
const saving = ref(false)

const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const rules: FormRules = {
  currentPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    {
      min: 10,
      message: '新密码至少 10 位',
      trigger: 'blur'
    }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== form.newPassword) {
          callback(new Error('两次输入的新密码不一致'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ]
}

const resetForm = () => {
  form.currentPassword = ''
  form.newPassword = ''
  form.confirmPassword = ''
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
    await changePassword({
      currentPassword: form.currentPassword,
      newPassword: form.newPassword
    })
    ElMessage.success('密码修改成功')
    resetForm()
    formRef.value?.clearValidate()
  } catch (error) {
    console.error('修改密码失败:', error)
    ElMessage.error('密码修改失败，请重试')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.security-page {
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

.security-card {
  border-radius: 16px;
  border: 1px solid var(--border-base);
}

.card-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.security-form {
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
