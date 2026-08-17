/**
 * 错误码枚举
 * 与后端错误码体系对应
 */
export enum ErrorCode {
  // 通用错误 (1000-1999)
  UNKNOWN_ERROR = 1000,
  NETWORK_ERROR = 1001,
  TIMEOUT_ERROR = 1002,
  REQUEST_CANCELLED = 1003,

  // 认证授权错误 (2000-2999)
  UNAUTHORIZED = 2000,
  TOKEN_EXPIRED = 2001,
  TOKEN_INVALID = 2002,
  FORBIDDEN = 2003,
  INSUFFICIENT_PERMISSIONS = 2004,

  // 业务逻辑错误 (3000-3999)
  VALIDATION_ERROR = 3000,
  BUSINESS_ERROR = 3001,
  NOT_FOUND = 3002,
  DUPLICATE_ERROR = 3003,
  CONFLICT_ERROR = 3004,

  // 服务器错误 (5000-5999)
  SERVER_ERROR = 5000,
  SERVICE_UNAVAILABLE = 5001,
  DATABASE_ERROR = 5002,
}

/**
 * 错误信息映射
 */
export const ErrorMessages: Record<ErrorCode, string> = {
  [ErrorCode.UNKNOWN_ERROR]: '未知错误',
  [ErrorCode.NETWORK_ERROR]: '网络错误，请检查网络连接',
  [ErrorCode.TIMEOUT_ERROR]: '请求超时，请稍后重试',
  [ErrorCode.REQUEST_CANCELLED]: '请求已取消',

  [ErrorCode.UNAUTHORIZED]: '未授权，请重新登录',
  [ErrorCode.TOKEN_EXPIRED]: '登录已过期，请重新登录',
  [ErrorCode.TOKEN_INVALID]: '登录凭证无效，请重新登录',
  [ErrorCode.FORBIDDEN]: '拒绝访问',
  [ErrorCode.INSUFFICIENT_PERMISSIONS]: '权限不足',

  [ErrorCode.VALIDATION_ERROR]: '数据验证失败',
  [ErrorCode.BUSINESS_ERROR]: '业务处理失败',
  [ErrorCode.NOT_FOUND]: '请求的资源不存在',
  [ErrorCode.DUPLICATE_ERROR]: '数据已存在',
  [ErrorCode.CONFLICT_ERROR]: '数据冲突',

  [ErrorCode.SERVER_ERROR]: '服务器错误',
  [ErrorCode.SERVICE_UNAVAILABLE]: '服务暂时不可用',
  [ErrorCode.DATABASE_ERROR]: '数据库错误',
}

/**
 * API 错误类
 */
export class ApiError extends Error {
  code: ErrorCode
  statusCode?: number
  errors?: string[]

  constructor(
    code: ErrorCode,
    message?: string,
    statusCode?: number,
    errors?: string[]
  ) {
    super(message || ErrorMessages[code])
    this.name = 'ApiError'
    this.code = code
    this.statusCode = statusCode
    this.errors = errors
  }
}
