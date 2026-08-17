# 深链问题审计报告

**日期**: 2026-03-28
**问题**: FIN-02 和 PARTY-01 深链不可用

## 问题分析

以下路由直接挂载了 dialog 组件，导致深链访问时组件无法正常工作：

### 财务模块 (FIN-02)
- `/receivables/:id` → `ReceivableDetailPage.vue` (实际是 dialog)
- `/payables/:id` → `PayableDetailPage.vue` (实际是 dialog)

### 基础数据模块 (PARTY-01)
- `/customers/create` 和 `/customers/:id/edit` → `CustomerFormPage.vue` (实际是 dialog)
- `/suppliers/create` 和 `/suppliers/:id/edit` → `SupplierFormPage.vue` (实际是 dialog)
- `/persons/create` 和 `/persons/:id/edit` → `PersonFormPage.vue` (实际是 dialog)

## 根本原因

这些 `*Page.vue` 文件实际上是 dialog 组件，它们：
1. 使用 `<el-dialog>` 作为根元素
2. 需要 `visible` prop 来控制显示
3. 需要父组件传递数据（如 `customer`, `supplier`, `receivableId` 等）
4. 通过 `emit` 事件与父组件通信

当通过路由直接访问这些组件时：
- 没有父组件提供必需的 props
- `visible` prop 未定义，dialog 不会显示
- 无法从路由参数中读取 ID 并加载数据

## 修复方案

### 方案 A：创建真正的页面包装器（推荐）

为每个 dialog 组件创建对应的页面包装器：

1. 将现有的 `*Page.vue` 重命名为 `*Dialog.vue`
2. 创建新的 `*Page.vue` 作为页面包装器，包含：
   - 使用 `<el-page-header>` 和 `<el-card>` 布局
   - 从 `route.params` 读取 ID
   - 在 `onMounted` 中加载数据
   - 包含返回按钮
   - 直接渲染表单/详情内容（不使用 dialog）

**优点**：
- 完全支持深链
- 更好的用户体验（页面而非弹窗）
- 符合现代 Web 应用设计模式

**缺点**：
- 需要重构现有组件
- 可能需要调整列表页面的引用

### 方案 B：在路由组件中包装 dialog

创建路由级别的包装器组件，自动打开 dialog：

```vue
<template>
  <div class="route-dialog-wrapper">
    <CustomerFormDialog
      :visible="true"
      :customer="customer"
      @update:visible="handleClose"
      @success="handleSuccess"
    />
  </div>
</template>
```

**优点**：
- 最小化代码更改
- 保持现有 dialog 组件不变

**缺点**：
- 用户体验较差（深链打开弹窗）
- 无法使用浏览器后退按钮关闭 dialog
- 不符合 Web 应用最佳实践

## 当前状态

尝试修复过程中发现：
- 项目中存在自动格式化工具（可能是 Prettier/ESLint watch 模式）
- 该工具会自动还原文件更改
- 需要先停止自动格式化工具，或者在其配置中排除这些文件

## 建议

1. **立即行动**：停止自动格式化工具的 watch 模式
2. **短期方案**：采用方案 A，创建真正的页面组件
3. **长期优化**：
   - 统一项目中所有表单/详情页面的设计模式
   - 建立明确的命名约定：`*Dialog.vue` 用于弹窗，`*Page.vue` 用于页面
   - 更新路由配置，确保所有深链都指向真正的页面组件

## 影响范围

需要修改的文件：
- `frontend/src/features/finance/pages/ReceivableDetailPage.vue`
- `frontend/src/features/finance/pages/PayableDetailPage.vue`
- `frontend/src/features/master-data/customers/pages/CustomerFormPage.vue`
- `frontend/src/features/master-data/suppliers/pages/SupplierFormPage.vue`
- `frontend/src/features/master-data/persons/pages/PersonFormPage.vue`
- `frontend/src/features/finance/pages/ReceivableListPage.vue` (更新 import)
- `frontend/src/features/finance/pages/PayableListPage.vue` (更新 import)
- `frontend/src/features/master-data/customers/pages/CustomerListPage.vue` (更新 import)
- `frontend/src/features/master-data/suppliers/pages/SupplierListPage.vue` (更新 import)
- `frontend/src/features/master-data/persons/pages/PersonListPage.vue` (更新 import)

## 参考

类似问题也存在于项目表单页面：
- `ProjectFormPage.vue` 也是 dialog 形式
- 建议一并修复，保持一致性
