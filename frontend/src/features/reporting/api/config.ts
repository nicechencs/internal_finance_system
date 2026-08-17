import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { SystemConfig, UpdateConfigRequest } from '@/features/reporting/types/config'

// 获取所有配置
export const getAllConfigs = () => {
  return request<ApiResponse<SystemConfig[]>>({
    url: '/configs',
    method: 'get'
  })
}

// 根据 key 获取配置
export const getConfigByKey = (key: string) => {
  return request<ApiResponse<SystemConfig>>({
    url: `/configs/${key}`,
    method: 'get'
  })
}

// 更新配置
export const updateConfig = (key: string, value: string) => {
  return request<ApiResponse<SystemConfig>>({
    url: `/configs/${key}`,
    method: 'put',
    data: { configValue: value } as UpdateConfigRequest
  })
}
