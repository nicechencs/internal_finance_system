# 演示数据自动化初始化方案

## 实施时间
2026-03-14

## 问题
用户反馈：开发环境使用演示数据时，应该在启动脚本中自动调用生成演示数据，而不是手动执行。

## 解决方案

### 1. 创建独立的初始化脚本

#### Windows 脚本：`init-demo-data.bat`
**位置**：项目根目录

**功能**：
- 检查 Docker 和数据库容器状态
- 自动连接到 PostgreSQL 容器
- 执行 SQL 脚本导入演示数据
- 提供详细的成功/失败反馈

**使用方法**：
```bash
# 在项目根目录执行
init-demo-data.bat
```

#### Linux/Mac 脚本：`init-demo-data.sh`
**位置**：项目根目录

**功能**：与 Windows 版本相同

**使用方法**：
```bash
chmod +x init-demo-data.sh
./init-demo-data.sh
```

### 2. 集成到开发环境启动脚本

#### 修改 `start-dev.bat`

**新增步骤**：
1. 启动 PostgreSQL 数据库
2. 等待数据库就绪（5秒）
3. 启动后端 API（创建表结构和默认数据）
4. 等待后端初始化数据库（10秒）
5. **自动导入演示数据**（如果数据库为空）
6. 启动前端开发服务器

**关键代码**：
```batch
echo [6/7] 导入演示数据（如果数据库为空）...
type docs\02_Database\seed_demo_data.sql | docker exec -i finance_postgres psql -U postgres -d finance_dev
```

### 3. 更新文档

#### 更新 `README.md`
- 添加"快速启动"章节，说明 `start-dev.bat` 会自动初始化演示数据
- 添加"初始化演示数据"章节，说明独立脚本的使用方法
- 列出演示数据包含的内容

#### 更新 `docs/02_Database/README_DEMO_DATA.md`
- 添加"快速开始"章节，推荐使用自动化脚本
- 将原有的手动执行方法移到"手动执行（高级用户）"章节
- 保留详细的使用说明和故障排查指南

#### 更新 `CLAUDE.md`
- 添加"常用脚本"章节
- 说明 `start-dev.bat` 会自动导入演示数据
- 说明 `init-demo-data.bat` 的用途

## 技术实现

### 数据导入流程

```
启动开发环境
    ↓
启动 PostgreSQL 容器
    ↓
等待数据库就绪（5秒）
    ↓
启动后端 API
    ↓
后端自动创建表结构和默认数据
    ↓
等待后端初始化完成（10秒）
    ↓
检查并导入演示数据
    ├─ 如果数据库为空 → 导入演示数据
    └─ 如果已有数据 → 跳过（SQL 脚本内部判断）
    ↓
启动前端开发服务器
```

### SQL 脚本幂等性

`seed_demo_data.sql` 脚本具有幂等性：
```sql
-- 检查是否已有演示数据
IF EXISTS (SELECT 1 FROM accounts WHERE NOT is_deleted LIMIT 1) THEN
    RAISE NOTICE '演示数据已存在，跳过创建';
    RETURN;
END IF;
```

这确保了：
- 多次执行脚本不会重复创建数据
- 不会破坏已有的数据
- 可以安全地在启动脚本中自动执行

### Docker 命令执行

使用管道将 SQL 文件内容传递给 Docker 容器内的 psql：

**Windows**：
```batch
type docs\02_Database\seed_demo_data.sql | docker exec -i finance_postgres psql -U postgres -d finance_dev
```

**Linux/Mac**：
```bash
cat docs/02_Database/seed_demo_data.sql | docker exec -i finance_postgres psql -U postgres -d finance_dev
```

## 用户体验改进

### 改进前
1. 用户启动 `start-dev.bat`
2. 系统启动，但没有演示数据
3. 用户需要手动执行 SQL 脚本或使用 psql 命令
4. 步骤繁琐，容易出错

### 改进后
1. 用户启动 `start-dev.bat`
2. 系统自动启动并导入演示数据
3. 用户可以直接登录查看演示数据
4. 一键完成，零配置

### 灵活性
- 如果用户不需要演示数据，SQL 脚本会自动跳过
- 如果用户需要重新导入，可以单独执行 `init-demo-data.bat`
- 如果用户需要清理数据，可以参考文档中的清理步骤

## 测试验证

### 测试场景

#### 场景 1：首次启动（数据库为空）
- ✅ 启动 `start-dev.bat`
- ✅ 后端创建表结构和默认数据
- ✅ 自动导入演示数据
- ✅ 前端可以查看演示数据

#### 场景 2：再次启动（已有数据）
- ✅ 启动 `start-dev.bat`
- ✅ SQL 脚本检测到已有数据
- ✅ 跳过导入，不重复创建
- ✅ 系统正常运行

#### 场景 3：单独初始化演示数据
- ✅ 执行 `init-demo-data.bat`
- ✅ 检查数据库连接
- ✅ 导入演示数据
- ✅ 显示成功消息

#### 场景 4：数据库未启动
- ✅ 执行 `init-demo-data.bat`
- ✅ 检测到数据库未运行
- ✅ 显示错误消息并退出
- ✅ 提示用户先启动数据库

## 文件清单

### 新增文件
1. `init-demo-data.bat` - Windows 演示数据初始化脚本
2. `init-demo-data.sh` - Linux/Mac 演示数据初始化脚本

### 修改文件
1. `start-dev.bat` - 添加自动导入演示数据步骤
2. `README.md` - 更新快速启动和演示数据说明
3. `docs/02_Database/README_DEMO_DATA.md` - 添加快速开始章节
4. `CLAUDE.md` - 添加常用脚本说明（已自动更新）

### 保持不变
1. `docs/02_Database/seed_demo_data.sql` - SQL 脚本本身不变
2. 后端代码 - 不再包含硬编码演示数据
3. 前端代码 - 无需修改

## 最佳实践

### 开发环境
- ✅ 使用 `start-dev.bat` 一键启动
- ✅ 自动导入演示数据
- ✅ 快速开始开发

### 测试环境
- ✅ 可以使用相同的脚本
- ✅ 或创建专门的测试数据脚本
- ✅ 根据测试需求定制数据

### 生产环境
- ✅ 不执行演示数据脚本
- ✅ 只创建系统必需数据（管理员、分类）
- ✅ 业务数据通过系统界面或正式导入流程创建

## 后续优化建议

1. **环境变量控制**：添加 `INIT_DEMO_DATA=true/false` 环境变量，让用户可以选择是否导入演示数据

2. **数据集选择**：创建多个演示数据脚本（minimal、standard、full），让用户选择导入的数据量

3. **进度显示**：在导入过程中显示进度条或详细日志

4. **错误恢复**：如果导入失败，提供自动回滚或重试机制

5. **数据验证**：导入后自动验证数据完整性和一致性

## 总结

本次改进实现了：
- ✅ 开发环境启动时自动初始化演示数据
- ✅ 提供独立的初始化脚本供手动执行
- ✅ 保持 SQL 脚本的幂等性和安全性
- ✅ 更新所有相关文档
- ✅ 提升用户体验，减少手动操作

用户现在只需执行 `start-dev.bat`，即可获得一个包含演示数据的完整开发环境，无需任何额外配置。
