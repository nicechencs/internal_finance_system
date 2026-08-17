export type RuleMatchField =
  | 'CounterpartyName'
  | 'Counterparty'
  | 'Description'
  | 'Memo'
  | 'Amount'

export type RuleMatchOperator =
  | 'Contains'
  | 'Equals'
  | 'StartsWith'
  | 'EndsWith'
  | 'Regex'
  | 'Range'

export interface Rule {
  id: number
  name: string
  categoryId: number
  categoryName: string
  matchField: RuleMatchField
  matchOperator: RuleMatchOperator
  matchValue: string
  matchValueMax?: string | null
  priority: number
  isActive: boolean
  createdAt: string
}

export interface CreateRuleRequest {
  name: string
  categoryId: number
  matchField: RuleMatchField
  matchOperator: RuleMatchOperator
  matchValue: string
  matchValueMax?: string | null
  priority: number
}

export interface UpdateRuleRequest {
  name: string
  categoryId: number
  matchField: RuleMatchField
  matchOperator: RuleMatchOperator
  matchValue: string
  matchValueMax?: string | null
  priority: number
  isActive: boolean
}
