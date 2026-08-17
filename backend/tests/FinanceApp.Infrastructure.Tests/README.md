# Infrastructure 测试说明

当前说明只保留实际仍存在的测试模块和运行方式，不再维护过时的覆盖率或 JWT 描述。

## 当前覆盖范围

- `Repositories/RepositoryTests.cs`
- `Configurations/BankTransactionConfigurationTests.cs`
- `Configurations/UserConfigurationTests.cs`
- `Services/PasswordServiceTests.cs`
- `Services/DataPermissionServiceTests.cs`
- `Data/UnitOfWorkTests.cs`
- `Data/LegacySchemaUpgraderTests.cs`

## 运行方式

```bash
dotnet test backend/tests/FinanceApp.Infrastructure.Tests/FinanceApp.Infrastructure.Tests.csproj
```

## 说明

- 以 Repository、EF Core 配置、基础服务和数据层组件测试为主
- 当前不再声明固定测试数量或覆盖率百分比，避免文档和代码再次失真
- 如新增测试模块，请优先更新本页的“当前覆盖范围”
