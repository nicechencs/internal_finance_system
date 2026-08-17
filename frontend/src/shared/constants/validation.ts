/**
 * 表单验证规则常量
 * 与 Element Plus 的 FormRules 兼容
 */
import type { FormItemRule } from 'element-plus'

// ============================
// 通用验证规则
// ============================

/** 必填 - 字符串 */
export const required = (message = '此项为必填项'): FormItemRule => ({
  required: true,
  message,
  trigger: 'blur'
})

/** 必选 - 下拉选择 */
export const requiredSelect = (message = '请选择'): FormItemRule => ({
  required: true,
  message,
  trigger: 'change'
})

// ============================
// 长度验证
// ============================

/** 字符串长度范围 */
export const lengthRange = (
  min: number,
  max: number,
  message?: string
): FormItemRule => ({
  min,
  max,
  message: message ?? `长度须在 ${min} 到 ${max} 个字符之间`,
  trigger: 'blur'
})

/** 最大长度 */
export const maxLength = (max: number, message?: string): FormItemRule => ({
  max,
  message: message ?? `最多 ${max} 个字符`,
  trigger: 'blur'
})

// ============================
// 格式验证
// ============================

/** 手机号 */
export const phone: FormItemRule = {
  pattern: /^1[3-9]\d{9}$/,
  message: '请输入有效的手机号码',
  trigger: 'blur'
}

/** 电子邮箱 */
export const email: FormItemRule = {
  type: 'email',
  message: '请输入有效的电子邮箱',
  trigger: 'blur'
}

/** 银行卡号 (16-19位数字) */
export const bankCard: FormItemRule = {
  pattern: /^\d{16,19}$/,
  message: '请输入有效的银行卡号',
  trigger: 'blur'
}

/** URL 地址 */
export const url: FormItemRule = {
  type: 'url',
  message: '请输入有效的 URL 地址',
  trigger: 'blur'
}

// ============================
// 数值验证
// ============================

/**
 * 金额验证（正数，最多两位小数）
 */
export const amount: FormItemRule = {
  pattern: /^\d+(\.\d{1,2})?$/,
  message: '请输入有效金额（最多两位小数）',
  trigger: 'blur'
}

/**
 * 正整数
 */
export const positiveInteger: FormItemRule = {
  pattern: /^[1-9]\d*$/,
  message: '请输入正整数',
  trigger: 'blur'
}

/**
 * 数值范围验证
 */
export const numberRange = (
  min: number,
  max: number,
  message?: string
): FormItemRule => ({
  type: 'number',
  min,
  max,
  message: message ?? `数值须在 ${min} 到 ${max} 之间`,
  trigger: 'blur'
})

// ============================
// 业务特定规则
// ============================

/** 账户名称规则 */
export const accountNameRules: FormItemRule[] = [
  required('请输入账户名称'),
  lengthRange(2, 50, '账户名称长度须在 2 到 50 个字符之间')
]

/** 项目名称规则 */
export const projectNameRules: FormItemRule[] = [
  required('请输入项目名称'),
  lengthRange(2, 100, '项目名称长度须在 2 到 100 个字符之间')
]

/** 金额输入规则 */
export const amountRules: FormItemRule[] = [
  required('请输入金额'),
  amount
]

/** 备注 / 描述规则 */
export const descriptionRules: FormItemRule[] = [
  maxLength(500, '描述最多 500 个字符')
]
