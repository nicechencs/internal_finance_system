/**
 * 颜色系统 - 单一事实源 (Single Source of Truth)
 *
 * 修改颜色只需改此文件，CSS 变量由 applyTheme() 自动注入。
 * 组件层通过 CSS 变量（var(--color-primary) 等）消费，无需改动。
 *
 * 文件结构：
 *   1. 原始色板 (Raw Palette)  — 所有原子色值
 *   2. 语义色 (Semantic)       — 品牌色、图表色等
 *   3. 图表 Token              — Tooltip、渐变、卡片
 *   4. CSS 变量映射            — 注入 :root 的完整映射
 *   5. applyTheme()            — 执行注入
 */

// ============================================================
// 1. 原始色板 (Raw Palette)
// ============================================================

/** Blue 色阶（主色） */
const blue = {
  400: '#60A5FA',
  500: '#3B82F6',
  600: '#2563EB',
  700: '#1D4ED8',
  800: '#1E40AF',
  50:  '#EFF6FF',
  100: '#DBEAFE',
  200: '#BFDBFE',
  300: '#93C5FD',
  950: '#F0F7FF',
  975: '#F8FBFF',
  999: '#FDFEFF',
} as const

/** @deprecated 已被 blue 主色替代，保留仅为兼容旧引用 */
const indigo = {
  400: '#818CF8',
  500: '#6366F1',
  600: '#4F46E5',
  700: '#4338CA',
  800: '#3730A3',
  50:  '#EEF2FF',
  100: '#E0E7FF',
  200: '#C7D2FE',
  300: '#A5B4FC',
  950: '#F5F7FF',
  975: '#FAFBFF',
  999: '#FDFEFF',
} as const

/** Emerald 色阶（成功/收入） */
const emerald = {
  400: '#34D399',
  500: '#10B981',
  600: '#059669',
  700: '#047857',
  100: '#D1FAE5',
  200: '#A7F3D0',
  300: '#6EE7B7',
  50:  '#ECFDF5',
} as const

/** Amber 色阶（警告） */
const amber = {
  400: '#FBBF24',
  500: '#F59E0B',
  600: '#D97706',
  700: '#B45309',
  100: '#FEF3C7',
  200: '#FDE68A',
  300: '#FCD34D',
  50:  '#FFFBEB',
} as const

/** Red 色阶（危险/支出） */
const red = {
  400: '#F87171',
  500: '#EF4444',
  600: '#DC2626',
  700: '#B91C1C',
  100: '#FEE2E2',
  200: '#FECACA',
  300: '#FCA5A5',
  50:  '#FEF2F2',
} as const

/** @deprecated 转账已改用中性色+主色 accent，保留仅为兼容旧引用 */
const violet = {
  500: '#8B5CF6',
  600: '#7C3AED',
  50:  '#F5F3FF',
} as const

/** @deprecated 已从默认图表色盘移除，保留仅为兼容旧引用 */
const cyan = {
  500: '#06B6D4',
} as const

/** Slate 色阶（中性色） */
const slate = {
  50:  '#F8FAFC',
  100: '#F1F5F9',
  200: '#E2E8F0',
  300: '#CBD5E1',
  400: '#94A3B8',
  500: '#64748B',
  600: '#475569',
  700: '#334155',
  800: '#1E293B',
  850: '#2D3B4F',
  900: '#0F172A',
  925: '#1A2332',
} as const

/** Gray 色阶（Element Plus info 兼容） */
const gray = {
  400: '#9CA3AF',
  500: '#6B7280',
  600: '#4B5563',
  700: '#374151',
  100: '#F3F4F6',
  200: '#E5E7EB',
  300: '#D1D5DB',
  50:  '#F9FAFB',
} as const

// ============================================================
// 2. 语义色 (Semantic Colors)
// ============================================================

/** 品牌语义色 — 供 JS/TS 逻辑层使用 */
export const BRAND_COLORS = {
  primary: blue[600],     // 蓝色（利润/主色）
  success: emerald[500],  // 翡翠绿（收入/应收）
  danger:  red[500],      // 红色（支出/应付）
  warning: amber[500],    // 琥珀（警告）
  balance: blue[500],     // 余额（主色系浅层级）
  transfer: slate[500],   // 转账（中性色，不再使用紫色）
  /** @deprecated 使用 transfer 代替 */
  purple:  slate[500],
} as const

/** 图表业务色 */
export const CHART_COLORS = {
  income:     BRAND_COLORS.success,
  expense:    BRAND_COLORS.danger,
  profit:     BRAND_COLORS.primary,
  receivable: BRAND_COLORS.success,
  payable:    BRAND_COLORS.danger,
  balance:    BRAND_COLORS.balance,
} as const

/** 图表调色板（饼图/多系列图轮换）— 同色系蓝色层级盘 */
export const CHART_PALETTE = [
  blue[600],   // 深蓝 — 主色
  blue[500],   // 蓝
  blue[400],   // 浅蓝
  blue[300],   // 淡蓝
  slate[400],  // 石板灰 — 长尾/末位类别
] as const

/** 分类图第 6 色（仅在确有 6+ 类别时手动引入） */
export const CHART_PALETTE_EXTRA = slate[300] // '#CBD5E1'

/** 图表坐标轴 / 网格通用色 */
export const CHART_AXIS = {
  axisLine:  slate[200],
  axisLabel: slate[500],
  splitLine: slate[100],
} as const

// ============================================================
// 3. 图表 Token
// ============================================================

/** 图表 Tooltip 统一风格（白底深字，全站一致） */
export const CHART_TOOLTIP = {
  bg:            'rgba(255, 255, 255, 0.96)',
  text:          slate[700],
  secondaryText: slate[500],
  border:        slate[200],
  shadow:        '0 4px 12px rgba(0, 0, 0, 0.08)',
} as const

/** 图表面积渐变（起止透明度配对） */
export const CHART_GRADIENT = {
  income:   { start: `rgba(16, 185, 129, 0.18)`,  end: `rgba(16, 185, 129, 0.01)` },
  expense:  { start: `rgba(239, 68, 68, 0.18)`,   end: `rgba(239, 68, 68, 0.01)` },
  profit:   { start: `rgba(37, 99, 235, 0.18)`,   end: `rgba(37, 99, 235, 0.01)` },
  balance:  { start: `rgba(59, 130, 246, 0.18)`,  end: `rgba(59, 130, 246, 0.02)` },
  transfer: { start: `rgba(100, 116, 139, 0.16)`, end: `rgba(100, 116, 139, 0.01)` },
  /** @deprecated 使用 transfer 代替 */
  purple:   { start: `rgba(100, 116, 139, 0.16)`, end: `rgba(100, 116, 139, 0.01)` },
} as const

/** 统计卡片语义色（背景渐变 + 边框 + 图标背景） */
export const CARD_COLORS = {
  income: {
    gradient: `linear-gradient(135deg, rgba(16, 185, 129, 0.04) 0%, rgba(236, 253, 245, 0.95) 100%)`,
    border:   `rgba(16, 185, 129, 0.05)`,
    iconBg:   `rgba(16, 185, 129, 0.04)`,
  },
  expense: {
    gradient: `linear-gradient(135deg, rgba(239, 68, 68, 0.04) 0%, rgba(254, 242, 242, 0.95) 100%)`,
    border:   `rgba(239, 68, 68, 0.05)`,
    iconBg:   `rgba(239, 68, 68, 0.04)`,
  },
  profit: {
    gradient: `linear-gradient(135deg, rgba(37, 99, 235, 0.04) 0%, rgba(239, 246, 255, 0.95) 100%)`,
    border:   `rgba(37, 99, 235, 0.05)`,
    iconBg:   `rgba(37, 99, 235, 0.04)`,
  },
  balance: {
    gradient: `linear-gradient(135deg, rgba(59, 130, 246, 0.03) 0%, rgba(239, 246, 255, 0.95) 100%)`,
    border:   `rgba(59, 130, 246, 0.05)`,
    iconBg:   `rgba(59, 130, 246, 0.03)`,
  },
  transfer: {
    gradient: `linear-gradient(135deg, rgba(100, 116, 139, 0.03) 0%, rgba(248, 250, 252, 0.95) 100%)`,
    border:   `rgba(37, 99, 235, 0.04)`,
    iconBg:   `rgba(100, 116, 139, 0.03)`,
  },
  /** @deprecated 使用 transfer 代替 */
  purple: {
    gradient: `linear-gradient(135deg, rgba(100, 116, 139, 0.03) 0%, rgba(248, 250, 252, 0.95) 100%)`,
    border:   `rgba(37, 99, 235, 0.04)`,
    iconBg:   `rgba(100, 116, 139, 0.03)`,
  },
  neutral: {
    gradient: `linear-gradient(135deg, rgba(100, 116, 139, 0.03) 0%, rgba(248, 250, 252, 0.95) 100%)`,
    border:   `rgba(148, 163, 184, 0.06)`,
    iconBg:   `rgba(100, 116, 139, 0.03)`,
  },
} as const

// ============================================================
// 4. CSS 变量映射 — 注入 :root 的完整颜色 token
//    修改颜色只需改上方的原始色板或语义色，此处映射自动生效
// ============================================================

export const CSS_COLOR_VARS: Record<string, string> = {
  // 主色 Primary
  '--color-primary':         blue[600],
  '--color-primary-light-1': blue[500],
  '--color-primary-light-2': blue[400],
  '--color-primary-light-3': blue[300],
  '--color-primary-light-4': blue[200],
  '--color-primary-light-5': blue[100],
  '--color-primary-light-6': blue[50],
  '--color-primary-light-7': blue[950],
  '--color-primary-light-8': blue[975],
  '--color-primary-light-9': blue[999],
  '--color-primary-dark-1':  blue[700],
  '--color-primary-dark-2':  blue[800],

  // 成功 Success
  '--color-success':         emerald[500],
  '--color-success-light-1': emerald[400],
  '--color-success-light-2': emerald[300],
  '--color-success-light-3': emerald[200],
  '--color-success-light-4': emerald[100],
  '--color-success-light-5': emerald[50],
  '--color-success-dark-1':  emerald[600],
  '--color-success-dark-2':  emerald[700],

  // 警告 Warning
  '--color-warning':         amber[500],
  '--color-warning-light-1': amber[400],
  '--color-warning-light-2': amber[300],
  '--color-warning-light-3': amber[200],
  '--color-warning-light-4': amber[100],
  '--color-warning-light-5': amber[50],
  '--color-warning-dark-1':  amber[600],
  '--color-warning-dark-2':  amber[700],

  // 危险 Danger
  '--color-danger':         red[500],
  '--color-danger-light-1': red[400],
  '--color-danger-light-2': red[300],
  '--color-danger-light-3': red[200],
  '--color-danger-light-4': red[100],
  '--color-danger-light-5': red[50],
  '--color-danger-dark-1':  red[600],
  '--color-danger-dark-2':  red[700],

  // 信息 Info（灰色，Element Plus 兼容）
  '--color-info':         gray[500],
  '--color-info-light-1': gray[400],
  '--color-info-light-2': gray[300],
  '--color-info-light-3': gray[200],
  '--color-info-light-4': gray[100],
  '--color-info-light-5': gray[50],
  '--color-info-dark-1':  gray[600],
  '--color-info-dark-2':  gray[700],

  // 背景色
  '--bg-page':           slate[50],
  '--bg-card':           '#FFFFFF',
  '--bg-sidebar':        slate[800],
  '--bg-sidebar-active': slate[700],
  '--bg-sidebar-hover':  slate[850],
  '--bg-header':         '#FFFFFF',
  '--bg-overlay':        'rgba(0, 0, 0, 0.5)',
  '--bg-hover':          slate[100],
  '--bg-stripe':         '#FAFBFC',

  // 文本色
  '--text-primary':     slate[800],
  '--text-regular':     slate[700],
  '--text-secondary':   slate[500],
  '--text-placeholder': slate[400],
  '--text-disabled':    slate[300],
  '--text-inverse':     '#FFFFFF',
  '--text-on-dark':     slate[200],

  // 边框色
  '--border-base':  slate[200],
  '--border-light': slate[100],
  '--border-dark':  slate[300],

  // 主色浅底（供侧边栏 active、标签浅底等消费）
  '--primary-surface': blue[50],
  '--primary-soft-bg': `rgba(37, 99, 235, 0.15)`,

  // Element Plus 兼容映射（引用上方已定义的变量）
  '--el-color-primary':          blue[600],
  '--el-color-primary-light-1':  blue[500],
  '--el-color-primary-light-2':  blue[400],
  '--el-color-primary-light-3':  blue[300],
  '--el-color-primary-light-4':  blue[200],
  '--el-color-primary-light-5':  blue[100],
  '--el-color-primary-light-6':  blue[50],
  '--el-color-primary-light-7':  blue[950],
  '--el-color-primary-light-8':  blue[975],
  '--el-color-primary-light-9':  blue[999],
  '--el-color-primary-dark-2':   blue[800],
  '--el-color-success':          emerald[500],
  '--el-color-success-light-3':  emerald[200],
  '--el-color-success-light-5':  emerald[50],
  '--el-color-warning':          amber[500],
  '--el-color-warning-light-3':  amber[200],
  '--el-color-warning-light-5':  amber[50],
  '--el-color-danger':           red[500],
  '--el-color-danger-light-3':   red[200],
  '--el-color-danger-light-5':   red[50],
  '--el-color-info':             gray[500],
  '--el-color-info-light-3':     gray[200],
  '--el-color-info-light-5':     gray[50],
}

/** 暗色模式颜色覆盖（仅覆盖需要变化的变量） */
export const CSS_DARK_VARS: Record<string, string> = {
  '--bg-page':           slate[900],
  '--bg-card':           slate[800],
  '--bg-sidebar':        slate[900],
  '--bg-header':         slate[800],
  '--bg-hover':          slate[700],
  '--bg-stripe':         slate[925],

  '--text-primary':      slate[100],
  '--text-regular':      slate[300],
  '--text-secondary':    slate[400],
  '--text-placeholder':  slate[500],
  '--text-disabled':     slate[600],

  '--border-base':       slate[700],
  '--border-light':      slate[800],
  '--border-dark':       slate[600],
}

// ============================================================
// 5. applyTheme() — 将颜色 token 注入 :root CSS 变量
//    在 main.ts 中调用一次即可，后续改颜色只需改上方色板
// ============================================================

export function applyTheme(vars: Record<string, string> = CSS_COLOR_VARS): void {
  const root = document.documentElement
  for (const [key, value] of Object.entries(vars)) {
    root.style.setProperty(key, value)
  }
}
