import { ref, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance } from 'element-plus'
import type { ApiResponse } from '@/shared/utils/request'
import type { AxiosResponse } from 'axios'

/**
 * 表单对话框配置选项
 */
export interface UseFormDialogOptions<T, C, U> {
  /** 创建数据的 API 函数 */
  createApi: (data: C) => Promise<AxiosResponse<ApiResponse<T>>>
  /** 更新数据的 API 函数 */
  updateApi: (id: number, data: U) => Promise<AxiosResponse<ApiResponse<T>>>
  /** 初始表单数据工厂函数 */
  initialFormData: () => C
  /** 数据转换为表单数据的函数（编辑时使用） */
  toFormData?: (data: T) => C | U
  /** 成功提示消息 */
  successMessage?: {
    create?: string
    update?: string
  }
}

/**
 * 表单对话框通用逻辑 Composable
 *
 * @template T - 实体数据类型
 * @template C - 创建请求数据类型
 * @template U - 更新请求数据类型
 *
 * @example
 * ```ts
 * const {
 *   visible,
 *   loading,
 *   formData,
 *   formRef,
 *   isEdit,
 *   open,
 *   handleSubmit,
 *   handleClose
 * } = useFormDialog<Account, CreateAccountRequest, UpdateAccountRequest>({
 *   createApi: createAccount,
 *   updateApi: updateAccount,
 *   initialFormData: () => ({ name: '', accountType: '', ... })
 * })
 * ```
 */
export function useFormDialog<T extends { id: number }, C, U = C>(
  options: UseFormDialogOptions<T, C, U>
) {
  const {
    createApi,
    updateApi,
    initialFormData,
    toFormData,
    successMessage = {
      create: '创建成功',
      update: '更新成功'
    }
  } = options

  // 对话框可见性
  const visible = ref(false)

  // 提交加载状态
  const loading = ref(false)

  // 表单引用
  const formRef = ref<FormInstance>()

  // 当前编辑的数据
  const currentData = ref<T | null>(null)

  // 表单数据
  const formData = ref<C | U>(initialFormData())

  // 是否为编辑模式
  const isEdit = ref(false)

  /**
   * 打开对话框
   * @param data - 编辑时传入的数据，不传则为新增模式
   */
  const open = (data?: T) => {
    if (data) {
      isEdit.value = true
      currentData.value = data
      formData.value = toFormData ? toFormData(data) : ({ ...data } as any)
    } else {
      isEdit.value = false
      currentData.value = null
      formData.value = initialFormData()
    }
    visible.value = true
  }

  /**
   * 关闭对话框
   */
  const handleClose = () => {
    visible.value = false
    formRef.value?.resetFields()
    nextTick(() => {
      formData.value = initialFormData()
      currentData.value = null
      isEdit.value = false
    })
  }

  /**
   * 提交表单
   * @returns 成功返回 true，失败返回 false
   */
  const handleSubmit = async (): Promise<boolean> => {
    if (!formRef.value) return false

    try {
      const valid = await formRef.value.validate()
      if (!valid) return false

      loading.value = true

      if (isEdit.value && currentData.value) {
        await updateApi(currentData.value.id, formData.value as U)
        ElMessage.success(successMessage.update!)
      } else {
        await createApi(formData.value as C)
        ElMessage.success(successMessage.create!)
      }

      handleClose()
      return true
    } catch (error) {
      console.error('提交失败:', error)
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    visible,
    loading,
    formData,
    formRef,
    isEdit,
    currentData,
    open,
    handleSubmit,
    handleClose
  }
}
