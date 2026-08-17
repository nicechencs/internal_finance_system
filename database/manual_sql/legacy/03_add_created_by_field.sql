-- =============================================
-- 添加 created_by 字段（数据权限功能）
-- =============================================

-- 为所有业务表添加 created_by 字段
ALTER TABLE accounts ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE categories ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE customers ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE persons ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE projects ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE import_batches ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE bank_transactions ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE transactions ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE transaction_allocations ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE receivables ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE receivable_details ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE payables ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE payable_details ADD COLUMN IF NOT EXISTS created_by BIGINT;
ALTER TABLE classification_rules ADD COLUMN IF NOT EXISTS created_by BIGINT;

-- 添加外键约束
ALTER TABLE accounts ADD CONSTRAINT fk_accounts_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE categories ADD CONSTRAINT fk_categories_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE customers ADD CONSTRAINT fk_customers_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE suppliers ADD CONSTRAINT fk_suppliers_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE persons ADD CONSTRAINT fk_persons_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE projects ADD CONSTRAINT fk_projects_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE import_batches ADD CONSTRAINT fk_import_batches_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE bank_transactions ADD CONSTRAINT fk_bank_transactions_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE transactions ADD CONSTRAINT fk_transactions_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE transaction_allocations ADD CONSTRAINT fk_transaction_allocations_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE receivables ADD CONSTRAINT fk_receivables_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE receivable_details ADD CONSTRAINT fk_receivable_details_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE payables ADD CONSTRAINT fk_payables_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE payable_details ADD CONSTRAINT fk_payable_details_created_by FOREIGN KEY (created_by) REFERENCES users(id);
ALTER TABLE classification_rules ADD CONSTRAINT fk_classification_rules_created_by FOREIGN KEY (created_by) REFERENCES users(id);

-- 添加索引
CREATE INDEX IF NOT EXISTS idx_accounts_created_by ON accounts(created_by);
CREATE INDEX IF NOT EXISTS idx_categories_created_by ON categories(created_by);
CREATE INDEX IF NOT EXISTS idx_customers_created_by ON customers(created_by);
CREATE INDEX IF NOT EXISTS idx_suppliers_created_by ON suppliers(created_by);
CREATE INDEX IF NOT EXISTS idx_persons_created_by ON persons(created_by);
CREATE INDEX IF NOT EXISTS idx_projects_created_by ON projects(created_by);
CREATE INDEX IF NOT EXISTS idx_import_batches_created_by ON import_batches(created_by);
CREATE INDEX IF NOT EXISTS idx_bank_transactions_created_by ON bank_transactions(created_by);
CREATE INDEX IF NOT EXISTS idx_transactions_created_by ON transactions(created_by);
CREATE INDEX IF NOT EXISTS idx_receivables_created_by ON receivables(created_by);
CREATE INDEX IF NOT EXISTS idx_payables_created_by ON payables(created_by);
CREATE INDEX IF NOT EXISTS idx_classification_rules_created_by ON classification_rules(created_by);
