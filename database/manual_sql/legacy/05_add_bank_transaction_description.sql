-- 为银行流水表添加 description 字段（分离"交易描述"和"摘要"）
-- 之前两个字段被合并到 memo 中，现在分开保存

ALTER TABLE bank_transactions ADD COLUMN IF NOT EXISTS description TEXT;

COMMENT ON COLUMN bank_transactions.description IS '交易描述（华夏银行col11）';
COMMENT ON COLUMN bank_transactions.memo IS '摘要/备注（华夏银行col12，支付宝扩展信息JSON）';
