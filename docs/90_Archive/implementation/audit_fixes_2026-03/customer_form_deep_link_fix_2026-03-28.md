# 客户表单页面深链修复

**日期**: 2026-03-28
**修复文件**: `frontend/src/features/master-data/customers/pages/CustomerFormPage.vue`

## 问题描述

原 `CustomerFormPage.vue` 是一个纯 dialog 组件，无法通过路由直接访问。虽然路由配置了 `/customers/create` 和 `/customers/:id/edit`，但访问这些 URL 时无法正常显示表单。

## 解决方案

改造 `CustomerFormPage.vue`，使其同时支持两种模式：

### 1. Dialog 模式
- 当 `visible` prop 存在时，显示为弹窗
- 用于列表页的快速新增/编辑操作
- 保持向后兼容性

### 2. Page 模式
- 当从路由访问时（无 `visible` prop），显示为完整页面
- 支持深链访问：`/customers/create` 和 `/customers/:id/edit`
- 提供返回按钮和独立的保存/取消按钮

## 实现细节

### 模式检测
```typescript
// 判断是 Dialog 模式还是 Page 模式
const isDialogMode = computed(() => props.visible !== undefined)

// 判断是编辑模式还是创建模式
const isEdit = computed(() => {
  if (isDialogMode.value) {
    return !!props.customer
  } else {
    return !!route.params.id
  }
})
```

### 数据加载
- Dialog 模式：通过 `customer` prop 传入数据
- Page 模式：通过 `route.params.id` 从 API 加载数据

### 保存后行为
- Dialog 模式：触发 `success` 事件，关闭弹窗
- Page 模式：跳转回列表页 `/customers`

## 测试验证

### 1. Dialog 模式测试
- 访问 `/customers`
- 点击"新增客户"按钮，验证弹窗打开
- 填写表单并保存，验证数据创建成功
- 点击编辑按钮，验证弹窗显示现有数据
- 修改数据并保存，验证更新成功

### 2. Page 模式测试
- 直接访问 `/customers/create`，验证显示创建表单页面
- 填写表单并保存，验证跳转回列表页
- 直接访问 `/customers/123/edit`，验证显示编辑表单页面
- 验证数据正确加载
- 修改数据并保存，验证跳转回列表页
- 点击"返回"按钮，验证跳转回列表页

### 3. 构建测试
```bash
cd frontend
npm run type-check  # TypeScript 类型检查通过
npm run build       # 构建成功
```

## 附加修复

修复了 `SupplierFormPage.vue` 中的重复 `</style>` 标签语法错误。

## 影响范围

- 修改文件：`CustomerFormPage.vue`
- 影响功能：客户管理模块
- 向后兼容：是（Dialog 模式完全兼容）
- 新增功能：支持深链访问

## 后续建议

可以考虑将其他表单页面（项目、供应商、人员等）也改造为支持双模式，以提供更好的用户体验和深链支持。
