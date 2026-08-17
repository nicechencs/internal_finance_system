import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  ChangePasswordRequest,
  LoginRequest,
  LoginResponse,
  UpdateProfileRequest,
  User
} from '@/shared/types/common'

export const login = (data: LoginRequest) => {
  return request<ApiResponse<LoginResponse>>({
    url: '/auth/login',
    method: 'post',
    data
  })
}

export const logout = () => {
  return request<ApiResponse<null>>({
    url: '/auth/logout',
    method: 'post'
  })
}

export const getCurrentUser = () => {
  return request<ApiResponse<User>>({
    url: '/auth/me',
    method: 'get'
  })
}

export const changePassword = (data: ChangePasswordRequest) => {
  return request<ApiResponse<null>>({
    url: '/auth/change-password',
    method: 'post',
    data
  })
}

export const updateProfile = (data: UpdateProfileRequest) => {
  return request<ApiResponse<User>>({
    url: '/auth/profile',
    method: 'put',
    data
  })
}
