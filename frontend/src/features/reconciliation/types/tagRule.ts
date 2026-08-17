export type TagRuleMatchField =
  | 'CounterpartyName'
  | 'Counterparty'
  | 'Description'
  | 'Memo'
  | 'Amount'

export type TagRuleMatchOperator =
  | 'Contains'
  | 'Equals'
  | 'StartsWith'
  | 'EndsWith'
  | 'Regex'
  | 'Range'

export interface TagRuleTagItem {
  tagId: number
  tagName: string
  tagColor?: string
}

export interface TagRule {
  id: number
  ruleName: string
  priority: number
  targetScope: string
  matchField: TagRuleMatchField
  matchOperator: TagRuleMatchOperator
  matchValue: string
  matchValueMax?: string | null
  isActive: boolean
  tags: TagRuleTagItem[]
  createdAt: string
}

export interface CreateTagRuleRequest {
  ruleName: string
  priority: number
  targetScope: string
  matchField: TagRuleMatchField
  matchOperator: TagRuleMatchOperator
  matchValue: string
  matchValueMax?: string | null
  tagIds: number[]
  newTagNames: string[]
}

export interface UpdateTagRuleRequest {
  ruleName: string
  priority: number
  targetScope: string
  matchField: TagRuleMatchField
  matchOperator: TagRuleMatchOperator
  matchValue: string
  matchValueMax?: string | null
  isActive: boolean
  tagIds: number[]
  newTagNames: string[]
}

export interface RunTagRulesRequest {
  targetScope: string
  entityIds?: number[]
}

export interface RunTagRulesResult {
  scannedCount: number
  addedCount: number
  skippedCount: number
}

// ─────────── 两步重跑（preview + confirm）───────────

export interface RerunPreviewRequest {
  targetScope: string
  entityIds?: number[]
}

export interface MatchedRuleInfo {
  ruleId: number
  ruleName: string
  priority: number
}

export interface TagToAddInfo {
  tagId: number
  tagName: string
  tagColor?: string | null
}

export interface RerunCandidate {
  transactionId: number
  transactionDate: string
  amount: number
  counterparty?: string | null
  description?: string | null
  matchedRules: MatchedRuleInfo[]
  tagsToAdd: TagToAddInfo[]
}

export interface RerunPreviewResponse {
  totalScanned: number
  totalAffected: number
  totalTagsToAdd: number
  candidates: RerunCandidate[]
}

export interface RerunConfirmRequest {
  targetScope: string
  transactionIds: number[]
}

export interface RerunConfirmResponse {
  scannedCount: number
  addedCount: number
  skippedCount: number
  message: string
}
