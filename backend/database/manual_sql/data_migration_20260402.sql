-- =====================================================
-- 财务结算优化 - 数据迁移脚本
-- 创建日期: 2026-04-02
-- 说明: 为现有数据计算分配状态，创建虚拟交易等
-- =====================================================

-- 阶段 1: 为现有交易计算分配状态
-- =====================================================

DO $$
BEGIN
    RAISE NOTICE '开始计算交易分配状态...';

    UPDATE transactions t
    SET allocation_status = CASE
        -- 完全分配：已绑定金额 >= 交易金额
        WHEN (
            SELECT COALESCE(SUM(rd.amount), 0)
            FROM receivable_details rd
            WHERE rd.transaction_id = t.id AND rd.deleted_at IS NULL
        ) + (
            SELECT COALESCE(SUM(pd.amount), 0)
            FROM payable_details pd
            WHERE pd.transaction_id = t.id AND pd.deleted_at IS NULL
        ) >= t.amount THEN 'fullyallocated'

        -- 部分分配：已绑定金额 > 0 但 < 交易金额
        WHEN (
            SELECT COALESCE(SUM(rd.amount), 0)
            FROM receivable_details rd
            WHERE rd.transaction_id = t.id AND rd.deleted_at IS NULL
        ) + (
            SELECT COALESCE(SUM(pd.amount), 0)
            FROM payable_details pd
            WHERE pd.transaction_id = t.id AND pd.deleted_at IS NULL
        ) > 0 THEN 'partiallyallocated'

        -- 未分配
        ELSE 'unallocated'
    END
    WHERE t.deleted_at IS NULL;

    RAISE NOTICE '交易分配状态计算完成';
END $$;


-- 阶段 2: 为未关联交易的收款记录创建虚拟交易
-- =====================================================

DO $$
DECLARE
    default_account_id BIGINT;
    created_count INT := 0;
BEGIN
    RAISE NOTICE '开始为未关联交易的收款记录创建虚拟交易...';

    -- 获取默认账户ID（假设有一个默认账户）
    SELECT id INTO default_account_id
    FROM accounts
    WHERE is_deleted = false
    ORDER BY id
    LIMIT 1;

    IF default_account_id IS NULL THEN
        RAISE EXCEPTION '未找到可用账户，无法创建虚拟交易';
    END IF;

    -- 为未关联交易的应收款明细创建虚拟交易
    WITH new_transactions AS (
        INSERT INTO transactions (
            transaction_date,
            amount,
            transaction_type,
            transfer_direction,
            account_id,
            description,
            status,
            is_allocated,
            allocation_status,
            created_at,
            updated_at,
            is_deleted
        )
        SELECT
            rd.payment_date,
            rd.amount,
            'income',
            'in',
            default_account_id,
            CONCAT('历史收款补录 - ', COALESCE(r.description, '应收款ID: ' || r.id)),
            'confirmed',
            true,
            'fullyallocated',
            NOW(),
            NOW(),
            false
        FROM receivable_details rd
        JOIN receivables r ON rd.receivable_id = r.id
        WHERE rd.transaction_id IS NULL
          AND rd.deleted_at IS NULL
          AND r.deleted_at IS NULL
        RETURNING id, description
    )
    SELECT COUNT(*) INTO created_count FROM new_transactions;

    RAISE NOTICE '为应收款创建了 % 笔虚拟交易', created_count;

    -- 更新 receivable_details 关联到新创建的交易
    -- 注意：这里需要根据金额和日期匹配，可能不够精确
    -- 建议手动审核或使用更精确的匹配逻辑
    UPDATE receivable_details rd
    SET transaction_id = t.id
    FROM transactions t
    WHERE rd.transaction_id IS NULL
      AND rd.deleted_at IS NULL
      AND t.description LIKE '%历史收款补录%'
      AND t.transaction_date = rd.payment_date
      AND t.amount = rd.amount
      AND t.transaction_type = 'income';

    RAISE NOTICE '应收款明细关联完成';
END $$;


-- 阶段 3: 为未关联交易的付款记录创建虚拟交易
-- =====================================================

DO $$
DECLARE
    default_account_id BIGINT;
    created_count INT := 0;
BEGIN
    RAISE NOTICE '开始为未关联交易的付款记录创建虚拟交易...';

    SELECT id INTO default_account_id
    FROM accounts
    WHERE is_deleted = false
    ORDER BY id
    LIMIT 1;

    IF default_account_id IS NULL THEN
        RAISE EXCEPTION '未找到可用账户，无法创建虚拟交易';
    END IF;

    -- 为未关联交易的应付款明细创建虚拟交易
    WITH new_transactions AS (
        INSERT INTO transactions (
            transaction_date,
            amount,
            transaction_type,
            transfer_direction,
            account_id,
            description,
            status,
            is_allocated,
            allocation_status,
            created_at,
            updated_at,
            is_deleted
        )
        SELECT
            pd.payment_date,
            pd.amount,
            'expense',
            'out',
            default_account_id,
            CONCAT('历史付款补录 - ', COALESCE(p.description, '应付款ID: ' || p.id)),
            'confirmed',
            true,
            'fullyallocated',
            NOW(),
            NOW(),
            false
        FROM payable_details pd
        JOIN payables p ON pd.payable_id = p.id
        WHERE pd.transaction_id IS NULL
          AND pd.deleted_at IS NULL
          AND p.deleted_at IS NULL
        RETURNING id, description
    )
    SELECT COUNT(*) INTO created_count FROM new_transactions;

    RAISE NOTICE '为应付款创建了 % 笔虚拟交易', created_count;

    -- 更新 payable_details 关联到新创建的交易
    UPDATE payable_details pd
    SET transaction_id = t.id
    FROM transactions t
    WHERE pd.transaction_id IS NULL
      AND pd.deleted_at IS NULL
      AND t.description LIKE '%历史付款补录%'
      AND t.transaction_date = pd.payment_date
      AND t.amount = pd.amount
      AND t.transaction_type = 'expense';

    RAISE NOTICE '应付款明细关联完成';
END $$;


-- 阶段 4: 数据一致性检查
-- =====================================================

DO $$
DECLARE
    inconsistent_receivables INT;
    inconsistent_payables INT;
    unlinked_receivable_details INT;
    unlinked_payable_details INT;
BEGIN
    RAISE NOTICE '开始数据一致性检查...';

    -- 检查应收款金额不一致
    SELECT COUNT(*) INTO inconsistent_receivables
    FROM receivables r
    LEFT JOIN (
        SELECT receivable_id, SUM(amount) as total
        FROM receivable_details
        WHERE deleted_at IS NULL
        GROUP BY receivable_id
    ) rd ON r.id = rd.receivable_id
    WHERE r.deleted_at IS NULL
      AND r.received_amount != COALESCE(rd.total, 0);

    IF inconsistent_receivables > 0 THEN
        RAISE WARNING '发现 % 条应收款金额不一致', inconsistent_receivables;
    END IF;

    -- 检查应付款金额不一致
    SELECT COUNT(*) INTO inconsistent_payables
    FROM payables p
    LEFT JOIN (
        SELECT payable_id, SUM(amount) as total
        FROM payable_details
        WHERE deleted_at IS NULL
        GROUP BY payable_id
    ) pd ON p.id = pd.payable_id
    WHERE p.deleted_at IS NULL
      AND p.paid_amount != COALESCE(pd.total, 0);

    IF inconsistent_payables > 0 THEN
        RAISE WARNING '发现 % 条应付款金额不一致', inconsistent_payables;
    END IF;

    -- 检查仍未关联交易的明细
    SELECT COUNT(*) INTO unlinked_receivable_details
    FROM receivable_details
    WHERE transaction_id IS NULL AND deleted_at IS NULL;

    IF unlinked_receivable_details > 0 THEN
        RAISE WARNING '仍有 % 条应收款明细未关联交易', unlinked_receivable_details;
    END IF;

    SELECT COUNT(*) INTO unlinked_payable_details
    FROM payable_details
    WHERE transaction_id IS NULL AND deleted_at IS NULL;

    IF unlinked_payable_details > 0 THEN
        RAISE WARNING '仍有 % 条应付款明细未关联交易', unlinked_payable_details;
    END IF;

    RAISE NOTICE '数据一致性检查完成';
END $$;


-- 阶段 5: 生成迁移报告
-- =====================================================

DO $$
DECLARE
    total_transactions INT;
    unallocated_count INT;
    partially_allocated_count INT;
    fully_allocated_count INT;
    virtual_transactions_count INT;
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE '数据迁移完成报告';
    RAISE NOTICE '========================================';

    SELECT COUNT(*) INTO total_transactions
    FROM transactions WHERE deleted_at IS NULL;

    SELECT COUNT(*) INTO unallocated_count
    FROM transactions
    WHERE allocation_status = 'unallocated' AND deleted_at IS NULL;

    SELECT COUNT(*) INTO partially_allocated_count
    FROM transactions
    WHERE allocation_status = 'partiallyallocated' AND deleted_at IS NULL;

    SELECT COUNT(*) INTO fully_allocated_count
    FROM transactions
    WHERE allocation_status = 'fullyallocated' AND deleted_at IS NULL;

    SELECT COUNT(*) INTO virtual_transactions_count
    FROM transactions
    WHERE description LIKE '%历史%补录%' AND deleted_at IS NULL;

    RAISE NOTICE '总交易数: %', total_transactions;
    RAISE NOTICE '未分配: %', unallocated_count;
    RAISE NOTICE '部分分配: %', partially_allocated_count;
    RAISE NOTICE '完全分配: %', fully_allocated_count;
    RAISE NOTICE '虚拟交易数: %', virtual_transactions_count;
    RAISE NOTICE '========================================';
END $$;
