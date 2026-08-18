export const DEFAULT_SITE_NAME = '财务管理系统'
export const DEFAULT_SITE_NAME_EN = 'Finance Management System'
export const SITE_NAME_MAX_LENGTH = 50
export const SITE_NAME_EN_MAX_LENGTH = 80

export interface SiteBrand {
  siteName: string
  siteNameEn: string
}

export interface UpdateSiteBrandRequest {
  siteName: string
  siteNameEn?: string
}
