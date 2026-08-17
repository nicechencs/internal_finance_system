<template>
  <el-tag :type="tagType" :size="size">
    {{ label }}
  </el-tag>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  getTransactionTypeLabel,
  getTransactionTypeTagType
} from '@/shared/utils/transactionType'

const props = withDefaults(defineProps<{
  transactionType?: string
  transferDirection?: string | null
  size?: 'small' | 'default' | 'large'
}>(), {
  size: 'small'
})

const label = computed(() =>
  getTransactionTypeLabel(props.transactionType, props.transferDirection)
)
const tagType = computed(() => getTransactionTypeTagType(props.transactionType))
</script>
