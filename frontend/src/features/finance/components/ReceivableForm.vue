<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑应收' : '新增应收'"
    width="600px"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="业务类型" prop="receivableTypeId">
        <el-select v-model="form.receivableTypeId" placeholder="请选择业务类型" clearable style="width: 100%">
          <el-option
            v-for="type in receivableTypes"
            :key="type.id"
            :label="type.name"
            :value="type.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="项目" prop="projectId">
        <SearchableSelect
          v-model="form.projectId"
          :options="projects"
          entity-name="项目"
          :clearable="false"
        />
        <div v-if="selectedProject" class="project-finance-card">
          <div class="project-finance-card__title">项目财务概况</div>
          <div class="project-finance-card__items">
            <div class="project-finance-card__item">
              <span class="label">合同金额</span>
              <span class="value">¥{{ selectedProject.contractAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</span>
            </div>
            <div class="project-finance-card__item">
              <span class="label">已收金额</span>
              <span class="value">¥{{ selectedProject.receivedAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</span>
            </div>
            <div class="project-finance-card__item">
              <span class="label">应收余额</span>
              <span class="value highlight">¥{{ selectedProject.receivableAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</span>
            </div>
          </div>
          <div class="project-finance-card__actions">
            <el-button size="small" @click="fillAmount(selectedProject.contractAmount)">填入合同金额</el-button>
            <el-button size="small" type="primary" @click="fillAmount(selectedProject.receivableAmount)">填入应收余额</el-button>
          </div>
        </div>
      </el-form-item>

      <el-form-item label="对方类型">
        <el-radio-group v-model="form.counterpartyType" @change="handleCounterpartyTypeChange">
          <el-radio label="customer">客户</el-radio>
          <el-radio label="supplier">供应商</el-radio>
          <el-radio label="person">人员</el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item v-if="form.counterpartyType === 'customer'" label="客户" prop="customerId">
        <SearchableSelect
          v-model="form.customerId"
          :options="customers"
          entity-name="客户"
          :clearable="false"
        />
      </el-form-item>

      <el-form-item v-if="form.counterpartyType === 'supplier'" label="供应商" prop="supplierId">
        <SearchableSelect
          v-model="form.supplierId"
          :options="suppliers"
          entity-name="供应商"
          :clearable="false"
        />
      </el-form-item>

      <el-form-item v-if="form.counterpartyType === 'person'" label="人员" prop="personId">
        <SearchableSelect
          v-model="form.personId"
          :options="persons"
          entity-name="人员"
          :clearable="false"
        />
      </el-form-item>

      <el-form-item label="应收金额" prop="totalAmount">
        <el-input-number
          v-model="form.totalAmount"
          :precision="2"
          :min="0.01"
          :controls="false"
          style="width: 100%"
          placeholder="请输入应收金额"
        />
      </el-form-item>

      <el-form-item label="到期日期" prop="dueDate">
        <el-date-picker
          v-model="form.dueDate"
          type="date"
          placeholder="请选择到期日期"
          style="width: 100%"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
        />
      </el-form-item>

      <el-form-item label="描述">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="3"
          placeholder="请输入描述信息"
          maxlength="500"
          show-word-limit
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createReceivable, updateReceivable, getReceivableTypes } from '@/features/finance/api/receivable'
import { getActiveProjects } from '@/features/master-data/projects/api/project'
import { getActiveCustomers } from '@/features/master-data/customers/api/customer'
import { getActiveSuppliers } from '@/features/master-data/suppliers/api/supplier'
import { getActivePersons } from '@/features/master-data/persons/api/person'
import type { Receivable, CreateReceivableRequest, ReceivableType } from '@/features/finance/types/receivable'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Customer } from '@/features/master-data/customers/types/customer'
import type { Supplier } from '@/features/master-data/suppliers/types/supplier'
import type { Person } from '@/features/master-data/persons/types/person'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'

interface Props {
  visible: boolean
  receivable?: Receivable | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const formRef = ref<FormInstance>()
const submitting = ref(false)
const projects = ref<Project[]>([])
const customers = ref<Customer[]>([])
const suppliers = ref<Supplier[]>([])
const persons = ref<Person[]>([])
const receivableTypes = ref<ReceivableType[]>([])

const loadReceivableTypes = async () => {
  try {
    const { data } = await getReceivableTypes()
    receivableTypes.value = data.data
  } catch {
    // 静默失败
  }
}

const form = reactive({
  projectId: undefined as number | undefined,
  counterpartyType: 'customer' as 'customer' | 'supplier' | 'person',
  customerId: undefined as number | undefined,
  supplierId: undefined as number | undefined,
  personId: undefined as number | undefined,
  receivableTypeId: undefined as number | undefined,
  totalAmount: 0,
  dueDate: '',
  description: ''
})

onMounted(() => {
  loadReceivableTypes()
})

const rules: FormRules = {
  projectId: [{ required: true, message: '请选择项目', trigger: 'change' }],
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  supplierId: [{ required: true, message: '请选择供应商', trigger: 'change' }],
  personId: [{ required: true, message: '请选择人员', trigger: 'change' }],
  totalAmount: [
    { required: true, message: '请输入应收金额', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金额必须大于0', trigger: 'blur' }
  ]
}

const isEdit = computed(() => !!props.receivable)

const selectedProject = computed(() => {
  if (!form.projectId) return null
  return projects.value.find(p => p.id === form.projectId) || null
})

const fillAmount = (amount: number) => {
  form.totalAmount = amount
}

const handleCounterpartyTypeChange = () => {
  form.customerId = undefined
  form.supplierId = undefined
  form.personId = undefined
}

const loadDropdownOptions = async () => {
  const results = await Promise.allSettled([
    getActiveProjects(),
    getActiveCustomers(),
    getActiveSuppliers(),
    getActivePersons()
  ])

  if (results[0].status === 'fulfilled') projects.value = results[0].value.data.data
  if (results[1].status === 'fulfilled') customers.value = results[1].value.data.data
  if (results[2].status === 'fulfilled') suppliers.value = results[2].value.data.data
  if (results[3].status === 'fulfilled') persons.value = results[3].value.data.data

  const failed = results.filter(r => r.status === 'rejected')
  if (failed.length > 0) {
    console.error(`下拉选项加载：${results.length - failed.length}/${results.length} 成功`)
  }
}

watch(() => props.visible, async (val) => {
  if (val) {
    await loadDropdownOptions()
    if (props.receivable) {
      const r = props.receivable
      const counterpartyType = r.customerId ? 'customer' : r.supplierId ? 'supplier' : r.personId ? 'person' : 'customer'
      Object.assign(form, {
        projectId: r.projectId,
        counterpartyType,
        customerId: r.customerId,
        supplierId: r.supplierId,
        personId: r.personId,
        receivableTypeId: r.receivableTypeId ?? undefined,
        totalAmount: r.totalAmount,
        dueDate: r.dueDate || '',
        description: r.description || ''
      })
    } else {
      formRef.value?.resetFields()
      form.projectId = undefined
      form.counterpartyType = 'customer'
      form.customerId = undefined
      form.supplierId = undefined
      form.personId = undefined
      form.receivableTypeId = undefined
      form.totalAmount = 0
      form.dueDate = ''
      form.description = ''
    }
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
      const data: CreateReceivableRequest = {
        projectId: form.projectId!,
        customerId: form.counterpartyType === 'customer' ? form.customerId : undefined,
        supplierId: form.counterpartyType === 'supplier' ? form.supplierId : undefined,
        personId: form.counterpartyType === 'person' ? form.personId : undefined,
        receivableTypeId: form.receivableTypeId || undefined,
        totalAmount: form.totalAmount,
        dueDate: form.dueDate || undefined,
        description: form.description || undefined
      }

      if (isEdit.value) {
        await updateReceivable(props.receivable!.id, data)
        ElMessage.success('更新成功')
      } else {
        await createReceivable(data)
        ElMessage.success('创建成功')
      }

      emit('success')
      handleClose()
    } catch (error) {
      ElMessage.error(isEdit.value ? '更新失败' : '创建失败')
    } finally {
      submitting.value = false
    }
  })
}
</script>

<style scoped>
.project-finance-card {
  margin-top: 8px;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 6px;
  border: 1px solid #e4e7ed;
}

.project-finance-card__title {
  font-size: 13px;
  font-weight: 600;
  color: #606266;
  margin-bottom: 8px;
}

.project-finance-card__items {
  display: flex;
  gap: 16px;
  margin-bottom: 10px;
}

.project-finance-card__item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.project-finance-card__item .label {
  font-size: 12px;
  color: #909399;
}

.project-finance-card__item .value {
  font-size: 14px;
  font-weight: 500;
  color: #303133;
}

.project-finance-card__item .value.highlight {
  color: #e6a23c;
}

.project-finance-card__actions {
  display: flex;
  gap: 8px;
}
</style>
