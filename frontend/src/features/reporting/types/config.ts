// 系统配置
export interface SystemConfig {
  id: number
  configKey: string
  configValue: string
  description?: string
  category: string
  isSystem: boolean
  createdAt: string
  updatedAt: string
}

// 更新配置请求
export interface UpdateConfigRequest {
  configValue: string
}
