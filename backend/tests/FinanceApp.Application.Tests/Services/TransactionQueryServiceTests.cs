using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TransactionQueryServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<ILogger<TransactionQueryService>> _loggerMock;
    private readonly TransactionQueryService _service;

    public TransactionQueryServiceTests()
    {
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _loggerMock = new Mock<ILogger<TransactionQueryService>>();

        _service = new TransactionQueryService(
            _transactionRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            Mapper,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService());
    }

    [Fact]
    public async Task GetPagedAsync_WithTagFiltersAndProjectFilter_ShouldApplyIntersectionForAllocatedTransactions()
    {
        var account = new Account
        {
            Id = 10L,
            Name = "Main Account",
            AccountType = AccountType.Bank,
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            Currency = "CNY"
        };
        var matchingProject = new Project { Id = 300L, Name = "Project A" };
        var otherProject = new Project { Id = 301L, Name = "Project B" };
        var tag = new Tag
        {
            Id = 1001L,
            Scope = TagScope.Transaction,
            Name = "Priority",
            Color = "#ff6600",
            SortOrder = 1
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 3, 26),
                TransactionType = TransactionType.Expense,
                Amount = 100m,
                Description = "matching transaction",
                IsAllocated = true,
                CreatedBy = 1L,
                Allocations = new List<TransactionAllocation>
                {
                    new()
                    {
                        Id = 11L,
                        TransactionId = 1L,
                        ProjectId = matchingProject.Id,
                        Project = matchingProject,
                        Amount = 100m
                    }
                }
            },
            new()
            {
                Id = 2L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 3, 25),
                TransactionType = TransactionType.Expense,
                Amount = 200m,
                Description = "tagged but other project",
                IsAllocated = true,
                CreatedBy = 1L,
                Allocations = new List<TransactionAllocation>
                {
                    new()
                    {
                        Id = 12L,
                        TransactionId = 2L,
                        ProjectId = otherProject.Id,
                        Project = otherProject,
                        Amount = 200m
                    }
                }
            },
            new()
            {
                Id = 3L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 3, 24),
                TransactionType = TransactionType.Expense,
                Amount = 300m,
                Description = "project match but untagged",
                IsAllocated = true,
                CreatedBy = 1L,
                Allocations = new List<TransactionAllocation>
                {
                    new()
                    {
                        Id = 13L,
                        TransactionId = 3L,
                        ProjectId = matchingProject.Id,
                        Project = matchingProject,
                        Amount = 300m
                    }
                }
            }
        };

        var tagBindings = new List<TagBinding>
        {
            new()
            {
                Id = 1L,
                OwnerType = TagScope.Transaction,
                OwnerId = 1L,
                TagId = tag.Id,
                Tag = tag
            },
            new()
            {
                Id = 2L,
                OwnerType = TagScope.Transaction,
                OwnerId = 2L,
                TagId = tag.Id,
                Tag = tag
            }
        };

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(tagBindings.AsQueryable().BuildMock().Object);

        var request = new PageRequest
        {
            Page = 1,
            PageSize = 10,
            ProjectId = matchingProject.Id,
            TagFilters = new List<TagFilterGroup>
            {
                new()
                {
                    Scope = TagScope.Transaction,
                    TagIds = new List<long> { tag.Id },
                    MatchMode = TagMatchMode.Or
                }
            }
        };

        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(1L);
        result.Items[0].Amount.Should().Be(100m);
        result.Items[0].Tags.Should().ContainSingle(t => t.TagId == tag.Id && t.TagName == tag.Name);
    }

    [Fact]
    public async Task GetPagedAsync_WithAllocationStatusAndExcludeTransfer_ShouldFilterAndPopulateAvailableAmount()
    {
        var account = new Account
        {
            Id = 10L,
            Name = "Main Account",
            AccountType = AccountType.Bank,
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            Currency = "CNY"
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 8, 1),
                TransactionType = TransactionType.Income,
                Amount = 1000m,
                AllocationStatus = AllocationStatus.Unallocated,
                CreatedBy = 1L
            },
            new()
            {
                Id = 2L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 8, 2),
                TransactionType = TransactionType.Expense,
                Amount = 800m,
                AllocationStatus = AllocationStatus.PartiallyAllocated,
                CreatedBy = 1L,
                PayableDetails = new List<PayableDetail>
                {
                    new() { Id = 21L, TransactionId = 2L, Amount = 300m }
                }
            },
            new()
            {
                Id = 3L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 8, 3),
                TransactionType = TransactionType.Income,
                Amount = 500m,
                AllocationStatus = AllocationStatus.FullyAllocated,
                CreatedBy = 1L
            },
            new()
            {
                Id = 4L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 8, 4),
                TransactionType = TransactionType.Transfer,
                Amount = 200m,
                AllocationStatus = AllocationStatus.Unallocated,
                CreatedBy = 1L
            }
        };

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetPagedAsync(new PageRequest
        {
            Page = 1,
            PageSize = 10,
            AllocationStatus = "Unallocated,PartiallyAllocated",
            ExcludeTransfer = true,
            MinAmount = 100m
        });

        result.Total.Should().Be(2);
        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 2L, 1L });
        result.Items.Should().Contain(i => i.Id == 1L && i.AvailableAmount == 1000m && i.AllocationStatus == "Unallocated");
        result.Items.Should().Contain(i => i.Id == 2L && i.AvailableAmount == 500m && i.AllocationStatus == "PartiallyAllocated");
    }

    [Fact]
    public async Task GetPagedAsync_WithoutExcludeTransfer_ShouldKeepUnallocatedTransfers()
    {
        var account = new Account
        {
            Id = 10L,
            Name = "Main Account",
            AccountType = AccountType.Bank,
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            Currency = "CNY"
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 4L,
                AccountId = account.Id,
                Account = account,
                TransactionDate = new DateTime(2026, 8, 4),
                TransactionType = TransactionType.Transfer,
                Amount = 200m,
                AllocationStatus = AllocationStatus.Unallocated,
                CreatedBy = 1L
            }
        };

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TagBinding>().AsQueryable().BuildMock().Object);

        var result = await _service.GetPagedAsync(new PageRequest
        {
            Page = 1,
            PageSize = 10,
            AllocationStatus = "Unallocated"
        });

        result.Total.Should().Be(1);
        result.Items[0].Id.Should().Be(4L);
    }
}
