# Finance Settlement Transaction Binding Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tighten settlement-detail and transaction binding rules without changing database schema or API contracts.

**Architecture:** Add a shared application-layer validator for settlement binding, wire it into receivable/payable settlement entry points, then protect transaction updates from breaking existing bindings.

**Tech Stack:** C#, .NET, EF Core repositories, xUnit, Moq, FluentAssertions

---

### Task 1: Lock Binding Rules With Tests

**Files:**
- Modify: `backend/tests/FinanceApp.Application.Tests/Services/ReceivableServiceTests.cs`
- Modify: `backend/tests/FinanceApp.Application.Tests/Services/PayableServiceTests.cs`
- Modify: `backend/tests/FinanceApp.Application.Tests/Services/TransactionServiceTests.cs`

- [ ] Add failing tests for receivable binding direction, opposite-side conflict, and cumulative amount overflow.
- [ ] Run focused receivable tests and confirm they fail for the expected reasons.
- [ ] Add failing tests for payable binding direction, opposite-side conflict, and cumulative amount overflow.
- [ ] Run focused payable tests and confirm they fail for the expected reasons.
- [ ] Add failing tests for transaction update protections on type and amount when linked settlement details already exist.
- [ ] Run focused transaction tests and confirm they fail for the expected reasons.

### Task 2: Implement Shared Binding Validation

**Files:**
- Add: `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/ISettlementTransactionBindingService.cs`
- Add: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/SettlementTransactionBindingService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/FinanceSettlementModuleExtensions.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/PayableService.cs`

- [ ] Add the binding service interface and implementation.
- [ ] Validate transaction existence, deleted-state, permission, direction, opposite-side exclusivity, and cumulative linked amount.
- [ ] Inject the binding service into receivable and payable services.
- [ ] Call the validator before creating new settlement details.
- [ ] Run the focused receivable/payable tests and confirm they pass.

### Task 3: Protect Transaction Updates

**Files:**
- Modify: `backend/FinanceApp.Application/Modules/TransactionProcessing/Services/TransactionService.cs`
- Modify: `backend/tests/FinanceApp.Application.Tests/Services/TransactionServiceTests.cs`

- [ ] Reuse repository data to compute linked receivable/payable totals during transaction update.
- [ ] Reject type changes that break existing settlement-side rules.
- [ ] Reject amount reductions below linked totals.
- [ ] Run focused transaction tests and confirm they pass.

### Task 4: Record and Verify

**Files:**
- Modify: `docs/superpowers/plans/2026-03-31-finance-settlement-transaction-binding-phase2.md`

- [ ] Append execution notes and verification commands to this plan after implementation.
- [ ] Run focused test command for the touched service test classes.
- [ ] Run full backend test suite if focused tests are green.

## Execution Notes

- Added module-local binding validator:
  - `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/ISettlementTransactionBindingService.cs`
  - `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/SettlementTransactionBindingService.cs`
- Wired validator into:
  - `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableService.cs`
  - `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/PayableService.cs`
  - `backend/FinanceApp.Application/Modules/FinanceSettlement/FinanceSettlementModuleExtensions.cs`
- Added transaction update protection in:
  - `backend/FinanceApp.Application/Modules/TransactionProcessing/Services/TransactionService.cs`
- Added regression tests in:
  - `backend/tests/FinanceApp.Application.Tests/Services/ReceivableServiceTests.cs`
  - `backend/tests/FinanceApp.Application.Tests/Services/PayableServiceTests.cs`
  - `backend/tests/FinanceApp.Application.Tests/Services/TransactionServiceTests.cs`

## Verification

Focused test command:

```bash
dotnet test backend/tests/FinanceApp.Application.Tests/FinanceApp.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ReceivableServiceTests|FullyQualifiedName~PayableServiceTests|FullyQualifiedName~TransactionServiceTests"
```

Focused result:

- 77 passed
- 0 failed

Full backend regression:

```bash
dotnet test backend/FinanceApp.sln --no-restore
```

Full regression result:

- Domain: 93 passed
- Application: 482 passed
- Infrastructure: 53 passed
- API: 211 passed
- Total: 839 passed, 0 failed

## Residual Risk

- Concurrent requests can still race on the same transaction binding total because the current phase does not add a database constraint or serializable lock strategy.
- The current fix prevents new inconsistent single-request writes and closes the main application-layer loopholes, but a fully hard guarantee for concurrent over-binding needs database-level support in a later phase.
