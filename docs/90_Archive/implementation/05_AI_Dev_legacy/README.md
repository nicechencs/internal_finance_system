# 05_AI_Dev 文档说明

> 本目录包含 AI 辅助开发过程中的指南、记录和待办事项。
> 最后更新：2026-03-14

## 📁 文档结构

### 开发指南（持续参考）

| 文件 | 说明 | 用途 |
|------|------|------|
| `business_scenarios_qa.md` | 业务场景问答和设计决策 | 理解复杂业务逻辑（应付管理、费用分摊等） |
| `development_guide.md` | 完整开发指南（1895 行） | 全景参考：环境搭建、架构、编码规范、测试、部署 |
| `logging_standards.txt` | 日志记录规范 | 后端日志级别、格式、各层要求、敏感信息保护 |
| `api_contract.md` | API 契约管理 | 前后端类型同步、命名规范、自动生成流程 |

### 已完成工作记录

| 文件 | 说明 | 状态 |
|------|------|------|
| `code_review_summary.md` | 代码审查总结（2026-03-13） | P0/P1 已修复，P2 待实现 |
| `data_linkage_and_inline_edit_plan.txt` | 数据关联+行内编辑方案 | Phase 0-3 已完成 |
| `Docker_Deployment_Test_Report.md` | Docker 部署测试报告 | 测试通过，含默认账户和常用命令 |

### 待办事项

| 文件 | 说明 |
|------|------|
| `backlog.md` | 功能待办清单（11 项，按优先级分组） |

## 🔄 文档整理记录（2026-03-14）

### 删除的重复文档（21 个）

**与 01_Requirements 重复（5 个）**
- system_overview.md → 见 `docs/01_Requirements/01_system_overview.md`
- system_architecture.md → 见 `docs/01_Requirements/02_system_architecture.md`
- system_modules.md → 见 `docs/01_Requirements/03_core_modules.md`
- er_diagram.md → 见 `docs/02_Database/02_er_diagram.md`
- ai_dev_prompts.md → 见 `docs/04_Prompts/01_prompt_templates.md`

**合并的 logging 文档（5→1）**
- logging_improvements_summary.txt ┐
- logging_work_summary.txt         ├─→ `logging_standards.txt`
- logging_final_summary.txt        │
- logging_completion_report.txt    ┘

**合并的 code_review 文档（4→1）**
- code_review_plan.md      ┐
- code_review_results.md   ├─→ `code_review_summary.md`
- fix_summary.md           │
- completion_report.md     ┘

**合并的 API 契约文档（3→1）**
- API_Contract_Management.md     ┐
- API_Contract_Implementation.md ├─→ `api_contract.md`
- API_Contract_QuickRef.md       ┘

**整理的问题记录（4→1）**
- ui_issues.md            ┐
- log_issues.md           ├─→ `backlog.md`（提取未实现功能）
- missing_features.md     │
- fix_report_20260314.md  ┘

### 精简原则

✅ **保留的内容**
- 核心规范和标准（日志规范、API 契约、命名约定）
- 业务决策记录（business_scenarios_qa.md）
- 未实现功能清单（backlog.md）
- 数据关联关系参考
- 常见问题和故障排查

✅ **精简的内容**
- 重复的文件清单（已提交到 git，无需在文档中重复）
- 冗长的实施过程描述（保留摘要即可）
- 重复的总结和效果对比
- 过时的待办事项（已完成的工作）

✅ **删除的内容**
- 与其他目录完全重复的文档
- 已解决问题的详细记录（问题已修复，无参考价值）
- 多个版本的同一份报告（只保留最终版本）

## 📊 整理成果

- 文档数量：40 个 → 19 个（-52.5%）
- 总行数：约 5000 行 → 约 1500 行（-70%）
- 05_AI_Dev 目录：23 个 → 8 个（-65%）

## 🎯 快速导航

**新手入门** → `development_guide.md`
**理解业务** → `business_scenarios_qa.md`
**编写日志** → `logging_standards.txt`
**API 开发** → `api_contract.md`
**查看待办** → `backlog.md`
**部署参考** → `Docker_Deployment_Test_Report.md`
