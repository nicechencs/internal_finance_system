# 演示数据说明

## 概述

本目录包含用于开发和测试环境的演示数据初始化脚本。**生产环境请勿使用这些脚本。**

## 快速开始（推荐）

### 方法 1：使用自动化脚本（最简单）

项目根目录提供了自动化脚本，可一键初始化演示数据：

**Windows**：
```bash
# 在项目根目录执行
init-demo-data.bat
```

**Linux/Mac**：
```bash
# 在项目根目录执行
chmod +x init-demo-data.sh
./init-demo-data.sh
```

### 方法 2：启动脚本自动初始化

使用 `start-dev.bat` 启动开发环境时，会自动检查并导入演示数据：

```bash
# Windows
start-dev.bat

# 脚本会自动：
# 1. 启动数据库
# 2. 启动后端（创建表结构和默认数据）
# 3. 检查并导入演示数据
# 4. 启动前端
```

## 文件说明

- `seed_demo_data.sql` - PostgreSQL 演示数据初始化脚本
- `../../init-demo-data.bat` - Windows 自动化初始化脚本
- `../../init-demo-data.sh` - Linux/Mac 自动化初始化脚本

## 手动执行（高级用户）

## 手动执行（高级用户）

如需手动执行 SQL 脚本：

### 前提条件

1. 已完成数据库表结构创建（通过 EF Core 迁移或 `EnsureCreated()`）
2. 已创建默认管理员用户（系统启动时自动创建）
3. 已创建默认分类数据（系统启动时自动创建）

### 执行脚本

#### 方法 1：使用 psql 命令行

```bash
# 进入 psql
psql -U postgres -d finance_dev

# 执行脚本
\i docs/02_Database/seed_demo_data.sql

# 或者直接执行
psql -U postgres -d finance_dev -f docs/02_Database/seed_demo_data.sql
```

#### 方法 2：使用 pgAdmin 或其他 GUI 工具

1. 打开 pgAdmin 连接到数据库
2. 打开查询工具
3. 加载 `seed_demo_data.sql` 文件
4. 执行脚本

### 演示数据内容

脚本会创建以下演示数据：

- **4 个账户**：工商银行、招商银行、支付宝、建设银行
- **6 个客户**：示例科技、示例贸易、示例创新、示例制造、示例电商、示例软件
- **6 个供应商**：示例云、示例网络、示例办公、示例外包、示例物业、示例差旅
- **4 个人员**：张三、李四、王五、赵六

### 注意事项

1. **幂等性**：脚本会检查是否已存在账户数据，如果存在则跳过创建
2. **依赖关系**：脚本依赖 admin 用户的存在，如果找不到会报错
3. **项目和交易数据**：由于业务复杂性，项目、交易、应收应付等数据建议通过系统界面手动创建，或根据实际需求编写额外的 SQL 脚本

## 清理演示数据

如需清理演示数据，可执行以下 SQL（**谨慎操作**）：

```sql
-- 删除所有演示数据（保留管理员和分类）
DELETE FROM transaction_allocations;
DELETE FROM transactions;
DELETE FROM receivables;
DELETE FROM payables;
DELETE FROM projects;
DELETE FROM persons WHERE NOT is_deleted;
DELETE FROM suppliers WHERE NOT is_deleted;
DELETE FROM customers WHERE NOT is_deleted;
DELETE FROM accounts WHERE NOT is_deleted;

-- 重置序列（可选）
ALTER SEQUENCE accounts_id_seq RESTART WITH 1;
ALTER SEQUENCE customers_id_seq RESTART WITH 1;
ALTER SEQUENCE suppliers_id_seq RESTART WITH 1;
ALTER SEQUENCE persons_id_seq RESTART WITH 1;
ALTER SEQUENCE projects_id_seq RESTART WITH 1;
ALTER SEQUENCE transactions_id_seq RESTART WITH 1;
ALTER SEQUENCE receivables_id_seq RESTART WITH 1;
ALTER SEQUENCE payables_id_seq RESTART WITH 1;
```

## 生产环境部署

**重要提醒**：

1. 生产环境**不应该**执行演示数据脚本
2. 生产环境只需要：
   - 默认管理员用户（系统自动创建）
   - 默认分类数据（系统自动创建）
3. 所有业务数据应通过系统界面或正式的数据导入流程创建
4. 确保修改默认管理员密码（`admin123`）

## 自定义演示数据

如需创建自定义的演示数据，可以：

1. 复制 `seed_demo_data.sql` 为新文件
2. 根据实际需求修改数据内容
3. 添加项目、交易等更复杂的数据（需注意外键关系）

## 故障排查

### 错误：未找到 admin 用户

**原因**：数据库未初始化或 admin 用户被删除

**解决**：启动后端应用，系统会自动创建 admin 用户

### 错误：演示数据已存在

**原因**：数据库中已有账户数据

**解决**：如需重新创建，请先清理现有数据（参考"清理演示数据"章节）

### 错误：外键约束违反

**原因**：引用的数据不存在（如分类、客户等）

**解决**：确保先创建被引用的数据，或调整脚本中的数据创建顺序
