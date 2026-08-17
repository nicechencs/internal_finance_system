-- ============================================
-- 演示数据初始化脚本
-- 用途：为开发/测试环境创建虚构演示数据（非真实公司/个人）
-- 注意：生产环境请勿执行此脚本
-- ============================================

-- 使用说明：
-- 1. 确保已执行数据库迁移或表结构创建
-- 2. 确保已创建默认管理员用户（admin）
-- 3. 在 psql 中执行：\i seed_demo_data.sql
-- 4. 或使用：psql -U postgres -d finance_dev -f seed_demo_data.sql

-- 获取 admin 用户 ID（用于 created_by 字段）
DO $$
DECLARE
    v_admin_id BIGINT;
BEGIN
    SELECT id INTO v_admin_id FROM users WHERE username = 'admin' AND NOT is_deleted LIMIT 1;

    IF v_admin_id IS NULL THEN
        RAISE EXCEPTION '未找到 admin 用户，请先执行数据库初始化';
    END IF;

    -- 检查是否已有演示数据
    IF EXISTS (SELECT 1 FROM accounts WHERE NOT is_deleted LIMIT 1) THEN
        RAISE NOTICE '演示数据已存在，跳过创建';
        RETURN;
    END IF;

    RAISE NOTICE '开始创建演示数据，admin_id = %', v_admin_id;

    -- ========== 1. 账户（账号均为虚构演示值） ==========
    INSERT INTO accounts (name, account_type, account_number, bank_name, opening_balance, current_balance, currency, description, is_active, created_by, created_at, updated_at)
    VALUES
        ('工商银行对公账户', 'Bank', '6222020000000000001', '中国工商银行', 500000.00, 500000.00, 'CNY', '演示用主要对公账户', true, v_admin_id, NOW(), NOW()),
        ('招商银行对公账户', 'Bank', '6225880000000000002', '招商银行', 200000.00, 200000.00, 'CNY', '演示用备用对公账户', true, v_admin_id, NOW(), NOW()),
        ('公司支付宝账户', 'Alipay', 'demo-alipay@example.com', NULL, 50000.00, 50000.00, 'CNY', '演示用日常小额支付账户', true, v_admin_id, NOW(), NOW()),
        ('建设银行对公账户', 'Bank', '6217000000000000003', '中国建设银行', 100000.00, 100000.00, 'CNY', '演示用项目账户', true, v_admin_id, NOW(), NOW());

    -- ========== 2. 客户（名称、税号、联系方式均为虚构演示数据） ==========
    INSERT INTO customers (name, short_name, contact_person, contact_phone, contact_email, address, tax_number, description, is_active, created_by, created_at, updated_at)
    VALUES
        ('示例科技有限公司', '示例科技', '张经理', '010-0000-0001', 'alice.demo@example.com', '示例市朝阳区示例路1号', '91110000MA0000001X', '演示客户：软件开发合作', true, v_admin_id, NOW(), NOW()),
        ('示例贸易集团', '示例贸易', '李总', '021-0000-0002', 'bob.demo@example.com', '示例市浦东新区示例大道88号', '91310000MA0000002X', '演示客户：年度框架合作', true, v_admin_id, NOW(), NOW()),
        ('示例创新科技', '示例创新', '王工', '0755-0000-0003', 'carol.demo@example.com', '示例市南山区示例园2号', '91440300MA0000003X', '演示客户：系统集成项目', true, v_admin_id, NOW(), NOW()),
        ('示例制造企业', '示例制造', '陈厂长', '020-0000-0004', 'david.demo@example.com', '示例市天河区示例工业园', '91440100MA0000004X', '演示客户：数字化转型项目', true, v_admin_id, NOW(), NOW()),
        ('示例电商平台', '示例电商', '周总监', '0571-0000-0005', 'erin.demo@example.com', '示例市滨江区示例小镇', '91330100MA0000005X', '演示客户：电商平台对接', true, v_admin_id, NOW(), NOW()),
        ('示例软件公司', '示例软件', '刘经理', '028-0000-0006', 'frank.demo@example.com', '示例市高新区示例软件园', '91510100MA0000006X', '演示客户：软件外包合作', true, v_admin_id, NOW(), NOW());

    -- ========== 3. 供应商（名称、税号、账号均为虚构演示数据） ==========
    INSERT INTO suppliers (name, short_name, contact_person, contact_phone, contact_email, address, tax_number, bank_account, bank_name, description, is_active, created_by, created_at, updated_at)
    VALUES
        ('示例云服务有限公司', '示例云', '客服部', '400-000-0001', 'cloud@example.com', '示例市余杭区示例路100号', '91330000MA0000007X', '6222020000000000101', '中国工商银行示例分行', '演示供应商：云服务器', true, v_admin_id, NOW(), NOW()),
        ('示例网络科技有限公司', '示例网络', '商务部', '400-000-0002', 'network@example.com', '示例市海淀区示例路200号', '91110000MA0000008X', '6225880000000000102', '招商银行示例分行', '演示供应商：CDN 与网络服务', true, v_admin_id, NOW(), NOW()),
        ('示例办公用品有限公司', '示例办公', '销售部', '010-0000-0007', 'office@example.com', '示例市丰台区示例物流园', '91110000MA0000009X', '6217000000000000103', '中国银行示例分行', '演示供应商：办公用品', true, v_admin_id, NOW(), NOW()),
        ('示例外包服务公司', '示例外包', '项目部', '0755-0000-0008', 'outsource@example.com', '示例市福田区示例大厦', '91440300MA0000010X', '6221550000000000104', '平安银行示例分行', '演示供应商：软件开发外包', true, v_admin_id, NOW(), NOW()),
        ('示例物业管理公司', '示例物业', '管理处', '021-0000-0009', 'property@example.com', '示例市静安区示例路300号', '91310000MA0000011X', '6222600000000000105', '交通银行示例分行', '演示供应商：物业管理', true, v_admin_id, NOW(), NOW()),
        ('示例差旅服务公司', '示例差旅', '客服中心', '020-0000-0010', 'travel@example.com', '示例市越秀区示例路400号', '91440100MA0000012X', '6225680000000000106', '广发银行示例分行', '演示供应商：差旅预订', true, v_admin_id, NOW(), NOW());

    -- ========== 4. 定期存款账户 ==========
    INSERT INTO accounts (name, account_type, account_number, bank_name, opening_balance, current_balance, currency, description, is_active, created_by, created_at, updated_at, interest_start_date, maturity_date, interest_rate, auto_renewal)
    VALUES
        ('工商银行定期一年', 'FixedDeposit', '6222020000000000200', '中国工商银行', 300000.00, 300000.00, 'CNY', '演示用一年期定期存款', true, v_admin_id, NOW(), NOW(), '2026-01-15', '2027-01-15', 1.65, false),
        ('招商银行定期半年', 'FixedDeposit', '6225880000000000201', '招商银行', 200000.00, 200000.00, 'CNY', '演示用半年期定期存款', true, v_admin_id, NOW(), NOW(), '2026-02-01', '2026-08-01', 1.45, false);

    -- ========== 5. 定期存款台账记录 ==========
    INSERT INTO fixed_deposit_records (account_id, principal, deposit_date, maturity_date, term_months, interest_rate, status, deposit_transaction_id, is_early_withdrawal, notes, created_by, created_at, updated_at)
    SELECT a.id, 300000.00, '2026-01-15', '2027-01-15', 12, 1.65, 'Active', 0, false, '演示：一年期定存', v_admin_id, NOW(), NOW()
    FROM accounts a WHERE a.account_number = '6222020000000000200' AND NOT a.is_deleted
    UNION ALL
    SELECT a.id, 200000.00, '2026-02-01', '2026-08-01', 6, 1.45, 'Active', 0, false, '演示：半年期定存', v_admin_id, NOW(), NOW()
    FROM accounts a WHERE a.account_number = '6225880000000000201' AND NOT a.is_deleted;

    -- ========== 6. 人员（姓名为常见占位名，联系方式均为虚构） ==========
    INSERT INTO persons (name, person_type, phone, email, bank_account, bank_name, join_date, is_active, created_by, created_at, updated_at)
    VALUES
        ('张三', 'Employee', '13800138001', 'zhangsan@example.com', '6222020000000000301', '工商银行', '2023-01-15', true, v_admin_id, NOW(), NOW()),
        ('李四', 'Employee', '13800138002', 'lisi@example.com', '6222020000000000302', '工商银行', '2023-03-01', true, v_admin_id, NOW(), NOW()),
        ('王五', 'Partner', '13800138003', 'wangwu@example.com', '6225880000000000303', '招商银行', '2022-06-01', true, v_admin_id, NOW(), NOW()),
        ('赵六', 'Contractor', '13800138004', 'zhaoliu@example.com', '6217000000000000304', '建设银行', '2024-01-01', true, v_admin_id, NOW(), NOW());

    RAISE NOTICE '演示数据创建完成';
    RAISE NOTICE '- 6 个账户（含 2 个定期存款账户）';
    RAISE NOTICE '- 6 个客户';
    RAISE NOTICE '- 6 个供应商';
    RAISE NOTICE '- 4 个人员';
    RAISE NOTICE '- 2 条定期存款台账记录';
    RAISE NOTICE '';
    RAISE NOTICE '注意：项目、交易、应收应付数据请通过系统界面手动创建，或根据实际业务需求编写额外的 SQL 脚本';

END $$;
