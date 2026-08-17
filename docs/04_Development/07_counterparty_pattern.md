# 往来单位业务实现模式 (Counterparty Implementation Pattern)

状态：Active  
适用对象：开发 / 架构 / AI  
适用范围：FinanceSettlement / TransactionProcessing  
事实源级别：Primary  
最后核对日期：2026-03-24  
版本：1.0 (2026-03-24)

## 1. 业务背景
在财务系统中，应收应付记录或交易流水可能对应多种类型的外部主体（如：供应商、客户、内部员工）。为了保持模块边界清晰且不破坏数据库引用完整性，采用“互斥型多项关联”模式。

## 2. 核心要素 (Key Elements)

### A. 领域定义 (Domain Entities)
在主记录实体中定义所有可能的往来单位外键：
```csharp
public long? SupplierId { get; set; }
public long? CustomerId { get; set; }
public long? PersonId { get; set; }

// 对应的导航属性
public Supplier? Supplier { get; set; }
public Customer? Customer { get; set; }
public Person? Person { get; set; }
```

### B. 数据库约束 (Infrastructure Configuration)
必须在 `IEntityTypeConfiguration` 中定义 `CheckConstraint`，通过数据库引擎确保数据一致性：
```csharp
builder.HasCheckConstraint(
    "CK_entity_exactly_one_counterparty",
    "(CASE WHEN supplier_id IS NOT NULL THEN 1 ELSE 0 END + " +
    "CASE WHEN customer_id IS NOT NULL THEN 1 ELSE 0 END + " +
    "CASE WHEN person_id IS NOT NULL THEN 1 ELSE 0 END) = 1");
```

### C. 业务验证逻辑 (Application Validation)
在 Service 层提供统一的验证方法，作为业务守卫：
```csharp
private async Task ValidateCounterparty(long? supplierId, long? customerId, long? personId)
{
    var selectedCount = (supplierId.HasValue ? 1 : 0) + 
                        (customerId.HasValue ? 1 : 0) + 
                        (personId.HasValue ? 1 : 0);
    
    if (selectedCount == 0) throw new ValidationException("必须选择一个对方");
    if (selectedCount > 1) throw new ValidationException("只能选择一个对方");
    
    // 异步检查该 ID 是否在对应的 MasterData 仓库中真实存在
    // ...
}
```

### D. 审计与展示 (Audit & Metadata)
定义通用描述方法，以便在审计日志（AuditLog）或 UI 提示中展示具体的往来单位身份：
```csharp
private static string DescribeCounterparty(long? supplierId, long? customerId, long? personId)
{
    if (supplierId.HasValue) return $"供应商(Id={supplierId.Value})";
    if (customerId.HasValue) return $"客户(Id={customerId.Value})";
    if (personId.HasValue) return $"人员(Id={personId.Value})";
    return "无";
}
```

## 3. 最佳实践建议 (Best Practices)

1.  **读写分离**：建议将统计查询（Trend/Aging）与增删改逻辑拆分到不同的服务类中，避免 Service 过厚。
2.  **状态保护**：一旦发生关联交易（如 `PaidAmount > 0`），应禁止修改往来单位类型。
3.  **前端跳转**：在前端 Page 页面，根据返回的 ID 是否为空，动态分流至对应的详情页（如 `CustomerDetail` vs `SupplierDetail`）。
4.  **日志记录**：在 `Update` 操作中，若往来单位发生变更，必须显式记录旧值与新值的类型描述。

## 4. 评审清单 (Checklist for Review)
- [ ] 实体类是否包含所有往来单位类型的可选 ID？
- [ ] EF Core 配置中是否包含 `CheckConstraint` 数据库约束？
- [ ] `Service` 的 `Create/Update` 是否调用了 `ValidateCounterparty`？
- [ ] 审计日志是否使用了 `DescribeCounterparty` 记录业务语境？
- [ ] 前端列表是否对不同类型的往来单位提供了正确的链接跳转？
