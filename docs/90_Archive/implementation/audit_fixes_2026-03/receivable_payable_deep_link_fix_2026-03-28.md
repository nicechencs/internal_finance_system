# 应收应付详情页面深链修复

**日期**: 2026-03-28
**类型**: Bug 修复

## 问题描述

应收/应付详情页面（`ReceivableDetailPage.vue` 和 `PayableDetailPage.vue`）原本是纯 Dialog 组件，无法通过路由直接访问。虽然路由配置了 `/receivables/:id` 和 `/payables/:id`，但访问这些 URL 时会显示空白页面。

## 修复方案

改造两个详情页面组件，使其同时支持两种模式：

### 1. Dialog 模式
- 当 `visible` prop 存在时，显示为弹窗
- 用于从列表页点击查看详情的场景

### 2. Page 模式
- 当从路由直接访问时，显示为完整页面
- 支持深链分享和浏览器前进/后退

## 实现细节

### 关键逻辑

```typescript
// 判断是否为 Dialog 模式
const isDialogMode = computed(() => props.visible !== undefined)

// 获取有效的 ID（优先使用 prop，否则从路由获取）
const effectiveId = computed(() => {
  if (props.receivableId) return props.receivableId
  const routeId = route.params.id
  return routeId ? Number(routeId) : null
})

// Dialog 模式：监听 visible 变化
watch(() => props.visible, async (val) => {
  if (val && effectiveId.value) {
    await loadReceivable()
  }
})

// Page 模式：组件挂载时加载
onMounted(() => {
  if (!isDialogMode.value && effectiveId.value) {
    loadReceivable()
  }
})
```

### 模板结构

```vue
<template>
  <!-- Dialog 模式 -->
  <el-dialog v-if="isDialogMode" ...>
    <!-- 详情内容 -->
  </el-dialog>

  <!-- Page 模式 -->
  <div v-else class="page-container">
    <el-page-header @back="handleBack" title="返回" content="应收详情" />
    <el-card>
      <!-- 详情内容 -->
    </el-card>
  </div>
</template>
```

## 修改文件

1. `frontend/src/features/finance/pages/ReceivableDetailPage.vue`
   - 添加 Page 模式布局
   - 修改 Props 为可选
   - 添加 `isDialogMode` 和 `effectiveId` 计算属性
   - 添加 `onMounted` 钩子
   - 添加 `handleBack` 方法

2. `frontend/src/features/finance/pages/PayableDetailPage.vue`
   - 统一 `isDialogMode` 判断逻辑（与应收页面保持一致）
   - 将 `currentId` 重命名为 `effectiveId`
   - 添加错误提示

## 测试场景

### Dialog 模式（原有功能）
1. 从财务管理页面点击应收/应付记录
2. 弹出详情对话框
3. 可以查看详情、登记收款/付款
4. 关闭对话框返回列表

### Page 模式（新增功能）
1. 直接访问 `/finance/receivables/123`
2. 显示完整页面布局
3. 点击返回按钮回到上一页
4. 支持浏览器前进/后退
5. 可以分享 URL 给他人

## 兼容性

- 完全向后兼容，不影响现有 Dialog 模式的使用
- Props 改为可选，但保持相同的接口
- 所有现有调用方式无需修改

## 相关路由

```typescript
{
  path: 'receivables/:id',
  name: 'ReceivableDetail',
  component: () => import('@/features/finance/pages/ReceivableDetailPage.vue'),
  meta: { title: '应收账款详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
},
{
  path: 'payables/:id',
  name: 'PayableDetail',
  component: () => import('@/features/finance/pages/PayableDetailPage.vue'),
  meta: { title: '应付账款详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/finance' }
}
```
