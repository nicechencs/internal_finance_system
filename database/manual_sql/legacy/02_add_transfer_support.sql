-- 账户转账功能：新增 related_transaction_id 字段
-- 用于关联转账交易的配对记录

-- 1. 添加 related_transaction_id 字段
ALTER TABLE transactions ADD COLUMN related_transaction_id BIGINT;

-- 2. 添加外键约束
ALTER TABLE transactions ADD CONSTRAINT fk_transactions_related_transaction
    FOREIGN KEY (related_transaction_id) REFERENCES transactions(id);

-- 3. 添加索引
CREATE INDEX idx_transactions_related ON transactions(related_transaction_id) WHERE is_deleted = false;

-- 4. 更新注释
COMMENT ON COLUMN transactions.transaction_type IS '交易类型：income-收入, expense-支出, transfer-转账';
COMMENT ON COLUMN transactions.related_transaction_id IS '关联交易ID（转账时关联配对记录）';
