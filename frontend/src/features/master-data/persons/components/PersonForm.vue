<template>
  <el-form
    ref="formRef"
    :model="formData"
    :rules="rules"
    label-width="100px"
  >
    <el-form-item label="姓名" prop="name">
      <SearchableInput
        v-model="formData.name"
        :fetch-options="getActivePersons"
        placeholder="请输入或选择姓名"
      />
    </el-form-item>

    <el-form-item label="人员类型" prop="personType">
      <el-select v-model="formData.personType" placeholder="请选择人员类型" style="width: 100%">
        <el-option label="员工" value="Employee" />
        <el-option label="承包商" value="Contractor" />
        <el-option label="合伙人" value="Partner" />
      </el-select>
    </el-form-item>

    <el-form-item label="部门" prop="department">
      <el-input v-model="formData.department" placeholder="请输入部门" clearable />
    </el-form-item>

    <el-form-item label="职位" prop="position">
      <el-input v-model="formData.position" placeholder="请输入职位" clearable />
    </el-form-item>

    <el-form-item label="证件号码" prop="idNumber">
      <el-input v-model="formData.idNumber" placeholder="可选，证件号码" />
    </el-form-item>

    <el-form-item label="电话" prop="phone">
      <el-input v-model="formData.phone" placeholder="请输入电话" />
    </el-form-item>

    <el-form-item label="邮箱" prop="email">
      <el-input v-model="formData.email" placeholder="请输入邮箱" />
    </el-form-item>

    <el-form-item label="开户行" prop="bankName">
      <el-input v-model="formData.bankName" placeholder="请输入开户行" />
    </el-form-item>

    <el-form-item label="银行账号" prop="bankAccount">
      <el-input v-model="formData.bankAccount" placeholder="请输入银行账号" />
    </el-form-item>

    <el-form-item label="入职日期" prop="joinDate">
      <el-date-picker
        v-model="formData.joinDate"
        type="date"
        placeholder="请选择入职日期"
        style="width: 100%"
        format="YYYY-MM-DD"
        value-format="YYYY-MM-DD"
      />
    </el-form-item>

    <el-form-item label="离职日期" prop="leaveDate" v-if="isEdit">
      <el-date-picker
        v-model="formData.leaveDate"
        type="date"
        placeholder="请选择离职日期"
        style="width: 100%"
        format="YYYY-MM-DD"
        value-format="YYYY-MM-DD"
      />
    </el-form-item>

    <el-form-item label="状态" prop="isActive" v-if="isEdit">
      <el-switch v-model="formData.isActive" />
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { CreatePersonRequest, UpdatePersonRequest } from '@/features/master-data/persons/types/person'
import { getActivePersons } from '@/features/master-data/persons/api/person'
import SearchableInput from '@/shared/ui/SearchableInput.vue'

interface Props {
  formData: CreatePersonRequest & UpdatePersonRequest
  rules: FormRules
  isEdit: boolean
}

defineProps<Props>()

const formRef = ref<FormInstance>()

// 暴露 validate 和 clearValidate 方法给父组件
defineExpose({
  validate: () => formRef.value?.validate(),
  clearValidate: () => formRef.value?.clearValidate()
})
</script>

<style scoped>
</style>
