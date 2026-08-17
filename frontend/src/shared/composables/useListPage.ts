import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { PageRequest, PageResponse } from '@/shared/types/common'
import type { ApiResponse } from '@/shared/utils/request'
import type { AxiosResponse } from 'axios'

/**
 * 列表页面配置选项
 */
export interface UseListPageOptions<T, S extends Record<string, any> = Record<string, any>> {
  /** 数据加载函数 */
  fetchData: (params: any) => Promise<AxiosResponse<ApiResponse<PageResponse<T>>>>
  /** 删除函数（可选） */
  deleteData?: (id: number) => Promise<any>
  /** 删除确认消息 */
  deleteMessage?: string
  /** 初始搜索表单数据 */
  initialSearchForm?: S
  /** 初始页码，默认 1 */
  initialPage?: number
  /** 初始每页条数，默认 20 */
  initialPageSize?: number
  /** 是否在组件挂载时自动加载数据，默认 true */
  autoLoad?: boolean
  /** 参数预处理（处理日期范围、null 过滤等） */
  transformParams?: (params: any) => Record<string, any>
  /** 删除成功后回调 */
  onDeleteSuccess?: () => void
  /** 加载成功后回调 */
  onLoadSuccess?: (data: PageResponse<T>) => void
}

/**
 * 列表页面通用逻辑 Composable
 *
 * @template T - 列表项数据类型
 * @template S - 搜索表单数据类型
 *
 * @example
 * ```ts
 * const {
 *   loading,
 *   tableData,
 *   pagination,
 *   searchForm,
 *   loadData,
 *   handleReset,
 *   handleSearch,
 *   handleSizeChange,
 *   handlePageChange,
 *   handleDelete
 * } = useListPage<Account, { name: string }>({
 *   fetchData: getAccounts,
 *   deleteData: deleteAccount,
 *   deleteMessage: '确定要删除此账户吗？',
 *   initialSearchForm: { name: '' }
 * })
 * ```
 */
export function useListPage<T, S extends Record<string, any> = Record<string, any>>(options: UseListPageOptions<T, S>) {
  const {
    fetchData,
    deleteData,
    deleteMessage = '确定要删除这条记录吗？',
    initialSearchForm = {} as S,
    initialPage = 1,
    initialPageSize = 20,
    autoLoad = true,
    transformParams,
    onDeleteSuccess,
    onLoadSuccess
  } = options

  // 加载状态
  const loading = ref(false)

  // 表格数据
  const tableData = ref<T[]>([])

  // 搜索表单
  const searchForm = reactive<S>({ ...initialSearchForm })

  // 分页信息
  const pagination = reactive({
    page: initialPage,
    pageSize: initialPageSize,
    total: 0
  })

  /**
   * 加载数据
   */
  const loadData = async () => {
    loading.value = true
    try {
      let params: any = {
        page: pagination.page,
        pageSize: pagination.pageSize,
        ...searchForm
      }

      if (transformParams) {
        params = transformParams(params)
      }

      const response = await fetchData(params)
      const pageData = response.data.data
      tableData.value = pageData.items
      pagination.total = pageData.total
      onLoadSuccess?.(pageData)
    } catch (error) {
      console.error('加载数据失败:', error)
      ElMessage.error('数据加载失败')
      tableData.value = []
      pagination.total = 0
    } finally {
      loading.value = false
    }
  }

  /**
   * 重置搜索条件
   */
  const handleReset = () => {
    Object.assign(searchForm, { ...initialSearchForm })
    pagination.page = 1
    loadData()
  }

  /**
   * 搜索（重置到第一页）
   */
  const handleSearch = () => {
    pagination.page = 1
    loadData()
  }

  /**
   * 每页条数变更
   */
  const handleSizeChange = () => {
    pagination.page = 1
    loadData()
  }

  /**
   * 页码变更
   */
  const handlePageChange = () => {
    loadData()
  }

  /**
   * 设置页码
   */
  const setPage = (page: number) => {
    pagination.page = page
  }

  /**
   * 删除记录（带确认弹窗）
   */
  const handleDelete = async (row: { id: number }) => {
    if (!deleteData) {
      console.warn('未配置 deleteData 函数')
      return
    }

    try {
      await ElMessageBox.confirm(deleteMessage, '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      })

      await deleteData(row.id)
      ElMessage.success('删除成功')
      await loadData()
      onDeleteSuccess?.()
    } catch (error) {
      if (error !== 'cancel') {
        // 非用户取消的错误，不额外提示（API 拦截器已处理）
      }
    }
  }

  // 自动加载数据
  if (autoLoad) {
    onMounted(() => {
      loadData()
    })
  }

  return {
    loading,
    tableData,
    searchForm,
    pagination,
    loadData,
    handleReset,
    handleSearch,
    handleSizeChange,
    handlePageChange,
    handleDelete,
    setPage
  }
}
