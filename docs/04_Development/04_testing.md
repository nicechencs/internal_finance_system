# 测试说明

状态：Active
适用对象：开发 / 测试 / AI
事实源级别：Primary
最后核对日期：2026-03-21
代码依据：[`backend/tests`](../../backend/tests), [`backend/tests/FinanceApp.Api.Tests/Integration/IntegrationTestFactory.cs`](../../backend/tests/FinanceApp.Api.Tests/Integration/IntegrationTestFactory.cs)

## 测试分层

- Application 层单元测试
- Infrastructure 层单元测试
- API 集成测试
- 前端测试

## 当前稳定事实

- 后端集成测试使用 WebApplicationFactory
- 测试数据库以 EF Core InMemory 为主
- 集成测试客户端开启 Cookie 处理

## 常用命令

```bash
dotnet test
```

## 注意事项

- 测试说明不再把 JWT 作为当前认证前提
- 旧版测试实现总结已归档，不再作为现行事实源

## 相关文档

- [`backend/tests/README.md`](../../backend/tests/README.md)
- [`backend/tests/FinanceApp.Api.Tests/Integration/README.md`](../../backend/tests/FinanceApp.Api.Tests/Integration/README.md)
- [`backend/tests/FinanceApp.Infrastructure.Tests/README.md`](../../backend/tests/FinanceApp.Infrastructure.Tests/README.md)
