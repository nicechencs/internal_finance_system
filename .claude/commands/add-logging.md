# add-logging

为 .NET 后端代码添加完整的日志记录系统。

## 使用场景

- 为新创建的 Controller 或 Service 添加日志
- 为现有代码补充或完善日志记录
- 统一日志格式和分级

## 日志记录规范

### 一、日志格式模式

#### 1.1 Controller 层日志

**入口日志（LogDebug）**
```csharp
_logger.LogDebug("XxxController.MethodName 调用: UserId={UserId}, Param1={Param1}, Param2={Param2}",
    userId, param1, param2);
```

**成功日志（LogInformation）**
```csharp
_logger.LogInformation("成功: UserId={UserId}, ResultId={ResultId}, Count={Count}",
    userId, result.Id, result.Count);
```

**异常日志（LogError）**
```csharp
_logger.LogError(ex, "失败: UserId={UserId}, Param1={Param1}",
    userId, param1);
```

#### 1.2 Service 层日志

**方法入口（LogInformation）**
```csharp
_logger.LogInformation("XxxService.MethodName - 开始操作: Param1={Param1}, Param2={Param2}, Param3={Param3}",
    param1, param2, param3);
```

**中间步骤（LogDebug）**
```csharp
_logger.LogDebug("验证参数: Param1={Param1}", param1);
_logger.LogDebug("查询数据库: Id={Id}", id);
_logger.LogDebug("计算结果: Value={Value}", value);
```

**业务警告（LogWarning）**
```csharp
_logger.LogWarning("数据不存在: Id={Id}", id);
_logger.LogWarning("验证失败: Expected={Expected}, Actual={Actual}", expected, actual);
```

**成功完成（LogInformation）**
```csharp
_logger.LogInformation("操作成功: Id={Id}, Name={Name}, Status={Status}",
    result.Id, result.Name, result.Status);
```

**异常处理（LogError）**
```csharp
catch (ValidationException)
{
    throw;  // 业务异常不记录，由调用者处理
}
catch (NotFoundException)
{
    throw;  // 业务异常不记录，由调用者处理
}
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败: Param1={Param1}, Param2={Param2}",
        param1, param2);
    throw;
}
```

### 二、日志分级策略

| 级别 | Controller 层 | Service 层 | 使用场景 |
|------|--------------|-----------|---------|
| **Debug** | API 调用入口 | 中间步骤、参数验证、数据库查询 | 开发调试信息 |
| **Information** | 操作成功 | 方法入口、业务操作成功、关键步骤完成 | 正常业务操作 |
| **Warning** | 参数验证失败 | 业务验证失败、资源不存在、状态异常 | 业务警告 |
| **Error** | 异常捕获 | 异常捕获、事务回滚、操作失败 | 系统错误 |

### 三、结构化日志参数规范

#### 3.1 命名约定
- 使用 PascalCase：`{UserId}`, `{TransactionId}`, `{AccountId}`
- 业务实体用完整名称：`{TransactionId}` 而非 `{TxId}`
- 数值类型直接使用：`{Amount}`, `{Count}`, `{Total}`
- 状态/类型用英文：`{Status}`, `{Type}`, `{Direction}`

#### 3.2 参数顺序
优先级：主体ID → 操作类型 → 关键数据 → 统计数据

```csharp
_logger.LogInformation(
    "导入批次处理完成: BatchId={BatchId}, Status={Status}, Success={Success}, Failed={Failed}, Duplicate={Duplicate}",
    batch.Id, batch.Status, successCount, failedCount, duplicateCount);
```

### 四、特殊场景日志模式

#### 4.1 私有方法日志

**验证方法**
```csharp
private void ValidateData(DataRequest request)
{
    _logger.LogDebug("开始验证数据: Count={Count}", request.Items.Count);

    // 验证逻辑
    if (invalid)
    {
        _logger.LogWarning("验证失败: Reason={Reason}", reason);
        throw new ValidationException(reason);
    }

    _logger.LogDebug("验证通过");
}
```

**计算方法**
```csharp
private decimal CalculateAmount(decimal total, decimal rate)
{
    var result = Math.Round(total * rate / 100, 2);
    _logger.LogDebug("计算金额: Total={Total}, Rate={Rate}%, Result={Result}",
        total, rate, result);
    return result;
}
```

**更新方法**
```csharp
private async Task UpdateBalance(Account account, decimal amount)
{
    _logger.LogDebug("开始更新余额: AccountId={AccountId}, Amount={Amount}",
        account.Id, amount);

    var oldBalance = account.Balance;
    account.Balance += amount;
    await _repository.UpdateAsync(account);

    _logger.LogInformation("余额更新成功: AccountId={AccountId}, OldBalance={OldBalance}, NewBalance={NewBalance}, Change={Change}",
        account.Id, oldBalance, account.Balance, amount);
}
```

#### 4.2 批量操作日志

**批量操作入口**
```csharp
_logger.LogInformation("XxxService.BatchCreate - 开始批量创建: TotalCount={TotalCount}", items.Count);
```

**逐项处理**
```csharp
for (int i = 0; i < items.Count; i++)
{
    try
    {
        _logger.LogDebug("处理第 {Index}/{Total} 项: Name={Name}",
            i + 1, items.Count, items[i].Name);

        var result = await CreateSingleItem(items[i]);
        successCount++;

        _logger.LogDebug("第 {Index}/{Total} 项创建成功: Id={Id}",
            i + 1, items.Count, result.Id);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "第 {Index}/{Total} 项创建失败: Name={Name}, Error={Error}",
            i + 1, items.Count, items[i].Name, ex.Message);
        failedCount++;
    }
}
```

**批量操作完成**
```csharp
_logger.LogInformation("批量创建完成: TotalCount={TotalCount}, SuccessCount={SuccessCount}, FailedCount={FailedCount}",
    items.Count, successCount, failedCount);

if (failedCount > 0)
{
    _logger.LogWarning("批量创建存在失败项: FailedCount={FailedCount}", failedCount);
}
```

#### 4.3 事务操作日志

```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync();
try
{
    _logger.LogDebug("开始数据库事务");

    // 业务操作
    await _repository.CreateAsync(entity);
    _logger.LogDebug("实体创建成功: Id={Id}", entity.Id);

    await _repository.UpdateAsync(relatedEntity);
    _logger.LogDebug("关联实体更新成功: Id={Id}", relatedEntity.Id);

    await transaction.CommitAsync();
    _logger.LogInformation("事务提交成功");
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "事务回滚: Reason={Reason}", ex.Message);
    throw;
}
```

#### 4.4 性能监控日志

```csharp
var sw = Stopwatch.StartNew();

var result = await GetComplexReport(request);

sw.Stop();
_logger.LogInformation("报表生成完成: Type={Type}, ElapsedMs={ElapsedMs}, RowCount={RowCount}",
    reportType, sw.ElapsedMilliseconds, result.Count);

if (sw.ElapsedMilliseconds > 3000)
{
    _logger.LogWarning("报表生成耗时过长: Type={Type}, ElapsedMs={ElapsedMs}",
        reportType, sw.ElapsedMilliseconds);
}
```

### 五、敏感信息保护

#### 5.1 禁止记录的信息
- ❌ 密码（明文或哈希）
- ❌ JWT Token 内容
- ❌ 银行卡号完整信息
- ❌ 身份证号完整信息

#### 5.2 允许记录的信息
- ✅ 用户ID
- ✅ 用户名
- ✅ 金额
- ✅ 日期
- ✅ 业务对象ID

#### 5.3 敏感操作日志示例

**登录操作**
```csharp
// ❌ 错误：记录了密码
_logger.LogInformation("用户登录: Username={Username}, Password={Password}", username, password);

// ✅ 正确：只记录用户名和结果
_logger.LogInformation("AuthService.LoginAsync - 开始处理登录请求: Username={Username}", username);
_logger.LogDebug("找到用户: UserId={UserId}, 开始验证密码", user.Id);
_logger.LogInformation("用户登录成功: UserId={UserId}, Username={Username}, Role={Role}",
    user.Id, user.Username, user.Role);
```

**Token 生成**
```csharp
// ❌ 错误：记录了 Token 内容
_logger.LogInformation("生成 Token: {Token}", token);

// ✅ 正确：只记录生成事实
_logger.LogDebug("生成 JWT Token: UserId={UserId}", user.Id);
```

### 六、完整示例

#### 6.1 Controller 完整示例

```csharp
[HttpPost]
public async Task<ActionResult<ApiResponse<TransactionDto>>> Create([FromBody] CreateTransactionRequest request)
{
    var userId = GetCurrentUserId();
    _logger.LogDebug("TransactionsController.Create 调用: UserId={UserId}, Amount={Amount}, Type={Type}, AccountId={AccountId}",
        userId, request.Amount, request.Type, request.AccountId);

    try
    {
        var result = await _transactionService.CreateAsync(request);

        _logger.LogInformation("成功: UserId={UserId}, TransactionId={TransactionId}, Amount={Amount}",
            userId, result.Id, result.Amount);

        return Ok(ApiResponse<TransactionDto>.SuccessResponse(result, "交易创建成功"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "失败: UserId={UserId}, Amount={Amount}, Type={Type}",
            userId, request.Amount, request.Type);
        throw;
    }
}
```

#### 6.2 Service 完整示例

```csharp
public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
{
    _logger.LogInformation("TransactionService.CreateAsync - 开始创建交易记录: 金额={Amount}, 类型={Type}, 账户={AccountId}, 分类={CategoryId}",
        request.Amount, request.Type, request.AccountId, request.CategoryId);

    try
    {
        // 验证交易类型
        _logger.LogDebug("验证交易类型: {Type}", request.Type);
        if (!Enum.IsDefined(typeof(TransactionType), request.Type))
        {
            _logger.LogWarning("交易类型无效: {Type}", request.Type);
            throw new ValidationException($"无效的交易类型: {request.Type}");
        }

        // 验证账户
        _logger.LogDebug("验证账户存在性: AccountId={AccountId}", request.AccountId);
        var account = await _accountRepository.GetByIdAsync(request.AccountId);
        if (account == null)
        {
            _logger.LogWarning("账户不存在: AccountId={AccountId}", request.AccountId);
            throw new NotFoundException($"账户不存在: {request.AccountId}");
        }
        _logger.LogDebug("账户验证通过: AccountId={AccountId}, AccountName={AccountName}",
            account.Id, account.Name);

        // 创建交易实体
        _logger.LogDebug("创建交易实体");
        var transaction = new Transaction
        {
            Amount = request.Amount,
            TransactionType = request.Type,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            TransactionDate = request.TransactionDate,
            Description = request.Description
        };

        await _transactionRepository.CreateAsync(transaction);
        _logger.LogDebug("交易实体保存成功: TransactionId={TransactionId}", transaction.Id);

        // 更新账户余额
        await UpdateAccountBalance(account, request.Amount, request.Type);

        _logger.LogInformation("交易记录创建成功: Id={TransactionId}, 金额={Amount}, 类型={Type}, 账户={AccountId}, 分类={CategoryId}",
            transaction.Id, transaction.Amount, transaction.TransactionType, transaction.AccountId, transaction.CategoryId);

        return _mapper.Map<TransactionDto>(transaction);
    }
    catch (ValidationException)
    {
        throw;
    }
    catch (NotFoundException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建交易记录失败: 金额={Amount}, 类型={Type}, 账户={AccountId}",
            request.Amount, request.Type, request.AccountId);
        throw;
    }
}

private async Task UpdateAccountBalance(Account account, decimal amount, TransactionType type)
{
    _logger.LogDebug("开始更新账户余额: AccountId={AccountId}, 金额={Amount}, 类型={Type}",
        account.Id, amount, type);

    var oldBalance = account.CurrentBalance;

    if (type == TransactionType.Income)
    {
        account.CurrentBalance += amount;
    }
    else
    {
        account.CurrentBalance -= amount;
    }

    await _accountRepository.UpdateAsync(account);

    _logger.LogInformation("账户余额更新成功: AccountId={AccountId}, 旧余额={OldBalance}, 新余额={NewBalance}, 变化={Change}, 类型={Type}",
        account.Id, oldBalance, account.CurrentBalance, amount, type);
}
```

## 实施步骤

1. **确认文件类型**：Controller 还是 Service
2. **添加 ILogger 注入**（如果还没有）
3. **按照规范添加日志**：
   - Controller：入口 LogDebug + 成功 LogInformation + 异常 LogError
   - Service：入口 LogInformation + 中间步骤 LogDebug + 成功 LogInformation + 异常 LogError
4. **检查敏感信息**：确保不记录密码、Token 等
5. **验证编译**：确保代码可以编译通过
6. **测试日志输出**：运行代码查看日志是否正确输出

## 注意事项

1. **函数名只在入口记录**：函数内部的后续日志不需要重复函数名前缀
2. **参数命名统一**：使用 PascalCase，保持一致性
3. **日志分级合理**：Debug 用于调试，Information 用于业务操作，Warning 用于业务异常，Error 用于系统异常
4. **异常处理分层**：业务异常（ValidationException、NotFoundException）不记录日志直接抛出，系统异常记录后抛出
5. **批量操作记录进度**：使用 "第 X/总数" 格式
6. **性能监控**：超过 3 秒的操作记录 Warning
7. **事务操作**：记录开始、提交、回滚
8. **敏感信息保护**：绝不记录密码、Token 等敏感信息
