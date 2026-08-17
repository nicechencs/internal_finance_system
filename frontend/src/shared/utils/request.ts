import axios from 'axios'
import type { AxiosError, AxiosInstance, AxiosResponse } from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useUserStore } from '@/features/auth/stores/user'
import { ApiError, ErrorCode } from '@/shared/types/error'
import { serializeQueryParams } from '@/shared/utils/queryParams'

export interface ApiResponse<T = any> {
  success: boolean
  data: T
  message: string
  errors?: string[]
}

const service: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json'
  },
  paramsSerializer: {
    serialize: serializeQueryParams
  }
})

service.interceptors.response.use(
  (response: AxiosResponse<ApiResponse>) => {
    const res = response.data

    if (!res.success) {
      const errorMessage = res.message || '请求失败'
      ElMessage.error(errorMessage)

      if (res.errors?.length) {
        res.errors.forEach((item) => {
          ElMessage.error(item)
        })
      }

      return Promise.reject(
        new ApiError(
          ErrorCode.BUSINESS_ERROR,
          errorMessage,
          response.status,
          res.errors
        )
      )
    }

    return response
  },
  (error: AxiosError<ApiResponse>) => {
    if (axios.isCancel(error)) {
      return Promise.reject(
        new ApiError(ErrorCode.REQUEST_CANCELLED, '请求已取消')
      )
    }

    if (!error.response) {
      const requestUrl = error.config?.url || ''
      const isSessionProbe = requestUrl.includes('/auth/me')
      const errorCode = error.code === 'ECONNABORTED'
        ? ErrorCode.TIMEOUT_ERROR
        : ErrorCode.NETWORK_ERROR
      if (!isSessionProbe) {
        ElMessage.error(
          errorCode === ErrorCode.TIMEOUT_ERROR
            ? '请求超时，请检查网络后重试'
            : '无法连接到服务器，请确认后端服务已启动'
        )
      }
      return Promise.reject(new ApiError(errorCode))
    }

    const { status, data } = error.response
    const requestUrl = error.config?.url || ''
    const isSessionProbe = requestUrl.includes('/auth/me')

    let errorCode: ErrorCode
    let errorMessage: string

    switch (status) {
      case 401: {
        errorCode = ErrorCode.UNAUTHORIZED
        errorMessage = data?.message || '未登录或登录已失效'
        const userStore = useUserStore()
        userStore.logout()
        if (router.currentRoute.value.path !== '/login') {
          router.push('/login')
        }
        break
      }
      case 403:
        errorCode = ErrorCode.FORBIDDEN
        errorMessage = data?.message || '拒绝访问'
        break
      case 404:
        errorCode = ErrorCode.NOT_FOUND
        errorMessage = data?.message || '请求的资源不存在'
        break
      case 400:
        errorCode = ErrorCode.VALIDATION_ERROR
        errorMessage = data?.message || '请求参数错误'
        break
      case 429:
        errorCode = ErrorCode.BUSINESS_ERROR
        errorMessage = data?.message || '请求过于频繁，请稍后再试'
        break
      case 500:
        errorCode = ErrorCode.SERVER_ERROR
        errorMessage = data?.message || '服务器错误'
        break
      case 503:
        errorCode = ErrorCode.SERVICE_UNAVAILABLE
        errorMessage = '服务暂时不可用'
        break
      default:
        errorCode = ErrorCode.UNKNOWN_ERROR
        errorMessage = data?.message || '请求失败'
        break
    }

    if (!(status === 401 && isSessionProbe)) {
      ElMessage.error(errorMessage)
    }

    if (status === 400 && data?.errors?.length) {
      data.errors.forEach((item) => {
        ElMessage.error(item)
      })
    }

    return Promise.reject(
      new ApiError(errorCode, errorMessage, status, data?.errors)
    )
  }
)

export default service
