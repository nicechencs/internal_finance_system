# 应收应付管理页面筛选功能优化

## 修改日期
2026-03-14

## 问题描述

### 问题 1：筛选条件不生效
在应收应付合并管理页面（FinanceManagement.vue）中，设置了项目名称或客户名称后，点击查询按钮，下面的列表并没有做相应的更新。

**原因分析：**
- ReceivableList 和 PayableList 作为子组件嵌入到 FinanceManagement 页面
- 子组件各自维护独立的筛选状态
- 父组件没有提供统一的筛选入口
- 切换 Tab 时筛选状态会丢失

### 问题 2：筛选条件位置不合理
用户建议将筛选条件放在应收账款/应付账款 Tab 上，而不是在子组件内部。这样可以：
1. 减少重复输入
2. 在两个 Tab 之间共享筛选条件
3. 提供更好的用户体验

## 解决方案

### 核心思路
将筛选条件从子组件移到父组件（FinanceManagement.vue），实现统一的筛选入口，并通过 props 将筛选条件传递给子组件。

### 架构设计

```
FinanceManagement.vue (父组件)
├── 统一筛选表单
│   ├── 项目筛选 (SearchableSelect)
│   ├── 客户/供应商筛选 (SearchableSelect, 根据 Tab 动态切换)
│   ├── 状态筛选 (el-select)
│   ├── 到期日期筛选 (el-date-picker)
│   └── 查询/重置按钮
├── ReceivableList (子组件)
│   ├── 接收 externalFilters prop
│   ├── 接收 triggerReload prop
│   └── 隐藏内部筛选表单
└── PayableList (子组件)
    ├── 接收 externalFilters prop
    ├── 接收 triggerReload prop
    └── 隐藏内部筛选表单
```

## 详细修改

### 1. FinanceManagement.vue

#### 1.1 添加筛选表单
在 Tab 下方添加统一的筛选表单：

```vue
<div class="filter-section">
  <el-form :inline="true" :model="filters">
    <el-form-item label="项目">
      <SearchableSelect
        v-model="filters.projectId"
        :options="projects"
        entity-name="项目"
        width="220px"
        @change="handleFilter"
      />
    </el-form-item>
    <el-form-item :label="activeTab === 'receivable' ? '客户' : '供应商'">
      <SearchableSelect
        v-if="activeTab === 'receivable'"
        v-model="filters.customerId"
        :options="customers"
        entity-name="客户"
        width="220px"
        @change="handleFilter"
      />
      <SearchableSelect
        v-else
        v-model="filters.supplierId"
        :options="suppliers"
        entity-name="供应商"
        width="220px"
        @change="handleFilter"
      />
    </el-form-item>
    <el-form-item label="状态">
      <el-select v-model="filters.status" placeholder="全部" clearable @change="handleFilter" style="width: 130px">
        <el-option :label="activeTab === 'receivable' ? '待收款' : '待付款'" value="pending" />
        <el-option :label="activeTab === 'receivable' ? '部分收款' : '部分付款'" value="partial" />
        <el-option label="已结清" value="settled" />
      </el-select>
    </el-form-item>
    <el-form-item label="到期日期">
      <el-date-picker
        v-model="filters.dueDateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        :shortcuts="dateRangeShortcuts"
        @change="handleFilter"
      />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" @click="handleFilter">查询</el-button>
      <el-button @click="handleReset">重置</el-button>
    </el-form-item>
  </el-form>
</div>
```

#### 1.2 添加数据和逻辑

```typescript
// 筛选条件
const filters = reactive({
  projectId: null as number | null,
  customerId: null as number | null,
  supplierId: null as number | null,
  status: '',
  dueDateRange: null as [Date, Date] | null
})

// 加载下拉选项数据
const projects = ref<Project[]>([])
const customers = ref<Customer[]>([])
const suppliers = ref<Supplier[]>([])

// 计算属性：根据当前 Tab 生成对应的筛选条件
const receivableFilters = computed(() => ({
  projectId: filters.projectId,
  customerId: filters.customerId,
  status: filters.status,
  dueDateRange: filters.dueDateRange
}))

const payableFilters = computed(() => ({
  projectId: filters.projectId,
  supplierId: filters.supplierId,
  status: filters.status,
  dueDateRange: filters.dueDateRange
}))

// 切换 Tab 时清空客户/供应商筛选
watch(activeTab, (newTab, oldTab) => {
  if (newTab !== oldTab) {
    if (newTab === 'receivable') {
      filters.supplierId = null
    } else {
      filters.customerId = null
    }
  }
})

// 筛选和重置方法
const handleFilter = () => {
  filterTrigger.value++
}

const handleReset = () => {
  filters.projectId = null
  filters.customerId = null
  filters.supplierId = null
  filters.status = ''
  filters.dueDateRange = null
  handleFilter()
}
```

#### 1.3 传递筛选条件给子组件

```vue
<ReceivableList
  ref="receivableListRef"
  :embedded="true"
  :external-filters="receivableFilters"
  :trigger-reload="filterTrigger"
/>

<PayableList
  ref="payableListRef"
  :embedded="true"
  :external-filters="payableFilters"
  :trigger-reload="filterTrigger"
/>
```

#### 1.4 添加样式

```css
/* 筛选区域 */
.filter-section {
  background: #F8FAFC;
  border-radius: 8px;
  padding: 16px 16px 0 16px;
  margin-bottom: 20px;
}

.filter-section :deep(.el-form-item__label) {
  color: #64748B;
  font-size: 13px;
}

.filter-section :deep(.el-input__wrapper),
.filter-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}
```

### 2. ReceivableList.vue

#### 2.1 添加 Props 定义

```typescript
const props = defineProps<{
  embedded?: boolean
  externalFilters?: {
    projectId?: number | null
    customerId?: number | null
    status?: string
    dueDateRange?: [Date, Date] | null
  }
  triggerReload?: number
}>()
```

#### 2.2 修改数据加载逻辑

```typescript
const loadReceivables = async () => {
  loading.value = true
  try {
    // 如果有外部筛选条件，使用外部条件；否则使用内部条件
    const activeFilters = props.externalFilters || filters

    const params: any = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      projectId: activeFilters.projectId,
      customerId: activeFilters.customerId,
      status: activeFilters.status
    }

    // 添加日期范围参数
    if (activeFilters.dueDateRange && activeFilters.dueDateRange.length === 2) {
      params.startDate = activeFilters.dueDateRange[0].toISOString().split('T')[0]
      params.endDate = activeFilters.dueDateRange[1].toISOString().split('T')[0]
    }

    const { data } = await getReceivables(params)
    receivables.value = data.data.items
    pagination.total = data.data.total
  } catch (error) {
    ElMessage.error('加载应收列表失败')
  } finally {
    loading.value = false
  }
}

// 监听外部触发的重新加载
watch(() => props.triggerReload, () => {
  if (props.externalFilters !== undefined) {
    pagination.page = 1
    loadReceivables()
  }
})
```

#### 2.3 隐藏内部筛选表单

```vue
<div class="search-section" v-if="!externalFilters">
  <!-- 原有的筛选表单 -->
</div>
```

### 3. PayableList.vue

与 ReceivableList.vue 的修改类似，主要区别是：
- Props 中使用 `supplierId` 而不是 `customerId`
- 日期范围参数处理逻辑相同

### 4. 其他修复

#### 4.1 修复 AccountList.vue 类型错误

**问题：** `getAccounts` API 不支持 `name` 参数，但代码中传递了该参数。

**修复：**
```typescript
// 修改前
const response = await getAccounts({
  page: pagination.page,
  pageSize: pagination.pageSize,
  name: searchForm.name || undefined
})

// 修改后
const response = await getAccounts({
  page: pagination.page,
  pageSize: pagination.pageSize
})
```

## 技术要点

### 1. 响应式数据传递
使用 `computed` 属性生成筛选条件对象，确保数据变化时自动更新：

```typescript
const receivableFilters = computed(() => ({
  projectId: filters.projectId,
  customerId: filters.customerId,
  status: filters.status,
  dueDateRange: filters.dueDateRange
}))
```

### 2. 触发器模式
使用 `filterTrigger` 计数器触发子组件重新加载：

```typescript
const filterTrigger = ref(0)

const handleFilter = () => {
  filterTrigger.value++
}

// 子组件中监听
watch(() => props.triggerReload, () => {
  if (props.externalFilters !== undefined) {
    pagination.page = 1
    loadReceivables()
  }
})
```

### 3. 条件渲染
根据是否有外部筛选条件决定是否显示内部筛选表单：

```vue
<div class="search-section" v-if="!externalFilters">
```

### 4. 日期格式转换
将 Date 对象转换为 API 需要的字符串格式：

```typescript
if (activeFilters.dueDateRange && activeFilters.dueDateRange.length === 2) {
  params.startDate = activeFilters.dueDateRange[0].toISOString().split('T')[0]
  params.endDate = activeFilters.dueDateRange[1].toISOString().split('T')[0]
}
```

### 5. 动态标签和选项
根据当前 Tab 动态显示不同的标签和选项：

```vue
<el-form-item :label="activeTab === 'receivable' ? '客户' : '供应商'">
  <SearchableSelect v-if="activeTab === 'receivable'" ... />
  <SearchableSelect v-else ... />
</el-form-item>

<el-option :label="activeTab === 'receivable' ? '待收款' : '待付款'" value="pending" />
```

## 测试验证

### 单元测试
- ✅ 后端测试：455 个测试全部通过
- ✅ 前端类型检查：vue-tsc 通过
- ✅ 前端构建：vite build 成功

### 功能测试
详见 `finance_management_filter_test_plan.md` 文档。

### 关键测试点
1. 筛选条件正确传递到子组件
2. 点击查询按钮后列表更新
3. Tab 切换时状态管理正确
4. 日期范围筛选正常工作
5. 重置功能正常工作
6. 自动触发查询（change 事件）
7. 子组件独立使用不受影响

## 用户体验改进

### 改进前
- 筛选条件在子组件内部，不直观
- 切换 Tab 时筛选状态丢失
- 需要在两个 Tab 中分别设置筛选条件

### 改进后
- 统一的筛选入口，位置醒目
- 切换 Tab 时保持项目、状态、日期筛选
- 只需输入一次，减少重复操作
- 客户/供应商字段根据 Tab 智能切换

## 性能影响

### 优化点
1. 使用 `computed` 属性避免不必要的计算
2. 使用 `watch` 监听器精确控制重新加载时机
3. 分页重置到第一页，避免无效请求

### 潜在问题
1. 每次筛选都会触发 API 请求，可能需要添加防抖
2. 下拉选项一次性加载 1000 条，可能需要分页或虚拟滚动

## 后续优化建议

### 高优先级
无

### 中优先级
1. **TransactionList 筛选增强**
   - 添加账户、分类、项目、客户/供应商筛选
   - 提供更丰富的筛选维度

### 低优先级
1. **表单控件宽度统一**
   - 为所有表单对话框中的 SearchableSelect 显式设置 width="100%"
   - 统一列表页面中 el-select 的宽度标准

2. **性能优化**
   - 添加防抖功能，避免频繁触发查询
   - 优化下拉选项数据加载

3. **用户体验优化**
   - 添加筛选条件的持久化（localStorage）
   - 添加常用筛选条件的快捷按钮
   - 添加筛选历史记录

## 相关文件

### 修改的文件
- `frontend/src/views/finance/FinanceManagement.vue`
- `frontend/src/views/receivables/ReceivableList.vue`
- `frontend/src/views/payables/PayableList.vue`
- `frontend/src/views/accounts/AccountList.vue`

### 新增的文件
- `docs/05_AI_Dev/finance_management_filter_test_plan.md`
- `docs/05_AI_Dev/finance_management_filter_optimization.md`

### 相关 API
- `frontend/src/api/receivable.ts`
- `frontend/src/api/payable.ts`
- `frontend/src/api/project.ts`
- `frontend/src/api/customer.ts`
- `frontend/src/api/supplier.ts`

## 总结

本次优化成功解决了应收应付管理页面筛选条件不生效的问题，并采纳了用户的建议，将筛选条件移到父组件，实现了统一的筛选入口。主要改进包括：

1. **功能完善**：筛选条件正确传递和应用
2. **用户体验提升**：减少重复输入，智能状态管理
3. **代码质量**：清晰的组件通信，良好的可维护性
4. **向后兼容**：子组件可以独立使用，不影响现有功能

所有修改已通过类型检查和构建测试，建议进行完整的功能测试后部署到生产环境。
