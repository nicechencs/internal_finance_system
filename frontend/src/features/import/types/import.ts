// 导入批次
export interface ImportBatch {
  id: number
  fileName: string
  accountId: number
  accountName: string
  totalCount: number
  successCount: number
  duplicateCount: number
  errorCount: number
  errorMessage?: string
  status: string
  createdAt: string
}

// 银行流水预览
export interface BankTransactionPreview {
  rowNumber: number
  transactionDate: string
  transactionTime?: string
  amount: number
  balance?: number
  direction: string // in/out
  counterpartyName: string
  counterpartyAccount?: string
  counterpartyBank?: string
  transactionNumber?: string
  description?: string
  memo?: string
  uniqueHash: string
  isDuplicate: boolean
  isFileConflict: boolean
  isRecoverable: boolean
  conflictReason?: string
  suggestedCategory?: string
  suggestedProject?: string
  matchedCategoryId?: number
  matchedCategoryName?: string
}

// 导入预览响应
export interface ImportPreviewResponse {
  batchId: number
  fileName: string
  totalRows: number
  duplicateRows: number
  fileConflictRows: number
  recoverableRows: number
  newRows: number
  detectedFormat: string
  previews: BankTransactionPreview[]
}

// 导入确认请求
export interface ImportConfirmRequest {
  batchId: number
  selectedRowNumbers: number[]
}
