using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Domain.Constants;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Api.Tests.Integration;

public class SiteBrandIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SiteBrandIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPublicBrand_Anonymous_ReturnsDefaultsWithoutPrivateFields()
    {
        var response = await Client.GetAsync("/api/public/brand");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PublicBrandDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.SiteName.Should().Be(SiteBrandDefaults.SiteName);
        payload.Data.SiteNameEn.Should().Be(SiteBrandDefaults.SiteNameEn);

        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("audit_retention");
        raw.Should().NotContain("configKey");
        raw.Should().NotContain("config_key");
        raw.Should().NotContain("BootstrapAdmin");
    }

    [Fact]
    public async Task UpdateSiteBrand_Anonymous_ReturnsUnauthorized()
    {
        var response = await Client.PutAsJsonAsync("/api/configs/site-brand", new
        {
            siteName = "未授权站点",
            siteNameEn = "Unauthorized"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSiteBrand_Viewer_ReturnsForbidden()
    {
        await LoginAsAsync(UserRole.Viewer, "viewer1", "Viewer123!");

        var response = await Client.PutAsJsonAsync("/api/configs/site-brand", new
        {
            siteName = "查看者站点",
            siteNameEn = "Viewer Brand"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSiteBrand_Admin_PersistsAndPublicReadReflectsChange()
    {
        await LoginAsAsync(UserRole.Admin, "admin1", "Admin123!");

        var updateResponse = await Client.PutAsJsonAsync("/api/configs/site-brand", new
        {
            siteName = "  持久化站点  ",
            siteNameEn = "  Persisted Brand  "
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<PublicBrandDto>>(JsonOptions);
        updated!.Data!.SiteName.Should().Be("持久化站点");
        updated.Data.SiteNameEn.Should().Be("Persisted Brand");

        using var anonymousClient = Factory.CreateClient();
        var publicResponse = await anonymousClient.GetAsync("/api/public/brand");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicBrand = await publicResponse.Content.ReadFromJsonAsync<ApiResponse<PublicBrandDto>>(JsonOptions);
        publicBrand!.Data!.SiteName.Should().Be("持久化站点");
        publicBrand.Data.SiteNameEn.Should().Be("Persisted Brand");

        var stored = DbContext.SystemConfigs.Single(c => c.ConfigKey == SiteBrandDefaults.SiteNameKey);
        stored.ConfigValue.Should().Be("持久化站点");
    }

    [Fact]
    public async Task UpdateSiteBrand_InvalidName_ReturnsBadRequest()
    {
        await LoginAsAsync(UserRole.Admin, "admin2", "Admin123!");

        var response = await Client.PutAsJsonAsync("/api/configs/site-brand", new
        {
            siteName = "<b>xss</b>",
            siteNameEn = "Brand"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task LoginAsAsync(UserRole role, string username, string password)
    {
        var user = new User
        {
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            FullName = username,
            Role = role,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();
    }
}
