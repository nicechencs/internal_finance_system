<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h2 class="page-title">站点设置</h2>
        <p class="page-desc">自定义登录页、导航和浏览器标题中显示的站点名称。留空英文副标题则不显示。</p>
      </div>
    </div>

    <el-card class="settings-card" shadow="never" v-loading="loading">
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        class="settings-form"
      >
        <el-form-item label="站点名称" prop="siteName">
          <el-input
            v-model="form.siteName"
            maxlength="50"
            show-word-limit
            placeholder="例如：财务管理系统"
          />
        </el-form-item>
        <el-form-item label="英文副标题" prop="siteNameEn">
          <el-input
            v-model="form.siteNameEn"
            maxlength="80"
            show-word-limit
            placeholder="例如：Finance Management System（选填）"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="saving" @click="handleSubmit">保存</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { getPublicBrand, updateSiteBrand } from '@/features/system/api/siteBrand'
import { useSiteBrandStore } from '@/features/system/stores/siteBrand'
import {
  DEFAULT_SITE_NAME,
  DEFAULT_SITE_NAME_EN,
  SITE_NAME_EN_MAX_LENGTH,
  SITE_NAME_MAX_LENGTH
} from '@/features/system/types/siteBrand'

const brandStore = useSiteBrandStore()
const formRef = ref<FormInstance>()
const loading = ref(false)
const saving = ref(false)

const form = reactive({
  siteName: DEFAULT_SITE_NAME,
  siteNameEn: DEFAULT_SITE_NAME_EN
})

const containsUnsafeCharacters = (value: string) => /[<>\u0000-\u001F\u007F]/.test(value)

const rules: FormRules = {
  siteName: [
    { required: true, message: '请输入站点名称', trigger: 'blur' },
    { min: 1, max: SITE_NAME_MAX_LENGTH, message: `站点名称长度不能超过 ${SITE_NAME_MAX_LENGTH} 个字符`, trigger: 'blur' },
    {
      validator: (_rule, value: string, callback) => {
        if (value && containsUnsafeCharacters(value)) {
          callback(new Error('站点名称不能包含 HTML 标签或控制字符'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ],
  siteNameEn: [
    { max: SITE_NAME_EN_MAX_LENGTH, message: `英文副标题长度不能超过 ${SITE_NAME_EN_MAX_LENGTH} 个字符`, trigger: 'blur' },
    {
      validator: (_rule, value: string, callback) => {
        if (value && containsUnsafeCharacters(value)) {
          callback(new Error('英文副标题不能包含 HTML 标签或控制字符'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ]
}

const loadForm = async () => {
  loading.value = true
  try {
    const res = await getPublicBrand()
    const brand = res.data?.data
    form.siteName = brand?.siteName?.trim() || DEFAULT_SITE_NAME
    form.siteNameEn = brand?.siteNameEn ?? DEFAULT_SITE_NAME_EN
    brandStore.apply({
      siteName: form.siteName,
      siteNameEn: form.siteNameEn
    })
  } catch {
    form.siteName = brandStore.siteName
    form.siteNameEn = brandStore.siteNameEn
  } finally {
    loading.value = false
  }
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
    const res = await updateSiteBrand({
      siteName: form.siteName.trim(),
      siteNameEn: form.siteNameEn.trim()
    })
    const brand = res.data?.data
    brandStore.apply(brand || {
      siteName: form.siteName.trim(),
      siteNameEn: form.siteNameEn.trim()
    })
    form.siteName = brandStore.siteName
    form.siteNameEn = brandStore.siteNameEn
    ElMessage.success('站点名称已更新')
  } catch (error) {
    console.error('更新站点名称失败:', error)
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadForm()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.page-header {
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

.settings-card {
  max-width: 640px;
  border-radius: 12px;
}

.settings-form {
  max-width: 480px;
}
</style>
