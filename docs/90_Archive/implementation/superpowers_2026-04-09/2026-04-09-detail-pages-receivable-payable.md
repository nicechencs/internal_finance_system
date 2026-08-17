# Detail Pages Receivable & Payable Enhancement Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enhance all 4 detail pages (Customer, Supplier, Person, Project) to display both receivable and payable summaries and record tabs.

**Architecture:** Each detail page gets a unified finance summary (receivable + payable) and two new tabs for receivable/payable records. Backend adds entity-filtered endpoints on ReceivablesController/PayablesController, plus upgrades finance summary DTOs to include both dimensions. Frontend adds corresponding API calls, types, and tab components.

**Tech Stack:** .NET 8 (EF Core), Vue 3 + TypeScript, Element Plus

---

## File Structure

### Backend - New/Modified Files

| File | Action | Responsibility |
|------|--------|----------------|
| `backend/.../FinanceSettlement/Interfaces/IReceivableService.cs` | Modify | Add `GetByCustomerIdAsync`, `GetBySupplierIdAsync`, `GetByPersonIdAsync` |
| `backend/.../FinanceSettlement/Services/ReceivableService.cs` | Modify | Implement 3 new GetBy methods (follow `GetByProjectIdAsync` pattern) |
| `backend/.../FinanceSettlement/Interfaces/IPayableService.cs` | Modify | Add `GetByCustomerIdAsync`, `GetBySupplierIdAsync`, `GetByPersonIdAsync`, `GetByProjectIdAsync` |
| `backend/.../FinanceSettlement/Services/PayableService.cs` | Modify | Implement 4 new GetBy methods |
| `backend/.../Api/Controllers/FinanceSettlement/ReceivablesController.cs` | Modify | Add 3 endpoints: customer/{id}, supplier/{id}, person/{id} |
| `backend/.../Api/Controllers/FinanceSettlement/PayablesController.cs` | Modify | Add 4 endpoints: customer/{id}, supplier/{id}, person/{id}, project/{id} |
| `backend/.../MasterData/DTOs/Customer/CustomerFinanceSummaryDto.cs` | Modify | Add payable fields |
| `backend/.../MasterData/DTOs/Supplier/SupplierFinanceSummaryDto.cs` | Modify | Add receivable fields |
| `backend/.../MasterData/DTOs/Person/PersonFinanceSummaryDto.cs` | Modify | Add receivable + payable fields |
| `backend/.../MasterData/Services/CustomerService.cs` | Modify | `GetFinanceSummaryAsync` adds payable aggregation |
| `backend/.../MasterData/Services/SupplierService.cs` | Modify | `GetFinanceSummaryAsync` adds receivable aggregation |
| `backend/.../MasterData/Services/PersonService.cs` | Modify | `GetFinanceSummaryAsync` adds receivable + payable aggregation |

### Frontend - New/Modified Files

| File | Action | Responsibility |
|------|--------|----------------|
| `frontend/src/features/finance/api/receivable.ts` | Modify | Add `getReceivablesByCustomer`, `getReceivablesBySupplier`, `getReceivablesByPerson` |
| `frontend/src/features/finance/api/payable.ts` | Modify | Add `getPayablesByCustomer`, `getPayablesBySupplier`, `getPayablesByPerson`, `getPayablesByProject` |
| `frontend/src/features/master-data/customers/types/customer.ts` | Modify | Extend `CustomerFinanceSummary` with payable fields |
| `frontend/src/features/master-data/suppliers/types/supplier.ts` | Modify | Extend `SupplierFinanceSummary` with receivable fields |
| `frontend/src/features/master-data/persons/types/person.ts` | Modify | Extend `PersonFinanceSummary` with receivable + payable fields |
| `frontend/src/shared/ui/ReceivableRecordsTable.vue` | Create | Reusable receivable records table component |
| `frontend/src/shared/ui/PayableRecordsTable.vue` | Create | Reusable payable records table component |
| `frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue` | Modify | Add payable summary cards + receivable/payable tabs |
| `frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue` | Modify | Add receivable summary cards + receivable/payable tabs |
| `frontend/src/features/master-data/persons/pages/PersonDetailPage.vue` | Modify | Replace cost summary with receivable/payable summary + tabs |
| `frontend/src/features/master-data/projects/pages/ProjectDetailPage.vue` | Modify | Add payable tab + payable summary in overview |

---

## Task 1: Backend - Add Receivable GetBy Entity Methods

**Files:**
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IReceivableService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableService.cs`

- [ ] **Step 1: Add interface methods**

In `IReceivableService.cs`, add after line 13 (`Task<List<ReceivableDto>> GetByProjectIdAsync(long projectId);`):

```csharp
    Task<List<ReceivableDto>> GetByCustomerIdAsync(long customerId);
    Task<List<ReceivableDto>> GetBySupplierIdAsync(long supplierId);
    Task<List<ReceivableDto>> GetByPersonIdAsync(long personId);
```

- [ ] **Step 2: Implement GetByCustomerIdAsync**

In `ReceivableService.cs`, add after the `GetByProjectIdAsync` method (after line 698):

```csharp
    public async Task<List<ReceivableDto>> GetByCustomerIdAsync(long customerId)
    {
        _logger.LogInformation("[ReceivableService.GetByCustomerIdAsync] CustomerId={CustomerId}", customerId);

        var query = ApplyPermissionFilter(
            IncludeAll(_receivableRepository.GetQueryable())
                .Where(r => r.CustomerId == customerId));

        var receivables = await query
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<ReceivableDto>>(receivables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Receivable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[ReceivableService.GetByCustomerIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }
```

- [ ] **Step 3: Implement GetBySupplierIdAsync**

In `ReceivableService.cs`, add after `GetByCustomerIdAsync`:

```csharp
    public async Task<List<ReceivableDto>> GetBySupplierIdAsync(long supplierId)
    {
        _logger.LogInformation("[ReceivableService.GetBySupplierIdAsync] SupplierId={SupplierId}", supplierId);

        var query = ApplyPermissionFilter(
            IncludeAll(_receivableRepository.GetQueryable())
                .Where(r => r.SupplierId == supplierId));

        var receivables = await query
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<ReceivableDto>>(receivables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Receivable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[ReceivableService.GetBySupplierIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }
```

- [ ] **Step 4: Implement GetByPersonIdAsync**

In `ReceivableService.cs`, add after `GetBySupplierIdAsync`:

```csharp
    public async Task<List<ReceivableDto>> GetByPersonIdAsync(long personId)
    {
        _logger.LogInformation("[ReceivableService.GetByPersonIdAsync] PersonId={PersonId}", personId);

        var query = ApplyPermissionFilter(
            IncludeAll(_receivableRepository.GetQueryable())
                .Where(r => r.PersonId == personId));

        var receivables = await query
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<ReceivableDto>>(receivables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Receivable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[ReceivableService.GetByPersonIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }
```

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build backend/FinanceApp.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IReceivableService.cs backend/FinanceApp.Application/Modules/FinanceSettlement/Services/ReceivableService.cs
git commit -m "feat: add receivable GetBy methods for customer/supplier/person"
```

---

## Task 2: Backend - Add Payable GetBy Entity Methods

**Files:**
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IPayableService.cs`
- Modify: `backend/FinanceApp.Application/Modules/FinanceSettlement/Services/PayableService.cs`

- [ ] **Step 1: Add interface methods**

In `IPayableService.cs`, add after the existing method declarations:

```csharp
    Task<List<PayableDto>> GetByCustomerIdAsync(long customerId);
    Task<List<PayableDto>> GetBySupplierIdAsync(long supplierId);
    Task<List<PayableDto>> GetByPersonIdAsync(long personId);
    Task<List<PayableDto>> GetByProjectIdAsync(long projectId);
```

- [ ] **Step 2: Implement all 4 GetBy methods**

In `PayableService.cs`, add before the closing brace of the class. Follow the same pattern as ReceivableService — use `ApplyPermissionFilter`, `IncludeAll`, tag binding, and logging. Check if `IncludeAll` and `ApplyPermissionFilter` exist in PayableService; if not, use the direct queryable pattern with `.Include(p => p.Supplier).Include(p => p.Customer).Include(p => p.Person).Include(p => p.Project).Include(p => p.PayableType).Include(p => p.Details)`:

```csharp
    public async Task<List<PayableDto>> GetByCustomerIdAsync(long customerId)
    {
        _logger.LogInformation("[PayableService.GetByCustomerIdAsync] CustomerId={CustomerId}", customerId);

        var query = ApplyPermissionFilter(
            IncludeAll(_payableRepository.GetQueryable())
                .Where(p => p.CustomerId == customerId));

        var payables = await query
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<PayableDto>>(payables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Payable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[PayableService.GetByCustomerIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }

    public async Task<List<PayableDto>> GetBySupplierIdAsync(long supplierId)
    {
        _logger.LogInformation("[PayableService.GetBySupplierIdAsync] SupplierId={SupplierId}", supplierId);

        var query = ApplyPermissionFilter(
            IncludeAll(_payableRepository.GetQueryable())
                .Where(p => p.SupplierId == supplierId));

        var payables = await query
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<PayableDto>>(payables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Payable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[PayableService.GetBySupplierIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }

    public async Task<List<PayableDto>> GetByPersonIdAsync(long personId)
    {
        _logger.LogInformation("[PayableService.GetByPersonIdAsync] PersonId={PersonId}", personId);

        var query = ApplyPermissionFilter(
            IncludeAll(_payableRepository.GetQueryable())
                .Where(p => p.PersonId == personId));

        var payables = await query
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<PayableDto>>(payables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Payable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[PayableService.GetByPersonIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }

    public async Task<List<PayableDto>> GetByProjectIdAsync(long projectId)
    {
        _logger.LogInformation("[PayableService.GetByProjectIdAsync] ProjectId={ProjectId}", projectId);

        var query = ApplyPermissionFilter(
            IncludeAll(_payableRepository.GetQueryable())
                .Where(p => p.ProjectId == projectId));

        var payables = await query
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        var result = _mapper.Map<List<PayableDto>>(payables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Payable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[PayableService.GetByProjectIdAsync] 成功, Count={Count}", result.Count);
        return result;
    }
```

Note: If `PayableService` does not have `IncludeAll` or `ApplyPermissionFilter` helper methods, check how `GetPagedAsync` loads related entities and replicate that pattern. The key is to include all navigation properties and apply tag bindings.

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build backend/FinanceApp.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add backend/FinanceApp.Application/Modules/FinanceSettlement/Interfaces/IPayableService.cs backend/FinanceApp.Application/Modules/FinanceSettlement/Services/PayableService.cs
git commit -m "feat: add payable GetBy methods for customer/supplier/person/project"
```

---

## Task 3: Backend - Add Controller Endpoints

**Files:**
- Modify: `backend/FinanceApp.Api/Controllers/FinanceSettlement/ReceivablesController.cs`
- Modify: `backend/FinanceApp.Api/Controllers/FinanceSettlement/PayablesController.cs`

- [ ] **Step 1: Add 3 receivable endpoints**

In `ReceivablesController.cs`, add after the `GetByProject` endpoint (after line 77):

```csharp
    [HttpGet("customer/{customerId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetByCustomer(long customerId)
    {
        Logger.LogInformation("[ReceivablesController.GetByCustomer] CustomerId={CustomerId}", customerId);
        var result = await _receivableService.GetByCustomerIdAsync(customerId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("supplier/{supplierId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetBySupplier(long supplierId)
    {
        Logger.LogInformation("[ReceivablesController.GetBySupplier] SupplierId={SupplierId}", supplierId);
        var result = await _receivableService.GetBySupplierIdAsync(supplierId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("person/{personId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetByPerson(long personId)
    {
        Logger.LogInformation("[ReceivablesController.GetByPerson] PersonId={PersonId}", personId);
        var result = await _receivableService.GetByPersonIdAsync(personId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }
```

- [ ] **Step 2: Add 4 payable endpoints**

In `PayablesController.cs`, add after the existing endpoints (before the closing brace of the class):

```csharp
    [HttpGet("customer/{customerId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetByCustomer(long customerId)
    {
        Logger.LogInformation("[PayablesController.GetByCustomer] CustomerId={CustomerId}", customerId);
        var result = await _payableService.GetByCustomerIdAsync(customerId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("supplier/{supplierId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetBySupplier(long supplierId)
    {
        Logger.LogInformation("[PayablesController.GetBySupplier] SupplierId={SupplierId}", supplierId);
        var result = await _payableService.GetBySupplierIdAsync(supplierId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("person/{personId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetByPerson(long personId)
    {
        Logger.LogInformation("[PayablesController.GetByPerson] PersonId={PersonId}", personId);
        var result = await _payableService.GetByPersonIdAsync(personId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("project/{projectId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetByProject(long projectId)
    {
        Logger.LogInformation("[PayablesController.GetByProject] ProjectId={ProjectId}", projectId);
        var result = await _payableService.GetByProjectIdAsync(projectId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build backend/FinanceApp.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add backend/FinanceApp.Api/Controllers/FinanceSettlement/ReceivablesController.cs backend/FinanceApp.Api/Controllers/FinanceSettlement/PayablesController.cs
git commit -m "feat: add receivable/payable controller endpoints for customer/supplier/person/project"
```

---

## Task 4: Backend - Upgrade Finance Summary DTOs and Services

**Files:**
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Customer/CustomerFinanceSummaryDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Supplier/SupplierFinanceSummaryDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/DTOs/Person/PersonFinanceSummaryDto.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Services/CustomerService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Services/SupplierService.cs`
- Modify: `backend/FinanceApp.Application/Modules/MasterData/Services/PersonService.cs`

- [ ] **Step 1: Upgrade CustomerFinanceSummaryDto**

Replace the entire file content of `CustomerFinanceSummaryDto.cs`:

```csharp
namespace FinanceApp.Application.Modules.MasterData.DTOs.Customer;

public class CustomerFinanceSummaryDto
{
    // Receivable
    public decimal TotalReceivable { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal ReceivableRemaining { get; set; }
    public int ReceivableOverdueCount { get; set; }
    public decimal ReceivableOverdueAmount { get; set; }

    // Payable
    public decimal TotalPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PayableRemaining { get; set; }
    public int PayableOverdueCount { get; set; }
    public decimal PayableOverdueAmount { get; set; }

    public int ProjectCount { get; set; }
}
```

- [ ] **Step 2: Upgrade SupplierFinanceSummaryDto**

Replace the entire file content of `SupplierFinanceSummaryDto.cs`:

```csharp
namespace FinanceApp.Application.Modules.MasterData.DTOs.Supplier;

public class SupplierFinanceSummaryDto
{
    // Receivable
    public decimal TotalReceivable { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal ReceivableRemaining { get; set; }
    public int ReceivableOverdueCount { get; set; }
    public decimal ReceivableOverdueAmount { get; set; }

    // Payable
    public decimal TotalPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PayableRemaining { get; set; }
    public int PayableOverdueCount { get; set; }
    public decimal PayableOverdueAmount { get; set; }

    public int ProjectCount { get; set; }
}
```

- [ ] **Step 3: Upgrade PersonFinanceSummaryDto**

Replace the entire file content of `PersonFinanceSummaryDto.cs`:

```csharp
namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class PersonFinanceSummaryDto
{
    // Cost (existing)
    public decimal TotalCost { get; set; }
    public decimal DirectCost { get; set; }
    public decimal AllocatedCost { get; set; }
    public int TransactionCount { get; set; }

    // Receivable
    public decimal TotalReceivable { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal ReceivableRemaining { get; set; }
    public int ReceivableOverdueCount { get; set; }
    public decimal ReceivableOverdueAmount { get; set; }

    // Payable
    public decimal TotalPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PayableRemaining { get; set; }
    public int PayableOverdueCount { get; set; }
    public decimal PayableOverdueAmount { get; set; }

    public int ProjectCount { get; set; }
}
```

- [ ] **Step 4: Upgrade CustomerService.GetFinanceSummaryAsync**

Replace the `GetFinanceSummaryAsync` method in `CustomerService.cs` (lines 417-463):

```csharp
    public async Task<CustomerFinanceSummaryDto> GetFinanceSummaryAsync(long customerId)
    {
        _logger.LogDebug("CustomerService.GetFinanceSummaryAsync - CustomerId={CustomerId}", customerId);

        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
        {
            _logger.LogWarning("客户不存在: Id={Id}", customerId);
            throw new NotFoundException("客户不存在");
        }

        var today = DateTime.UtcNow.Date;

        // Receivable aggregation
        var receivableQuery = _receivableRepository.GetQueryable()
            .Where(r => r.CustomerId == customerId);

        var receivableSummary = await receivableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalReceivable = g.Sum(r => r.TotalAmount),
                TotalReceived = g.Sum(r => r.ReceivedAmount),
                ReceivableRemaining = g.Sum(r => r.RemainingAmount),
                OverdueCount = g.Count(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today),
                OverdueAmount = g.Where(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today).Sum(r => r.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Payable aggregation
        var payableQuery = _payableRepository.GetQueryable()
            .Where(p => p.CustomerId == customerId);

        var payableSummary = await payableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPayable = g.Sum(p => p.TotalAmount),
                TotalPaid = g.Sum(p => p.PaidAmount),
                PayableRemaining = g.Sum(p => p.RemainingAmount),
                OverdueCount = g.Count(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today),
                OverdueAmount = g.Where(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today).Sum(p => p.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Project count from both receivables and payables
        var receivableProjectIds = await receivableQuery
            .Where(r => r.ProjectId != 0)
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync();

        var payableProjectIds = await payableQuery
            .Where(p => p.ProjectId.HasValue)
            .Select(p => p.ProjectId!.Value)
            .Distinct()
            .ToListAsync();

        var projectCount = receivableProjectIds.Union(payableProjectIds).Distinct().Count();

        var result = new CustomerFinanceSummaryDto
        {
            TotalReceivable = receivableSummary?.TotalReceivable ?? 0,
            TotalReceived = receivableSummary?.TotalReceived ?? 0,
            ReceivableRemaining = receivableSummary?.ReceivableRemaining ?? 0,
            ReceivableOverdueCount = receivableSummary?.OverdueCount ?? 0,
            ReceivableOverdueAmount = receivableSummary?.OverdueAmount ?? 0,
            TotalPayable = payableSummary?.TotalPayable ?? 0,
            TotalPaid = payableSummary?.TotalPaid ?? 0,
            PayableRemaining = payableSummary?.PayableRemaining ?? 0,
            PayableOverdueCount = payableSummary?.OverdueCount ?? 0,
            PayableOverdueAmount = payableSummary?.OverdueAmount ?? 0,
            ProjectCount = projectCount
        };

        _logger.LogInformation("查询客户财务汇总成功: CustomerId={CustomerId}, TotalReceivable={TotalReceivable}, TotalPayable={TotalPayable}",
            customerId, result.TotalReceivable, result.TotalPayable);

        return result;
    }
```

Note: `CustomerService` must inject `IRepository<Payable> _payableRepository`. Check if it's already injected — if not, add it to the constructor. You will need `using FinanceApp.Domain.Entities;` and `using FinanceApp.Domain.Enums;` (for `PayableStatus`).

- [ ] **Step 5: Upgrade SupplierService.GetFinanceSummaryAsync**

Replace the `GetFinanceSummaryAsync` method in `SupplierService.cs` (lines 447-494). Follow the same pattern as CustomerService above but with supplier-scoped queries:

```csharp
    public async Task<SupplierFinanceSummaryDto> GetFinanceSummaryAsync(long supplierId)
    {
        _logger.LogDebug("SupplierService.GetFinanceSummaryAsync - SupplierId={SupplierId}", supplierId);

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
        {
            _logger.LogWarning("供应商不存在: Id={Id}", supplierId);
            throw new NotFoundException("供应商不存在");
        }

        var today = DateTime.UtcNow.Date;

        // Receivable aggregation
        var receivableQuery = _receivableRepository.GetQueryable()
            .Where(r => r.SupplierId == supplierId);

        var receivableSummary = await receivableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalReceivable = g.Sum(r => r.TotalAmount),
                TotalReceived = g.Sum(r => r.ReceivedAmount),
                ReceivableRemaining = g.Sum(r => r.RemainingAmount),
                OverdueCount = g.Count(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today),
                OverdueAmount = g.Where(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today).Sum(r => r.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Payable aggregation (existing logic)
        var payableQuery = _payableRepository.GetQueryable()
            .Where(p => p.SupplierId == supplierId);

        var payableSummary = await payableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPayable = g.Sum(p => p.TotalAmount),
                TotalPaid = g.Sum(p => p.PaidAmount),
                PayableRemaining = g.Sum(p => p.RemainingAmount),
                OverdueCount = g.Count(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today),
                OverdueAmount = g.Where(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today).Sum(p => p.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        var receivableProjectIds = await receivableQuery
            .Where(r => r.ProjectId != 0)
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync();

        var payableProjectIds = await payableQuery
            .Where(p => p.ProjectId.HasValue)
            .Select(p => p.ProjectId!.Value)
            .Distinct()
            .ToListAsync();

        var projectCount = receivableProjectIds.Union(payableProjectIds).Distinct().Count();

        var result = new SupplierFinanceSummaryDto
        {
            TotalReceivable = receivableSummary?.TotalReceivable ?? 0,
            TotalReceived = receivableSummary?.TotalReceived ?? 0,
            ReceivableRemaining = receivableSummary?.ReceivableRemaining ?? 0,
            ReceivableOverdueCount = receivableSummary?.OverdueCount ?? 0,
            ReceivableOverdueAmount = receivableSummary?.OverdueAmount ?? 0,
            TotalPayable = payableSummary?.TotalPayable ?? 0,
            TotalPaid = payableSummary?.TotalPaid ?? 0,
            PayableRemaining = payableSummary?.PayableRemaining ?? 0,
            PayableOverdueCount = payableSummary?.OverdueCount ?? 0,
            PayableOverdueAmount = payableSummary?.OverdueAmount ?? 0,
            ProjectCount = projectCount
        };

        _logger.LogInformation("查询供应商财务汇总成功: SupplierId={SupplierId}, TotalReceivable={TotalReceivable}, TotalPayable={TotalPayable}",
            supplierId, result.TotalReceivable, result.TotalPayable);

        return result;
    }
```

Note: `SupplierService` must inject `IRepository<Receivable> _receivableRepository`. Check if it's already injected — if not, add it to the constructor.

- [ ] **Step 6: Upgrade PersonService.GetFinanceSummaryAsync**

Replace the `GetFinanceSummaryAsync` method in `PersonService.cs` (lines 473-532). Keep the existing cost calculation, add receivable and payable:

```csharp
    public async Task<PersonFinanceSummaryDto> GetFinanceSummaryAsync(long personId)
    {
        _logger.LogDebug("PersonService.GetFinanceSummaryAsync - PersonId={PersonId}", personId);

        var person = await _personRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (person == null)
        {
            _logger.LogWarning("人员不存在: PersonId={PersonId}", personId);
            throw new NotFoundException("人员不存在");
        }

        var today = DateTime.UtcNow.Date;

        // Direct cost (existing)
        var directTransactions = await _transactionRepository.GetQueryable()
            .Where(t => t.PersonId == personId &&
                       !t.IsAllocated &&
                       t.TransactionType == TransactionType.Expense)
            .ToListAsync();
        var directCost = directTransactions.Sum(t => t.Amount);

        // Allocated cost (existing)
        var allocations = await _allocationRepository.GetQueryable()
            .Include(a => a.Transaction)
            .Where(a => a.PersonId == personId &&
                       a.Transaction.TransactionType == TransactionType.Expense)
            .ToListAsync();
        var allocatedCost = allocations.Sum(a => a.Amount);

        // Receivable aggregation
        var receivableQuery = _receivableRepository.GetQueryable()
            .Where(r => r.PersonId == personId);

        var receivableSummary = await receivableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalReceivable = g.Sum(r => r.TotalAmount),
                TotalReceived = g.Sum(r => r.ReceivedAmount),
                ReceivableRemaining = g.Sum(r => r.RemainingAmount),
                OverdueCount = g.Count(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today),
                OverdueAmount = g.Where(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today).Sum(r => r.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Payable aggregation
        var payableQuery = _payableRepository.GetQueryable()
            .Where(p => p.PersonId == personId);

        var payableSummary = await payableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPayable = g.Sum(p => p.TotalAmount),
                TotalPaid = g.Sum(p => p.PaidAmount),
                PayableRemaining = g.Sum(p => p.RemainingAmount),
                OverdueCount = g.Count(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today),
                OverdueAmount = g.Where(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today).Sum(p => p.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Project count from all sources
        var txProjectIds = directTransactions
            .Where(t => t.ProjectId.HasValue)
            .Select(t => t.ProjectId!.Value)
            .Concat(allocations
                .Where(a => a.Transaction.ProjectId.HasValue)
                .Select(a => a.Transaction.ProjectId!.Value));

        var receivableProjectIds = await receivableQuery
            .Where(r => r.ProjectId != 0)
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync();

        var payableProjectIds = await payableQuery
            .Where(p => p.ProjectId.HasValue)
            .Select(p => p.ProjectId!.Value)
            .Distinct()
            .ToListAsync();

        var projectCount = txProjectIds
            .Concat(receivableProjectIds.Select(id => id))
            .Concat(payableProjectIds)
            .Distinct()
            .Count();

        var totalCount = directTransactions.Count + allocations.Count;

        var result = new PersonFinanceSummaryDto
        {
            TotalCost = directCost + allocatedCost,
            DirectCost = directCost,
            AllocatedCost = allocatedCost,
            TransactionCount = totalCount,
            TotalReceivable = receivableSummary?.TotalReceivable ?? 0,
            TotalReceived = receivableSummary?.TotalReceived ?? 0,
            ReceivableRemaining = receivableSummary?.ReceivableRemaining ?? 0,
            ReceivableOverdueCount = receivableSummary?.OverdueCount ?? 0,
            ReceivableOverdueAmount = receivableSummary?.OverdueAmount ?? 0,
            TotalPayable = payableSummary?.TotalPayable ?? 0,
            TotalPaid = payableSummary?.TotalPaid ?? 0,
            PayableRemaining = payableSummary?.PayableRemaining ?? 0,
            PayableOverdueCount = payableSummary?.OverdueCount ?? 0,
            PayableOverdueAmount = payableSummary?.OverdueAmount ?? 0,
            ProjectCount = projectCount
        };

        _logger.LogInformation("查询人员财务汇总成功: PersonId={PersonId}, TotalCost={TotalCost}, TotalReceivable={TotalReceivable}, TotalPayable={TotalPayable}",
            personId, result.TotalCost, result.TotalReceivable, result.TotalPayable);

        return result;
    }
```

Note: `PersonService` must inject `IRepository<Receivable> _receivableRepository` if not already present. Check the constructor and add if needed.

- [ ] **Step 7: Build to verify**

Run: `dotnet build backend/FinanceApp.sln`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add backend/FinanceApp.Application/Modules/MasterData/
git commit -m "feat: upgrade finance summary DTOs to include both receivable and payable data"
```

---

## Task 5: Frontend - Add API Methods

**Files:**
- Modify: `frontend/src/features/finance/api/receivable.ts`
- Modify: `frontend/src/features/finance/api/payable.ts`

- [ ] **Step 1: Add receivable entity API methods**

In `frontend/src/features/finance/api/receivable.ts`, add after the `getReceivablesByProject` export (after line 44):

```typescript
export const getReceivablesByCustomer = (customerId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/customer/${customerId}`, method: 'get' })

export const getReceivablesBySupplier = (supplierId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/supplier/${supplierId}`, method: 'get' })

export const getReceivablesByPerson = (personId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/person/${personId}`, method: 'get' })
```

- [ ] **Step 2: Add payable entity API methods**

In `frontend/src/features/finance/api/payable.ts`, add after the `getPayableAging` export (after line 41):

```typescript
export const getPayablesByCustomer = (customerId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/customer/${customerId}`, method: 'get' })

export const getPayablesBySupplier = (supplierId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/supplier/${supplierId}`, method: 'get' })

export const getPayablesByPerson = (personId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/person/${personId}`, method: 'get' })

export const getPayablesByProject = (projectId: number) =>
  request<ApiResponse<Payable[]>>({ url: `/payables/project/${projectId}`, method: 'get' })
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/finance/api/receivable.ts frontend/src/features/finance/api/payable.ts
git commit -m "feat: add receivable/payable API methods for entity filtering"
```

---

## Task 6: Frontend - Update Type Definitions

**Files:**
- Modify: `frontend/src/features/master-data/customers/types/customer.ts`
- Modify: `frontend/src/features/master-data/suppliers/types/supplier.ts`
- Modify: `frontend/src/features/master-data/persons/types/person.ts`

- [ ] **Step 1: Update CustomerFinanceSummary**

In `frontend/src/features/master-data/customers/types/customer.ts`, replace the `CustomerFinanceSummary` interface (lines 51-58):

```typescript
export interface CustomerFinanceSummary {
  totalReceivable: number
  totalReceived: number
  receivableRemaining: number
  receivableOverdueCount: number
  receivableOverdueAmount: number
  totalPayable: number
  totalPaid: number
  payableRemaining: number
  payableOverdueCount: number
  payableOverdueAmount: number
  projectCount: number
}
```

- [ ] **Step 2: Update SupplierFinanceSummary**

In `frontend/src/features/master-data/suppliers/types/supplier.ts`, replace the `SupplierFinanceSummary` interface (lines 51-58):

```typescript
export interface SupplierFinanceSummary {
  totalReceivable: number
  totalReceived: number
  receivableRemaining: number
  receivableOverdueCount: number
  receivableOverdueAmount: number
  totalPayable: number
  totalPaid: number
  payableRemaining: number
  payableOverdueCount: number
  payableOverdueAmount: number
  projectCount: number
}
```

- [ ] **Step 3: Update PersonFinanceSummary**

In `frontend/src/features/master-data/persons/types/person.ts`, replace the `PersonFinanceSummary` interface (lines 53-60):

```typescript
export interface PersonFinanceSummary {
  totalCost: number
  directCost: number
  allocatedCost: number
  transactionCount: number
  totalReceivable: number
  totalReceived: number
  receivableRemaining: number
  receivableOverdueCount: number
  receivableOverdueAmount: number
  totalPayable: number
  totalPaid: number
  payableRemaining: number
  payableOverdueCount: number
  payableOverdueAmount: number
  projectCount: number
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/master-data/customers/types/customer.ts frontend/src/features/master-data/suppliers/types/supplier.ts frontend/src/features/master-data/persons/types/person.ts
git commit -m "feat: update finance summary types to include receivable and payable fields"
```

---

## Task 7: Frontend - Create Reusable Record Table Components

**Files:**
- Create: `frontend/src/shared/ui/ReceivableRecordsTable.vue`
- Create: `frontend/src/shared/ui/PayableRecordsTable.vue`

- [ ] **Step 1: Create ReceivableRecordsTable.vue**

Create `frontend/src/shared/ui/ReceivableRecordsTable.vue`:

```vue
<template>
  <div>
    <div class="records-summary mb-4">
      <el-descriptions :column="4" border size="small">
        <el-descriptions-item label="应收总额">
          <span class="amount-primary">{{ formatCurrency(summary.totalAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="已收金额">
          <span class="amount-success">{{ formatCurrency(summary.receivedAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="未收金额">
          <span class="amount-warning">{{ formatCurrency(summary.remainingAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="逾期">
          <span :class="summary.overdueCount > 0 ? 'amount-danger' : ''">
            {{ summary.overdueCount }} 笔
          </span>
        </el-descriptions-item>
      </el-descriptions>
    </div>
    <el-table :data="records" v-loading="loading" border>
      <el-table-column prop="description" label="描述" min-width="150">
        <template #default="{ row }">{{ row.description || '-' }}</template>
      </el-table-column>
      <el-table-column prop="totalAmount" label="应收金额" width="140" align="right">
        <template #default="{ row }">{{ formatCurrency(row.totalAmount) }}</template>
      </el-table-column>
      <el-table-column prop="receivedAmount" label="已收金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-success">{{ formatCurrency(row.receivedAmount) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="remainingAmount" label="未收金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-warning">{{ formatCurrency(row.remainingAmount) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="dueDate" label="到期日" width="120">
        <template #default="{ row }">{{ row.dueDate ? formatDate(row.dueDate) : '-' }}</template>
      </el-table-column>
      <el-table-column prop="status" label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="settledAt" label="结算时间" width="120">
        <template #default="{ row }">{{ row.settledAt ? formatDate(row.settledAt) : '-' }}</template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Receivable } from '@/features/finance/types/receivable'
import { formatDateTime } from '@/shared/utils/formatters'
import { formatRMB } from '@/shared/utils/formatters'

const props = defineProps<{
  records: Receivable[]
  loading: boolean
}>()

const formatCurrency = (amount: number) => formatRMB(amount)
const formatDate = (date: string) => formatDateTime(date, 'date')

const summary = computed(() => {
  const today = new Date().toISOString().split('T')[0]
  return {
    totalAmount: props.records.reduce((sum, r) => sum + r.totalAmount, 0),
    receivedAmount: props.records.reduce((sum, r) => sum + r.receivedAmount, 0),
    remainingAmount: props.records.reduce((sum, r) => sum + r.remainingAmount, 0),
    overdueCount: props.records.filter(r => r.status !== 'settled' && r.dueDate && r.dueDate < today).length
  }
})

const getStatusType = (status: string) => {
  const map: Record<string, string> = { pending: 'warning', partial: 'info', settled: 'success' }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = { pending: '待收款', partial: '部分收款', settled: '已结清' }
  return map[status] || status
}
</script>

<style scoped>
.records-summary { margin-bottom: 16px; }
.amount-primary { color: var(--color-primary); font-weight: 600; }
.amount-success { color: var(--color-success); font-weight: 600; }
.amount-warning { color: var(--color-warning); font-weight: 600; }
.amount-danger { color: var(--color-danger); font-weight: 600; }
.mb-4 { margin-bottom: 16px; }
</style>
```

- [ ] **Step 2: Create PayableRecordsTable.vue**

Create `frontend/src/shared/ui/PayableRecordsTable.vue`:

```vue
<template>
  <div>
    <div class="records-summary mb-4">
      <el-descriptions :column="4" border size="small">
        <el-descriptions-item label="应付总额">
          <span class="amount-primary">{{ formatCurrency(summary.totalAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="已付金额">
          <span class="amount-success">{{ formatCurrency(summary.paidAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="未付金额">
          <span class="amount-warning">{{ formatCurrency(summary.remainingAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="逾期">
          <span :class="summary.overdueCount > 0 ? 'amount-danger' : ''">
            {{ summary.overdueCount }} 笔
          </span>
        </el-descriptions-item>
      </el-descriptions>
    </div>
    <el-table :data="records" v-loading="loading" border>
      <el-table-column prop="description" label="描述" min-width="150">
        <template #default="{ row }">{{ row.description || '-' }}</template>
      </el-table-column>
      <el-table-column prop="totalAmount" label="应付金额" width="140" align="right">
        <template #default="{ row }">{{ formatCurrency(row.totalAmount) }}</template>
      </el-table-column>
      <el-table-column prop="paidAmount" label="已付金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-success">{{ formatCurrency(row.paidAmount) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="remainingAmount" label="未付金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-warning">{{ formatCurrency(row.remainingAmount) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="dueDate" label="到期日" width="120">
        <template #default="{ row }">{{ row.dueDate ? formatDate(row.dueDate) : '-' }}</template>
      </el-table-column>
      <el-table-column prop="status" label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="settledAt" label="结算时间" width="120">
        <template #default="{ row }">{{ row.settledAt ? formatDate(row.settledAt) : '-' }}</template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Payable } from '@/features/finance/types/payable'
import { formatDateTime, formatRMB } from '@/shared/utils/formatters'

const props = defineProps<{
  records: Payable[]
  loading: boolean
}>()

const formatCurrency = (amount: number) => formatRMB(amount)
const formatDate = (date: string) => formatDateTime(date, 'date')

const summary = computed(() => {
  const today = new Date().toISOString().split('T')[0]
  return {
    totalAmount: props.records.reduce((sum, p) => sum + p.totalAmount, 0),
    paidAmount: props.records.reduce((sum, p) => sum + p.paidAmount, 0),
    remainingAmount: props.records.reduce((sum, p) => sum + p.remainingAmount, 0),
    overdueCount: props.records.filter(p => p.status !== 'settled' && p.dueDate && p.dueDate < today).length
  }
})

const getStatusType = (status: string) => {
  const map: Record<string, string> = { pending: 'warning', partial: 'info', settled: 'success' }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = { pending: '待付款', partial: '部分付款', settled: '已结清' }
  return map[status] || status
}
</script>

<style scoped>
.records-summary { margin-bottom: 16px; }
.amount-primary { color: var(--color-primary); font-weight: 600; }
.amount-success { color: var(--color-success); font-weight: 600; }
.amount-warning { color: var(--color-warning); font-weight: 600; }
.amount-danger { color: var(--color-danger); font-weight: 600; }
.mb-4 { margin-bottom: 16px; }
</style>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/shared/ui/ReceivableRecordsTable.vue frontend/src/shared/ui/PayableRecordsTable.vue
git commit -m "feat: create reusable ReceivableRecordsTable and PayableRecordsTable components"
```

---

## Task 8: Frontend - Update CustomerDetailPage

**Files:**
- Modify: `frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue`

- [ ] **Step 1: Update the finance summary cards**

Replace the `financeSummaryCards` computed (lines 227-236) and the finance summary `SummaryOverview` section (lines 75-83) in the template.

In the template, replace the single "财务概览" `SummaryOverview` block (lines 75-83) with two blocks:

```vue
      <SummaryOverview
        title="应收概览"
        subtitle="应收账款汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无应收数据"
      >
        <DetailSummaryCards :items="receivableSummaryCards" />
      </SummaryOverview>

      <SummaryOverview
        title="应付概览"
        subtitle="应付账款汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无应付数据"
      >
        <DetailSummaryCards :items="payableSummaryCards" />
      </SummaryOverview>
```

- [ ] **Step 2: Update the script — add imports and data**

Add new imports:

```typescript
import { getReceivablesByCustomer } from '@/features/finance/api/receivable'
import { getPayablesByCustomer } from '@/features/finance/api/payable'
import ReceivableRecordsTable from '@/shared/ui/ReceivableRecordsTable.vue'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import type { Receivable } from '@/features/finance/types/receivable'
import type { Payable } from '@/features/finance/types/payable'
```

Add reactive state:

```typescript
const receivables = ref<Receivable[]>([])
const receivablesLoading = ref(false)
const receivablesLoaded = ref(false)
const payables = ref<Payable[]>([])
const payablesLoading = ref(false)
const payablesLoaded = ref(false)
```

- [ ] **Step 3: Replace financeSummaryCards with two computed properties**

Remove the old `financeSummaryCards` computed and add:

```typescript
const receivableSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalReceivable', label: '应收总额', value: formatCurrency(s.totalReceivable), meta: `${s.projectCount} 个关联项目`, tone: 'balance' as const },
    { key: 'totalReceived', label: '已收金额', value: formatCurrency(s.totalReceived), tone: 'income' as const },
    { key: 'receivableRemaining', label: '未收余额', value: formatCurrency(s.receivableRemaining), tone: 'expense' as const },
    { key: 'receivableOverdue', label: '逾期', value: `${s.receivableOverdueCount} 笔`, meta: s.receivableOverdueAmount > 0 ? formatCurrency(s.receivableOverdueAmount) : undefined, tone: s.receivableOverdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})

const payableSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalPayable', label: '应付总额', value: formatCurrency(s.totalPayable), tone: 'balance' as const },
    { key: 'totalPaid', label: '已付金额', value: formatCurrency(s.totalPaid), tone: 'income' as const },
    { key: 'payableRemaining', label: '未付余额', value: formatCurrency(s.payableRemaining), tone: 'expense' as const },
    { key: 'payableOverdue', label: '逾期', value: `${s.payableOverdueCount} 笔`, meta: s.payableOverdueAmount > 0 ? formatCurrency(s.payableOverdueAmount) : undefined, tone: s.payableOverdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})
```

- [ ] **Step 4: Add tabs for receivable and payable records**

In the template, add two new `el-tab-pane` inside the `el-tabs` after the existing "交易记录" tab-pane (after line 161):

```vue
          <el-tab-pane label="应收记录" name="receivables">
            <ReceivableRecordsTable :records="receivables" :loading="receivablesLoading" />
          </el-tab-pane>
          <el-tab-pane label="应付记录" name="payables">
            <PayableRecordsTable :records="payables" :loading="payablesLoading" />
          </el-tab-pane>
```

- [ ] **Step 5: Add load functions and update handleTabChange**

```typescript
const loadReceivables = async () => {
  const id = Number(route.params.id)
  if (!id || receivablesLoaded.value) return
  receivablesLoading.value = true
  try {
    const { data } = await getReceivablesByCustomer(id)
    receivables.value = data.data
    receivablesLoaded.value = true
  } catch {
    ElMessage.error('加载应收记录失败')
  } finally {
    receivablesLoading.value = false
  }
}

const loadPayables = async () => {
  const id = Number(route.params.id)
  if (!id || payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const { data } = await getPayablesByCustomer(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch {
    ElMessage.error('加载应付记录失败')
  } finally {
    payablesLoading.value = false
  }
}
```

Update `handleTabChange`:

```typescript
const handleTabChange = (tab: string | number) => {
  if (tab === 'transactions' && !transactionsLoaded.value) loadTransactions()
  if (tab === 'receivables' && !receivablesLoaded.value) loadReceivables()
  if (tab === 'payables' && !payablesLoaded.value) loadPayables()
}
```

Update `handleLinkSuccess` to also refresh receivables/payables:

```typescript
const handleLinkSuccess = () => {
  refreshStatistics()
  loadFinanceSummary()
  receivablesLoaded.value = false
  payablesLoaded.value = false
}
```

- [ ] **Step 6: Verify frontend compiles**

Run: `cd frontend && npx vue-tsc --noEmit`
Expected: No errors

- [ ] **Step 7: Commit**

```bash
git add frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue
git commit -m "feat: customer detail — add payable summary + receivable/payable record tabs"
```

---

## Task 9: Frontend - Update SupplierDetailPage

**Files:**
- Modify: `frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue`

- [ ] **Step 1: Update the finance summary section**

Replace the single "财务概览" `SummaryOverview` block (lines 55-63) with two blocks:

```vue
      <SummaryOverview
        title="应收概览"
        subtitle="应收账款汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无应收数据"
      >
        <DetailSummaryCards :items="receivableSummaryCards" />
      </SummaryOverview>

      <SummaryOverview
        title="应付概览"
        subtitle="应付账款汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无应付数据"
      >
        <DetailSummaryCards :items="payableSummaryCards" />
      </SummaryOverview>
```

- [ ] **Step 2: Add imports and state**

```typescript
import { getReceivablesBySupplier } from '@/features/finance/api/receivable'
import { getPayablesBySupplier } from '@/features/finance/api/payable'
import ReceivableRecordsTable from '@/shared/ui/ReceivableRecordsTable.vue'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import type { Receivable } from '@/features/finance/types/receivable'
import type { Payable } from '@/features/finance/types/payable'

const receivables = ref<Receivable[]>([])
const receivablesLoading = ref(false)
const receivablesLoaded = ref(false)
const payables = ref<Payable[]>([])
const payablesLoading = ref(false)
const payablesLoaded = ref(false)
```

- [ ] **Step 3: Replace financeSummaryCards with two computed properties**

Remove the old `financeSummaryCards` computed and add:

```typescript
const receivableSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalReceivable', label: '应收总额', value: formatCurrency(s.totalReceivable), meta: `${s.projectCount} 个关联项目`, tone: 'balance' as const },
    { key: 'totalReceived', label: '已收金额', value: formatCurrency(s.totalReceived), tone: 'income' as const },
    { key: 'receivableRemaining', label: '未收余额', value: formatCurrency(s.receivableRemaining), tone: 'expense' as const },
    { key: 'receivableOverdue', label: '逾期', value: `${s.receivableOverdueCount} 笔`, meta: s.receivableOverdueAmount > 0 ? formatCurrency(s.receivableOverdueAmount) : undefined, tone: s.receivableOverdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})

const payableSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalPayable', label: '应付总额', value: formatCurrency(s.totalPayable), tone: 'balance' as const },
    { key: 'totalPaid', label: '已付金额', value: formatCurrency(s.totalPaid), tone: 'income' as const },
    { key: 'payableRemaining', label: '未付余额', value: formatCurrency(s.payableRemaining), tone: 'expense' as const },
    { key: 'payableOverdue', label: '逾期', value: `${s.payableOverdueCount} 笔`, meta: s.payableOverdueAmount > 0 ? formatCurrency(s.payableOverdueAmount) : undefined, tone: s.payableOverdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})
```

- [ ] **Step 4: Add receivable/payable tabs**

After the existing "交易记录" tab-pane (after line 123):

```vue
          <el-tab-pane label="应收记录" name="receivables">
            <ReceivableRecordsTable :records="receivables" :loading="receivablesLoading" />
          </el-tab-pane>
          <el-tab-pane label="应付记录" name="payables">
            <PayableRecordsTable :records="payables" :loading="payablesLoading" />
          </el-tab-pane>
```

- [ ] **Step 5: Add load functions and update handleTabChange**

```typescript
const loadReceivables = async () => {
  const id = Number(route.params.id)
  if (!id || receivablesLoaded.value) return
  receivablesLoading.value = true
  try {
    const { data } = await getReceivablesBySupplier(id)
    receivables.value = data.data
    receivablesLoaded.value = true
  } catch {
    ElMessage.error('加载应收记录失败')
  } finally {
    receivablesLoading.value = false
  }
}

const loadPayables = async () => {
  const id = Number(route.params.id)
  if (!id || payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const { data } = await getPayablesBySupplier(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch {
    ElMessage.error('加载应付记录失败')
  } finally {
    payablesLoading.value = false
  }
}

// Update handleTabChange:
const handleTabChange = (tab: string | number) => {
  if (tab === 'transactions' && !transactionsLoaded.value) loadTransactions()
  if (tab === 'receivables' && !receivablesLoaded.value) loadReceivables()
  if (tab === 'payables' && !payablesLoaded.value) loadPayables()
}
```

Update `handleLinkSuccess`:

```typescript
const handleLinkSuccess = () => {
  refreshStatistics()
  loadFinanceSummary()
  receivablesLoaded.value = false
  payablesLoaded.value = false
}
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue
git commit -m "feat: supplier detail — add receivable summary + receivable/payable record tabs"
```

---

## Task 10: Frontend - Update PersonDetailPage

**Files:**
- Modify: `frontend/src/features/master-data/persons/pages/PersonDetailPage.vue`

- [ ] **Step 1: Replace the "成本概览" section with receivable/payable summaries**

Replace the "成本概览" `SummaryOverview` block (lines 55-63) with three blocks:

```vue
      <SummaryOverview
        title="成本概览"
        subtitle="人员关联成本汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无成本数据"
      >
        <DetailSummaryCards :items="costSummaryCards" />
      </SummaryOverview>

      <SummaryOverview
        title="应收概览"
        subtitle="应收账款汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无应收数据"
      >
        <DetailSummaryCards :items="receivableSummaryCards" />
      </SummaryOverview>

      <SummaryOverview
        title="应付概览"
        subtitle="应付账款汇总"
        :loading="financeSummaryLoading"
        :empty="!financeSummary"
        empty-text="暂无应付数据"
      >
        <DetailSummaryCards :items="payableSummaryCards" />
      </SummaryOverview>
```

- [ ] **Step 2: Add imports and state**

```typescript
import { getReceivablesByPerson } from '@/features/finance/api/receivable'
import { getPayablesByPerson } from '@/features/finance/api/payable'
import ReceivableRecordsTable from '@/shared/ui/ReceivableRecordsTable.vue'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import type { Receivable } from '@/features/finance/types/receivable'
import type { Payable } from '@/features/finance/types/payable'

const receivables = ref<Receivable[]>([])
const receivablesLoading = ref(false)
const receivablesLoaded = ref(false)
const payables = ref<Payable[]>([])
const payablesLoading = ref(false)
const payablesLoaded = ref(false)
```

- [ ] **Step 3: Replace financeSummaryCards with three computed properties**

Remove the old `financeSummaryCards` and add:

```typescript
const costSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalCost', label: '总成本', value: formatCurrency(s.totalCost), meta: `${s.transactionCount} 笔交易`, tone: 'balance' as const },
    { key: 'directCost', label: '直接成本', value: formatCurrency(s.directCost), tone: 'expense' as const },
    { key: 'allocatedCost', label: '分摊成本', value: formatCurrency(s.allocatedCost), meta: `${s.projectCount} 个关联项目`, tone: 'transfer' as const },
  ]
})

const receivableSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalReceivable', label: '应收总额', value: formatCurrency(s.totalReceivable), tone: 'balance' as const },
    { key: 'totalReceived', label: '已收金额', value: formatCurrency(s.totalReceived), tone: 'income' as const },
    { key: 'receivableRemaining', label: '未收余额', value: formatCurrency(s.receivableRemaining), tone: 'expense' as const },
    { key: 'receivableOverdue', label: '逾期', value: `${s.receivableOverdueCount} 笔`, meta: s.receivableOverdueAmount > 0 ? formatCurrency(s.receivableOverdueAmount) : undefined, tone: s.receivableOverdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})

const payableSummaryCards = computed<DetailSummaryCardItem[]>(() => {
  const s = financeSummary.value
  if (!s) return []
  return [
    { key: 'totalPayable', label: '应付总额', value: formatCurrency(s.totalPayable), tone: 'balance' as const },
    { key: 'totalPaid', label: '已付金额', value: formatCurrency(s.totalPaid), tone: 'income' as const },
    { key: 'payableRemaining', label: '未付余额', value: formatCurrency(s.payableRemaining), tone: 'expense' as const },
    { key: 'payableOverdue', label: '逾期', value: `${s.payableOverdueCount} 笔`, meta: s.payableOverdueAmount > 0 ? formatCurrency(s.payableOverdueAmount) : undefined, tone: s.payableOverdueCount > 0 ? 'expense' as const : 'neutral' as const }
  ]
})
```

- [ ] **Step 4: Add receivable/payable tabs**

After the existing "交易记录" tab-pane (after line 123):

```vue
          <el-tab-pane label="应收记录" name="receivables">
            <ReceivableRecordsTable :records="receivables" :loading="receivablesLoading" />
          </el-tab-pane>
          <el-tab-pane label="应付记录" name="payables">
            <PayableRecordsTable :records="payables" :loading="payablesLoading" />
          </el-tab-pane>
```

- [ ] **Step 5: Add load functions and update handleTabChange**

```typescript
const loadReceivables = async () => {
  const id = Number(route.params.id)
  if (!id || receivablesLoaded.value) return
  receivablesLoading.value = true
  try {
    const { data } = await getReceivablesByPerson(id)
    receivables.value = data.data
    receivablesLoaded.value = true
  } catch {
    ElMessage.error('加载应收记录失败')
  } finally {
    receivablesLoading.value = false
  }
}

const loadPayables = async () => {
  const id = Number(route.params.id)
  if (!id || payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const { data } = await getPayablesByPerson(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch {
    ElMessage.error('加载应付记录失败')
  } finally {
    payablesLoading.value = false
  }
}

// Update handleTabChange:
const handleTabChange = (tab: string | number) => {
  if (tab === 'transactions' && !transactionsLoaded.value) loadTransactions()
  if (tab === 'receivables' && !receivablesLoaded.value) loadReceivables()
  if (tab === 'payables' && !payablesLoaded.value) loadPayables()
}
```

Update `handleLinkSuccess`:

```typescript
const handleLinkSuccess = () => {
  refreshStatistics()
  loadFinanceSummary()
  receivablesLoaded.value = false
  payablesLoaded.value = false
}
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/master-data/persons/pages/PersonDetailPage.vue
git commit -m "feat: person detail — add receivable/payable summaries + record tabs"
```

---

## Task 11: Frontend - Update ProjectDetailPage

**Files:**
- Modify: `frontend/src/features/master-data/projects/pages/ProjectDetailPage.vue`

- [ ] **Step 1: Add payable tab**

In the template, after the existing "收款计划" tab-pane (after line 270), add:

```vue
          <el-tab-pane label="应付记录" name="payables">
            <PayableRecordsTable :records="payables" :loading="payablesLoading" />
          </el-tab-pane>
```

- [ ] **Step 2: Add imports and state**

```typescript
import { getPayablesByProject } from '@/features/finance/api/payable'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import type { Payable } from '@/features/finance/types/payable'

const payables = ref<Payable[]>([])
const payablesLoading = ref(false)
const payablesLoaded = ref(false)
```

- [ ] **Step 3: Add loadPayables and update handleTabChange**

```typescript
const loadPayables = async () => {
  if (payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getPayablesByProject(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch (error) {
    console.error('加载应付记录失败:', error)
  } finally {
    payablesLoading.value = false
  }
}
```

Update `handleTabChange` (replace lines 435-442):

```typescript
const handleTabChange = (tab: string) => {
  if ((tab === 'transactions' || tab === 'allocations') && !transactionsLoaded.value) {
    loadTransactions()
  }
  if (tab === 'receivables' && !receivablesLoaded.value) {
    loadReceivables()
  }
  if (tab === 'payables' && !payablesLoaded.value) {
    loadPayables()
  }
}
```

Update `handleLinkSuccess`:

```typescript
const handleLinkSuccess = () => {
  transactionsLoaded.value = false
  receivablesLoaded.value = false
  payablesLoaded.value = false
  loadTransactions()
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/master-data/projects/pages/ProjectDetailPage.vue
git commit -m "feat: project detail — add payable record tab"
```

---

## Task 12: Verify and Final Commit

- [ ] **Step 1: Build backend**

Run: `dotnet build backend/FinanceApp.sln`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Check frontend compiles**

Run: `cd frontend && npx vue-tsc --noEmit`
Expected: No type errors

- [ ] **Step 3: Run backend tests**

Run: `dotnet test backend/FinanceApp.sln`
Expected: All tests pass (some finance summary tests may need updating due to DTO field name changes)

- [ ] **Step 4: Fix any broken tests**

If existing tests reference old field names like `TotalRemaining` on `CustomerFinanceSummaryDto`, update them to `ReceivableRemaining`. If tests reference `PayableRemaining` on `PersonFinanceSummaryDto`, verify that field still exists under the same name.

- [ ] **Step 5: Smoke test**

Start both backend and frontend, navigate to each detail page, verify:
- Customer detail: shows receivable + payable summary cards, 3 tabs (交易/应收/应付)
- Supplier detail: shows receivable + payable summary cards, 3 tabs
- Person detail: shows cost + receivable + payable summary cards, 3 tabs
- Project detail: shows existing overview + 4 tabs (交易/分摊/收款计划/应付记录)

- [ ] **Step 6: Final commit if any fixes were needed**

```bash
git add -A
git commit -m "fix: address review findings from detail pages enhancement"
```
