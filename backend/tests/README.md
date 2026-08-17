# 后端测试说明

后端测试按项目分为 API 测试和 Infrastructure 测试，详细总体口径以 [`docs/04_Development/04_testing.md`](../../docs/04_Development/04_testing.md) 为准。

## 测试项目

- `backend/tests/FinanceApp.Api.Tests`
- `backend/tests/FinanceApp.Infrastructure.Tests`

## 常用命令

```bash
dotnet test backend/tests/FinanceApp.Api.Tests/FinanceApp.Api.Tests.csproj
dotnet test backend/tests/FinanceApp.Infrastructure.Tests/FinanceApp.Infrastructure.Tests.csproj
dotnet test backend/tests/FinanceApp.Api.Tests/FinanceApp.Api.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## 当前事实

- API 集成测试使用 `WebApplicationFactory`
- 集成测试客户端开启 Cookie 处理
- 测试数据库当前以 EF Core InMemory 为主
- 详细实现说明应以各测试项目源码和局部 README 为准
