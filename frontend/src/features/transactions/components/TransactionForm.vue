<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑交易' : '新增交易'"
    width="800px"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="交易日期" prop="transactionDate">
        <el-date-picker
          v-model="form.transactionDate"
          type="date"
          placeholder="选择日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </el-form-item>
      <el-form-item label="交易类型" prop="transactionType">
        <el-radio-group v-model="form.transactionType">
          <el-radio label="Income">收入</el-radio>
          <el-radio label="Expense">支出</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="金额" prop="amount">
        <el-input-number v-model="form.amount" :precision="2" :min="0.01" :controls="false" style="width: 100%" />
      </el-form-item>
      <el-form-item label="账户" prop="accountId">
        <SearchableSelect
          v-model="form.accountId"
          :options="transactionAccounts"
          entity-name="账户"
          :clearable="false"
        />
        <div class="field-tip">仅展示可用于经营收支的账户，定期账户请前往定期台账。</div>
      </el-form-item>
      <el-form-item label="分类" prop="categoryId">
        <el-select v-model="form.categoryId" placeholder="请选择分类" filterable clearable style="width: 100%">
          <el-option v-for="category in filteredCategories" :key="category.id" :label="category.name" :value="category.id" />
          <template #footer>
            <el-button v-if="!isAddingCategory" text bg size="small" @click="isAddingCategory = true">
              + 新建分类
            </el-button>
            <template v-else>
              <el-input v-model="newCategoryName" size="small" placeholder="输入分类名称" @keyup.enter="handleCreateCategory" />
              <div style="display: flex; justify-content: flex-end; margin-top: 6px; gap: 6px;">
                <el-button size="small" @click="isAddingCategory = false; newCategoryName = ''">取消</el-button>
                <el-button type="primary" size="small" :loading="creatingCategory" @click="handleCreateCategory">确认</el-button>
              </div>
            </template>
          </template>
        </el-select>
      </el-form-item>
      <el-form-item label="项目" prop="projectId">
        <SearchableSelect
          v-model="form.projectId"
          :options="projects"
          entity-name="项目"
        />
      </el-form-item>
      <el-form-item label="对方类型">
        <el-radio-group v-model="form.counterpartyType" @change="handleCounterpartyTypeChange">
          <el-radio label="">无</el-radio>
          <el-radio label="customer">客户</el-radio>
          <el-radio label="supplier">供应商</el-radio>
          <el-radio label="person">人员</el-radio>
        </el-radio-group>
        <el-text v-if="transaction?.counterparty" type="info" size="small" style="margin-left: 12px">
          原始对方：{{ transaction.counterparty }}
        </el-text>
      </el-form-item>
      <el-form-item v-if="form.counterpartyType === 'customer'" label="客户">
        <SearchableSelect
          v-model="form.customerId"
          :options="customers"
          entity-name="客户"
        />
      </el-form-item>
      <el-form-item v-if="form.counterpartyType === 'supplier'" label="供应商">
        <SearchableSelect
          v-model="form.supplierId"
          :options="suppliers"
          entity-name="供应商"
        />
      </el-form-item>
      <el-form-item v-if="form.counterpartyType === 'person'" label="人员">
        <SearchableSelect
          v-model="form.personId"
          :options="persons"
          entity-name="人员"
        />
      </el-form-item>
      <el-form-item label="描述" prop="description">
        <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入描述" />
      </el-form-item>
      <el-divider>费用分摊配置（可选）</el-divider>
      <el-form-item>
        <el-button type="primary" size="small" @click="handleAddAllocation">添加分摊项</el-button>
        <el-text type="info" size="small" style="margin-left: 10px">如需将费用分摊到多个项目或人员，请添加分摊项</el-text>
      </el-form-item>
      <div v-if="form.allocations && form.allocations.length > 0" class="allocations">
        <el-card v-for="(allocation, index) in form.allocations" :key="index" class="allocation-item" shadow="never">
          <el-form-item label="项目">
            <SearchableSelect
              v-model="allocation.projectId"
              :options="projects"
              entity-name="项目"
            />
          </el-form-item>
          <el-form-item label="分摊方式">
            <el-radio-group v-model="allocation.allocationType">
              <el-radio label="amount">固定金额</el-radio>
              <el-radio label="rate">百分比</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item v-if="allocation.allocationType === 'amount'" label="金额">
            <el-input-number v-model="allocation.amount" :precision="2" :min="0.01" :controls="false" style="width: 100%" />
          </el-form-item>
          <el-form-item v-else label="百分比">
            <el-input-number v-model="allocation.allocationRate" :precision="2" :min="0.01" :max="100" :controls="false" style="width: 100%" />
            <span style="margin-left: 5px">%</span>
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="allocation.description" placeholder="请输入备注" />
          </el-form-item>
          <el-button type="danger" size="small" @click="handleRemoveAllocation(index)">删除</el-button>
        </el-card>
        <el-alert v-if="allocationValidation.message" :title="allocationValidation.message" :type="allocationValidation.type" :closable="false" style="margin-top: 10px" />
      </div>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createTransaction, updateTransaction } from '@/features/transactions/api/transaction'
import { getActiveAccounts } from '@/features/master-data/accounts/api/account'
import { getActiveCategories, createCategory } from '@/features/master-data/categories/api/category'
import { getActiveProjects } from '@/features/master-data/projects/api/project'
import { getActiveCustomers } from '@/features/master-data/customers/api/customer'
import { getActiveSuppliers } from '@/features/master-data/suppliers/api/supplier'
import { getActivePersons } from '@/features/master-data/persons/api/person'
import type { Transaction, CreateAllocationRequest, CreateTransactionRequest } from '@/features/transactions/types/transaction'
import type { Account } from '@/features/master-data/accounts/types/account'
import type { Category } from '@/features/master-data/categories/types/category'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Customer } from '@/features/master-data/customers/types/customer'
import type { Supplier } from '@/features/master-data/suppliers/types/supplier'
import type { Person } from '@/features/master-data/persons/types/person'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import { getTodayDateString, toDateOnlyString } from '@/shared/utils/date'

interface Props {
  visible: boolean
  transaction?: Transaction | null
  draft?: Partial<CreateTransactionRequest> | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const formRef = ref<FormInstance>()
const submitting = ref(false)
const accounts = ref<Account[]>([])
const transactionAccounts = computed(() => {
  const editingAccountId = props.transaction?.accountId
  return accounts.value.filter(account => {
    if (account.accountType !== 'FixedDeposit') return true
    return account.id === editingAccountId
  })
})
const categories = ref<Category[]>([])
const projects = ref<Project[]>([])
const customers = ref<Customer[]>([])
const suppliers = ref<Supplier[]>([])
const persons = ref<Person[]>([])
const isAddingCategory = ref(false)
const newCategoryName = ref('')
const creatingCategory = ref(false)

interface AllocationForm extends CreateAllocationRequest {
  allocationType: 'amount' | 'rate'
}

const form = reactive({
  transactionDate: getTodayDateString(),
  transactionType: 'Expense' as 'Income' | 'Expense',
  amount: undefined as number | undefined,
  accountId: undefined as number | undefined,
  categoryId: undefined as number | undefined,
  projectId: undefined as number | undefined,
  counterpartyType: '' as '' | 'customer' | 'supplier' | 'person',
  customerId: undefined as number | undefined,
  supplierId: undefined as number | undefined,
  personId: undefined as number | undefined,
  description: '',
  allocations: [] as AllocationForm[]
})

const filteredCategories = computed(() =>
  categories.value.filter(category => category.categoryType === form.transactionType)
)

const getCounterpartyType = (source?: {
  customerId?: number
  supplierId?: number
  personId?: number
} | null) => {
  if (source?.customerId) return 'customer'
  if (source?.supplierId) return 'supplier'
  if (source?.personId) return 'person'
  return ''
}

const mapDraftAllocations = (allocations?: CreateAllocationRequest[]) =>
  allocations?.map(allocation => ({
    projectId: allocation.projectId,
    personId: allocation.personId,
    amount: allocation.amount,
    allocationRate: allocation.allocationRate,
    description: allocation.description ?? '',
    allocationType: allocation.allocationRate ? 'rate' : 'amount'
  })) ?? []

const resetCreateForm = (draft?: Partial<CreateTransactionRequest> | null) => {
  Object.assign(form, {
    transactionDate: draft?.transactionDate ?? getTodayDateString(),
    transactionType: draft?.transactionType ?? 'Expense',
    amount: draft?.amount,
    accountId: undefined,
    categoryId: draft?.categoryId,
    projectId: draft?.projectId,
    counterpartyType: getCounterpartyType(draft),
    customerId: draft?.customerId,
    supplierId: draft?.supplierId,
    personId: draft?.personId,
    description: draft?.description ?? '',
    allocations: mapDraftAllocations(draft?.allocations)
  })
}

const rules: FormRules = {
  transactionDate: [{ required: true, message: '请选择交易日期', trigger: 'change' }],
  transactionType: [{ required: true, message: '请选择交易类型', trigger: 'change' }],
  amount: [{ required: true, message: '请输入金额', trigger: 'blur' }],
  accountId: [{ required: true, message: '请选择账户', trigger: 'change' }]
}

const isEdit = computed(() => !!props.transaction)

const allocationValidation = computed(() => {
  if (!form.allocations || form.allocations.length === 0) {
    return { message: '', type: 'info' as const }
  }
  let totalAmount = 0
  let totalRate = 0
  for (const allocation of form.allocations) {
    if (allocation.allocationType === 'amount' && allocation.amount) {
      totalAmount += allocation.amount
    } else if (allocation.allocationType === 'rate' && allocation.allocationRate) {
      totalRate += allocation.allocationRate
    }
  }
  const hasAmount = form.allocations.some(a => a.allocationType === 'amount')
  const hasRate = form.allocations.some(a => a.allocationType === 'rate')
  if (hasAmount && hasRate) {
    return { message: '不支持混合使用固定金额和百分比分摊', type: 'error' as const }
  }
  if (hasAmount) {
    const diff = Math.abs(totalAmount - (form.amount ?? 0))
    if (diff > 0.01) {
      return { message: `分摊金额总和(${totalAmount.toFixed(2)})必须等于交易金额(${(form.amount ?? 0).toFixed(2)})`, type: 'error' as const }
    }
    return { message: `分摊金额总和: ${totalAmount.toFixed(2)}`, type: 'success' as const }
  }
  if (hasRate) {
    const diff = Math.abs(totalRate - 100)
    if (diff > 0.01) {
      return { message: `分摊百分比总和(${totalRate.toFixed(2)}%)必须等于100%`, type: 'error' as const }
    }
    return { message: `分摊百分比总和: ${totalRate.toFixed(2)}%`, type: 'success' as const }
  }
  return { message: '', type: 'info' as const }
})

const handleCounterpartyTypeChange = () => {
  form.customerId = undefined
  form.supplierId = undefined
  form.personId = undefined
}

watch([() => form.transactionType, categories], () => {
  const selectedCategory = categories.value.find(category => category.id === form.categoryId)
  if (selectedCategory && selectedCategory.categoryType !== form.transactionType) {
    form.categoryId = undefined
  }
}, { deep: true })

const handleCreateCategory = async () => {
  const name = newCategoryName.value.trim()
  if (!name) return
  creatingCategory.value = true
  try {
    const categoryType = form.transactionType === 'Income' ? 'Income' : 'Expense'
    const { data } = await createCategory({ name, categoryType })
    categories.value.push(data.data)
    form.categoryId = data.data.id
    ElMessage.success(`分类"${name}"创建成功`)
    isAddingCategory.value = false
    newCategoryName.value = ''
  } catch (error) {
    ElMessage.error('创建分类失败')
  } finally {
    creatingCategory.value = false
  }
}

const loadDropdownOptions = async () => {
  const results = await Promise.allSettled([
    getActiveAccounts(),
    getActiveCategories(),
    getActiveProjects(),
    getActiveCustomers(),
    getActiveSuppliers(),
    getActivePersons()
  ])

  if (results[0].status === 'fulfilled') accounts.value = results[0].value.data.data
  if (results[1].status === 'fulfilled') categories.value = results[1].value.data.data
  if (results[2].status === 'fulfilled') projects.value = results[2].value.data.data
  if (results[3].status === 'fulfilled') customers.value = results[3].value.data.data
  if (results[4].status === 'fulfilled') suppliers.value = results[4].value.data.data
  if (results[5].status === 'fulfilled') persons.value = results[5].value.data.data

  const failed = results.filter(r => r.status === 'rejected')
  if (failed.length > 0) {
    console.error(`下拉选项加载：${results.length - failed.length}/${results.length} 成功`)
  }
}

watch(() => props.visible, async (val) => {
  if (val) {
    await loadDropdownOptions()
    if (props.transaction) {
      const t = props.transaction
      const counterpartyType = getCounterpartyType(t)
      Object.assign(form, {
        transactionDate: toDateOnlyString(t.transactionDate),
        transactionType: t.transactionType,
        amount: t.amount,
        accountId: t.accountId,
        categoryId: t.categoryId,
        projectId: t.projectId,
        counterpartyType,
        customerId: t.customerId,
        supplierId: t.supplierId,
        personId: t.personId,
        description: t.description,
        allocations: t.allocations.map(a => ({
          projectId: a.projectId,
          personId: a.personId,
          amount: a.amount,
          allocationRate: a.allocationRate,
          description: a.description,
          allocationType: a.allocationRate ? 'rate' : 'amount'
        }))
      })
    } else {
      // 重置表单为新增状态
      Object.assign(form, {
        transactionDate: props.draft?.transactionDate ?? getTodayDateString(),
        transactionType: props.draft?.transactionType ?? 'Expense',
        amount: props.draft?.amount,
        accountId: undefined,
        categoryId: props.draft?.categoryId,
        projectId: props.draft?.projectId,
        counterpartyType: getCounterpartyType(props.draft),
        customerId: props.draft?.customerId,
        supplierId: props.draft?.supplierId,
        personId: props.draft?.personId,
        description: props.draft?.description ?? '',
        allocations: mapDraftAllocations(props.draft?.allocations)
      })
      formRef.value?.clearValidate()
    }
  }
})

const handleAddAllocation = () => {
  form.allocations.push({
    projectId: undefined,
    personId: undefined,
    amount: undefined,
    allocationRate: undefined,
    description: '',
    allocationType: 'amount'
  })
}

const handleRemoveAllocation = (index: number) => {
  form.allocations.splice(index, 1)
}

const handleClose = () => {
  emit('update:visible', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    if (form.allocations.length > 0 && allocationValidation.value.type === 'error') {
      ElMessage.error(allocationValidation.value.message)
      return
    }
    submitting.value = true
    try {
      const allocations = form.allocations.length > 0
        ? form.allocations.map(a => ({
            projectId: a.projectId,
            personId: a.personId,
            amount: a.allocationType === 'amount' ? a.amount : undefined,
            allocationRate: a.allocationType === 'rate' ? a.allocationRate : undefined,
            description: a.description
          }))
        : undefined
      const data = {
        transactionDate: form.transactionDate,
        transactionType: form.transactionType,
        amount: form.amount!,
        accountId: form.accountId!,
        categoryId: form.categoryId,
        projectId: form.projectId,
        customerId: form.counterpartyType === 'customer' ? form.customerId : undefined,
        supplierId: form.counterpartyType === 'supplier' ? form.supplierId : undefined,
        personId: form.counterpartyType === 'person' ? form.personId : undefined,
        description: form.description,
        allocations
      }
      if (isEdit.value) {
        await updateTransaction(props.transaction!.id, data)
        ElMessage.success('更新成功')
      } else {
        const response = await createTransaction(data)
        ElMessage.success('创建成功')
        emit('success', response.data.data)
        return
      }
      emit('success')
    } catch (error) {
      ElMessage.error(isEdit.value ? '更新失败' : '创建失败')
    } finally {
      submitting.value = false
    }
  })
}
</script>

<style scoped>
.allocations {
  margin-top: 10px;
}
.allocation-item {
  margin-bottom: 10px;
  border: 1px solid var(--border-base);
}
</style>
