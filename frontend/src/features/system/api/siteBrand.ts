import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { SiteBrand, UpdateSiteBrandRequest } from '@/features/system/types/siteBrand'

export const getPublicBrand = () => {
  return request<ApiResponse<SiteBrand>>({
    url: '/public/brand',
    method: 'get'
  })
}

export const updateSiteBrand = (data: UpdateSiteBrandRequest) => {
  return request<ApiResponse<SiteBrand>>({
    url: '/configs/site-brand',
    method: 'put',
    data
  })
}
