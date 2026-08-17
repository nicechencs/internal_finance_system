-- =====================================================
-- 数据权限控制支持 - 数据库迁移脚本
-- 版本: v1.0
-- 创建日期: 2026-03-14
-- 说明: 添加 CreatedBy 字段和相关索引，支持数据权限控制
-- =====================================================

-- 1. 修改 users 表：将 role 字段从 VARCHAR 改为枚举类型
-- 注意：PostgreSQL 需要先创建枚举类型
DO $$
BEGIN
    -- 创建 user_role 枚举类型（如果不存在）
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'user_role') THEN
        CREATE TYPE user_role AS ENUM ('admin', 'accountant', 'viewer');
    END IF;
END $$;

-- 修改 users 表的 role 字段类型
ALTER TABLE users
    ALTER COLUMN role TYPE user_role USING role::user_role;

-- 2. 为所有业务表添加 created_by 字段
-- 注意：默认值设为 1（假设 ID=1 是管理员用户）

-- 账户表
ALTER TABLE accounts
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 分类表
ALTER TABLE categories
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 分类规则表
ALTER TABLE classification_rules
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 客户表
ALTER TABLE customers
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 供应商表
ALTER TABLE suppliers
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 人员表
ALTER TABLE persons
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 项目表
ALTER TABLE projects
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 应收表
ALTER TABLE receivables
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 应收明细表
ALTER TABLE receivable_details
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 应付表
ALTER TABLE payables
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 应付明细表
ALTER TABLE payable_details
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 银行流水表
ALTER TABLE bank_transactions
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 交易分摊表
ALTER TABLE transaction_allocations
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 系统配置表
ALTER TABLE system_configs
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 审计日志表
ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 导入批次表
ALTER TABLE import_batches
    ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 3. 更新现有数据：将所有记录的 created_by 设置为管理员用户 ID（假设为 1）
-- 注意：如果系统中还没有用户，这一步可以跳过

UPDATE accounts SET created_by = 1 WHERE created_by IS NULL;
UPDATE categories SET created_by = 1 WHERE created_by IS NULL;
UPDATE classification_rules SET created_by = 1 WHERE created_by IS NULL;
UPDATE customers SET created_by = 1 WHERE created_by IS NULL;
UPDATE suppliers SET created_by = 1 WHERE created_by IS NULL;
UPDATE persons SET created_by = 1 WHERE created_by IS NULL;
UPDATE projects SET created_by = 1 WHERE created_by IS NULL;
UPDATE receivables SET created_by = 1 WHERE created_by IS NULL;
UPDATE receivable_details SET created_by = 1 WHERE created_by IS NULL;
UPDATE payables SET created_by = 1 WHERE created_by IS NULL;
UPDATE payable_details SET created_by = 1 WHERE created_by IS NULL;
UPDATE bank_transactions SET created_by = 1 WHERE created_by IS NULL;
UPDATE transaction_allocations SET created_by = 1 WHERE created_by IS NULL;
UPDATE system_configs SET created_by = 1 WHERE created_by IS NULL;
UPDATE audit_logs SET created_by = 1 WHERE created_by IS NULL;
UPDATE import_batches SET created_by = 1 WHERE created_by IS NULL;

-- 4. 添加外键约束
-- 注意：确保 users 表中存在 ID=1 的用户，否则外键约束会失败

ALTER TABLE accounts
    ADD CONSTRAINT fk_accounts_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE categories
    ADD CONSTRAINT fk_categories_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE classification_rules
    ADD CONSTRAINT fk_classification_rules_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE customers
    ADD CONSTRAINT fk_customers_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE suppliers
    ADD CONSTRAINT fk_suppliers_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE persons
    ADD CONSTRAINT fk_persons_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE projects
    ADD CONSTRAINT fk_projects_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE receivables
    ADD CONSTRAINT fk_receivables_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE receivable_details
    ADD CONSTRAINT fk_receivable_details_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE payables
    ADD CONSTRAINT fk_payables_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE payable_details
    ADD CONSTRAINT fk_payable_details_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE bank_transactions
    ADD CONSTRAINT fk_bank_transactions_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE transaction_allocations
    ADD CONSTRAINT fk_transaction_allocations_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE system_configs
    ADD CONSTRAINT fk_system_configs_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE audit_logs
    ADD CONSTRAINT fk_audit_logs_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

ALTER TABLE import_batches
    ADD CONSTRAINT fk_import_batches_created_by
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;

-- 5. 创建索引以提高查询性能
-- 注意：使用 WHERE is_deleted = false 过滤已删除记录

CREATE INDEX IF NOT EXISTS idx_accounts_created_by
    ON accounts(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_categories_created_by
    ON categories(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_classification_rules_created_by
    ON classification_rules(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_customers_created_by
    ON customers(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_suppliers_created_by
    ON suppliers(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_persons_created_by
    ON persons(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_projects_created_by
    ON projects(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_receivables_created_by
    ON receivables(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_receivable_details_created_by
    ON receivable_details(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_payables_created_by
    ON payables(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_payable_details_created_by
    ON payable_details(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_bank_transactions_created_by
    ON bank_transactions(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_transaction_allocations_created_by
    ON transaction_allocations(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_system_configs_created_by
    ON system_configs(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_audit_logs_created_by
    ON audit_logs(created_by) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_import_batches_created_by
    ON import_batches(created_by) WHERE is_deleted = false;

-- 6. 验证迁移结果
-- 检查所有表是否都有 created_by 字段
SELECT
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name = 'created_by'
ORDER BY table_name;

-- 检查外键约束是否创建成功
SELECT
    tc.table_name,
    tc.constraint_name,
    tc.constraint_type,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND kcu.column_name = 'created_by'
ORDER BY tc.table_name;

-- 检查索引是否创建成功
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE indexname LIKE '%created_by%'
ORDER BY tablename;

-- =====================================================
-- 迁移完成
-- =====================================================
