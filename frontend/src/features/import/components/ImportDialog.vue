<template>
  <el-dialog
    v-model="visible"
    :title="`批量导入${moduleLabel}`"
    width="600px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <div class="import-container">
      <!-- 步骤指示 -->
      <el-steps :active="currentStep" finish-status="success" align-center>
        <el-step title="选择文件" />
        <el-step title="上传解析" />
        <el-step title="完成" />
      </el-steps>

      <!-- 步骤1: 选择文件 -->
      <div v-if="currentStep === 0" class="step-content">
        <el-upload
          ref="uploadRef"
          :auto-upload="false"
          :limit="1"
          :on-change="handleFileChange"
          :on-exceed="handleExceed"
          :accept="acceptTypes"
          drag
        >
          <el-icon class="el-icon--upload"><upload-filled /></el-icon>
          <div class="el-upload__text">
            将文件拖到此处，或<em>点击上传</em>
          </div>
          <template #tip>
            <div class="el-upload__tip">
              仅支持 .xlsx 格式，如有 .xls 文件请先转换为 .xlsx，单次最多导入 500 条数据
            </div>
          </template>
        </el-upload>

        <div v-if="selectedFile" class="file-info">
          <el-icon><document /></el-icon>
          <span>{{ selectedFile.name }}</span>
          <el-button link type="danger" @click="removeFile">删除</el-button>
        </div>

        <!-- 模板下载提示 -->
        <div class="template-hint">
          <el-alert
            title="请确保 Excel 文件格式正确"
            type="info"
            :closable="false"
            show-icon
          >
            <template #default>
              <div><strong>文件格式要求：</strong>仅支持 .xlsx 格式（Excel 2007 及以上版本）</div>
              <div v-html="templateHint" style="margin-top: 8px;"></div>
            </template>
          </el-alert>
        </div>
      </div>

      <!-- 步骤2: 上传中 -->
      <div v-if="currentStep === 1" class="step-content">
        <div class="upload-progress">
          <el-progress
            :percentage="uploadProgress"
            :status="uploadStatus"
          />
          <p class="progress-text">{{ progressText }}</p>
        </div>
      </div>

      <!-- 步骤3: 完成 -->
      <div v-if="currentStep === 2" class="step-content">
        <el-result
          :icon="result.success ? 'success' : 'warning'"
          :title="result.title"
          :sub-title="result.subTitle"
        >
          <template #extra>
            <div v-if="result.errors && result.errors.length > 0" class="error-list">
              <el-divider content-position="left">错误详情</el-divider>
              <el-scrollbar max-height="200px">
                <div
                  v-for="(error, index) in result.errors"
                  :key="index"
                  class="error-item"
                >
                  <el-tag type="danger" size="small">第 {{ error.index + 1 }} 行</el-tag>
                  <span>{{ error.message }}</span>
                </div>
              </el-scrollbar>
            </div>
          </template>
        </el-result>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button
          v-if="currentStep === 0"
          type="primary"
          :disabled="!selectedFile"
          :loading="uploading"
          @click="handleUpload"
        >
          开始导入
        </el-button>
        <el-button
          v-if="currentStep === 2"
          type="primary"
          @click="handleClose"
        >
          完成
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { UploadFilled, Document } from '@element-plus/icons-vue'
import type { UploadInstance, UploadRawFile, UploadFile } from 'element-plus'
import { batchImportFile } from '@/features/import/api/import'

interface Props {
  modelValue: boolean
  moduleType: 'customer' | 'supplier' | 'person' | 'project'
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const uploadRef = ref<UploadInstance>()
const selectedFile = ref<UploadRawFile | null>(null)
const currentStep = ref(0)
const uploading = ref(false)
const uploadProgress = ref(0)
const uploadStatus = ref<'success' | 'exception' | 'warning' | ''>('')

const result = ref({
  success: false,
  title: '',
  subTitle: '',
  errors: [] as Array<{ index: number; message: string }>
})

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const acceptTypes = '.xlsx'

const moduleLabel = computed(() => {
  const labels = {
    customer: '客户',
    supplier: '供应商',
    person: '人员',
    project: '项目'
  }
  return labels[props.moduleType]
})

const templateHint = computed(() => {
  const hints = {
    customer: '必填字段：<strong>客户名称</strong><br/>可选字段：简称、联系人、联系电话、联系邮箱、地址、税号、备注',
    supplier: '必填字段：<strong>供应商名称</strong><br/>可选字段：简称、联系人、联系电话、联系邮箱、地址、税号、银行账号、开户行、备注',
    person: '必填字段：<strong>姓名、人员类型</strong><br/>可选字段：证件号码、电话、邮箱、银行账号、开户行、入职日期',
    project: '必填字段：<strong>项目名称、合同金额</strong><br/>可选字段：项目编号、客户ID、开始日期、结束日期、状态、备注'
  }
  return hints[props.moduleType]
})

const progressText = computed(() => {
  if (uploadProgress.value < 100) {
    return '正在上传文件...'
  }
  return '正在解析数据...'
})

const handleFileChange = (file: UploadFile) => {
  selectedFile.value = file.raw as UploadRawFile
}

const handleExceed = () => {
  ElMessage.warning('只能上传一个文件')
}

const removeFile = () => {
  selectedFile.value = null
  uploadRef.value?.clearFiles()
}

const handleUpload = async () => {
  if (!selectedFile.value) {
    ElMessage.warning('请选择要上传的文件')
    return
  }

  uploading.value = true
  currentStep.value = 1
  uploadProgress.value = 0
  uploadStatus.value = ''

  try {
    // 模拟上传进度
    const progressInterval = setInterval(() => {
      if (uploadProgress.value < 90) {
        uploadProgress.value += 10
      }
    }, 200)

    const response = await batchImportFile(props.moduleType, selectedFile.value)

    clearInterval(progressInterval)
    uploadProgress.value = 100
    uploadStatus.value = 'success'

    // 等待一下让用户看到进度完成
    await new Promise(resolve => setTimeout(resolve, 500))

    currentStep.value = 2

    const data = response.data.data
    const successCount = data.successCount || 0
    const failedCount = data.failedCount || 0
    const totalCount = data.totalCount || 0

    if (failedCount === 0) {
      result.value = {
        success: true,
        title: '导入成功',
        subTitle: `成功导入 ${successCount} 条数据`,
        errors: []
      }
      emit('success')
    } else {
      result.value = {
        success: false,
        title: '部分导入成功',
        subTitle: `成功 ${successCount} 条，失败 ${failedCount} 条，共 ${totalCount} 条`,
        errors: data.errors || []
      }
      emit('success')
    }
  } catch (error: any) {
    uploadStatus.value = 'exception'
    currentStep.value = 2
    result.value = {
      success: false,
      title: '导入失败',
      subTitle: error.response?.data?.message || '文件上传失败，请检查文件格式',
      errors: []
    }
  } finally {
    uploading.value = false
  }
}

const handleClose = () => {
  if (uploading.value) {
    ElMessage.warning('正在导入中，请稍候...')
    return
  }

  visible.value = false
  // 重置状态
  setTimeout(() => {
    currentStep.value = 0
    selectedFile.value = null
    uploadProgress.value = 0
    uploadStatus.value = ''
    uploadRef.value?.clearFiles()
    result.value = {
      success: false,
      title: '',
      subTitle: '',
      errors: []
    }
  }, 300)
}
</script>

<style scoped>
.import-container {
  padding: 20px 0;
}

.step-content {
  margin-top: 30px;
  min-height: 300px;
}

.file-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 16px;
  padding: 12px;
  background: var(--bg-hover);
  border-radius: 8px;
}

.file-info .el-icon {
  font-size: 20px;
  color: var(--color-primary);
}

.file-info span {
  flex: 1;
  font-size: 14px;
  color: var(--text-regular);
}

.template-hint {
  margin-top: 20px;
}

.template-hint :deep(.el-alert__content) {
  line-height: 1.6;
}

.upload-progress {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 40px;
}

.progress-text {
  margin-top: 16px;
  font-size: 14px;
  color: var(--text-regular);
}

.error-list {
  width: 100%;
  text-align: left;
}

.error-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  margin-bottom: 8px;
  background: var(--color-danger-light-5);
  border-radius: 4px;
  font-size: 13px;
  color: var(--text-regular);
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

:deep(.el-upload-dragger) {
  padding: 40px;
}

:deep(.el-icon--upload) {
  font-size: 67px;
  color: var(--text-disabled);
  margin-bottom: 16px;
}

:deep(.el-upload__text) {
  font-size: 14px;
  color: var(--text-regular);
}

:deep(.el-upload__text em) {
  color: var(--color-primary);
  font-style: normal;
}

:deep(.el-upload__tip) {
  font-size: 12px;
  color: var(--text-placeholder);
  margin-top: 8px;
}
</style>
