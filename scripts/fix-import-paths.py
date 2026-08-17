#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
fix-import-paths.py
批量替换 frontend/src 下所有 .ts/.vue 文件中的旧 @/ 路径为新路径
执行: python scripts/fix-import-paths.py [--dry-run]
"""
import os
import sys
import re

SRC_ROOT = os.path.join("frontend", "src")

# 路径映射表：(旧路径, 新路径)
# 顺序：长前缀排在短前缀前面，防止误替换
MAPPINGS = [
    # ── views ──
    ('@/views/dashboard/components/StatCards.vue',     '@/features/dashboard/components/StatCards.vue'),
    ('@/views/settings/AccountSecurity.vue',           '@/features/auth/pages/AccountSecurityPage.vue'),
    ('@/views/settings/AccountProfile.vue',            '@/features/auth/pages/AccountProfilePage.vue'),
    ('@/views/settings/UserManagement.vue',            '@/features/system/pages/UserManagementPage.vue'),
    ('@/views/accounts/AccountList.vue',               '@/features/master-data/accounts/pages/AccountListPage.vue'),
    ('@/views/accounts/AccountDetail.vue',             '@/features/master-data/accounts/pages/AccountDetailPage.vue'),
    ('@/views/accounts/AccountForm.vue',               '@/features/master-data/accounts/components/AccountForm.vue'),
    ('@/views/categories/CategoryList.vue',            '@/features/master-data/categories/pages/CategoryListPage.vue'),
    ('@/views/categories/CategoryForm.vue',            '@/features/master-data/categories/components/CategoryForm.vue'),
    ('@/views/projects/ProjectList.vue',               '@/features/master-data/projects/pages/ProjectListPage.vue'),
    ('@/views/projects/ProjectDetail.vue',             '@/features/master-data/projects/pages/ProjectDetailPage.vue'),
    ('@/views/projects/ProjectForm.vue',               '@/features/master-data/projects/pages/ProjectFormPage.vue'),
    ('@/views/customers/CustomerList.vue',             '@/features/master-data/customers/pages/CustomerListPage.vue'),
    ('@/views/customers/CustomerDetail.vue',           '@/features/master-data/customers/pages/CustomerDetailPage.vue'),
    ('@/views/customers/CustomerForm.vue',             '@/features/master-data/customers/pages/CustomerFormPage.vue'),
    ('@/views/suppliers/SupplierList.vue',             '@/features/master-data/suppliers/pages/SupplierListPage.vue'),
    ('@/views/suppliers/SupplierDetail.vue',           '@/features/master-data/suppliers/pages/SupplierDetailPage.vue'),
    ('@/views/suppliers/SupplierForm.vue',             '@/features/master-data/suppliers/pages/SupplierFormPage.vue'),
    ('@/views/persons/PersonList.vue',                 '@/features/master-data/persons/pages/PersonListPage.vue'),
    ('@/views/persons/PersonDetail.vue',               '@/features/master-data/persons/pages/PersonDetailPage.vue'),
    ('@/views/persons/PersonForm.vue',                 '@/features/master-data/persons/pages/PersonFormPage.vue'),
    ('@/views/transactions/TransactionList.vue',       '@/features/transactions/pages/TransactionListPage.vue'),
    ('@/views/transactions/TransactionDetail.vue',     '@/features/transactions/pages/TransactionDetailPage.vue'),
    ('@/views/transactions/TransactionForm.vue',       '@/features/transactions/components/TransactionForm.vue'),
    ('@/views/finance/FinanceManagement.vue',          '@/features/finance/pages/FinanceManagementPage.vue'),
    ('@/views/receivables/ReceivableList.vue',         '@/features/finance/pages/ReceivableListPage.vue'),
    ('@/views/receivables/ReceivableDetail.vue',       '@/features/finance/pages/ReceivableDetailPage.vue'),
    ('@/views/receivables/ReceivableForm.vue',         '@/features/finance/components/ReceivableForm.vue'),
    ('@/views/payables/PayableList.vue',               '@/features/finance/pages/PayableListPage.vue'),
    ('@/views/payables/PayableDetail.vue',             '@/features/finance/pages/PayableDetailPage.vue'),
    ('@/views/payables/PayableForm.vue',               '@/features/finance/components/PayableForm.vue'),
    ('@/views/import/ImportPage.vue',                  '@/features/import/pages/ImportPage.vue'),
    ('@/views/rules/RuleList.vue',                     '@/features/reconciliation/pages/RuleListPage.vue'),
    ('@/views/rules/RuleForm.vue',                     '@/features/reconciliation/components/RuleForm.vue'),
    ('@/views/audit-logs/AuditLogList.vue',            '@/features/system/pages/AuditLogListPage.vue'),
    ('@/views/Login.vue',                              '@/features/auth/pages/LoginPage.vue'),
    ('@/views/Dashboard.vue',                          '@/features/dashboard/pages/DashboardPage.vue'),

    # ── api（长路径在前）──
    ('@/api/base/crudFactory',  '@/shared/api/base/crudFactory'),
    ('@/api/base/types',        '@/shared/api/base/types'),
    ('@/api/auditLog',          '@/features/system/api/auditLog'),
    ('@/api/account',           '@/features/master-data/accounts/api/account'),
    ('@/api/category',          '@/features/master-data/categories/api/category'),
    ('@/api/customer',          '@/features/master-data/customers/api/customer'),
    ('@/api/supplier',          '@/features/master-data/suppliers/api/supplier'),
    ('@/api/project',           '@/features/master-data/projects/api/project'),
    ('@/api/person',            '@/features/master-data/persons/api/person'),
    ('@/api/transaction',       '@/features/transactions/api/transaction'),
    ('@/api/link',              '@/features/transactions/api/link'),
    ('@/api/receivable',        '@/features/finance/api/receivable'),
    ('@/api/payable',           '@/features/finance/api/payable'),
    ('@/api/import',            '@/features/import/api/import'),
    ('@/api/rule',              '@/features/reconciliation/api/rule'),
    ('@/api/dashboard',         '@/features/dashboard/api/dashboard'),
    ('@/api/report',            '@/features/reporting/api/report'),
    ('@/api/config',            '@/features/reporting/api/config'),
    ('@/api/users',             '@/features/system/api/users'),
    ('@/api/auth',              '@/features/auth/api/auth'),

    # ── types ──
    ('@/types/transaction', '@/features/transactions/types/transaction'),
    ('@/types/receivable',  '@/features/finance/types/receivable'),
    ('@/types/dashboard',   '@/features/dashboard/types/dashboard'),
    ('@/types/auditLog',    '@/features/system/types/auditLog'),
    ('@/types/category',    '@/features/master-data/categories/types/category'),
    ('@/types/customer',    '@/features/master-data/customers/types/customer'),
    ('@/types/supplier',    '@/features/master-data/suppliers/types/supplier'),
    ('@/types/account',     '@/features/master-data/accounts/types/account'),
    ('@/types/project',     '@/features/master-data/projects/types/project'),
    ('@/types/payable',     '@/features/finance/types/payable'),
    ('@/types/person',      '@/features/master-data/persons/types/person'),
    ('@/types/import',      '@/features/import/types/import'),
    ('@/types/report',      '@/features/reporting/types/report'),
    ('@/types/config',      '@/features/reporting/types/config'),
    ('@/types/common',      '@/shared/types/common'),
    ('@/types/error',       '@/shared/types/error'),
    ('@/types/link',        '@/features/transactions/types/link'),
    ('@/types/rule',        '@/features/reconciliation/types/rule'),

    # ── stores ──
    ('@/stores/category', '@/features/master-data/categories/stores/category'),
    ('@/stores/customer', '@/features/master-data/customers/stores/customer'),
    ('@/stores/supplier', '@/features/master-data/suppliers/stores/supplier'),
    ('@/stores/account',  '@/features/master-data/accounts/stores/account'),
    ('@/stores/project',  '@/features/master-data/projects/stores/project'),
    ('@/stores/user',     '@/features/auth/stores/user'),

    # ── components（长路径在前）──
    ('@/components/ConvertTransactionToTransferDialog.vue', '@/features/transactions/components/ConvertTransactionToTransferDialog.vue'),
    ('@/components/TransactionSummaryCards.vue',            '@/features/transactions/components/TransactionSummaryCards.vue'),
    ('@/components/TransactionStatCards.vue',               '@/features/transactions/components/TransactionStatCards.vue'),
    ('@/components/SearchableFilterInput.vue',              '@/shared/ui/SearchableFilterInput.vue'),
    ('@/components/ProfitAnalysisCharts.vue',               '@/features/transactions/components/ProfitAnalysisCharts.vue'),
    ('@/components/DetailSummaryCards.vue',                 '@/shared/ui/DetailSummaryCards.vue'),
    ('@/components/BalanceTrendChart.vue',                  '@/features/transactions/components/BalanceTrendChart.vue'),
    ('@/components/BatchLinkDialog.vue',                    '@/features/transactions/components/BatchLinkDialog.vue'),
    ('@/components/SummaryOverview.vue',                    '@/shared/ui/SummaryOverview.vue'),
    ('@/components/SearchableSelect.vue',                   '@/shared/ui/SearchableSelect.vue'),
    ('@/components/SearchableInput.vue',                    '@/shared/ui/SearchableInput.vue'),
    ('@/components/RuleRerunDialog.vue',                    '@/features/reconciliation/components/RuleRerunDialog.vue'),
    ('@/components/TransferDialog.vue',                     '@/features/transactions/components/TransferDialog.vue'),
    ('@/components/MaturityAlert.vue',                      '@/features/system/components/MaturityAlert.vue'),
    ('@/components/ImportDialog.vue',                       '@/features/import/components/ImportDialog.vue'),
    ('@/components/LinkDialog.vue',                         '@/features/transactions/components/LinkDialog.vue'),
    ('@/components/StatCard.vue',                           '@/shared/ui/StatCard.vue'),

    # ── composables ──
    ('@/composables/useResizableTableColumns', '@/shared/composables/useResizableTableColumns'),
    ('@/composables/useFormDialog',            '@/shared/composables/useFormDialog'),
    ('@/composables/useInlineEdit',            '@/shared/composables/useInlineEdit'),
    ('@/composables/useListPage',              '@/shared/composables/useListPage'),
    ('@/composables/useConfirm',               '@/shared/composables/useConfirm'),
    ('@/composables/useDebounce',              '@/shared/composables/useDebounce'),
    ('@/composables/useCache',                 '@/shared/composables/useCache'),
    ('@/composables/useAuth',                  '@/shared/composables/useAuth'),

    # ── utils ──
    ('@/utils/transactionStatistics', '@/features/transactions/utils/transactionStatistics'),
    ('@/utils/dateShortcuts',         '@/shared/utils/dateShortcuts'),
    ('@/utils/formatters',            '@/shared/utils/formatters'),
    ('@/utils/request',               '@/shared/utils/request'),

    # ── constants ──
    ('@/constants/permissions', '@/shared/constants/permissions'),
    ('@/constants/validation',  '@/shared/constants/validation'),
    ('@/constants/colors',      '@/shared/constants/colors'),
    ('@/constants/enums',       '@/shared/constants/enums'),
    ('@/constants/table',       '@/shared/constants/table'),

    # ── directives / layouts ──
    ('@/directives/permission',  '@/shared/directives/permission'),
    ('@/layouts/MainLayout.vue', '@/shared/layouts/MainLayout.vue'),
]


def main():
    dry_run = '--dry-run' in sys.argv

    # 收集所有 .ts / .vue 文件
    files = []
    for root, _dirs, filenames in os.walk(SRC_ROOT):
        for fn in filenames:
            if fn.endswith('.ts') or fn.endswith('.vue'):
                files.append(os.path.join(root, fn))
    files.sort()

    total_files = len(files)
    changed_files = 0
    total_changes = 0

    print(f"扫描 {total_files} 个文件...")

    for fpath in files:
        with open(fpath, 'r', encoding='utf-8') as f:
            original = f.read()

        content = original
        file_changes = 0

        for old, new in MAPPINGS:
            escaped = re.escape(old)
            matches = len(re.findall(escaped, content))
            if matches > 0:
                content = re.sub(escaped, new, content)
                file_changes += matches

        if file_changes > 0:
            changed_files += 1
            total_changes += file_changes
            rel = fpath.replace('\\', '/')
            print(f"  [{file_changes} 处]  {rel}")

            if not dry_run:
                with open(fpath, 'w', encoding='utf-8', newline='') as f:
                    f.write(content)

    print()
    if dry_run:
        print("【DryRun 模式】未写入任何文件。")
    print(f"完成：共修改 {changed_files} 个文件，替换 {total_changes} 处。")


if __name__ == '__main__':
    main()
