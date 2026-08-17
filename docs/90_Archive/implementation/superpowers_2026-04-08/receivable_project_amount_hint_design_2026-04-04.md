# 应收表单 - 项目金额提示与回填

## 概述

在应收记录创建/编辑表单中，选择项目后展示项目财务概况卡片，允许用户一键将项目金额填入应收金额字段。

## 交互设计

1. 用户选择项目 → 项目选择框下方出现财务概况卡片
2. 卡片展示：合同金额、已收金额、应收余额
3. 卡片底部两个按钮：「填入合同金额」「填入应收余额」
4. 点击按钮 → 对应金额自动填入 `totalAmount` 字段
5. 清空项目选择 → 卡片隐藏

## 技术方案

**方案**：前端纯本地数据（活跃项目列表 API 已返回所有金额字段）

### 改动文件

仅 `frontend/src/features/finance/components/ReceivableForm.vue`：

- 新增 `selectedProject` computed：根据 `form.projectId` 从 `projects` 数组查找
- 新增卡片模板：`v-if="selectedProject"` 条件渲染
- 新增 `fillAmount(amount)` 方法：将金额赋值给 `form.totalAmount`
- 新增 scoped 样式：卡片布局

### 无后端改动

`GET /api/projects/active` 返回 `ProjectDto`，已包含 `contractAmount`、`receivedAmount`、`receivableAmount`。
