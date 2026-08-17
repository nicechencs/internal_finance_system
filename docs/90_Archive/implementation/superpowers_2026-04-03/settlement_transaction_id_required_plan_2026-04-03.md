# Settlement TransactionId Required Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make settlement `TransactionId` mandatory across backend contracts, schema, and frontend flows so issue 3 is fully closed.

**Architecture:** Tighten the contract from the edges inward. First lock behavior with failing tests, then make backend DTO/entity/service signatures non-nullable, then align EF schema and frontend form/types, and finally update the fix summary docs.

**Tech Stack:** .NET 8, EF Core 8, xUnit, Vue 3, TypeScript, Element Plus

---

### Task 1: Lock Missing-Transaction Behavior With Tests

**Files:**
- Modify: `backend/tests/FinanceApp.Application.Tests/Services/ReceivableServiceTests.cs`
- Modify: `backend/tests/FinanceApp.Application.Tests/Services/PayableServiceTests.cs`

- [ ] **Step 1: Write the failing receivable test**

```csharp
[Fact]
public async Task ReceivePaymentAsync_WithZeroTransactionId_ShouldThrowValidationException()
{
    // arrange valid receivable
    // act with TransactionId = 0
    // assert validation message contains "必须关联交易记录"
}
```

- [ ] **Step 2: Write the failing payable test**

```csharp
[Fact]
public async Task PayPaymentAsync_WithZeroTransactionId_ShouldThrowValidationException()
{
    // arrange valid payable
    // act with TransactionId = 0
    // assert validation message contains "必须关联交易记录"
}
```

- [ ] **Step 3: Run the targeted tests to verify they fail**

Run: `dotnet test backend/tests/FinanceApp.Application.Tests/FinanceApp.Application.Tests.csproj --filter "ReceivePaymentAsync_WithZeroTransactionId_ShouldThrowValidationException|PayPaymentAsync_WithZeroTransactionId_ShouldThrowValidationException"`

Expected: FAIL because current validation treats `0` as a lookup value instead of a missing binding.

### Task 2: Tighten Backend Contract And Validation

**Files:**
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Receivable/ReceivePaymentRequest.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/DTOs/Payable/PayPaymentRequest.cs`
- Modify: `backend/FinanceApp.Domain/Entities/ReceivableDetail.cs`
- Modify: `backend/FinanceApp.Domain/Entities/PayableDetail.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/ISettlementTransactionBindingService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/SettlementTransactionBindingService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/PayableService.cs`

- [ ] **Step 1: Make DTO and entity `TransactionId` non-nullable**

```csharp
public long TransactionId { get; set; }
```

- [ ] **Step 2: Update validation signatures to use `long transactionId`**

```csharp
Task ValidateReceivableBindingAsync(long transactionId, decimal amount, ...);
Task ValidatePayableBindingAsync(long transactionId, decimal amount, ...);
```

- [ ] **Step 3: Reject sentinel/invalid ids explicitly**

```csharp
if (transactionId <= 0)
{
    throw new ValidationException("收款登记必须关联交易记录");
}
```

- [ ] **Step 4: Remove nullable-only call sites**

```csharp
await _transactionAllocationHelper.UpdateAllocationStatusAsync(request.TransactionId, saveChanges: false);
```

- [ ] **Step 5: Re-run the targeted tests**

Run: `dotnet test backend/tests/FinanceApp.Application.Tests/FinanceApp.Application.Tests.csproj --filter "ReceivePaymentAsync_WithZeroTransactionId_ShouldThrowValidationException|PayPaymentAsync_WithZeroTransactionId_ShouldThrowValidationException"`

Expected: PASS

### Task 3: Enforce Schema Constraint Safely

**Files:**
- Modify: `backend/FinanceApp.Infrastructure/Data/Configurations/ReceivableDetailConfiguration.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/Configurations/PayableDetailConfiguration.cs`
- Create: `backend/FinanceApp.Infrastructure/Data/Migrations/<timestamp>_MakeSettlementTransactionIdRequired.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/Migrations/<timestamp>_MakeSettlementTransactionIdRequired.Designer.cs`
- Modify: `backend/FinanceApp.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 1: Mark settlement `transaction_id` columns as required in EF**

```csharp
builder.Property(e => e.TransactionId)
    .HasColumnName("transaction_id")
    .IsRequired();
```

- [ ] **Step 2: Generate a migration against the updated model**

Run: `dotnet ef migrations add MakeSettlementTransactionIdRequired --project backend/FinanceApp.Infrastructure --startup-project backend/FinanceApp.Api`

Expected: new migration files created

- [ ] **Step 3: Add safety checks to the migration**

```csharp
migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM receivable_details WHERE transaction_id IS NULL AND is_deleted = false) THEN
        RAISE EXCEPTION 'Cannot make receivable_details.transaction_id NOT NULL while null active rows exist';
    END IF;
    IF EXISTS (SELECT 1 FROM payable_details WHERE transaction_id IS NULL AND is_deleted = false) THEN
        RAISE EXCEPTION 'Cannot make payable_details.transaction_id NOT NULL while null active rows exist';
    END IF;
END $$;
""");
```

- [ ] **Step 4: Alter both columns to `NOT NULL`**

### Task 4: Align Frontend Types And Form UX

**Files:**
- Modify: `frontend/src/features/finance/types/receivable.ts`
- Modify: `frontend/src/features/finance/types/payable.ts`
- Modify: `frontend/src/features/finance/pages/ReceivableDetailPage.vue`
- Modify: `frontend/src/features/finance/pages/PayableDetailPage.vue`
- Modify: `frontend/src/features/finance/components/PayableDetailContent.vue`

- [ ] **Step 1: Make API request/detail types require `transactionId`**

```ts
transactionId: number
```

- [ ] **Step 2: Use `0` as the local unselected sentinel in forms**

```ts
transactionId: 0
```

- [ ] **Step 3: Tighten frontend rules so `transactionId` must be `>= 1`**

```ts
transactionId: [{ required: true, type: 'number', min: 1, message: '请选择交易', trigger: 'change' }]
```

- [ ] **Step 4: Remove payable dropdown clearability**

```vue
<el-select v-model="paymentForm.transactionId" filterable ...>
```

- [ ] **Step 5: Run frontend type-check**

Run: `npm run type-check`
Workdir: `frontend`
Expected: PASS

### Task 5: Update Documentation And Final Verification

**Files:**
- Modify: `docs/FIXES_SUMMARY.md`
- Modify: `docs/PENDING_FIXES.md`

- [ ] **Step 1: Update fix summary to mark issue 3 implemented**

- [ ] **Step 2: Replace pending-fix guidance with migration/deployment notes or remove stale pending state**

- [ ] **Step 3: Run focused backend verification**

Run: `dotnet test backend/tests/FinanceApp.Application.Tests/FinanceApp.Application.Tests.csproj --filter "ReceivePaymentAsync_WithZeroTransactionId_ShouldThrowValidationException|PayPaymentAsync_WithZeroTransactionId_ShouldThrowValidationException|ReceivePaymentAsync_WithNullTransactionId_ShouldThrowValidationException|PayPaymentAsync_WithNullTransactionId_ShouldThrowValidationException"`

Expected: PASS

- [ ] **Step 4: Run frontend type-check**

Run: `npm run type-check`
Workdir: `frontend`
Expected: PASS
