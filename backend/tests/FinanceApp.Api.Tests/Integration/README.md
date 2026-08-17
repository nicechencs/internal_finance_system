# API 集成测试说明

本目录包含 API 集成测试，重点验证关键业务流程在真实 HTTP 管道中的行为。

## 当前覆盖范围

- `ExcelImportIntegrationTests.cs`
- `TransactionAllocationIntegrationTests.cs`
- `TransactionAtomicityTests.cs`
- `RuleMatchingIntegrationTests.cs`
- `EndToEndIntegrationTests.cs`

## 测试基础设施

- `IntegrationTestFactory.cs`：创建测试服务器并替换运行环境
- `IntegrationTestBase.cs`：提供登录、数据清理、HTTP 请求和文件上传辅助方法
- 客户端已开启 Cookie 处理，认证通过登录后建立会话

## 运行方式

```bash
dotnet test backend/tests/FinanceApp.Api.Tests/FinanceApp.Api.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## 注意事项

- `GetAuthTokenAsync` / `SetAuthToken` 仍保留历史命名，但当前测试链路依赖的是登录后建立的 Cookie 会话
- 测试数据库当前以 EF Core InMemory 为主，不覆盖真实 PostgreSQL 的全部行为
- 如果测试失败涉及认证，请优先检查登录流程、Cookie 和测试环境配置，不再按 JWT 排查
