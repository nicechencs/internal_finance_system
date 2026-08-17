# API 契约管理

## 问题分析

当前存在的问题：
1. **命名不一致**：后端 C# PascalCase vs 前端 TypeScript camelCase
2. **响应结构差异**：后端 `{Success, Message, Data}` vs 前端期望 `{code, message, data}`
3. **手动维护双份类型**：后端 DTO 修改后前端需手动同步，易遗漏
4. **验证规则不同步**：后端 DataAnnotations vs 前端 Element Plus 验证

## 方案对比

| 方案 | 优点 | 缺点 | 适用场景 |
|------|------|------|----------|
| OpenAPI + 代码生成 | 单一数据源、自动生成、类型安全、CI/CD 集成 | 需额外工具链 | **推荐，已采用** |
| JSON Schema + 验证 | 语言无关、共享验证规则 | 需维护 Schema、学习成本高 | 跨语言项目 |
| 统一命名 + 自动转换 | 实施简单、灵活 | 仍需手动维护类型 | 小型项目 |
| Contract-First (gRPC) | 强类型、性能优秀 | 学习成本高、需改造架构 | 微服务架构 |

## 采用方案：OpenAPI + 代码生成 + camelCase 序列化

单一数据源：后端 DTO → OpenAPI → 前端 TypeScript，自动生成，零手动维护。

## 已完成的改进

### 1. JSON 序列化（Program.cs）

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
```

### 2. 统一 ApiResponse 结构

```csharp
public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string>? Errors { get; set; }
}
```

前端对应类型（`frontend/src/types/common.ts`）：

```typescript
export interface ApiResponse<T = any> {
  code: number
  message: string
  data?: T
  timestamp: string
  errors?: string[]
}
```

### 3. 全局异常处理

`GlobalExceptionHandlerMiddleware.cs` 已统一使用 `ApiResponse<object>.ErrorResponse()`，错误响应结构与正常响应一致。

## 命名规范速查表

| 层级 | 约定 | 示例 |
|-----|------|------|
| 后端 C# 类/属性 | PascalCase | `FullName`, `IsActive` |
| 后端 API 路由 | kebab-case | `/api/auth/login` |
| JSON 字段（序列化后） | camelCase | `fullName`, `isActive` |
| 前端 TypeScript | camelCase | `fullName`, `isActive` |
| 数据库字段 | snake_case | `full_name`, `is_active` |

## 自动化 API 类型生成

```bash
# 1. 安装工具
cd frontend && npm install -D swagger-typescript-api

# 2. 启动后端，确保 Swagger 可访问（http://localhost:5000/swagger）
cd backend/FinanceApp.Api && dotnet run

# 3. 生成前端类型（输出到 src/api/generated/api.ts）
cd frontend && npm run generate:api
```

生成后即可使用类型安全的 API 调用，TypeScript 自动推断响应类型并提供补全。

## 后端修改 DTO 后的检查清单

1. 添加 XML 注释（用于 Swagger 文档生成）
2. 运行后端，确认 Swagger 已更新
3. 前端执行 `npm run generate:api`
4. 检查 TypeScript 编译错误
5. 提交生成的类型文件

## 常见问题

**Q: 生成失败 "Cannot fetch swagger.json"**
A: 确保后端正在运行，访问 http://localhost:5000/swagger/v1/swagger.json

**Q: 生成的类型与预期不符**
A: 检查后端 DTO 是否有 `[JsonIgnore]` 等特殊标记

**Q: 前端字段名仍然是 PascalCase**
A: 检查 `Program.cs` 中的 `JsonNamingPolicy.CamelCase` 配置

**Q: 后端 DTO 修改后前端如何同步？**
A: 重新执行 `npm run generate:api`，然后提交生成的文件

## 未实施的改进

- [ ] CI/CD 自动检查（后端 DTO 变更时自动生成前端类型并检查差异）
- [ ] 契约测试（使用 Pact 等工具确保前后端契约一致）
- [ ] API 版本管理（使用 `/api/v1` 等版本化路由）

