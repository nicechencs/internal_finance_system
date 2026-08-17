<template>
  <!-- Dialog 模式 -->
  <el-dialog
    v-if="isDialogMode"
    :model-value="visible"
    :title="isEdit ? '编辑供应商' : '新增供应商'"
    width="600px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="供应商名称" prop="name">
        <SearchableInput
          v-model="formData.name"
          :fetch-options="getActiveSuppliers"
          placeholder="请输入或选择供应商名称"
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
        <el-input v-model="formData.address" type="textarea" :rows="2" placeholder="请输入地址" />
      </el-form-item>

      <el-form-item label="税号" prop="taxNumber">
        <el-input v-model="formData.taxNumber" placeholder="请输入税号" />
      </el-form-item>

      <el-form-item label="开户银行" prop="bankName">
        <el-input v-model="formData.bankName" placeholder="请输入开户银行" />
      </el-form-item>

      <el-form-item label="银行账号" prop="bankAccount">
        <el-input v-model="formData.bankAccount" placeholder="请输入银行账号" />
      </el-form-item>

      <el-form-item label="备注" prop="description">
        <el-input v-model="formData.description" type="textarea" :rows="3" placeholder="请输入备注" />
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
  <div v-else class="supplier-form-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-button :icon="ArrowLeft" @click="handleBack">返回</el-button>
          <span class="title">{{ isEdit ? '编辑供应商' : '新增供应商' }}</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="formData"
        :rules="rules"
        label-width="100px"
        style="max-width: 600px"
      >
        <el-form-item label="供应商名称" prop="name">
          <SearchableInput
            v-model="formData.name"
            :fetch-options="getActiveSuppliers"
            placeholder="请输入或选择供应商名称"
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
          <el-input v-model="formData.address" type="textarea" :rows="2" placeholder="请输入地址" />
        </el-form-item>

        <el-form-item label="税号" prop="taxNumber">
          <el-input v-model="formData.taxNumber" placeholder="请输入税号" />
        </el-form-item>

        <el-form-item label="开户银行" prop="bankName">
          <el-input v-model="formData.bankName" placeholder="请输入开户银行" />
        </el-form-item>

        <el-form-item label="银行账号" prop="bankAccount">
          <el-input v-model="formData.bankAccount" placeholder="请输入银行账号" />
        </el-form-item>

        <el-form-item label="备注" prop="description">
          <el-input v-model="formData.description" type="textarea" :rows="3" placeholder="请输入备注" />
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
import { ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import type { Supplier, CreateSupplierRequest, UpdateSupplierRequest } from '@/features/master-data/suppliers/types/supplier'
import { createSupplier, updateSupplier, getActiveSuppliers, getSupplierById } from '@/features/master-data/suppliers/api/supplier'
import SearchableInput from '@/shared/ui/SearchableInput.vue'

interface Props {
  visible?: boolean
  supplier?: Supplier | null
}

const props = withDefaults(defineProps<Props>(), {
  supplier: null
})
const emit = defineEmits(['update:visible', 'success'])

const route = useRoute()
const router = useRouter()
const formRef = ref<FormInstance>()
const loading = ref(false)

// 判断是 Dialog 模式还是 Page 模式
const isDialogMode = computed(() => props.visible !== undefined)

// 判断是编辑还是新增
const isEdit = computed(() => {
  if (isDialogMode.value) {
    return !!props.supplier
  } else {
    return !!route.params.id
  }
})

const formData = reactive<CreateSupplierRequest & UpdateSupplierRequest & { isActive: boolean }>({
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
  name: [{ required: true, message: '请输入供应商名称', trigger: 'blur' }],
  contactEmail: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ]
}

// 加载供应商数据（Page 模式编辑时）
const loadSupplier = async (id: number) => {
  try {
    loading.value = true
    const { data } = await getSupplierById(id)
    const supplier = data.data
    formData.name = supplier.name
    formData.shortName = supplier.shortName || ''
    formData.contactPerson = supplier.contactPerson || ''
    formData.contactPhone = supplier.contactPhone || ''
    formData.contactEmail = supplier.contactEmail || ''
    formData.address = supplier.address || ''
    formData.taxNumber = supplier.taxNumber || ''
    formData.bankAccount = supplier.bankAccount || ''
    formData.bankName = supplier.bankName || ''
    formData.description = supplier.description || ''
    formData.isActive = supplier.isActive
  } catch (error) {
    ElMessage.error('加载供应商信息失败')
    handleBack()
  } finally {
    loading.value = false
  }
}

// Dialog 模式：监听 visible 和 supplier 变化
watch(() => props.visible, (val) => {
  if (val && isDialogMode.value) {
    if (props.supplier) {
      // Edit mode
      formData.name = props.supplier.name
      formData.shortName = props.supplier.shortName || ''
      formData.contactPerson = props.supplier.contactPerson || ''
      formData.contactPhone = props.supplier.contactPhone || ''
      formData.contactEmail = props.supplier.contactEmail || ''
      formData.address = props.supplier.address || ''
      formData.taxNumber = props.supplier.taxNumber || ''
      formData.bankAccount = props.supplier.bankAccount || ''
      formData.bankName = props.supplier.bankName || ''
      formData.description = props.supplier.description || ''
      formData.isActive = props.supplier.isActive
    } else {
      // Add mode - reset form
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
    formRef.value?.clearValidate()
  }
})

// Page 模式：组件挂载时加载数据
onMounted(() => {
  if (!isDialogMode.value && route.params.id) {
    loadSupplier(Number(route.params.id))
  }
})

const handleClose = () => {
  emit('update:visible', false)
}

const handleBack = () => {
  router.push('/suppliers')
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        if (isEdit.value) {
          // Update
          const updateData: UpdateSupplierRequest = {
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
          const id = isDialogMode.value ? props.supplier!.id : Number(route.params.id)
          await updateSupplier(id, updateData)
          ElMessage.success('更新成功')
        } else {
          // Create
          const createData: CreateSupplierRequest = {
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
          await createSupplier(createData)
          ElMessage.success('创建成功')
        }

        if (isDialogMode.value) {
          emit('success')
        } else {
          router.push('/suppliers')
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
.supplier-form-page {
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
