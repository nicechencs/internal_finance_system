export enum LinkType {
  Customer = 1,
  Supplier = 2,
  Person = 3,
  Project = 4,
  Account = 5
}

export enum RuleRerunStrategy {
  Conservative = 1,
  Overwrite = 2
}

export enum BatchLinkEntityType {
  Customer = 1,
  Supplier = 2,
  Person = 3,
  Project = 4,
  Account = 5
}

export interface LinkPreviewRequest {
  linkType: LinkType
  entityId: number
}

export interface LinkCandidateDto {
  transactionId: number
  transactionDate: string
  amount: number
  transactionType: string
  accountName: string
  counterparty?: string
  description?: string
  categoryName?: string
  matchReason: string
}

export interface LinkPreviewResponse {
  linkType: LinkType
  entityId: number
  entityName: string
  totalMatched: number
  candidates: LinkCandidateDto[]
}

export interface LinkConfirmRequest {
  linkType: LinkType
  entityId: number
  transactionIds: number[]
}

export interface LinkConfirmResponse {
  linkedCount: number
  message: string
}

// ===== 批量智能关联 =====

export interface EntityMatchDto {
  entityType: BatchLinkEntityType
  entityId: number
  entityName: string
  matchReason: string
  extraInfo?: string
}

export interface BatchLinkCandidateDto {
  transactionId: number
  transactionDate: string
  amount: number
  transactionType: string
  accountName: string
  counterparty?: string
  description?: string
  matches: EntityMatchDto[]
}

export interface BatchLinkPreviewResponse {
  totalUnlinked: number
  totalMatched: number
  candidates: BatchLinkCandidateDto[]
}

export interface BatchLinkConfirmItem {
  transactionId: number
  entityType: BatchLinkEntityType
  entityId: number
}

export interface BatchLinkConfirmRequest {
  items: BatchLinkConfirmItem[]
}

export interface BatchLinkConfirmResponse {
  linkedCount: number
  message: string
}

// ===== 规则重跑 =====

export interface RuleRerunPreviewRequest {
  startDate?: string
  endDate?: string
  strategy: RuleRerunStrategy
}

export interface RuleRerunCandidateDto {
  transactionId: number
  transactionDate: string
  amount: number
  transactionType: string
  counterparty?: string
  description?: string
  currentCategoryName?: string
  newCategoryName?: string
  newCategoryId?: number
  willChange: boolean
}

export interface RuleRerunPreviewResponse {
  totalAffected: number
  wouldUpdate: number
  strategy: RuleRerunStrategy
  candidates: RuleRerunCandidateDto[]
}

export interface RuleRerunConfirmRequest {
  startDate?: string
  endDate?: string
  strategy: RuleRerunStrategy
  transactionIds?: number[]
}

export interface RuleRerunConfirmResponse {
  updatedCount: number
  skippedCount: number
  message: string
}


export interface LinkPreviewRequest {
  linkType: LinkType
  entityId: number
}

export interface LinkCandidateDto {
  transactionId: number
  transactionDate: string
  amount: number
  transactionType: string
  accountName: string
  counterparty?: string
  description?: string
  categoryName?: string
  matchReason: string
}

export interface LinkPreviewResponse {
  linkType: LinkType
  entityId: number
  entityName: string
  totalMatched: number
  candidates: LinkCandidateDto[]
}

export interface LinkConfirmRequest {
  linkType: LinkType
  entityId: number
  transactionIds: number[]
}

export interface LinkConfirmResponse {
  linkedCount: number
  message: string
}

export interface RuleRerunPreviewRequest {
  startDate?: string
  endDate?: string
  strategy: RuleRerunStrategy
}

export interface RuleRerunCandidateDto {
  transactionId: number
  transactionDate: string
  amount: number
  transactionType: string
  counterparty?: string
  description?: string
  currentCategoryName?: string
  newCategoryName?: string
  newCategoryId?: number
  willChange: boolean
}

export interface RuleRerunPreviewResponse {
  totalAffected: number
  wouldUpdate: number
  strategy: RuleRerunStrategy
  candidates: RuleRerunCandidateDto[]
}

export interface RuleRerunConfirmRequest {
  startDate?: string
  endDate?: string
  strategy: RuleRerunStrategy
  transactionIds?: number[]
}

export interface RuleRerunConfirmResponse {
  updatedCount: number
  skippedCount: number
  message: string
}
