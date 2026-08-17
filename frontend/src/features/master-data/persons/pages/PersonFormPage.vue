<template>
  <!-- Dialog 模式 -->
  <el-dialog
    v-if="isDialogMode"
    :model-value="visible"
    :title="isEdit ? '编辑人员' : '新增人员'"
    width="600px"
    @close="handleClose"
  >
    <PersonForm
      ref="formRef"
      :form-data="formData"
      :rules="rules"
      :is-edit="isEdit"
    />

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="loading" @click="handleSubmit">
        确定
      </el-button>
    </template>
  </el-dialog>

  <!-- Page 模式 -->
  <div v-else class="person-form-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-button :icon="ArrowLeft" @click="handleBack">返回</el-button>
          <span class="title">{{ isEdit ? '编辑人员' : '新增人员' }}</span>
        </div>
      </template>

      <PersonForm
        ref="formRef"
        :form-data="formData"
        :rules="rules"
        :is-edit="isEdit"
      />

      <div class="form-actions">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="loading" @click="handleSubmit">
          保存
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { Person, CreatePersonRequest, UpdatePersonRequest } from '@/features/master-data/persons/types/person'
import { createPerson, updatePerson, getPersonById } from '@/features/master-data/persons/api/person'
import PersonForm from '../components/PersonForm.vue'

interface Props {
  visible?: boolean
  person?: Person | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const route = useRoute()
const router = useRouter()

type PersonFormExpose = Pick<FormInstance, 'validate' | 'clearValidate'>

const formRef = ref<PersonFormExpose>()
const loading = ref(false)

// 判断是 Dialog 模式还是 Page 模式
const isDialogMode = computed(() => props.visible !== undefined)

// 判断是编辑还是新增
const isEdit = computed(() => {
  if (isDialogMode.value) {
    return !!props.person
  } else {
    return !!route.params.id
  }
})

const formData = reactive<CreatePersonRequest & UpdatePersonRequest>({
  name: '',
  personType: 'Employee',
  department: '',
  position: '',
  idNumber: '',
  phone: '',
  email: '',
  bankAccount: '',
  bankName: '',
  joinDate: '',
  leaveDate: '',
  isActive: true
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  personType: [{ required: true, message: '请选择人员类型', trigger: 'change' }],
  phone: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  idNumber: [
    { pattern: /^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$/, message: '请输入正确的证件号码', trigger: 'blur' }
  ]
}

// Page 模式：从路由加载数据
const loadPersonData = async () => {
  if (!isDialogMode.value && route.params.id) {
    loading.value = true
    try {
      const { data } = await getPersonById(Number(route.params.id))
      const person = data.data
      formData.name = person.name
      formData.personType = person.personType
      formData.department = person.department || ''
      formData.position = person.position || ''
      formData.idNumber = person.idNumber || ''
      formData.phone = person.phone || ''
      formData.email = person.email || ''
      formData.bankAccount = person.bankAccount || ''
      formData.bankName = person.bankName || ''
      formData.joinDate = person.joinDate || ''
      formData.leaveDate = person.leaveDate || ''
      formData.isActive = person.isActive
    } catch (error) {
      ElMessage.error('加载人员数据失败')
      router.push('/persons')
    } finally {
      loading.value = false
    }
  }
}

// Dialog 模式：监听 props 变化
watch(() => props.visible, (val) => {
  if (val && isDialogMode.value) {
    if (props.person) {
      // Edit mode
      formData.name = props.person.name
      formData.personType = props.person.personType
      formData.department = props.person.department || ''
      formData.position = props.person.position || ''
      formData.idNumber = props.person.idNumber || ''
      formData.phone = props.person.phone || ''
      formData.email = props.person.email || ''
      formData.bankAccount = props.person.bankAccount || ''
      formData.bankName = props.person.bankName || ''
      formData.joinDate = props.person.joinDate || ''
      formData.leaveDate = props.person.leaveDate || ''
      formData.isActive = props.person.isActive
    } else {
      // Add mode - reset form
      resetForm()
    }
    formRef.value?.clearValidate()
  }
})

const resetForm = () => {
  formData.name = ''
  formData.personType = 'Employee'
  formData.department = ''
  formData.position = ''
  formData.idNumber = ''
  formData.phone = ''
  formData.email = ''
  formData.bankAccount = ''
  formData.bankName = ''
  formData.joinDate = ''
  formData.leaveDate = ''
  formData.isActive = true
}

const handleClose = () => {
  emit('update:visible', false)
}

const handleBack = () => {
  router.push('/persons')
}

const handleSubmit = async () => {
  if (!formRef.value) return

  const isValid = await formRef.value.validate?.().catch(() => false)
  if (!isValid) return

  loading.value = true
  try {
    if (isEdit.value) {
      const personId = isDialogMode.value ? props.person!.id : Number(route.params.id)
      const updateData: UpdatePersonRequest = {
        name: formData.name,
        personType: formData.personType,
        department: formData.department || undefined,
        position: formData.position || undefined,
        idNumber: formData.idNumber || undefined,
        phone: formData.phone || undefined,
        email: formData.email || undefined,
        bankAccount: formData.bankAccount || undefined,
        bankName: formData.bankName || undefined,
        joinDate: formData.joinDate || undefined,
        leaveDate: formData.leaveDate || undefined,
        isActive: formData.isActive
      }
      await updatePerson(personId, updateData)
      ElMessage.success('更新成功')
    } else {
      const createData: CreatePersonRequest = {
        name: formData.name,
        personType: formData.personType,
        department: formData.department || undefined,
        position: formData.position || undefined,
        idNumber: formData.idNumber || undefined,
        phone: formData.phone || undefined,
        email: formData.email || undefined,
        bankAccount: formData.bankAccount || undefined,
        bankName: formData.bankName || undefined,
        joinDate: formData.joinDate || undefined
      }
      await createPerson(createData)
      ElMessage.success('创建成功')
    }

    if (isDialogMode.value) {
      emit('success')
      return
    }

    router.push('/persons')
  } catch (error) {
    console.error('操作失败:', error)
  } finally {
    loading.value = false
  }
}

// Page 模式：组件挂载时加载数据
onMounted(() => {
  if (!isDialogMode.value) {
    loadPersonData()
  }
})
</script>

<style scoped>
.person-form-page {
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid var(--el-border-color-light);
}
</style>
