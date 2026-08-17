<template>
  <!-- Dialog 模式 -->
  <el-dialog
    v-if="isDialogMode"
    :model-value="visible"
    :title="isEdit ? '编辑客户' : '新增客户'"
    width="600px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="客户名称" prop="name">
        <SearchableInput
          v-model="formData.name"
          :fetch-options="getActiveCustomers"
          placeholder="请输入或选择客户名称"
        />
      </el-form-item>

      <el-form-item label="简称" prop="shortName">
        <el-input v-model="formData.shortName" placeholder="请输入简称" />
      </el-form-item>

      <el-form-item label="联系人" prop="contactPerson">
        <el-input v-model="formData.contactPerson" placeholder="请输入联系人" />
      </el-form-item>

      <el-form-item label="联系电话" prop="contactPhone">
        <el-input v-model="formData.contactPhone" placeholder="请输入联系电话" />
      </el-form-item>

      <el-form-item label="联系邮箱" prop="contactEmail">
        <el-input v-model="formData.contactEmail" placeholder="请输入联系邮箱" />
      </el-form-item>

      <el-form-item label="地址" prop="address">
        <el-input
          v-model="formData.address"
          type="textarea"
          :rows="2"
          placeholder="请输入地址"
        />
      </el-form-item>

      <el-form-item label="税号" prop="taxNumber">
        <el-input v-model="formData.taxNumber" placeholder="请输入税号" />
      </el-form-item>

      <el-form-item label="银行账号" prop="bankAccount">
        <el-input v-model="formData.bankAccount" placeholder="请输入银行账号" clearable />
      </el-form-item>

      <el-form-item label="开户行" prop="bankName">
        <el-input v-model="formData.bankName" placeholder="请输入开户行" clearable />
      </el-form-item>

      <el-form-item label="备注" prop="description">
        <el-input
          v-model="formData.description"
          type="textarea"
          :rows="3"
          placeholder="请输入备注"
        />
      </el-form-item>

      <el-form-item label="状态" prop="isActive" v-if="isEdit">
        <el-switch v-model="formData.isActive" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="loading" @click="handleSubmit">
        确定
      </el-button>
    </template>
  </el-dialog>

  <!-- Page 模式 -->
  <div v-else class="customer-form-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-button link @click="handleBack">
            <el-icon><ArrowLeft /></el-icon>
            返回
          </el-button>
          <span class="title">{{ isEdit ? '编辑客户' : '新增客户' }}</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="formData"
        :rules="rules"
        label-width="100px"
        style="max-width: 600px"
      >
        <el-form-item label="客户名称" prop="name">
          <SearchableInput
            v-model="formData.name"
            :fetch-options="getActiveCustomers"
            placeholder="请输入或选择客户名称"
          />
        </el-form-item>

        <el-form-item label="简称" prop="shortName">
          <el-input v-model="formData.shortName" placeholder="请输入简称" />
        </el-form-item>

        <el-form-item label="联系人" prop="contactPerson">
          <el-input v-model="formData.contactPerson" placeholder="请输入联系人" />
        </el-form-item>

        <el-form-item label="联系电话" prop="contactPhone">
          <el-input v-model="formData.contactPhone" placeholder="请输入联系电话" />
        </el-form-item>

        <el-form-item label="联系邮箱" prop="contactEmail">
          <el-input v-model="formData.contactEmail" placeholder="请输入联系邮箱" />
        </el-form-item>

        <el-form-item label="地址" prop="address">
          <el-input
            v-model="formData.address"
            type="textarea"
            :rows="2"
            placeholder="请输入地址"
          />
        </el-form-item>

        <el-form-item label="税号" prop="taxNumber">
          <el-input v-model="formData.taxNumber" placeholder="请输入税号" />
        </el-form-item>

        <el-form-item label="银行账号" prop="bankAccount">
          <el-input v-model="formData.bankAccount" placeholder="请输入银行账号" clearable />
        </el-form-item>

        <el-form-item label="开户行" prop="bankName">
          <el-input v-model="formData.bankName" placeholder="请输入开户行" clearable />
        </el-form-item>

        <el-form-item label="备注" prop="description">
          <el-input
            v-model="formData.description"
            type="textarea"
            :rows="3"
            placeholder="请输入备注"
          />
        </el-form-item>

        <el-form-item label="状态" prop="isActive" v-if="isEdit">
          <el-switch v-model="formData.isActive" />
        </el-form-item>

        <el-form-item>
          <el-button @click="handleBack">取消</el-button>
          <el-button type="primary" :loading="loading" @click="handleSubmit">
            保存
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '@/features/master-data/customers/types/customer'
import { createCustomer, updateCustomer, getActiveCustomers, getCustomerById } from '@/features/master-data/customers/api/customer'
import SearchableInput from '@/shared/ui/SearchableInput.vue'

interface Props {
  visible?: boolean
  customer?: Customer | null
}

const props = withDefaults(defineProps<Props>(), {
  customer: null
})

const emit = defineEmits(['update:visible', 'success'])

const route = useRoute()
const router = useRouter()
const formRef = ref<FormInstance>()
const loading = ref(false)

// 判断是 Dialog 模式还是 Page 模式
const isDialogMode = computed(() => props.visible !== undefined)

// 判断是编辑模式还是创建模式
const isEdit = computed(() => {
  if (isDialogMode.value) {
    return !!props.customer
  } else {
    return !!route.params.id
  }
})

const formData = reactive<CreateCustomerRequest & UpdateCustomerRequest & { isActive: boolean }>({
  name: '',
  shortName: '',
  contactPerson: '',
  contactPhone: '',
  contactEmail: '',
  address: '',
  taxNumber: '',
  bankAccount: '',
  bankName: '',
  description: '',
  isActive: true
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
  contactEmail: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ]
}

// 加载客户数据（Page 模式编辑时使用）
const loadCustomer = async (id: number) => {
  try {
    loading.value = true
    const { data } = await getCustomerById(id)
    const customer = data.data
    formData.name = customer.name
    formData.shortName = customer.shortName || ''
    formData.contactPerson = customer.contactPerson || ''
    formData.contactPhone = customer.contactPhone || ''
    formData.contactEmail = customer.contactEmail || ''
    formData.address = customer.address || ''
    formData.taxNumber = customer.taxNumber || ''
    formData.bankAccount = customer.bankAccount || ''
    formData.bankName = customer.bankName || ''
    formData.description = customer.description || ''
    formData.isActive = customer.isActive
  } catch (error) {
    ElMessage.error('加载客户数据失败')
    handleBack()
  } finally {
    loading.value = false
  }
}

// Dialog 模式：监听 visible 和 customer 变化
watch(() => props.visible, (val) => {
  if (val && isDialogMode.value) {
    if (props.customer) {
      // Edit mode
      formData.name = props.customer.name
      formData.shortName = props.customer.shortName || ''
      formData.contactPerson = props.customer.contactPerson || ''
      formData.contactPhone = props.customer.contactPhone || ''
      formData.contactEmail = props.customer.contactEmail || ''
      formData.address = props.customer.address || ''
      formData.taxNumber = props.customer.taxNumber || ''
      formData.bankAccount = props.customer.bankAccount || ''
      formData.bankName = props.customer.bankName || ''
      formData.description = props.customer.description || ''
      formData.isActive = props.customer.isActive
    } else {
      // Add mode - reset form
      resetForm()
    }
    formRef.value?.clearValidate()
  }
})

// Page 模式：组件挂载时加载数据
onMounted(() => {
  if (!isDialogMode.value && route.params.id) {
    loadCustomer(Number(route.params.id))
  }
})

const resetForm = () => {
  formData.name = ''
  formData.shortName = ''
  formData.contactPerson = ''
  formData.contactPhone = ''
  formData.contactEmail = ''
  formData.address = ''
  formData.taxNumber = ''
  formData.bankAccount = ''
  formData.bankName = ''
  formData.description = ''
  formData.isActive = true
}

const handleClose = () => {
  emit('update:visible', false)
}

const handleBack = () => {
  router.push('/customers')
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        if (isEdit.value) {
          // Update
          const customerId = isDialogMode.value
            ? props.customer!.id
            : Number(route.params.id)

          const updateData: UpdateCustomerRequest = {
            name: formData.name,
            shortName: formData.shortName,
            contactPerson: formData.contactPerson,
            contactPhone: formData.contactPhone,
            contactEmail: formData.contactEmail,
            address: formData.address,
            taxNumber: formData.taxNumber,
            bankAccount: formData.bankAccount,
            bankName: formData.bankName,
            description: formData.description,
            isActive: formData.isActive
          }
          await updateCustomer(customerId, updateData)
          ElMessage.success('更新成功')
        } else {
          // Create
          const createData: CreateCustomerRequest = {
            name: formData.name,
            shortName: formData.shortName,
            contactPerson: formData.contactPerson,
            contactPhone: formData.contactPhone,
            contactEmail: formData.contactEmail,
            address: formData.address,
            taxNumber: formData.taxNumber,
            bankAccount: formData.bankAccount,
            bankName: formData.bankName,
            description: formData.description
          }
          await createCustomer(createData)
          ElMessage.success('创建成功')
        }

        // Dialog 模式：触发 success 事件
        if (isDialogMode.value) {
          emit('success')
        } else {
          // Page 模式：跳转回列表页
          router.push('/customers')
        }
      } catch (error) {
        console.error('操作失败:', error)
      } finally {
        loading.value = false
      }
    }
  })
}
</script>

<style scoped>
.customer-form-page {
  padding: 20px;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 12px;
}

.card-header .title {
  font-size: 18px;
  font-weight: 600;
}
</style>
