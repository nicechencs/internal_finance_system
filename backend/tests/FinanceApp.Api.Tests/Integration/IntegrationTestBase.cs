using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Domain.Constants;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Api.Tests.Integration;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFactory>, IDisposable
{
    protected readonly HttpClient Client;
    protected readonly IntegrationTestFactory Factory;
    protected readonly IServiceScope Scope;
    protected readonly AppDbContext DbContext;

    protected IntegrationTestBase(IntegrationTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 清理数据库
        CleanDatabase();
    }

    protected void CleanDatabase()
    {
        // 先清除所有 ChangeTracker 中的跟踪实体
        DbContext.ChangeTracker.Clear();

        DbContext.TransactionAllocations.RemoveRange(DbContext.TransactionAllocations.IgnoreQueryFilters());
        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();

        // Clear self-referencing foreign key before deleting transactions
        foreach (var t in DbContext.Transactions.IgnoreQueryFilters())
        {
            t.RelatedTransactionId = null;
        }
        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();

        DbContext.Transactions.RemoveRange(DbContext.Transactions.IgnoreQueryFilters());
        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();

        DbContext.BankTransactions.RemoveRange(DbContext.BankTransactions.IgnoreQueryFilters());
        DbContext.ImportBatches.RemoveRange(DbContext.ImportBatches.IgnoreQueryFilters());
        DbContext.ClassificationRules.RemoveRange(DbContext.ClassificationRules.IgnoreQueryFilters());
        DbContext.Accounts.RemoveRange(DbContext.Accounts.IgnoreQueryFilters());
        DbContext.Categories.RemoveRange(DbContext.Categories.IgnoreQueryFilters());
        DbContext.Projects.RemoveRange(DbContext.Projects.IgnoreQueryFilters());
        DbContext.Customers.RemoveRange(DbContext.Customers.IgnoreQueryFilters());
        DbContext.Suppliers.RemoveRange(DbContext.Suppliers.IgnoreQueryFilters());
        DbContext.Persons.RemoveRange(DbContext.Persons.IgnoreQueryFilters());
        DbContext.Users.RemoveRange(DbContext.Users.IgnoreQueryFilters());
        DbContext.SystemConfigs.RemoveRange(DbContext.SystemConfigs.IgnoreQueryFilters());
        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();

        Scope.ServiceProvider.GetService<IMemoryCache>()?.Remove(SiteBrandDefaults.PublicBrandCacheKey);
    }

    protected async Task<string> GetAuthTokenAsync()
    {
        Client.DefaultRequestHeaders.Authorization = null;

        // 创建测试用户（Admin 角色）
        var user = new User
        {
            Username = "testuser",
            NormalizedUsername = "TESTUSER",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            FullName = "测试用户",
            Email = "test@example.com",
            Role = UserRole.Admin,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // 登录获取 Token
        var loginRequest = new
        {
            Username = "testuser",
            Password = "Test123!"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        return string.Empty;
    }

    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        return await Client.PostAsJsonAsync(url, data);
    }

    protected async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        return await Client.PutAsJsonAsync(url, data);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    protected MultipartFormDataContent CreateMultipartContent(byte[] fileContent, string fileName, Dictionary<string, string>? formData = null)
    {
        var content = new MultipartFormDataContent();
        var fileStreamContent = new ByteArrayContent(fileContent);
        fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileStreamContent, "file", fileName);

        if (formData != null)
        {
            foreach (var kvp in formData)
            {
                content.Add(new StringContent(kvp.Value), kvp.Key);
            }
        }

        return content;
    }

    public void Dispose()
    {
        Scope?.Dispose();
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
