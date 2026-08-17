-- ============================================================
-- TransactionId 必填迁移 - 生产数据诊断脚本（只读）
-- 目的：摸清 receivable_details / payable_details 中
--       transaction_id 为 NULL 的存量数据规模和分布
-- ============================================================

-- 1. 总量概览
SELECT
    'receivable_details' AS table_name,
    COUNT(*) AS total_rows,
    COUNT(*) FILTER (WHERE transaction_id IS NULL) AS null_tx_rows,
    COUNT(*) FILTER (WHERE transaction_id IS NULL AND is_deleted = false) AS null_tx_active_rows
FROM receivable_details
UNION ALL
SELECT
    'payable_details',
    COUNT(*),
    COUNT(*) FILTER (WHERE transaction_id IS NULL),
    COUNT(*) FILTER (WHERE transaction_id IS NULL AND is_deleted = false)
FROM payable_details;

-- 2. 按月分布（活跃记录，未软删除）
SELECT '== receivable_details NULL transaction_id 按月分布 ==' AS info;
SELECT
    TO_CHAR(DATE_TRUNC('month', created_at), 'YYYY-MM') AS month,
    COUNT(*) AS count,
    SUM(amount) AS total_amount
FROM receivable_details
WHERE transaction_id IS NULL AND is_deleted = false
GROUP BY 1 ORDER BY 1;

SELECT '== payable_details NULL transaction_id 按月分布 ==' AS info;
SELECT
    TO_CHAR(DATE_TRUNC('month', created_at), 'YYYY-MM') AS month,
    COUNT(*) AS count,
    SUM(amount) AS total_amount
FROM payable_details
WHERE transaction_id IS NULL AND is_deleted = false
GROUP BY 1 ORDER BY 1;

-- 3. 关联的父记录详情（帮助判断能否人工补录）
SELECT '== receivable_details 无交易关联的活跃明细 ==' AS info;
SELECT
    rd.id AS detail_id,
    rd.receivable_id,
    rd.amount,
    rd.payment_date,
    rd.payment_method,
    rd.description AS detail_desc,
    r.project_id,
    r.total_amount AS receivable_total,
    r.received_amount,
    r.status AS receivable_status
FROM receivable_details rd
JOIN receivables r ON r.id = rd.receivable_id
WHERE rd.transaction_id IS NULL AND rd.is_deleted = false
ORDER BY rd.created_at DESC
LIMIT 50;

SELECT '== payable_details 无交易关联的活跃明细 ==' AS info;
SELECT
    pd.id AS detail_id,
    pd.payable_id,
    pd.amount,
    pd.payment_date,
    pd.payment_method,
    pd.description AS detail_desc,
    p.project_id,
    p.total_amount AS payable_total,
    p.paid_amount,
    p.status AS payable_status
FROM payable_details pd
JOIN payables p ON p.id = pd.payable_id
WHERE pd.transaction_id IS NULL AND pd.is_deleted = false
ORDER BY pd.created_at DESC
LIMIT 50;

-- 4. 检查是否有"已结清但明细缺交易"的父记录（影响金额一致性）
SELECT '== 已结清但有无交易明细的应收款 ==' AS info;
SELECT
    r.id, r.total_amount, r.received_amount, r.status,
    COUNT(rd.id) AS null_tx_detail_count,
    SUM(rd.amount) AS null_tx_detail_amount
FROM receivables r
JOIN receivable_details rd ON rd.receivable_id = r.id AND rd.is_deleted = false AND rd.transaction_id IS NULL
WHERE r.is_deleted = false AND r.status = 'settled'
GROUP BY r.id, r.total_amount, r.received_amount, r.status;

SELECT '== 已结清但有无交易明细的应付款 ==' AS info;
SELECT
    p.id, p.total_amount, p.paid_amount, p.status,
    COUNT(pd.id) AS null_tx_detail_count,
    SUM(pd.amount) AS null_tx_detail_amount
FROM payables p
JOIN payable_details pd ON pd.payable_id = p.id AND pd.is_deleted = false AND pd.transaction_id IS NULL
WHERE p.is_deleted = false AND p.status = 'settled'
GROUP BY p.id, p.total_amount, p.paid_amount, p.status;
