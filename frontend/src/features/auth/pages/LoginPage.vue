<template>
  <div class="login-page">
    <div class="brand-section">
      <div class="brand-content">
        <el-icon class="brand-icon" :size="64" color="#fff">
          <DataAnalysis />
        </el-icon>
        <h1 class="brand-title">财务管理系统</h1>
        <p class="brand-subtitle">Finance Management System</p>
        <p class="brand-desc">高效管理企业资金流、项目收支与日常财务协作</p>
      </div>
    </div>

    <div class="form-section">
      <div class="form-wrapper">
        <h2 class="form-title">欢迎登录</h2>
        <p class="form-subtitle">请输入您的账号信息</p>

        <el-form
          ref="formRef"
          :model="loginForm"
          :rules="rules"
          class="login-form"
          @keyup.enter="handleLogin"
        >
          <el-form-item prop="username">
            <el-input
              v-model="loginForm.username"
              placeholder="用户名"
              prefix-icon="User"
              size="large"
              class="login-input"
            />
          </el-form-item>
          <el-form-item prop="password">
            <el-input
              v-model="loginForm.password"
              type="password"
              placeholder="密码"
              prefix-icon="Lock"
              size="large"
              show-password
              class="login-input"
            />
          </el-form-item>
          <el-form-item>
            <el-button
              type="primary"
              :loading="loading"
              @click="handleLogin"
              class="login-btn"
              size="large"
            >
              登录
            </el-button>
          </el-form-item>
        </el-form>

        <p class="copyright">&copy; 2026 财务管理系统</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { DataAnalysis } from '@element-plus/icons-vue'
import { login } from '@/features/auth/api/auth'
import { useUserStore } from '@/features/auth/stores/user'

const router = useRouter()
const userStore = useUserStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

const loginForm = reactive({
  username: '',
  password: ''
})

const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const handleLogin = async () => {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
  } catch {
    return
  }

  loading.value = true
  try {
    const response = await login(loginForm)
    userStore.setUser(response.data.data.user)
    userStore.markSessionInitialized()
    if (response.data.data.mustChangePassword) {
      ElMessage.success('登录成功，请先修改密码')
      router.push('/account-security')
    } else {
      ElMessage.success('登录成功')
      router.push('/')
    }
  } catch (error) {
    console.error('登录失败:', error)
    ElMessage.error('登录失败，请检查用户名和密码')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  display: flex;
  height: 100vh;
  overflow: hidden;
}

.brand-section {
  position: relative;
  flex: 0 0 55%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark-2) 100%);
  overflow: hidden;
}

.brand-section::before {
  content: '';
  position: absolute;
  width: 300px;
  height: 300px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.05);
  top: -50px;
  right: -100px;
}

.brand-section::after {
  content: '';
  position: absolute;
  width: 400px;
  height: 400px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.04);
  bottom: -120px;
  left: -80px;
}

.brand-content {
  position: relative;
  z-index: 1;
  text-align: center;
  padding: 0 40px;
}

.brand-icon {
  margin-bottom: 24px;
  filter: drop-shadow(0 4px 12px rgba(0, 0, 0, 0.15));
}

.brand-title {
  margin: 0 0 12px;
  font-size: 32px;
  font-weight: 700;
  color: var(--text-inverse);
  letter-spacing: 2px;
}

.brand-subtitle {
  margin: 0 0 20px;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.6);
  letter-spacing: 1px;
}

.brand-desc {
  margin: 0;
  font-size: 15px;
  color: rgba(255, 255, 255, 0.5);
  line-height: 1.8;
}

.form-section {
  flex: 0 0 45%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-page);
}

.form-wrapper {
  width: 380px;
  padding: 0 20px;
}

.form-title {
  margin: 0 0 8px;
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
}

.form-subtitle {
  margin: 0 0 32px;
  font-size: 14px;
  color: var(--text-placeholder);
}

.login-form {
  margin-bottom: 40px;
}

.login-form :deep(.el-input__wrapper) {
  border-radius: 8px;
  padding: 4px 12px;
  box-shadow: 0 0 0 1px var(--border-base) inset;
  transition: box-shadow 0.2s ease;
}

.login-form :deep(.el-input__wrapper:hover) {
  box-shadow: 0 0 0 1px var(--text-disabled) inset;
}

.login-form :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 1px var(--color-primary) inset !important;
}

.login-form :deep(.el-form-item) {
  margin-bottom: 22px;
}

.login-form :deep(.el-input__prefix .el-icon) {
  color: var(--text-placeholder);
  font-size: 16px;
}

.login-btn {
  width: 100%;
  height: 42px;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 2px;
  background-color: var(--color-primary);
  border-color: var(--color-primary);
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.login-btn:hover,
.login-btn:focus {
  background-color: var(--color-primary-dark-1);
  border-color: var(--color-primary-dark-1);
}

.login-btn:active {
  background-color: var(--color-primary-dark-2);
  border-color: var(--color-primary-dark-2);
}

.copyright {
  text-align: center;
  font-size: 12px;
  color: var(--text-disabled);
  margin: 0;
}

@media (max-width: 768px) {
  .login-page {
    flex-direction: column;
    height: 100dvh;
    overflow-y: auto;
  }

  .brand-section {
    flex: 0 0 auto;
    min-height: 120px;
    padding: calc(20px + var(--safe-area-top)) 24px 20px;
  }

  .brand-section::before,
  .brand-section::after {
    display: none;
  }

  .brand-content {
    padding: 0;
  }

  .brand-icon {
    margin-bottom: 8px;
    font-size: 36px;
  }

  .brand-title {
    font-size: 20px;
    letter-spacing: 1px;
    margin-bottom: 6px;
  }

  .brand-subtitle {
    margin-bottom: 4px;
    font-size: 12px;
  }

  .brand-desc {
    font-size: 13px;
    line-height: 1.5;
  }

  .form-section {
    flex: 1;
    align-items: flex-start;
    padding: 32px 0 24px;
  }

  .form-wrapper {
    width: 100%;
    max-width: 380px;
    padding: 0 24px;
  }

  .login-btn {
    height: 48px;
    font-size: 16px;
  }
}
</style>
