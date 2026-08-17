<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑应付' : '新增应付'"
    width="600px"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="对方类型">
        <el-radio-group v-model="form.counterpartyType" @change="handleCounterpartyTypeChange">
          <el-radio label="supplier">供应商</el-radio>
          <el-radio label="customer">客户</el-radio>
          <el-radio label="person">人员</el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item v-if="form.counterpartyType === 'supplier'" label="供应商" prop="supplierId">
        <SearchableSelect
          v-model="form.supplierId"
          :options="suppliers"
          entity-name="供应商"
          :clearable="false"
        />
      </el-form-item>

      <el-form-item v-if="form.counterpartyType === 'customer'" label="客户" prop="customerId">
        <SearchableSelect
          v-model="form.customerId"
          :options="customers"
          entity-name="客户"
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

      <el-form-item label="项目" prop="projectId">
        <SearchableSelect
          v-model="form.projectId"
          :options="projects"
          entity-name="项目"
          placeholder="请选择项目（可选）"
        />
      </el-form-item>

      <el-form-item label="业务类型" prop="payableTypeId">
        <SearchableSelect
          v-model="form.payableTypeId"
          :options="payableTypes"
          entity-name="业务类型"
          placeholder="请选择业务类型（可选）"
        />
      </el-form-item>

      <el-form-item label="应付金额" prop="totalAmount">
        <el-input-number
          v-model="form.totalAmount"
          :precision="2"
          :min="0.01"
          :controls="false"
          style="width: 100%"
          placeholder="请输入应付金额"
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
import { ref, reactive, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createPayable, updatePayable, getPayableTypes } from '@/features/finance/api/payable'
import { getActiveProjects } from '@/features/master-data/projects/api/project'
import { getActiveSuppliers } from '@/features/master-data/suppliers/api/supplier'
import { getActiveCustomers } from '@/features/master-data/customers/api/customer'
import { getActivePersons } from '@/features/master-data/persons/api/person'
import type { Payable, CreatePayableRequest, PayableType } from '@/features/finance/types/payable'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Supplier } from '@/features/master-data/suppliers/types/supplier'
import type { Customer } from '@/features/master-data/customers/types/customer'
import type { Person } from '@/features/master-data/persons/types/person'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'

interface Props {
  visible: boolean
  payable?: Payable | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const formRef = ref<FormInstance>()
const submitting = ref(false)
const projects = ref<Project[]>([])
const suppliers = ref<Supplier[]>([])
const customers = ref<Customer[]>([])
const persons = ref<Person[]>([])
const payableTypes = ref<PayableType[]>([])

const form = reactive({
  counterpartyType: 'supplier' as 'supplier' | 'customer' | 'person',
  supplierId: undefined as number | undefined,
  customerId: undefined as number | undefined,
  personId: undefined as number | undefined,
  projectId: undefined as number | undefined,
  payableTypeId: undefined as number | undefined,
  totalAmount: 0,
  dueDate: '',
  description: ''
})

const rules: FormRules = {
  supplierId: [{ required: true, message: '请选择供应商', trigger: 'change' }],
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  personId: [{ required: true, message: '请选择人员', trigger: 'change' }],
  totalAmount: [
    { required: true, message: '请输入应付金额', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金额必须大于0', trigger: 'blur' }
  ]
}

const isEdit = computed(() => !!props.payable)

const handleCounterpartyTypeChange = () => {
  form.supplierId = undefined
  form.customerId = undefined
  form.personId = undefined
}

const loadDropdownOptions = async () => {
  const results = await Promise.allSettled([
    getActiveProjects(),
    getActiveSuppliers(),
    getActiveCustomers(),
    getActivePersons(),
    getPayableTypes()
  ])

  if (results[0].status === 'fulfilled') projects.value = results[0].value.data.data
  if (results[1].status === 'fulfilled') suppliers.value = results[1].value.data.data
  if (results[2].status === 'fulfilled') customers.value = results[2].value.data.data
  if (results[3].status === 'fulfilled') persons.value = results[3].value.data.data
  if (results[4].status === 'fulfilled') {
    // 只显示启用的业务类型
    const allTypes = results[4].value.data.data
    payableTypes.value = allTypes.filter((t: PayableType) => t.isActive)
  }

  const failed = results.filter(r => r.status === 'rejected')
  if (failed.length > 0) {
    console.error(`下拉选项加载：${results.length - failed.length}/${results.length} 成功`)
  }
}

watch(() => props.visible, async (val) => {
  if (val) {
    await loadDropdownOptions()
    if (props.payable) {
      const p = props.payable
      const counterpartyType = p.supplierId ? 'supplier' : p.customerId ? 'customer' : p.personId ? 'person' : 'supplier'
      Object.assign(form, {
        counterpartyType,
        supplierId: p.supplierId,
        customerId: p.customerId,
        personId: p.personId,
        projectId: p.projectId || undefined,
        payableTypeId: p.payableTypeId || undefined,
        totalAmount: p.totalAmount,
        dueDate: p.dueDate || '',
        description: p.description || ''
      })
    } else {
      formRef.value?.resetFields()
      form.counterpartyType = 'supplier'
      form.supplierId = undefined
      form.customerId = undefined
      form.personId = undefined
      form.projectId = undefined
      form.payableTypeId = undefined
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
      const data: CreatePayableRequest = {
        supplierId: form.counterpartyType === 'supplier' ? form.supplierId : undefined,
        customerId: form.counterpartyType === 'customer' ? form.customerId : undefined,
        personId: form.counterpartyType === 'person' ? form.personId : undefined,
        projectId: form.projectId || undefined,
        payableTypeId: form.payableTypeId || undefined,
        totalAmount: form.totalAmount,
        dueDate: form.dueDate || undefined,
        description: form.description || undefined
      }

      if (isEdit.value) {
        await updatePayable(props.payable!.id, data)
        ElMessage.success('更新成功')
      } else {
        await createPayable(data)
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
/* 表单样式继承自 Dialog 组件 */
</style>
