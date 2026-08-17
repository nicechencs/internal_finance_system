import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  CreateUserRequest,
  SetUserPasswordRequest,
  SetUserStatusRequest,
  UpdateUserRequest,
  UserAdmin
} from '@/shared/types/common'

export const getUsers = () => {
  return request<ApiResponse<UserAdmin[]>>({
    url: '/users',
    method: 'get'
  })
}

export const createUser = (data: CreateUserRequest) => {
  return request<ApiResponse<UserAdmin>>({
    url: '/users',
    method: 'post',
    data
  })
}

export const setUserPassword = (userId: number, data: SetUserPasswordRequest) => {
  return request<ApiResponse<null>>({
    url: `/users/${userId}/password`,
    method: 'put',
    data
  })
}

export const unlockUser = (userId: number) => {
  return request<ApiResponse<null>>({
    url: `/users/${userId}/unlock`,
    method: 'post'
  })
}

export const setUserStatus = (userId: number, data: SetUserStatusRequest) => {
  return request<ApiResponse<null>>({
    url: `/users/${userId}/status`,
    method: 'put',
    data
  })
}

export const updateUser = (userId: number, data: UpdateUserRequest) => {
  return request<ApiResponse<UserAdmin>>({
    url: `/users/${userId}`,
    method: 'put',
    data
  })
}
