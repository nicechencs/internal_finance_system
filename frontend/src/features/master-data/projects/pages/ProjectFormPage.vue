<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑项目' : '新增项目'"
    width="700px"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="项目编号" prop="projectCode">
        <el-input v-model="form.projectCode" placeholder="请输入项目编号或点击一键生成" clearable>
          <template v-if="!isEdit" #append>
            <el-button @click="handleGenerateProjectCode" :loading="generatingCode">一键生成</el-button>
          </template>
        </el-input>
      </el-form-item>
      <el-form-item label="项目名称" prop="name">
        <SearchableInput
          v-model="form.name"
          :fetch-options="getActiveProjects"
          placeholder="请输入或选择项目名称"
        />
      </el-form-item>
      <el-form-item label="客户" prop="customerId">
        <el-select v-model="form.customerId" placeholder="请选择客户" clearable style="width: 100%">
          <el-option v-for="customer in customers" :key="customer.id" :label="customer.name" :value="customer.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="合同金额" prop="contractAmount">
        <el-input-number v-model="form.contractAmount" :precision="2" :min="0" :controls="false" style="width: 100%" />
      </el-form-item>
      <el-form-item label="项目日期" prop="dateRange">
        <el-date-picker
          v-model="form.dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </el-form-item>
      <el-form-item label="项目状态" prop="status">
        <el-radio-group v-model="form.status">
          <el-radio label="Active">进行中</el-radio>
          <el-radio label="Completed">已完成</el-radio>
          <el-radio label="Cancelled">已取消</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="项目描述" prop="description">
        <el-input v-model="form.description" type="textarea" :rows="4" placeholder="请输入项目描述" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createProject, updateProject, getActiveProjects, generateProjectCode } from '@/features/master-data/projects/api/project'
import { getActiveCustomers } from '@/features/master-data/customers/api/customer'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Customer } from '@/features/master-data/customers/types/customer'
import SearchableInput from '@/shared/ui/SearchableInput.vue'
import { toDateOnlyString } from '@/shared/utils/date'
import { ApiError } from '@/shared/types/error'
import { canMutateProject } from '@/features/master-data/projects/utils/projectStatus'

interface Props {
  visible: boolean
  project?: Project | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const formRef = ref<FormInstance>()
const submitting = ref(false)
const generatingCode = ref(false)
const customers = ref<Customer[]>([])

const form = reactive({
  projectCode: '',
  name: '',
  customerId: undefined as number | undefined,
  contractAmount: 0,
  dateRange: null as [string, string] | null,
  status: 'Active' as 'Active' | 'Completed' | 'Cancelled',
  description: ''
})

const rules: FormRules = {
  projectCode: [{
    required: true,
    validator: (_rule, value, callback) => {
      if (!value || !String(value).trim()) {
        callback(new Error('请输入项目编号或点击一键生成'))
        return
      }
      callback()
    },
    trigger: 'blur'
  }],
  name: [{ required: true, message: '请输入项目名称', trigger: 'blur' }],
  contractAmount: [{ required: true, message: '请输入合同金额', trigger: 'blur' }],
  status: [{ required: true, message: '请选择项目状态', trigger: 'change' }]
}

const isEdit = computed(() => !!props.project)

const loadCustomers = async () => {
  try {
    const { data } = await getActiveCustomers()
    customers.value = data.data
  } catch (error) {
    ElMessage.error('加载客户列表失败')
  }
}

const handleGenerateProjectCode = async () => {
  generatingCode.value = true
  try {
    const { data } = await generateProjectCode()
    form.projectCode = data.data
    formRef.value?.clearValidate('projectCode')
    ElMessage.success('项目编号已生成')
  } catch (error) {
    ElMessage.error('生成项目编号失败')
  } finally {
    generatingCode.value = false
  }
}

watch(() => props.visible, (val) => {
  if (val && props.project) {
    Object.assign(form, {
      projectCode: props.project.projectCode || '',
      name: props.project.name,
      customerId: props.project.customerId,
      contractAmount: props.project.contractAmount,
      dateRange: props.project.startDate && props.project.endDate
        ? [toDateOnlyString(props.project.startDate), toDateOnlyString(props.project.endDate)]
        : null,
      status: props.project.status,
      description: props.project.description || ''
    })
  } else if (val) {
    formRef.value?.resetFields()
    form.projectCode = ''
    form.name = ''
    form.customerId = undefined
    form.contractAmount = 0
    form.dateRange = null
    form.status = 'Active'
    form.description = ''
  }
})

const handleClose = () => {
  emit('update:visible', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      const data = {
        projectCode: form.projectCode.trim(),
        name: form.name,
        customerId: form.customerId,
        contractAmount: form.contractAmount,
        startDate: form.dateRange ? form.dateRange[0] : undefined,
        endDate: form.dateRange ? form.dateRange[1] : undefined,
        status: form.status,
        description: form.description || undefined
      }
      if (isEdit.value) {
        if (!canMutateProject(props.project?.status)) {
          ElMessage.warning('已取消的项目不允许编辑')
          return
        }
        await updateProject(props.project!.id, data)
        ElMessage.success('更新成功')
        emit('success')
      } else {
        const response = await createProject(data)
        ElMessage.success('创建成功')
        emit('success', response.data.data)
      }
    } catch (error) {
      if (!(error instanceof ApiError)) {
        ElMessage.error(isEdit.value ? '更新失败' : '创建失败')
      }
    } finally {
      submitting.value = false
    }
  })
}

onMounted(() => {
  loadCustomers()
})
</script>

<style scoped>
</style>
