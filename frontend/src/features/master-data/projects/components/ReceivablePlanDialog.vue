<template>
  <el-dialog
    v-model="visible"
    title="初始化收款计划"
    width="700px"
    @close="handleClose"
  >
    <div class="mb-4">
      <p class="text-gray-600">项目：{{ project.name }}</p>
      <p class="text-gray-600">合同金额：¥{{ project.contractAmount?.toLocaleString() }}</p>
    </div>

    <el-form :model="form" label-width="100px">
      <el-form-item label="收款方式">
        <el-radio-group v-model="form.mode">
          <el-radio value="once">一次性收款</el-radio>
          <el-radio value="installment">分期收款</el-radio>
        </el-radio-group>
      </el-form-item>

      <template v-if="form.mode === 'installment'">
        <el-form-item label="期数">
          <el-select v-model="form.installmentCount" @change="generateInstallments">
            <el-option v-for="n in 9" :key="n + 1" :label="`${n + 1}期`" :value="n + 1" />
          </el-select>
        </el-form-item>

        <el-form-item label="快捷模板">
          <el-button-group>
            <el-button @click="applyTemplate('30-40-30')">30-40-30</el-button>
            <el-button @click="applyTemplate('50-50')">50-50</el-button>
            <el-button @click="applyTemplate('equal')">按月均分</el-button>
          </el-button-group>
        </el-form-item>

        <el-form-item label="收款明细">
          <el-table :data="form.installments" border>
            <el-table-column label="期次" width="120">
              <template #default="{ row }">
                <el-input v-model="row.name" placeholder="如：首付" />
              </template>
            </el-table-column>
            <el-table-column label="金额(¥)" width="150">
              <template #default="{ row }">
                <el-input-number
                  v-model="row.amount"
                  :min="0"
                  :max="project.contractAmount"
                  @change="() => updatePercentage()"
                />
              </template>
            </el-table-column>
            <el-table-column label="比例" width="100">
              <template #default="{ row }">
                {{ ((row.amount / project.contractAmount) * 100).toFixed(1) }}%
              </template>
            </el-table-column>
            <el-table-column label="到期日">
              <template #default="{ row }">
                <el-date-picker
                  v-model="row.dueDate"
                  type="date"
                  placeholder="选择日期"
                />
              </template>
            </el-table-column>
          </el-table>

          <div class="mt-2 flex justify-between items-center">
            <span>合计：¥{{ totalAmount.toLocaleString() }}</span>
            <span :class="isValid ? 'text-green-600' : 'text-red-600'">
              {{ isValid ? '✓ 金额正确' : '✗ 金额不等于合同金额' }}
            </span>
          </div>
        </el-form-item>
      </template>
    </el-form>

    <template #footer>
      <el-button @click="handleSkip">跳过</el-button>
      <el-button type="primary" :disabled="!isValid" @click="handleConfirm">
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

interface Props {
  project: any
  modelValue: boolean
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'confirm', 'skip'])

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const form = ref({
  mode: 'once',
  installmentCount: 3,
  installments: [] as Array<{
    name: string
    amount: number
    dueDate: Date | null
  }>
})

const totalAmount = computed(() => {
  return form.value.installments.reduce((sum, item) => sum + (item.amount || 0), 0)
})

const isValid = computed(() => {
  if (form.value.mode === 'once') return true
  return Math.abs(totalAmount.value - props.project.contractAmount) < 0.01
})

const generateInstallments = () => {
  const count = form.value.installmentCount
  const avgAmount = Math.floor(props.project.contractAmount / count)
  const remainder = props.project.contractAmount - avgAmount * count

  form.value.installments = Array.from({ length: count }, (_, i) => ({
    name: `第${i + 1}期`,
    amount: i === count - 1 ? avgAmount + remainder : avgAmount,
    dueDate: null
  }))
}

const applyTemplate = (template: string) => {
  const total = props.project.contractAmount
  let ratios: number[] = []

  switch (template) {
    case '30-40-30':
      ratios = [0.3, 0.4, 0.3]
      form.value.installmentCount = 3
      break
    case '50-50':
      ratios = [0.5, 0.5]
      form.value.installmentCount = 2
      break
    case 'equal':
      generateInstallments()
      return
  }

  form.value.installments = ratios.map((ratio, i) => ({
    name: ['首付', '进度款', '尾款'][i] || `第${i + 1}期`,
    amount: Math.round(total * ratio),
    dueDate: null
  }))

  // 调整最后一期以确保总和正确
  const sum = form.value.installments.reduce((s, item) => s + item.amount, 0)
  const lastInstallment = form.value.installments[form.value.installments.length - 1]
  if (lastInstallment) {
    lastInstallment.amount += total - sum
  }
}

const updatePercentage = () => {
  // 触发响应式更新
}

const handleConfirm = () => {
  emit('confirm', form.value)
  visible.value = false
}

const handleSkip = () => {
  emit('skip')
  visible.value = false
}

const handleClose = () => {
  visible.value = false
}

watch(() => props.modelValue, (val) => {
  if (val) {
    form.value.mode = 'once'
    generateInstallments()
  }
})
</script>

<style scoped>
.mb-4 {
  margin-bottom: 16px;
}

.text-gray-600 {
  color: #6b7280;
  margin: 4px 0;
}

.mt-2 {
  margin-top: 8px;
}

.flex {
  display: flex;
}

.justify-between {
  justify-content: space-between;
}

.items-center {
  align-items: center;
}

.text-green-600 {
  color: #16a34a;
  font-weight: 600;
}

.text-red-600 {
  color: #dc2626;
  font-weight: 600;
}
</style>

