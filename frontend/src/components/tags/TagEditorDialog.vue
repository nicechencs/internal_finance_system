<template>
  <el-dialog
    :model-value="visible"
    title="管理标签"
    width="480px"
    destroy-on-close
    @update:model-value="$emit('update:visible', $event)"
    @open="loadBindings"
  >
    <div v-if="ownerName" class="owner-info">
      <span class="owner-label">对象：</span>
      <span class="owner-name">{{ ownerName }}</span>
    </div>

    <div v-loading="loadingBindings" class="tag-editor-body">
      <p class="hint">选择要绑定的标签，保存后生效：</p>
      <div v-if="deletedBindings.length > 0" class="deleted-tag-warning">
        <p class="deleted-tag-title">以下历史标签已删除，当前会继续保留绑定记录：</p>
        <div class="deleted-tag-list">
          <el-tag
            v-for="binding in deletedBindings"
            :key="binding.id"
            type="info"
            effect="plain"
          >
            {{ binding.tagName }}
          </el-tag>
        </div>
      </div>
      <TagSelector
        v-model="selectedTagIds"
        :scope="ownerType"
        placeholder="选择标签"
        style="width: 100%"
      />
    </div>

    <template #footer>
      <el-button @click="$emit('update:visible', false)">取消</el-button>
      <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import TagSelector from '@/components/tags/TagSelector.vue'
import { getTagBindings, setTagBindings } from '@/features/master-data/tags/api/tag'
import type { TagBinding } from '@/features/master-data/tags/types/tag'

interface Props {
  visible: boolean
  ownerType: string
  ownerId: number | null
  ownerName?: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void
  (e: 'saved'): void
}>()

const selectedTagIds = ref<number[]>([])
const deletedBindings = ref<TagBinding[]>([])
const loadingBindings = ref(false)
const saving = ref(false)

const loadBindings = async () => {
  if (!props.ownerId) return
  loadingBindings.value = true
  try {
    const res = await getTagBindings({ ownerType: props.ownerType, ownerId: props.ownerId })
    const bindings = res.data?.data ?? []
    deletedBindings.value = bindings.filter((binding) => binding.tagIsDeleted)
    selectedTagIds.value = bindings.filter((binding) => !binding.tagIsDeleted).map((binding) => binding.tagId)
  } catch {
    selectedTagIds.value = []
    deletedBindings.value = []
  } finally {
    loadingBindings.value = false
  }
}

const handleSave = async () => {
  if (!props.ownerId) return
  saving.value = true
  try {
    const preservedTagIds = deletedBindings.value.map((binding) => binding.tagId)
    await setTagBindings({
      ownerType: props.ownerType,
      ownerId: props.ownerId,
      tagIds: Array.from(new Set([...selectedTagIds.value, ...preservedTagIds]))
    })
    ElMessage.success('标签保存成功')
    emit('saved')
    emit('update:visible', false)
  } catch {
    // 请求拦截器已自动弹出后端错误消息，此处不重复弹
  } finally {
    saving.value = false
  }
}

watch(() => props.visible, (val) => {
  if (!val) {
    selectedTagIds.value = []
    deletedBindings.value = []
  }
})
</script>

<style scoped>
.owner-info {
  margin-bottom: 12px;
  padding: 8px 12px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}

.owner-label {
  color: var(--el-text-color-secondary);
}

.owner-name {
  font-weight: 500;
}

.tag-editor-body {
  min-height: 80px;
}

.deleted-tag-warning {
  margin-bottom: 12px;
  padding: 10px 12px;
  background: var(--el-fill-color-light);
  border-radius: 6px;
}

.deleted-tag-title {
  margin: 0 0 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.deleted-tag-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.hint {
  font-size: 13px;
  color: var(--el-text-color-secondary);
  margin: 0 0 8px;
}
</style>
