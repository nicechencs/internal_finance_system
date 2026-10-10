<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEditMode ? '编辑交付记录' : '新增交付记录'"
    width="640px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="交付物" prop="title">
        <el-input v-model="form.title" maxlength="200" show-word-limit placeholder="请输入交付物名称" />
      </el-form-item>
      <el-form-item label="说明">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="3"
          maxlength="1000"
          show-word-limit
          placeholder="可选，说明交付内容"
        />
      </el-form-item>
      <el-form-item label="关联项目">
        <SearchableSelect
          v-model="form.projectId"
          :options="projects"
          entity-name="项目"
          placeholder="可选，不关联项目"
          clearable
        />
      </el-form-item>
      <el-form-item label="验收状态" prop="status">
        <el-select v-model="form.status" style="width: 100%">
          <el-option label="待验收" value="Pending" />
          <el-option label="已验收" value="Accepted" />
          <el-option label="已驳回" value="Rejected" />
        </el-select>
      </el-form-item>
      <el-form-item label="备注">
        <el-input
          v-model="form.notes"
          type="textarea"
          :rows="2"
          maxlength="1000"
          show-word-limit
          placeholder="可选"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEditMode ? '保存' : '创建' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import { createPersonDelivery, updatePersonDelivery } from '@/features/master-data/persons/api/person'
import type { PersonDelivery, PersonDeliveryRequest, PersonDeliveryStatus } from '@/features/master-data/persons/types/person'

interface ProjectOption {
  id: number
  name: string
}

const props = defineProps<{
  modelValue: boolean
  personId: number
  delivery?: PersonDelivery | null
  projects: ProjectOption[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)
const isEditMode = computed(() => !!props.delivery)

const form = reactive({
  title: '',
  description: '',
  projectId: null as number | null,
  status: 'Pending' as PersonDeliveryStatus,
  notes: ''
})

const rules: FormRules = {
  title: [{ required: true, message: '请输入交付物名称', trigger: 'blur' }],
  status: [{ required: true, message: '请选择验收状态', trigger: 'change' }]
}

const resetForm = () => {
  form.title = props.delivery?.title ?? ''
  form.description = props.delivery?.description ?? ''
  form.projectId = props.delivery?.projectId ?? null
  form.status = (props.delivery?.status as PersonDeliveryStatus) || 'Pending'
  form.notes = props.delivery?.notes ?? ''
}

watch(() => props.modelValue, (visible) => {
  if (visible) {
    resetForm()
  }
})

const handleClose = () => {
  emit('update:modelValue', false)
}

const handleSubmit = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  const payload: PersonDeliveryRequest = {
    title: form.title.trim(),
    description: form.description.trim() || undefined,
    projectId: form.projectId,
    status: form.status,
    notes: form.notes.trim() || undefined
  }

  if (props.delivery?.status === 'Accepted' && form.status === 'Accepted') {
    payload.acceptedAt = props.delivery.acceptedAt
    payload.acceptedBy = props.delivery.acceptedBy
  }

  submitting.value = true
  try {
    if (props.delivery) {
      await updatePersonDelivery(props.personId, props.delivery.id, payload)
      ElMessage.success('交付记录已更新')
    } else {
      await createPersonDelivery(props.personId, payload)
      ElMessage.success('交付记录已创建')
    }
    emit('success')
    handleClose()
  } catch {
    ElMessage.error(props.delivery ? '更新交付记录失败' : '创建交付记录失败')
  } finally {
    submitting.value = false
  }
}
</script>
