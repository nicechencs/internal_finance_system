# 财务管理系统 - AI 开发提示词模板

本文档提供系统各模块开发时可直接使用的提示词模板，用于指导 AI 辅助生成代码。

---

## 一、项目初始化

### 1.1 后端项目初始化

```
你是一个资深 .NET 开发工程师。请帮我创建一个 .NET 8 Web API 项目，项目名称为 FinanceApp。

技术要求：
- .NET 8 + ASP.NET Core Web API
- EF Core + PostgreSQL（Npgsql）
- JWT 认证
- 分层架构：Controller → Service → Repository → Entity
- 使用 AutoMapper 做 DTO 映射
- 使用 FluentValidation 做参数校验
- 使用 Serilog 做日志
- 使用 Swagger/OpenAPI 做接口文档

项目结构：
FinanceApp/
├── FinanceApp.Api/           # Web API 层
│   ├── Controllers/
│   ├── Filters/
│   ├── Middleware/
│   └── Program.cs
├── FinanceApp.Application/   # 应用服务层
│   ├── DTOs/
│   ├── Services/
│   ├── Interfaces/
│   └── Mappings/
├── FinanceApp.Domain/        # 领域层
│   ├── Entities/
│   ├── Enums/
│   └── Interfaces/
├── FinanceApp.Infrastructure/ # 基础设施层
│   ├── Data/
│   ├── Repositories/
│   └── Configurations/
└── FinanceApp.sln

请生成完整的项目骨架代码，包括：
1. 解决方案文件和各项目的 csproj
2. Program.cs 中的服务注册和中间件配置
3. DbContext 基础配置
4. JWT 认证配置
5. 全局异常处理中间件
6. 统一响应格式封装（ApiResponse<T>）
7. 分页请求和响应的基类
```

### 1.2 前端项目初始化

```
你是一个资深前端开发工程师。请帮我创建一个 Vue3 前端项目，用于财务管理系统。

技术要求：
- Vue 3 + TypeScript + Vite
- Element Plus 组件库
- Pinia 状态管理
- Vue Router 路由
- Axios 封装 HTTP 请求
- ECharts 图表

项目结构：
finance-web/
├── src/
│   ├── api/          # API 请求封装
│   ├── assets/       # 静态资源
│   ├── components/   # 公共组件
│   ├── composables/  # 组合式函数
│   ├── layouts/      # 布局组件
│   ├── router/       # 路由配置
│   ├── stores/       # Pinia 状态
│   ├── types/        # TypeScript 类型
│   ├── utils/        # 工具函数
│   └── views/        # 页面组件
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json

请生成：
1. 项目配置文件（vite.config.ts, tsconfig.json, package.json）
2. Axios 封装（请求拦截、响应拦截、JWT Token 自动携带、统一错误处理）
3. 路由配置（含路由守卫）
4. 基础布局组件（侧边栏 + 顶部导航 + 内容区）
5. Pinia 用户状态管理
6. API 基础类型定义（ApiResponse, PageRequest, PageResponse）
```

---

## 二、后端模块开发

### 2.1 账户管理模块

```
基于以下数据库表结构，请生成账户管理模块的完整代码：

数据库表 accounts：
- id BIGSERIAL PRIMARY KEY
- name VARCHAR(100) NOT NULL
- account_type VARCHAR(20) NOT NULL  -- bank, alipay
- account_number VARCHAR(50)
- bank_name VARCHAR(100)
- opening_balance DECIMAL(18,2) DEFAULT 0
- current_balance DECIMAL(18,2) DEFAULT 0
- currency VARCHAR(10) DEFAULT 'CNY'
- description TEXT
- is_active BOOLEAN DEFAULT true
- created_at TIMESTAMP
- updated_at TIMESTAMP
- is_deleted BOOLEAN DEFAULT false

请生成以下代码：
1. Entity 实体类（Account.cs）
2. EF Core 配置类（AccountConfiguration.cs）
3. DTO 类（CreateAccountDto, UpdateAccountDto, AccountDto, AccountSummaryDto）
4. Service 接口和实现（IAccountService, AccountService）
5. Controller（AccountsController）
6. AutoMapper Profile

API 接口要求：
- GET    /api/v1/accounts          获取列表（支持按类型、状态筛选）
- POST   /api/v1/accounts          创建账户
- GET    /api/v1/accounts/{id}     获取详情
- PUT    /api/v1/accounts/{id}     更新
- DELETE /api/v1/accounts/{id}     软删除
- GET    /api/v1/accounts/summary  余额汇总

所有接口使用统一的 ApiResponse<T> 响应格式。
```

### 2.2 Excel 导入模块

```
请为财务系统开发 Excel 银行流水导入模块。

业务流程：
1. 用户上传 Excel 文件，指定目标账户
2. 系统解析 Excel，提取流水数据
3. 通过 MD5(日期+金额+对方名称+摘要) 检测重复
4. 返回预览数据（含重复标记和自动分类建议）
5. 用户确认后写入 bank_transactions 表
6. 自动生成 transactions 记录
7. 根据 classification_rules 表自动匹配分类

涉及的数据库表：
- import_batches（导入批次）
- bank_transactions（银行流水，含 unique_hash 字段）
- transactions（交易记录）
- classification_rules（分类规则）

技术要求：
- 使用 EPPlus 或 NPOI 解析 Excel
- 支持不同银行的 Excel 格式（通过列映射配置）
- 大文件异步处理
- 事务保证数据一致性

请生成：
1. Excel 解析服务（IExcelParserService）
2. 导入服务（IImportService）
3. 重复检测逻辑
4. 自动分类匹配逻辑
5. ImportController
6. 相关 DTO
```

### 2.3 交易管理模块

```
请为财务系统开发交易管理模块，这是系统的核心模块。

数据库表 transactions：
- id, bank_transaction_id, transaction_date, amount
- transaction_type (income/expense)
- category_id, account_id, project_id
- customer_id, supplier_id, person_id
- description, status, is_allocated
- created_by, created_at, updated_at, is_deleted

关联表 transaction_allocations：
- id, transaction_id, project_id(nullable), person_id(nullable), amount, allocation_rate
- CHECK: project_id 和 person_id 至少一个不为空

功能要求：
1. CRUD 操作，支持多条件组合查询（日期范围、类型、分类、项目、客户、供应商、人员、金额范围、关键词）
2. 批量更新分类（选中多条交易，统一设置分类和项目）
3. 费用分摊（一条交易分摊到多个项目或多个人员，金额之和必须等于交易金额）
4. 创建/更新交易时自动更新账户余额
5. 删除交易时回滚账户余额
6. 所有操作记录审计日志

请生成完整的 Service、Controller、DTO 代码。
注意事务一致性和并发安全。
```

### 2.4 应收应付模块

```
请为财务系统开发应收应付管理模块。

应收账款表 receivables：
- id, project_id, customer_id
- total_amount, received_amount, remaining_amount
- due_date, status (pending/partial/settled)

应收明细表 receivable_details：
- id, receivable_id, transaction_id, payment_date, amount

应付账款表 payables：
- id, supplier_id, project_id
- total_amount, paid_amount, remaining_amount
- due_date, status

应付明细表 payable_details：
- id, payable_id, transaction_id, payment_date, amount

业务规则：
1. 创建应收/应付时，remaining_amount = total_amount
2. 登记收款/付款时：
   - 更新 received_amount / paid_amount
   - 重新计算 remaining_amount
   - remaining = 0 时自动标记为 settled
   - 0 < received < total 时标记为 partial
3. 逾期判定：查询时动态计算（due_date < 当前日期 AND status IN (pending, partial)），DTO 中返回 isOverdue 标记，不存储为数据库状态
4. 收款/付款可关联已有的 transaction 记录
5. 提供汇总统计接口

请生成完整代码。
```

### 2.5 报表模块

```
请为财务系统开发报表模块。

报表类型：
1. 月度利润报表 - 按月统计总收入、总支出、净利润，按分类细分
2. 现金流报表 - 期初余额、收入、支出、期末余额，支持按月展示趋势
3. 项目利润报表 - 每个项目的合同额、已收款、成本、利润、利润率
4. 人员成本分析 - 按人员统计工资、提成、报销、分红
5. 供应商支出统计 - 按供应商统计支出金额和排名
6. 年度经营概览 - 年度汇总 + 月度趋势 + TOP 排名

技术要求：
- 使用 LINQ 进行复杂聚合查询
- 报表接口支持自定义时间范围
- 返回结构化数据，前端负责图表渲染
- 考虑查询性能，必要时使用数据库视图

已有视图：
- v_project_profit（项目利润视图）
- v_account_balance（账户余额视图）

请生成 ReportService 和 ReportsController 的完整代码。
```

---

## 三、前端页面开发

### 3.1 账户管理页面

```
请使用 Vue3 + TypeScript + Element Plus 开发账户管理页面。

页面功能：
1. 账户列表（表格展示，支持按类型筛选）
2. 新增/编辑账户（弹窗表单）
3. 删除账户（确认弹窗）
4. 账户余额汇总卡片

API 接口：
- GET    /api/v1/accounts
- POST   /api/v1/accounts
- PUT    /api/v1/accounts/{id}
- DELETE /api/v1/accounts/{id}
- GET    /api/v1/accounts/summary

表格列：账户名称、类型（标签）、账号、银行、期初余额、当前余额、状态、操作

表单字段：
- 账户名称（必填）
- 账户类型（下拉：银行账户/支付宝，必填）
- 账号
- 银行名称（类型为银行时显示）
- 期初余额（数字输入）
- 备注

请生成：
1. 页面组件 AccountList.vue
2. 表单弹窗组件 AccountForm.vue
3. API 请求封装 account.ts
4. TypeScript 类型定义
```

### 3.2 Excel 导入页面

```
请使用 Vue3 + TypeScript + Element Plus 开发 Excel 导入页面。

页面流程（步骤条）：
Step 1: 选择账户 + 上传 Excel 文件
Step 2: 预览解析结果（表格展示，高亮重复行，显示自动分类建议，支持手动修改分类）
Step 3: 确认导入，显示导入结果

API 接口：
- POST   /api/v1/import/upload        上传并预览
- POST   /api/v1/import/{batchId}/confirm  确认导入
- GET    /api/v1/import/history        导入历史

功能细节：
- 上传组件限制 .xlsx 格式（不支持 .xls 格式）
- 如用户上传 .xls 文件，显示提示："不支持 .xls 格式，请在 Excel 中另存为 .xlsx 格式后重试"
- 预览表格中重复行用黄色背景标记
- 分类列使用下拉选择器，可修改自动匹配的结果
- 导入历史用表格展示，显示文件名、导入时间、成功/重复/失败条数

请生成完整的页面组件和相关代码。
```

### 3.3 交易管理页面

```
请使用 Vue3 + TypeScript + Element Plus 开发交易管理页面。

页面功能：
1. 高级搜索区域（可折叠）：
   - 日期范围选择器
   - 交易类型（收入/支出）
   - 分类（树形下拉）
   - 账户（下拉）
   - 项目（下拉）
   - 客户/供应商/人员（下拉）
   - 金额范围
   - 关键词搜索

2. 交易列表表格：
   - 列：日期、金额（收入绿色/支出红色）、类型、分类、账户、项目、对方、摘要、状态、操作
   - 支持多选
   - 底部显示选中条数和金额合计

3. 操作功能：
   - 新增交易（弹窗表单）
   - 编辑交易
   - 删除交易
   - 批量设置分类
   - 费用分摊（弹窗：添加多个项目和金额，实时校验合计）

请生成完整代码。
```

### 3.4 仪表盘页面

```
请使用 Vue3 + TypeScript + Element Plus + ECharts 开发仪表盘首页。

页面布局：
1. 顶部统计卡片（4列）：
   - 账户总余额（蓝色）
   - 本月收入（绿色）
   - 本月支出（红色）
   - 本月利润（橙色）

2. 中部图表区域（2列）：
   - 左：收入支出趋势折线图（近12个月）
   - 右：支出分类饼图（本月）

3. 下部列表区域（2列）：
   - 左：最近交易记录（最近10条）
   - 右：逾期应收提醒列表

API 接口：
- GET /api/v1/dashboard

使用 ECharts 渲染图表，响应式布局适配不同屏幕。
卡片数据使用 CountTo 动画效果。
```

### 3.5 报表页面

```
请使用 Vue3 + TypeScript + Element Plus + ECharts 开发报表分析页面。

页面结构（Tab 切换）：

Tab 1 - 月度利润：
- 时间选择器（年月）
- 收入/支出/利润汇总卡片
- 收入分类柱状图
- 支出分类柱状图

Tab 2 - 现金流：
- 日期范围选择器
- 账户筛选
- 现金流趋势图（折线图：期初、收入、支出、期末）
- 明细表格

Tab 3 - 项目利润：
- 日期范围选择器
- 项目利润排名柱状图
- 项目利润明细表格（合同额、已收、成本、利润、利润率）

Tab 4 - 人员成本：
- 日期范围选择器
- 人员类型筛选
- 人员成本堆叠柱状图（工资、提成、报销、分红）
- 明细表格

Tab 5 - 供应商支出：
- 日期范围选择器
- 供应商支出排名柱状图
- 明细表格

请生成完整代码，图表使用 ECharts，支持数据导出为 Excel。
```

---

## 四、通用功能开发

### 4.1 审计日志

```
请为系统添加审计日志功能。

要求：
1. 后端：创建 AuditLog 实体和 AuditService
2. 使用 EF Core 的 SaveChanges 拦截器自动记录变更
3. 记录内容：操作人、操作类型（增删改）、实体类型、实体ID、变更前后的值（JSON）
4. 提供查询接口，支持按实体类型、操作类型、时间范围筛选
5. 前端：操作日志查看页面（只读表格，支持查看变更详情弹窗）

数据库表 audit_logs：
- id, user_id, action, entity_type, entity_id
- old_value (JSONB), new_value (JSONB)
- ip_address, user_agent, created_at

请生成后端拦截器和前端页面的完整代码。
```

### 4.2 分类规则引擎

```
请为系统开发分类规则引擎。

规则表 classification_rules：
- id, rule_name, priority
- match_field (counterparty / memo / amount)
- match_operator (equals / contains / starts_with / ends_with / regex / range)
- match_value
- category_id, project_id, customer_id, supplier_id, person_id

匹配逻辑：
1. 导入银行流水时，对每条记录按 priority 降序逐条匹配规则
2. 匹配成功则自动填充 category_id, project_id 等字段
3. 匹配失败则标记为"待分类"

match_operator 实现：
- equals: 精确匹配
- contains: 包含匹配
- starts_with: 前缀匹配
- ends_with: 后缀匹配
- regex: 正则表达式匹配
- range: 金额范围匹配（match_value 格式："1000-5000"）

请生成：
1. 规则匹配引擎服务（IRuleEngineService）
2. 规则 CRUD 服务
3. 规则管理页面（表格 + 表单，支持测试匹配）
```

---

## 五、部署相关

### 5.1 Docker 部署

```
请为财务管理系统生成 Docker 部署配置。

系统组成：
- 后端：.NET 8 Web API
- 前端：Vue3 SPA（Nginx 托管）
- 数据库：PostgreSQL 14

请生成：
1. 后端 Dockerfile（多阶段构建）
2. 前端 Dockerfile（构建 + Nginx）
3. docker-compose.yml（含三个服务 + 数据卷 + 网络）
4. Nginx 配置文件（前端托管 + API 反向代理）
5. .env.example 环境变量模板
6. 数据库初始化脚本挂载配置

要求：
- 生产环境优化（镜像体积、安全性）
- 数据持久化（PostgreSQL 数据卷）
- 自动重启策略
- 健康检查配置
```

### 5.2 数据库备份

```
请为 PostgreSQL 数据库设计自动备份方案。

要求：
1. 每日凌晨 2:00 自动备份
2. 备份文件按日期命名，保留最近 90 天
3. 支持手动触发备份
4. 支持从备份恢复

请生成：
1. 备份 Shell 脚本
2. 恢复 Shell 脚本
3. Cron 定时任务配置
4. Docker 环境下的备份方案（docker-compose 中添加备份服务）
```

---

## 六、使用说明

### 使用方式

1. 复制对应模块的提示词
2. 粘贴到 AI 对话中
3. 根据实际情况调整细节（如字段名、业务规则）
4. AI 生成代码后，review 并集成到项目中

### 提示词使用技巧

- 先使用"项目初始化"模板搭建骨架
- 按开发路线图顺序逐模块开发
- 每个模块开发完成后，先测试再进入下一个
- 遇到问题时，将错误信息和上下文一起提供给 AI
- 可以在提示词中追加具体的业务规则或约束条件

### 自定义扩展

如需添加新模块，可参考以下模板结构：

```
请为财务系统开发 [模块名称] 模块。

数据库表 [表名]：
- [字段列表]

功能要求：
1. [功能1]
2. [功能2]

API 接口：
- [HTTP方法] [路径] [说明]

业务规则：
1. [规则1]
2. [规则2]

请生成完整的后端（Entity, DTO, Service, Controller）和前端（页面组件, API封装, 类型定义）代码。
```
