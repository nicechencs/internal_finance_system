using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api.Tests.Integration;

public class QueryBindingIntegrationTests : IntegrationTestBase
{
    public QueryBindingIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
        DbContext.TagBindings.RemoveRange(DbContext.TagBindings.IgnoreQueryFilters());
        DbContext.Tags.RemoveRange(DbContext.Tags.IgnoreQueryFilters());
        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();
    }

    [Fact]
    public async Task TransactionsGetPaged_ShouldBindTagFilters_FromDotNotationQuery()
    {
        await AuthenticateAsync();

        var (taggedTransactionId, untaggedTransactionId, tagId) = await SeedTaggedTransactionsAsync();

        var response = await GetAsync<ApiResponse<PageResponse<TransactionDto>>>(
            $"/api/transactions?page=1&pageSize=20&tagFilters[0].scope=transaction&tagFilters[0].tagIds[0]={tagId}&tagFilters[0].matchMode=or");

        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull();
        response.Data!.Total.Should().Be(1);
        response.Data.Items.Should().ContainSingle(item => item.Id == taggedTransactionId);
    }

    [Fact]
    public async Task TransactionsGetPaged_ShouldNotApplyTagFilters_FromBracketNotationQuery()
    {
        await AuthenticateAsync();

        var (taggedTransactionId, untaggedTransactionId, tagId) = await SeedTaggedTransactionsAsync();

        var response = await GetAsync<ApiResponse<PageResponse<TransactionDto>>>(
            $"/api/transactions?page=1&pageSize=20&tagFilters[0][scope]=transaction&tagFilters[0][tagIds][0]={tagId}&tagFilters[0][matchMode]=or");

        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull();
        response.Data!.Total.Should().Be(2);
        response.Data.Items.Should().Contain(item => item.Id == taggedTransactionId);
        response.Data.Items.Should().Contain(item => item.Id == untaggedTransactionId);
    }

    [Fact]
    public async Task TransactionsStatistics_ShouldBindTagFilters_FromDotNotationQuery()
    {
        await AuthenticateAsync();

        var (_, _, tagId) = await SeedTaggedTransactionsAsync();

        var response = await GetAsync<ApiResponse<TransactionStatisticsDto>>(
            $"/api/transactions/statistics?tagFilters[0].scope=transaction&tagFilters[0].tagIds[0]={tagId}&tagFilters[0].matchMode=or");

        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull();
        response.Data!.TotalCount.Should().Be(1);
        response.Data.ExpenseCount.Should().Be(1);
        response.Data.TotalExpense.Should().Be(100m);
        response.Data.TotalIncome.Should().Be(0m);
        response.Data.TransferCount.Should().Be(0);
    }


    [Fact]
    public async Task TransactionsGetPaged_ShouldApplyTagFilters_TogetherWithAccountFilter()
    {
        await AuthenticateAsync();

        var (matchingTransactionId, accountId, tagId) = await SeedTaggedTransactionsAcrossAccountsAsync();

        var response = await GetAsync<ApiResponse<PageResponse<TransactionDto>>>(
            $"/api/transactions?page=1&pageSize=20&accountId={accountId}&tagFilters[0].scope=transaction&tagFilters[0].tagIds[0]={tagId}&tagFilters[0].matchMode=or");

        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull();
        response.Data!.Total.Should().Be(1);
        response.Data.Items.Should().ContainSingle(item => item.Id == matchingTransactionId);
    }

    [Fact]
    public async Task TransactionsStatistics_ShouldApplyTagFilters_TogetherWithAccountFilter()
    {
        await AuthenticateAsync();

        var (_, accountId, tagId) = await SeedTaggedTransactionsAcrossAccountsAsync();

        var response = await GetAsync<ApiResponse<TransactionStatisticsDto>>(
            $"/api/transactions/statistics?accountId={accountId}&tagFilters[0].scope=transaction&tagFilters[0].tagIds[0]={tagId}&tagFilters[0].matchMode=or");

        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull();
        response.Data!.TotalCount.Should().Be(1);
        response.Data.ExpenseCount.Should().Be(1);
        response.Data.TotalExpense.Should().Be(100m);
        response.Data.TotalIncome.Should().Be(0m);
        response.Data.TransferCount.Should().Be(0);
    }

    private async Task AuthenticateAsync()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);
    }


    private async Task<(long MatchingTransactionId, long AccountId, long TagId)> SeedTaggedTransactionsAcrossAccountsAsync()
    {
        var matchingAccount = new Account
        {
            Name = "Account A",
            AccountType = AccountType.Bank,
            AccountNumber = "6222000099990001",
            BankName = "Test Bank A",
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            Currency = "CNY",
            IsActive = true
        };
        var otherAccount = new Account
        {
            Name = "Account B",
            AccountType = AccountType.Bank,
            AccountNumber = "6222000099990002",
            BankName = "Test Bank B",
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            Currency = "CNY",
            IsActive = true
        };

        DbContext.Accounts.AddRange(matchingAccount, otherAccount);
        await DbContext.SaveChangesAsync();

        var matchingTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow.Date,
            Amount = 100m,
            TransactionType = TransactionType.Expense,
            AccountId = matchingAccount.Id,
            Description = "matching tagged transaction",
            Status = TransactionStatus.Confirmed
        };
        var taggedOtherAccountTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow.Date.AddDays(-1),
            Amount = 150m,
            TransactionType = TransactionType.Expense,
            AccountId = otherAccount.Id,
            Description = "other account tagged transaction",
            Status = TransactionStatus.Confirmed
        };
        var untaggedSameAccountTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow.Date.AddDays(-2),
            Amount = 200m,
            TransactionType = TransactionType.Expense,
            AccountId = matchingAccount.Id,
            Description = "same account untagged transaction",
            Status = TransactionStatus.Confirmed
        };

        DbContext.Transactions.AddRange(matchingTransaction, taggedOtherAccountTransaction, untaggedSameAccountTransaction);
        await DbContext.SaveChangesAsync();

        var tag = new Tag
        {
            Scope = TagScope.Transaction,
            Name = "tag-filter",
            IsActive = true,
            SortOrder = 1
        };

        DbContext.Tags.Add(tag);
        await DbContext.SaveChangesAsync();

        DbContext.TagBindings.AddRange(
            new TagBinding
            {
                TagId = tag.Id,
                OwnerType = TagScope.Transaction,
                OwnerId = matchingTransaction.Id
            },
            new TagBinding
            {
                TagId = tag.Id,
                OwnerType = TagScope.Transaction,
                OwnerId = taggedOtherAccountTransaction.Id
            });
        await DbContext.SaveChangesAsync();

        DbContext.ChangeTracker.Clear();

        return (matchingTransaction.Id, matchingAccount.Id, tag.Id);
    }

    private async Task<(long TaggedTransactionId, long UntaggedTransactionId, long TagId)> SeedTaggedTransactionsAsync()
    {
        var account = new Account
        {
            Name = "测试账户",
            AccountType = AccountType.Bank,
            AccountNumber = "6222000012345678",
            BankName = "测试银行",
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            Currency = "CNY",
            IsActive = true
        };

        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();

        var taggedTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow.Date,
            Amount = 100m,
            TransactionType = TransactionType.Expense,
            AccountId = account.Id,
            Description = "带标签交易",
            Status = TransactionStatus.Confirmed
        };

        var untaggedTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow.Date.AddDays(-1),
            Amount = 200m,
            TransactionType = TransactionType.Expense,
            AccountId = account.Id,
            Description = "未打标签交易",
            Status = TransactionStatus.Confirmed
        };

        DbContext.Transactions.AddRange(taggedTransaction, untaggedTransaction);
        await DbContext.SaveChangesAsync();

        var tag = new Tag
        {
            Scope = TagScope.Transaction,
            Name = "测试标签",
            IsActive = true,
            SortOrder = 1
        };

        DbContext.Tags.Add(tag);
        await DbContext.SaveChangesAsync();

        DbContext.TagBindings.Add(new TagBinding
        {
            TagId = tag.Id,
            OwnerType = TagScope.Transaction,
            OwnerId = taggedTransaction.Id
        });
        await DbContext.SaveChangesAsync();

        DbContext.ChangeTracker.Clear();

        return (taggedTransaction.Id, untaggedTransaction.Id, tag.Id);
    }
}
