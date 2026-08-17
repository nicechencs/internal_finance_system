# 使用外部数据库部署指南

## 概述

本指南说明如何使用已有的 PostgreSQL 数据库（而不是在 docker-compose 中创建新数据库）部署应用。

---

## 前提条件

### 1. 已有 PostgreSQL 数据库

确保你的 PostgreSQL 数据库：
- **版本**：PostgreSQL 12+ （推荐 14+）
- **状态**：正在运行
- **网络**：应用容器可以访问

### 2. 数据库连接信息

准备以下信息：
- 主机地址（Host）
- 端口（Port，默认 5432）
- 数据库名称（Database）
- 用户名（Username）
- 密码（Password）

---

## 配置步骤

### 第一步：配置环境变量

编辑 `.env.production` 文件（从 `.env.production.example` 复制）：

```bash
# ============================================
# 数据库配置（外部数据库）
# ============================================

# 数据库主机地址
DB_HOST=your-database-host

# 数据库端口
DB_PORT=5432

# 数据库名称
DB_NAME=finance

# 数据库用户名
DB_USER=postgres

# 数据库密码
DB_PASSWORD=your-database-password
```

### 第二步：确定数据库主机地址

根据你的数据库位置，选择正确的 `DB_HOST`：

#### 场景 A：数据库在同一服务器（非 Docker）

```bash
DB_HOST=localhost
```

**注意**：Docker 容器内的 `localhost` 指向容器本身，需要使用特殊地址：

```bash
# Linux/Mac
DB_HOST=host.docker.internal

# 或使用服务器的实际 IP
DB_HOST=192.168.1.100
```

#### 场景 B：数据库在 Docker 容器中

**方案 1：使用容器名称**（推荐）

```bash
# 假设你的数据库容器名为 my_postgres_db
DB_HOST=my_postgres_db
```

**前提**：两个容器必须在同一 Docker 网络中。

检查数据库容器的网络：
```bash
docker inspect my_postgres_db | grep NetworkMode
```

将应用容器加入同一网络：
```yaml
# docker-compose.prod.yml
services:
  api:
    networks:
      - your_database_network  # 数据库所在的网络

networks:
  your_database_network:
    external: true  # 使用外部网络
```

**方案 2：使用容器 IP**

```bash
# 查看数据库容器 IP
docker inspect my_postgres_db | grep IPAddress

# 使用该 IP
DB_HOST=172.17.0.2
```

**方案 3：使用 host 网络模式**

修改 `docker-compose.prod.yml`：
```yaml
services:
  api:
    network_mode: "host"
    # 移除 ports 和 networks 配置
```

然后使用：
```bash
DB_HOST=localhost
```

#### 场景 C：数据库在远程服务器

```bash
# 使用 IP 地址
DB_HOST=192.168.1.50

# 或使用域名
DB_HOST=db.example.com
```

**注意**：确保防火墙允许连接。

#### 场景 D：云数据库（AWS RDS/阿里云等）

```bash
# 使用云数据库提供的连接地址
DB_HOST=mydb.abc123.us-east-1.rds.amazonaws.com
DB_PORT=5432
DB_USER=admin
DB_PASSWORD=your-cloud-db-password
```

---

## 数据库初始化

### 1. 创建数据库（如果不存在）

连接到 PostgreSQL：
```bash
# 方法 1：使用 psql
psql -h your-host -U postgres

# 方法 2：使用 Docker 容器
docker exec -it my_postgres_db psql -U postgres
```

创建数据库：
```sql
CREATE DATABASE finance;
```

### 2. 执行初始化脚本

```bash
# 方法 1：直接执行
psql -h your-host -U postgres -d finance -f docs/02_Database/01_database_schema.sql

# 方法 2：通过 Docker 容器
docker exec -i my_postgres_db psql -U postgres -d finance < docs/02_Database/01_database_schema.sql
```

### 3. 导入演示数据（可选）

```bash
psql -h your-host -U postgres -d finance -f docs/02_Database/seed_demo_data.sql
```

---

## 部署应用

### 使用 Docker Compose

```bash
# 1. 确保 .env.production 配置正确
cat .env.production

# 2. 启动应用（不包含数据库）
docker compose -f docker-compose.prod.yml up -d

# 3. 查看日志
docker compose -f docker-compose.prod.yml logs -f api

# 4. 检查健康状态
curl http://localhost:5000/health
```

### 使用 Dokploy

1. 在 Dokploy 中创建应用
2. 选择 **Docker Compose** 方式
3. 在 **Environment Variables** 中添加：
   ```
   DB_HOST=your-database-host
   DB_PORT=5432
   DB_NAME=finance
   DB_USER=postgres
   DB_PASSWORD=your-password
   ```
4. 点击 **Deploy**

---

## 验证连接

### 1. 检查 API 健康状态

```bash
curl http://localhost:5000/health
```

期望输出：
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "duration": "15ms"
    }
  ]
}
```

### 2. 查看 API 日志

```bash
docker logs finance_api
```

如果连接失败，会看到类似错误：
```
Failed to connect to database: could not connect to server
```

### 3. 测试数据库连接

从 API 容器内测试：
```bash
# 进入容器
docker exec -it finance_api bash

# 安装 psql（如果没有）
apt-get update && apt-get install -y postgresql-client

# 测试连接
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME
```

---

## 常见问题

### Q1: 连接超时 (Connection timeout)

**原因**：
- 数据库主机地址错误
- 防火墙阻止连接
- 数据库未启动

**解决**：
```bash
# 1. 检查数据库是否运行
docker ps | grep postgres
# 或
systemctl status postgresql

# 2. 测试网络连通性
ping your-database-host
telnet your-database-host 5432

# 3. 检查防火墙
sudo ufw status
sudo ufw allow 5432/tcp
```

### Q2: 认证失败 (Authentication failed)

**原因**：
- 用户名或密码错误
- 数据库不允许远程连接

**解决**：
```bash
# 1. 验证密码
psql -h your-host -U postgres -d finance

# 2. 检查 PostgreSQL 配置
# 编辑 postgresql.conf
listen_addresses = '*'  # 允许所有 IP 连接

# 编辑 pg_hba.conf，添加：
host    all             all             0.0.0.0/0               md5

# 3. 重启 PostgreSQL
sudo systemctl restart postgresql
# 或
docker restart my_postgres_db
```

### Q3: 数据库不存在 (Database does not exist)

**原因**：
- 数据库名称错误
- 数据库未创建

**解决**：
```bash
# 创建数据库
psql -h your-host -U postgres -c "CREATE DATABASE finance;"
```

### Q4: Docker 容器无法访问 localhost

**原因**：
- 容器内的 `localhost` 指向容器本身，不是宿主机

**解决**：
```bash
# 使用特殊地址
DB_HOST=host.docker.internal  # Docker Desktop (Mac/Windows)
DB_HOST=172.17.0.1            # Linux (Docker 默认网关)

# 或使用宿主机 IP
ip addr show  # 查看 IP
DB_HOST=192.168.1.100
```

### Q5: 容器间无法通信

**原因**：
- 容器不在同一网络

**解决**：
```bash
# 1. 查看数据库容器网络
docker inspect my_postgres_db | grep NetworkMode

# 2. 将应用容器加入同一网络
docker network connect your_network finance_api

# 3. 或在 docker-compose.prod.yml 中配置
networks:
  your_network:
    external: true
```

---

## 备份和恢复

### 备份数据库

由于数据库在外部，需要修改备份脚本：

编辑 `scripts/backup-database.sh`：
```bash
# 原来（容器内备份）
docker exec finance_db pg_dump -U postgres finance > "$BACKUP_FILE"

# 修改为（外部数据库备份）
pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME > "$BACKUP_FILE"
# 或
docker exec my_postgres_db pg_dump -U postgres finance > "$BACKUP_FILE"
```

### 恢复数据库

```bash
# 方法 1：直接恢复
psql -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME < backup.sql

# 方法 2：通过容器恢复
docker exec -i my_postgres_db psql -U postgres finance < backup.sql
```

---

## 性能优化

### 1. 连接池配置

在连接字符串中添加连接池参数：
```bash
ConnectionStrings__DefaultConnection="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Pooling=true;MinPoolSize=5;MaxPoolSize=100"
```

### 2. 数据库索引

确保关键字段已建立索引（初始化脚本已包含）。

### 3. 网络延迟

如果数据库在远程服务器，考虑：
- 使用同一数据中心
- 启用数据库连接压缩
- 优化查询减少往返次数

---

## 安全建议

### 1. 限制数据库访问

```sql
-- 创建专用用户（而不是使用 postgres 超级用户）
CREATE USER finance_app WITH PASSWORD 'strong-password';
GRANT ALL PRIVILEGES ON DATABASE finance TO finance_app;
```

更新 `.env.production`：
```bash
DB_USER=finance_app
DB_PASSWORD=strong-password
```

### 2. 使用 SSL 连接

```bash
ConnectionStrings__DefaultConnection="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};SslMode=Require"
```

### 3. 防火墙规则

仅允许应用服务器访问数据库：
```bash
# 在数据库服务器上
sudo ufw allow from 192.168.1.100 to any port 5432
```

---

## 对比：内置 vs 外部数据库

| 特性 | 内置数据库 | 外部数据库 |
|------|-----------|-----------|
| **部署复杂度** | 简单 | 中等 |
| **数据持久化** | Docker Volume | 独立管理 |
| **备份** | 容器内备份 | 需要调整脚本 |
| **性能** | 同主机，低延迟 | 取决于网络 |
| **扩展性** | 受限于单机 | 可独立扩展 |
| **维护** | 统一管理 | 分开维护 |
| **适用场景** | 开发/测试/小型项目 | 生产环境/大型项目 |

---

## 下一步

1. ✅ 配置 `.env.production` 中的数据库连接
2. ✅ 初始化数据库 schema
3. ✅ 部署应用
4. ✅ 验证连接和健康检查
5. ⏭️ 配置定期备份
6. ⏭️ 设置监控告警

---

## 相关文档

- [Dokploy 部署指南](./DOKPLOY_DEPLOYMENT.md)
- [GitHub Actions 部署](./DEPLOYMENT_FIXES.md)
- [部署方式对比](./DEPLOYMENT_COMPARISON.md)

---

**最后更新**：2026-03-18
