# 人员表单页面深链问题修复

**日期**: 2026-03-28
**状态**: ✅ 已完成

## 问题描述

`PersonFormPage.vue` 原本是一个纯 dialog 组件，无法通过路由直接访问。但路由配置中存在以下路由：
- `/persons/create` - 新建人员
- `/persons/:id/edit` - 编辑人员

这导致用户无法通过 URL 直接访问这些页面。

## 修复方案

改造 `PersonFormPage.vue`，让它同时支持两种模式：

### 1. Dialog 模式
当 `visible` prop 存在时，显示为弹窗（原有功能保持不变）

### 2. Page 模式
当从路由访问时，显示为完整页面：
- 使用 `<el-card>` 布局替代 `<el-dialog>`
- 添加返回按钮
- 从 `route.params.id` 读取 ID（编辑模式）
- 保存后自动跳转回列表页

## 实现细节

### 文件变更

1. **PersonFormPage.vue** (D:\demo\chen\finance_system\frontend\src\features\master-data\persons\pages\PersonFormPage.vue)
   - 添加路由检测逻辑 `isDialogMode`
   - 根据模式选择渲染 Dialog 或 Card 布局
   - Page 模式下从 API 加载数据
   - 添加 `handleBack` 方法处理返回操作

2. **PersonForm.vue** (新建) (D:\demo\chen\finance_system\frontend\src\features\master-data\persons\components\PersonForm.vue)
   - 提取表单逻辑到独立组件
   - 接收 `formData`、`rules`、`isEdit` props
   - 暴露 `validate` 和 `clearValidate` 方法

### 核心逻辑

```typescript
// 判断是 Dialog 模式还是 Page 模式
const isDialogMode = computed(() => props.visible !== undefined)

// 判断是编辑还是新增
const isEdit = computed(() => {
  if (isDialogMode.value) {
    return !!props.person
  } else {
    return !!route.params.id
  }
})

// Page 模式：从路由加载数据
const loadPersonData = async () => {
  if (!isDialogMode.value && route.params.id) {
    const { data } = await getPersonById(Number(route.params.id))
    // 填充表单数据
  }
}

// Page 模式：组件挂载时加载数据
onMounted(() => {
  if (!isDialogMode.value) {
    loadPersonData()
  }
})
```

## 测试验证

### 编译检查
```bash
cd frontend
npm run type-check  # ✅ 通过
npm run build       # ✅ 通过
```

### 功能测试

1. **Dialog 模式**（原有功能）
   - 在人员列表页点击"新增"按钮 → 弹窗显示
   - 在人员列表页点击"编辑"按钮 → 弹窗显示并加载数据

2. **Page 模式**（新增功能）
   - 访问 `/persons/create` → 显示新建人员页面
   - 访问 `/persons/123/edit` → 显示编辑人员页面并加载 ID=123 的数据
   - 点击"返回"按钮 → 跳转回 `/persons`
   - 点击"保存"按钮 → 保存成功后跳转回 `/persons`

## 影响范围

- ✅ 不影响现有 Dialog 模式功能
- ✅ 新增 Page 模式支持深链访问
- ✅ 代码结构更清晰（表单逻辑提取到独立组件）

## 后续建议

其他主数据模块（客户、供应商、项目）也存在相同问题，建议采用相同方案修复：
- `CustomerFormPage.vue`
- `SupplierFormPage.vue`
- `ProjectFormPage.vue`
