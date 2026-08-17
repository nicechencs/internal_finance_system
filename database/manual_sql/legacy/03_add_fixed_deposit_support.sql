-- =============================================
-- 定期存款管理 - 数据库迁移脚本
-- 日期: 2026-03-14
-- 描述: 为账户表添加定期存款相关字段
-- =============================================

-- 1. 添加定期存款相关字段
ALTER TABLE accounts ADD COLUMN interest_start_date TIMESTAMP;
ALTER TABLE accounts ADD COLUMN maturity_date TIMESTAMP;
ALTER TABLE accounts ADD COLUMN interest_rate DECIMAL(8, 4);
ALTER TABLE accounts ADD COLUMN auto_renewal BOOLEAN NOT NULL DEFAULT false;

-- 2. 添加到期日索引（仅定期存款类型且未删除）
CREATE INDEX idx_accounts_maturity_date ON accounts(maturity_date)
    WHERE is_deleted = false AND account_type = 'fixeddeposit';

-- 3. 更新注释
COMMENT ON COLUMN accounts.account_type IS '账户类型：bank-银行账户, alipay-支付宝账户, fixeddeposit-定期存款';
COMMENT ON COLUMN accounts.interest_start_date IS '起息日（仅定期存款）';
COMMENT ON COLUMN accounts.maturity_date IS '到期日（仅定期存款）';
COMMENT ON COLUMN accounts.interest_rate IS '年利率%（仅定期存款），如 3.5 表示 3.5%';
COMMENT ON COLUMN accounts.auto_renewal IS '是否自动续存（仅定期存款）';
