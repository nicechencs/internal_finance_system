<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑分类' : '新增分类'"
    width="600px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="分类名称" prop="name">
        <el-input v-model="formData.name" placeholder="请输入分类名称" />
      </el-form-item>

      <el-form-item label="类型" prop="categoryType" v-if="!isEdit">
        <el-select v-model="formData.categoryType" placeholder="请选择类型" style="width: 100%">
          <el-option label="收入" value="Income" />
          <el-option label="支出" value="Expense" />
        </el-select>
      </el-form-item>

      <el-form-item label="父分类" prop="parentId">
        <el-select
          v-model="formData.parentId"
          placeholder="请选择父分类（可选）"
          clearable
          style="width: 100%"
        >
          <el-option
            v-for="cat in parentCategories"
            :key="cat.id"
            :label="cat.name"
            :value="cat.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="描述" prop="description">
        <el-input
          v-model="formData.description"
          type="textarea"
          :rows="3"
          placeholder="请输入描述（可选）"
        />
      </el-form-item>

      <el-form-item label="状态" prop="isActive" v-if="isEdit">
        <el-switch v-model="formData.isActive" />
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
import { ref, reactive, watch, computed } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createCategory, updateCategory, getActiveCategories } from '@/features/master-data/categories/api/category'
import type { Category, CreateCategoryRequest, UpdateCategoryRequest } from '@/features/master-data/categories/types/category'

interface Props {
  visible: boolean
  category: Category | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const formRef = ref<FormInstance>()
const submitting = ref(false)
const parentCategories = ref<Category[]>([])

const isEdit = computed(() => !!props.category)

const formData = reactive<CreateCategoryRequest & UpdateCategoryRequest>({
  name: '',
  categoryType: 'Expense',
  parentId: undefined,
  description: '',
  isActive: true
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入分类名称', trigger: 'blur' }],
  categoryType: [{ required: true, message: '请选择类型', trigger: 'change' }]
}

watch(
  () => props.visible,
  async (val) => {
    if (val) {
      if (props.category) {
        formData.name = props.category.name
        formData.categoryType = props.category.categoryType
        formData.parentId = props.category.parentId
        formData.description = props.category.description || ''
        formData.isActive = props.category.isActive
      } else {
        resetForm()
      }
      await loadParentCategories()
    }
  }
)

const loadParentCategories = async () => {
  try {
    const response = await getActiveCategories()
    parentCategories.value = response.data.data.filter(
      (cat: Category) => cat.id !== props.category?.id
    )
  } catch (error) {
    console.error('Failed to load parent categories:', error)
  }
}

const resetForm = () => {
  formData.name = ''
  formData.categoryType = 'Expense'
  formData.parentId = undefined
  formData.description = ''
  formData.isActive = true
  formRef.value?.clearValidate()
}

const handleClose = () => {
  emit('update:visible', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    submitting.value = true
    try {
      if (isEdit.value && props.category) {
        await updateCategory(props.category.id, {
          name: formData.name,
          parentId: formData.parentId,
          description: formData.description,
          isActive: formData.isActive
        })
        ElMessage.success('更新成功')
      } else {
        await createCategory({
          name: formData.name,
          categoryType: formData.categoryType,
          parentId: formData.parentId,
          description: formData.description
        })
        ElMessage.success('创建成功')
      }
      emit('success')
      handleClose()
    } catch (error) {
      console.error('Failed to submit category:', error)
    } finally {
      submitting.value = false
    }
  })
}
</script>
