-- =============================================
-- 财务管理系统 - 数据库设计
-- 数据库: PostgreSQL 14+
-- 编码: UTF-8
-- =============================================

-- =============================================
-- 1. 基础配置表
-- =============================================

-- 1.1 用户表（预留多用户支持）
CREATE TABLE users (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    normalized_username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    security_stamp VARCHAR(64) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    role VARCHAR(20) NOT NULL DEFAULT 'viewer', -- admin, accountant, viewer
    is_active BOOLEAN NOT NULL DEFAULT true,
    must_change_password BOOLEAN NOT NULL DEFAULT false,
    access_failed_count INT NOT NULL DEFAULT 0,
    lockout_end_at TIMESTAMP,
    last_login_at TIMESTAMP,
    password_changed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT
);

CREATE INDEX idx_users_username ON users(username) WHERE is_deleted = false;
CREATE UNIQUE INDEX idx_users_normalized_username ON users(normalized_username) WHERE is_deleted = false;
CREATE INDEX idx_users_deleted ON users(is_deleted, deleted_at);
CREATE INDEX idx_users_active ON users(is_active) WHERE is_deleted = false;

COMMENT ON TABLE users IS '用户表';
COMMENT ON COLUMN users.role IS '角色：admin-管理员, accountant-会计, viewer-查看者';

-- =============================================
-- 2. 账户管理
-- =============================================

-- 2.1 账户表
CREATE TABLE accounts (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    account_type VARCHAR(20) NOT NULL, -- bank, alipay
    account_number VARCHAR(50),
    bank_name VARCHAR(100),
    opening_balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    current_balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    currency VARCHAR(10) NOT NULL DEFAULT 'CNY',
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    interest_rate DECIMAL(8, 4),
    interest_start_date TIMESTAMP,
    maturity_date TIMESTAMP,
    auto_renewal BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT
);

CREATE INDEX idx_accounts_type ON accounts(account_type) WHERE is_deleted = false;
CREATE INDEX idx_accounts_active ON accounts(is_active) WHERE is_deleted = false;
CREATE INDEX idx_accounts_maturity_date ON accounts(maturity_date) WHERE account_type = 'fixed_deposit' AND is_deleted = false;

COMMENT ON TABLE accounts IS '账户表';
COMMENT ON COLUMN accounts.account_type IS '账户类型：bank-银行账户, alipay-支付宝账户';

-- =============================================
-- 3. 基础数据表
-- =============================================

-- 3.1 分类表（收入/支出分类）
CREATE TABLE categories (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    parent_id BIGINT,
    category_type VARCHAR(20) NOT NULL, -- income, expense
    level INT NOT NULL DEFAULT 1,
    sort_order INT NOT NULL DEFAULT 0,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (parent_id) REFERENCES categories(id)
);

CREATE INDEX idx_categories_parent ON categories(parent_id) WHERE is_deleted = false;
CREATE INDEX idx_categories_type ON categories(category_type) WHERE is_deleted = false;

COMMENT ON TABLE categories IS '分类表';
COMMENT ON COLUMN categories.category_type IS '分类类型：income-收入, expense-支出';

-- 3.2 客户表
CREATE TABLE customers (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    short_name VARCHAR(100),
    contact_person VARCHAR(100),
    contact_phone VARCHAR(50),
    contact_email VARCHAR(100),
    address TEXT,
    tax_number VARCHAR(50),
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT
);

CREATE INDEX idx_customers_name ON customers(name) WHERE is_deleted = false;
CREATE INDEX idx_customers_active ON customers(is_active) WHERE is_deleted = false;

COMMENT ON TABLE customers IS '客户表';

-- 3.3 供应商表
CREATE TABLE suppliers (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    short_name VARCHAR(100),
    contact_person VARCHAR(100),
    contact_phone VARCHAR(50),
    contact_email VARCHAR(100),
    address TEXT,
    tax_number VARCHAR(50),
    bank_account VARCHAR(50),
    bank_name VARCHAR(100),
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT
);

CREATE INDEX idx_suppliers_name ON suppliers(name) WHERE is_deleted = false;
CREATE INDEX idx_suppliers_active ON suppliers(is_active) WHERE is_deleted = false;

COMMENT ON TABLE suppliers IS '供应商表';

-- 3.4 人员表
CREATE TABLE persons (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    person_type VARCHAR(20) NOT NULL, -- employee, partner, shareholder, contractor
    id_number VARCHAR(50),
    phone VARCHAR(50),
    email VARCHAR(100),
    bank_account VARCHAR(50),
    bank_name VARCHAR(100),
    join_date DATE,
    leave_date DATE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT
);

CREATE INDEX idx_persons_type ON persons(person_type) WHERE is_deleted = false;
CREATE INDEX idx_persons_active ON persons(is_active) WHERE is_deleted = false;

COMMENT ON TABLE persons IS '人员表';
COMMENT ON COLUMN persons.person_type IS '人员类型：employee-员工, partner-合伙人, shareholder-股东, contractor-外包';

-- 3.5 项目表
CREATE TABLE projects (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    project_code VARCHAR(50) UNIQUE,
    customer_id BIGINT,
    contract_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    received_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    receivable_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    total_cost DECIMAL(18, 2) NOT NULL DEFAULT 0,
    profit_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    profit_rate DECIMAL(5, 2) NOT NULL DEFAULT 0,
    start_date DATE,
    end_date DATE,
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- active, completed, cancelled
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE INDEX idx_projects_customer ON projects(customer_id) WHERE is_deleted = false;
CREATE INDEX idx_projects_status ON projects(status) WHERE is_deleted = false;
CREATE INDEX idx_projects_code ON projects(project_code) WHERE is_deleted = false;

COMMENT ON TABLE projects IS '项目表';
COMMENT ON COLUMN projects.status IS '项目状态：active-进行中, completed-已完成, cancelled-已取消';

-- =============================================
-- 4. 交易核心表
-- =============================================

-- 4.1 导入批次表
CREATE TABLE import_batches (
    id BIGSERIAL PRIMARY KEY,
    account_id BIGINT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_size BIGINT,
    import_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    record_count INT NOT NULL DEFAULT 0,
    success_count INT NOT NULL DEFAULT 0,
    duplicate_count INT NOT NULL DEFAULT 0,
    error_count INT NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, processing, completed, failed
    error_message TEXT,
    imported_by BIGINT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (account_id) REFERENCES accounts(id),
    FOREIGN KEY (imported_by) REFERENCES users(id)
);

CREATE INDEX idx_import_batches_account ON import_batches(account_id);
CREATE INDEX idx_import_batches_date ON import_batches(import_date);
CREATE INDEX idx_import_batches_status ON import_batches(status);

COMMENT ON TABLE import_batches IS '导入批次表';

-- 4.2 银行流水表
CREATE TABLE bank_transactions (
    id BIGSERIAL PRIMARY KEY,
    account_id BIGINT NOT NULL,
    import_batch_id BIGINT,
    transaction_date DATE NOT NULL,
    transaction_time TIME,
    amount DECIMAL(18, 2) NOT NULL,
    balance DECIMAL(18, 2),
    direction VARCHAR(10) NOT NULL, -- in, out
    counterparty VARCHAR(200),
    counterparty_account VARCHAR(50),
    counterparty_bank VARCHAR(100),
    memo TEXT,
    description TEXT,
    transaction_number VARCHAR(100),
    unique_hash VARCHAR(64) NOT NULL,
    is_processed BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (account_id) REFERENCES accounts(id),
    FOREIGN KEY (import_batch_id) REFERENCES import_batches(id)
);

CREATE UNIQUE INDEX idx_bank_transactions_hash ON bank_transactions(unique_hash) WHERE is_deleted = false;
CREATE INDEX idx_bank_transactions_account ON bank_transactions(account_id);
CREATE INDEX idx_bank_transactions_date ON bank_transactions(transaction_date);
CREATE INDEX idx_bank_transactions_processed ON bank_transactions(is_processed);

COMMENT ON TABLE bank_transactions IS '银行流水表';
COMMENT ON COLUMN bank_transactions.direction IS '方向：in-收入, out-支出';
COMMENT ON COLUMN bank_transactions.unique_hash IS '唯一标识：MD5(日期+金额+对方+摘要)';

-- 4.3 交易表（核心）
CREATE TABLE transactions (
    id BIGSERIAL PRIMARY KEY,
    bank_transaction_id BIGINT,
    transaction_date DATE NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    transaction_type VARCHAR(20) NOT NULL, -- income, expense
    transfer_direction VARCHAR(20) NOT NULL DEFAULT 'none', -- none, out, in
    category_id BIGINT,
    account_id BIGINT NOT NULL,
    project_id BIGINT,
    customer_id BIGINT,
    supplier_id BIGINT,
    person_id BIGINT,
    description TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'confirmed', -- pending, confirmed, cancelled
    is_allocated BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    related_transaction_id BIGINT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    FOREIGN KEY (bank_transaction_id) REFERENCES bank_transactions(id),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (account_id) REFERENCES accounts(id),
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
    FOREIGN KEY (person_id) REFERENCES persons(id),
    FOREIGN KEY (related_transaction_id) REFERENCES transactions(id)
);

CREATE INDEX idx_transactions_date ON transactions(transaction_date) WHERE is_deleted = false;
CREATE INDEX idx_transactions_type ON transactions(transaction_type) WHERE is_deleted = false;
CREATE INDEX idx_transactions_category ON transactions(category_id) WHERE is_deleted = false;
CREATE INDEX idx_transactions_account ON transactions(account_id) WHERE is_deleted = false;
CREATE INDEX idx_transactions_project ON transactions(project_id) WHERE is_deleted = false;
CREATE INDEX idx_transactions_customer ON transactions(customer_id) WHERE is_deleted = false;
CREATE INDEX idx_transactions_supplier ON transactions(supplier_id) WHERE is_deleted = false;
CREATE INDEX idx_transactions_person ON transactions(person_id) WHERE is_deleted = false;
CREATE INDEX idx_transactions_related ON transactions(related_transaction_id) WHERE is_deleted = false;

COMMENT ON TABLE transactions IS '交易表（核心）';
COMMENT ON COLUMN transactions.transaction_type IS '交易类型：income-收入, expense-支出, transfer-转账';
COMMENT ON COLUMN transactions.related_transaction_id IS '关联交易ID（转账时关联配对记录）';

-- 4.4 交易分摊表
-- 支持按项目分摊（如一笔外包费分摊到多个项目）和按人员分摊（如一笔个税分摊到多个员工）
CREATE TABLE transaction_allocations (
    id BIGSERIAL PRIMARY KEY,
    transaction_id BIGINT NOT NULL,
    project_id BIGINT,
    person_id BIGINT,
    amount DECIMAL(18, 2) NOT NULL,
    allocation_rate DECIMAL(5, 2),
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (transaction_id) REFERENCES transactions(id) ON DELETE CASCADE,
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (person_id) REFERENCES persons(id),
    CONSTRAINT chk_allocation_target CHECK (project_id IS NOT NULL OR person_id IS NOT NULL)
);

CREATE INDEX idx_allocations_transaction ON transaction_allocations(transaction_id);
CREATE INDEX idx_allocations_project ON transaction_allocations(project_id) WHERE project_id IS NOT NULL;
CREATE INDEX idx_allocations_person ON transaction_allocations(person_id) WHERE person_id IS NOT NULL;

COMMENT ON TABLE transaction_allocations IS '交易分摊表（支持按项目或按人员分摊）';
COMMENT ON COLUMN transaction_allocations.project_id IS '分摊目标项目（与 person_id 至少一个不为空）';
COMMENT ON COLUMN transaction_allocations.person_id IS '分摊目标人员（个税、社保等按人分摊场景）';

-- =============================================
-- 5. 应收应付管理
-- =============================================

-- 5.1 应收账款表
CREATE TABLE receivables (
    id BIGSERIAL PRIMARY KEY,
    project_id BIGINT NOT NULL,
    customer_id BIGINT NOT NULL,
    total_amount DECIMAL(18, 2) NOT NULL,
    received_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    remaining_amount DECIMAL(18, 2) NOT NULL,
    due_date DATE,
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, partial, settled（逾期通过查询时动态判断：due_date < CURRENT_DATE AND status IN ('pending','partial')）
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    settled_at TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE INDEX idx_receivables_project ON receivables(project_id);
CREATE INDEX idx_receivables_customer ON receivables(customer_id);
CREATE INDEX idx_receivables_status ON receivables(status);
CREATE INDEX idx_receivables_due_date ON receivables(due_date);

COMMENT ON TABLE receivables IS '应收账款表';

-- 5.2 应收款项明细表
CREATE TABLE receivable_details (
    id BIGSERIAL PRIMARY KEY,
    receivable_id BIGINT NOT NULL,
    transaction_id BIGINT,
    payment_date DATE NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    payment_method VARCHAR(50),
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (receivable_id) REFERENCES receivables(id) ON DELETE CASCADE,
    FOREIGN KEY (transaction_id) REFERENCES transactions(id)
);

CREATE INDEX idx_receivable_details_receivable ON receivable_details(receivable_id);
CREATE INDEX idx_receivable_details_date ON receivable_details(payment_date);

COMMENT ON TABLE receivable_details IS '应收款项明细表';

-- 5.3 应付账款表
CREATE TABLE payables (
    id BIGSERIAL PRIMARY KEY,
    supplier_id BIGINT NOT NULL,
    project_id BIGINT,
    total_amount DECIMAL(18, 2) NOT NULL,
    paid_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    remaining_amount DECIMAL(18, 2) NOT NULL,
    due_date DATE,
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, partial, settled（逾期通过查询时动态判断）
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    settled_at TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

CREATE INDEX idx_payables_supplier ON payables(supplier_id);
CREATE INDEX idx_payables_project ON payables(project_id);
CREATE INDEX idx_payables_status ON payables(status);
CREATE INDEX idx_payables_due_date ON payables(due_date);

COMMENT ON TABLE payables IS '应付账款表';

-- 5.4 应付款项明细表
CREATE TABLE payable_details (
    id BIGSERIAL PRIMARY KEY,
    payable_id BIGINT NOT NULL,
    transaction_id BIGINT,
    payment_date DATE NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    payment_method VARCHAR(50),
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (payable_id) REFERENCES payables(id) ON DELETE CASCADE,
    FOREIGN KEY (transaction_id) REFERENCES transactions(id)
);

CREATE INDEX idx_payable_details_payable ON payable_details(payable_id);
CREATE INDEX idx_payable_details_date ON payable_details(payment_date);

COMMENT ON TABLE payable_details IS '应付款项明细表';

-- =============================================
-- 6. 规则引擎
-- =============================================

-- 6.1 分类规则表
CREATE TABLE classification_rules (
    id BIGSERIAL PRIMARY KEY,
    rule_name VARCHAR(100) NOT NULL,
    priority INT NOT NULL DEFAULT 0,
    match_field VARCHAR(50) NOT NULL, -- counterparty, memo, amount
    match_operator VARCHAR(20) NOT NULL, -- equals, contains, starts_with, ends_with, regex, range
    match_value TEXT NOT NULL,
    category_id BIGINT,
    project_id BIGINT,
    customer_id BIGINT,
    supplier_id BIGINT,
    person_id BIGINT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
    FOREIGN KEY (person_id) REFERENCES persons(id)
);

CREATE INDEX idx_rules_priority ON classification_rules(priority DESC) WHERE is_active = true;
CREATE INDEX idx_rules_field ON classification_rules(match_field) WHERE is_active = true;

COMMENT ON TABLE classification_rules IS '分类规则表';
COMMENT ON COLUMN classification_rules.match_field IS '匹配字段：counterparty-对方名称, memo-摘要, amount-金额';
COMMENT ON COLUMN classification_rules.match_operator IS '匹配操作符：equals-等于, contains-包含, starts_with-开头, ends_with-结尾, regex-正则, range-范围';

-- =============================================
-- 7. 审计日志
-- =============================================

-- 7.1 操作日志表
CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT,
    action VARCHAR(50) NOT NULL, -- create, update, delete, import, export
    entity_type VARCHAR(50) NOT NULL, -- transaction, project, customer, etc.
    entity_id BIGINT,
    old_value JSONB,
    new_value JSONB,
    ip_address VARCHAR(50),
    user_agent TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_audit_logs_user ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_created ON audit_logs(created_at);

COMMENT ON TABLE audit_logs IS '操作日志表';

-- =============================================
-- 8. 系统配置
-- =============================================

-- 8.1 系统配置表
CREATE TABLE system_configs (
    id BIGSERIAL PRIMARY KEY,
    config_key VARCHAR(100) NOT NULL UNIQUE,
    config_value TEXT,
    config_type VARCHAR(20) NOT NULL DEFAULT 'string', -- string, number, boolean, json
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_by BIGINT
);

CREATE INDEX idx_configs_key ON system_configs(config_key) WHERE is_active = true;

COMMENT ON TABLE system_configs IS '系统配置表';

-- =============================================
-- 9. 初始化数据
-- =============================================

-- 9.1 不再内置默认管理员账号
-- 推荐通过以下方式初始化首个管理员：
-- 1. 启用 Auth:BootstrapAdmin
-- 2. 使用命令行：dotnet run --project backend/FinanceApp.Api -- auth-cli create-user ...

-- 9.2 默认分类
INSERT INTO categories (name, parent_id, category_type, level, sort_order, description) VALUES
-- 收入分类
('项目收入', NULL, 'income', 1, 1, '项目合同收入'),
('咨询收入', NULL, 'income', 1, 2, '咨询服务收入'),
('技术服务费', NULL, 'income', 1, 3, '技术服务费收入'),
('利息收入', NULL, 'income', 1, 4, '银行存款利息'),
('其他收入', NULL, 'income', 1, 5, '其他杂项收入'),
-- 支出分类
('人员工资', NULL, 'expense', 1, 10, '员工薪资支出'),
('社保公积金', NULL, 'expense', 1, 11, '社保和住房公积金'),
('办公租金', NULL, 'expense', 1, 12, '办公室租金'),
('办公用品', NULL, 'expense', 1, 13, '日常办公用品采购'),
('差旅费', NULL, 'expense', 1, 14, '出差差旅费用'),
('招待费', NULL, 'expense', 1, 15, '商务招待费'),
('服务器/云服务', NULL, 'expense', 1, 16, '服务器及云计算服务费用'),
('软件/工具订阅', NULL, 'expense', 1, 17, '软件许可及工具订阅费'),
('外包费用', NULL, 'expense', 1, 18, '外包项目费用'),
('税费', NULL, 'expense', 1, 19, '各类税费'),
('水电物业', NULL, 'expense', 1, 20, '水电费及物业管理费'),
('其他支出', NULL, 'expense', 1, 99, '其他杂项支出');

-- 9.3 系统配置
INSERT INTO system_configs (config_key, config_value, config_type, description) VALUES
('system_name', '财务管理系统', 'string', '站点名称'),
('system_name_en', 'Finance Management System', 'string', '站点英文副标题'),
('default_currency', 'CNY', 'string', '默认货币'),
('fiscal_year_start', '01-01', 'string', '财年开始日期（月-日）'),
('audit_retention_months', '6', 'number', '审计日志保留月数（3-6）');

-- =============================================
-- 10. 视图定义
-- =============================================

-- 10.1 项目利润视图
-- 成本计算逻辑：
--   1. 直接成本：transactions 中 is_allocated=false 且 project_id 直接关联本项目的支出
--   2. 分摊成本：transaction_allocations 中 project_id 关联本项目的分摊金额
--   两者相加得到 total_cost，避免重复计算
CREATE OR REPLACE VIEW v_project_profit AS
WITH project_direct_cost AS (
    -- 直接关联项目的支出（未使用费用分摊的交易）
    SELECT
        t.project_id,
        COALESCE(SUM(t.amount), 0) AS cost
    FROM transactions t
    WHERE t.transaction_type = 'expense'
      AND t.is_allocated = false
      AND t.is_deleted = false
      AND t.project_id IS NOT NULL
    GROUP BY t.project_id
),
project_allocated_cost AS (
    -- 通过费用分摊关联项目的支出
    SELECT
        ta.project_id,
        COALESCE(SUM(ta.amount), 0) AS cost
    FROM transaction_allocations ta
    JOIN transactions t ON t.id = ta.transaction_id
    WHERE t.transaction_type = 'expense'
      AND t.is_deleted = false
      AND ta.project_id IS NOT NULL
    GROUP BY ta.project_id
)
SELECT
    p.id,
    p.name AS project_name,
    p.project_code,
    c.name AS customer_name,
    p.contract_amount,
    p.received_amount,
    p.receivable_amount,
    COALESCE(dc.cost, 0) + COALESCE(ac.cost, 0) AS total_cost,
    p.received_amount - (COALESCE(dc.cost, 0) + COALESCE(ac.cost, 0)) AS profit_amount,
    CASE
        WHEN p.received_amount > 0 THEN
            ROUND((p.received_amount - (COALESCE(dc.cost, 0) + COALESCE(ac.cost, 0))) / p.received_amount * 100, 2)
        ELSE 0
    END AS profit_rate,
    p.status,
    p.start_date,
    p.end_date
FROM projects p
LEFT JOIN customers c ON p.customer_id = c.id
LEFT JOIN project_direct_cost dc ON dc.project_id = p.id
LEFT JOIN project_allocated_cost ac ON ac.project_id = p.id
WHERE p.is_deleted = false;

COMMENT ON VIEW v_project_profit IS '项目利润视图';

-- 10.2 账户余额视图
CREATE OR REPLACE VIEW v_account_balance AS
SELECT
    a.id,
    a.name AS account_name,
    a.account_type,
    a.opening_balance,
    COALESCE(SUM(CASE WHEN t.transaction_type = 'income' THEN t.amount ELSE 0 END), 0) AS total_income,
    COALESCE(SUM(CASE WHEN t.transaction_type = 'expense' THEN t.amount ELSE 0 END), 0) AS total_expense,
    a.opening_balance +
        COALESCE(SUM(CASE WHEN t.transaction_type = 'income' THEN t.amount ELSE 0 END), 0) -
        COALESCE(SUM(CASE WHEN t.transaction_type = 'expense' THEN t.amount ELSE 0 END), 0) AS current_balance
FROM accounts a
LEFT JOIN transactions t ON t.account_id = a.id AND t.is_deleted = false
WHERE a.is_deleted = false
GROUP BY a.id, a.name, a.account_type, a.opening_balance;

COMMENT ON VIEW v_account_balance IS '账户余额视图';

-- =============================================
-- 11. 触发器函数
-- =============================================

-- 11.1 更新时间戳触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 应用到所有需要的表
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_accounts_updated_at BEFORE UPDATE ON accounts
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_categories_updated_at BEFORE UPDATE ON categories
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customers_updated_at BEFORE UPDATE ON customers
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_suppliers_updated_at BEFORE UPDATE ON suppliers
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_persons_updated_at BEFORE UPDATE ON persons
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_projects_updated_at BEFORE UPDATE ON projects
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_transactions_updated_at BEFORE UPDATE ON transactions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_receivables_updated_at BEFORE UPDATE ON receivables
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_payables_updated_at BEFORE UPDATE ON payables
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_rules_updated_at BEFORE UPDATE ON classification_rules
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_configs_updated_at BEFORE UPDATE ON system_configs
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- 12. 数据库统计信息
-- =============================================

-- 查看所有表及其行数
CREATE OR REPLACE VIEW v_table_statistics AS
SELECT
    schemaname,
    relname AS tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||relname)) AS size,
    n_live_tup AS row_count
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(schemaname||'.'||relname) DESC;

COMMENT ON VIEW v_table_statistics IS '表统计信息视图';

-- =============================================
-- END OF SCHEMA
-- =============================================
