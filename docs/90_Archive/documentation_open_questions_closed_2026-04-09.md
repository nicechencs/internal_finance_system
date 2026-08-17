# 文档整理待确认事项

状态：Active
适用对象：产品 / 开发 / 测试
事实源级别：Secondary
最后核对日期：2026-04-03

## 本轮结论（2026-04-03）

- 本文档中的 5 项里，已有 4 项就问题本身已闭环，不再属于“待确认”
- 另有 1 项已可通过当前代码确认“尚未闭环”，后续应继续按 backlog / known issues 跟踪
- 因此本文档整体仍保持 `Active`，但不再适合把所有条目都视为“纯待确认”

## 核对结果

### 1. ~~图表空数据状态是否已全部修复~~ 已确认已修复

来源：

- 旧 `known_issues` 的 KI-007
- `frontend/src/features/finance/pages/FinanceManagementPage.vue`
- `frontend/tests/views/finance/FinanceManagement.test.ts`

当前判断：

- 前端已在趋势图和账龄图卡片中接入显式空态
- 空数组和全 0 数据场景已有组件测试覆盖
- 因此该项已闭环，不再继续跟踪

建议：

- 如后续扩展到其他图表模块，可复用相同空态判定模式

### 2. ~~各列表排序能力是否已全面统一~~ 已确认未闭环

来源：

- 旧 `known_issues` 的 KI-004
- `frontend/src/features/**/*ListPage.vue`

当前判断：

- 部分列表已支持排序，例如应收、应付、账户、分类、客户、人员、项目、供应商、规则、交易列表
- 但至少 `FixedDepositListPage` 与 `AuditLogListPage` 仍未接入 `@sort-change` / `sortable="custom"`
- 因此该项已可确认“尚未全面统一”，不再属于纯待确认项

建议：

- 后续继续按 backlog 中的“各模块列表排序能力统一”推进
- 完成后补一轮按模块的 UI 排序回归检查

### 3. ~~`voucher_no` 是否需要进入近期开发计划~~ 已确认

来源：

- 旧 `known_issues` 的 KI-005
- `docs/01_Product/03_import_rules.md`
- `docs/04_Development/05_backlog.md`

当前判断：

- `description` 与 `memo` 已拆分保存，但当前仍未看到 `voucher_no` 已落地
- “凭证号（voucher_no）功能评估与实施”已进入当前 backlog 的高优先级项
- 因此“是否进入近期开发计划”这一问题已闭环，剩余的是排期与实现

建议：

- 本文档不再继续以“待确认”方式跟踪
- 后续以 backlog、设计方案和实际实现进度为准

### 4. ~~自托管 PaaS 是否仍然是受支持的部署路径~~ 已确认

当时确认过某一自托管 PaaS 为主部署方案；旧镜像仓库流水线已移除（2026-03-25）。现行部署以通用 Docker Compose 为准。

### 5. ~~是否需要补充 Linux / macOS 本地一键启动脚本~~ 已确认

来源：

- 旧入口文档曾把 `start-dev.sh` 当作现成脚本描述，但当前仓库并不存在该文件
- `docs/04_Development/01_onboarding.md`
- `docs/04_Development/02_scripts.md`

当前判断：

- 当前主开发文档已明确说明：Windows 使用 `start-dev.bat`，Linux / macOS 需手动启动
- 因此“文档是否错误暗示存在 `start-dev.sh`”这一问题已闭环
- 是否后续补充脚本属于工程效率优化，不再是本文档中的待确认项

建议：

- 如果团队存在稳定的非 Windows 本地开发场景，后续再进入 backlog 评估是否补 `start-dev.sh`
- 若没有明确需求，继续保持“Windows 一键启动、跨平台手动启动”的现状即可

## 仍需后续跟踪

- 各模块列表排序能力统一
- `voucher_no` 功能评估与实施（已进入 backlog，待排期）

## 最近已确认（2026-04-03）

以下事项已通过代码验证或修复确认：

- ✅ 应收应付图表空数据状态已修复（空数组 / 全 0 数据显示空态）
- ✅ 交易删除逻辑完整（TXN-01 不存在）
- ✅ 定期存款 API 路径正确（FD-01 不存在）
- ✅ 用户管理和认证已集成审计日志（USER-01、AUTH-01 已修复）
- ✅ 规则管理权限前后端已统一（RULE-01 已修复）
- ✅ 应收应付筛选已联动统计（FIN-01 已修复）
- ✅ 深链问题已修复（FIN-02、PARTY-01 已修复）
