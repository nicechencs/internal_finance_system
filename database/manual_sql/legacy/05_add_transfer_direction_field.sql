-- 添加 transfer_direction 字段以支持转账方向标识
-- 用于标识转账交易的方向：None-非转账, Out-转出, In-转入

-- 1. 添加 transfer_direction 字段
ALTER TABLE transactions ADD COLUMN transfer_direction VARCHAR(10) NOT NULL DEFAULT 'none';

-- 2. 添加检查约束
ALTER TABLE transactions ADD CONSTRAINT chk_transactions_transfer_direction
    CHECK (transfer_direction IN ('none', 'out', 'in'));

-- 3. 更新注释
COMMENT ON COLUMN transactions.transfer_direction IS '转账方向：none-非转账, out-转出, in-转入';
