<template>
  <el-radio-group :model-value="modelValue" size="small" @update:model-value="emit('update:modelValue', $event)">
    <el-radio-button value="all">全部</el-radio-button>
    <el-radio-button value="Income">收入</el-radio-button>
    <el-radio-button value="Expense">支出</el-radio-button>
    <el-radio-button value="Transfer">转账</el-radio-button>
  </el-radio-group>
</template>

<script setup lang="ts">
import type { TransactionTypeFilter } from '@/shared/utils/transactionType'

defineProps<{
  modelValue: TransactionTypeFilter
}>()

const emit = defineEmits<{
  'update:modelValue': [value: TransactionTypeFilter]
}>()
</script>
