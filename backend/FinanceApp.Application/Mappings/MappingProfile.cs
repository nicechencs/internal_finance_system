using Mapster;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.MasterData.DTOs.Customer;
using FinanceApp.Application.Modules.MasterData.DTOs.Supplier;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Mappings;

public static class MappingConfig
{
    public static TypeAdapterConfig Configure(TypeAdapterConfig? config = null)
    {
        config ??= new TypeAdapterConfig();

        // User mappings
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.Role, src => src.Role.ToString());
        config.NewConfig<User, UserAdminDto>()
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.Role, src => src.Role.ToString())
            .Ignore(dest => dest.IsLocked);

        // Account mappings
        config.NewConfig<Account, AccountDto>()
            .Map(dest => dest.AccountType, src => src.AccountType.ToString())
            .Map(dest => dest.Balance, src => src.CurrentBalance)
            .Map(dest => dest.OpeningBalance, src => src.OpeningBalance)
            .Map(dest => dest.CurrentBalance, src => src.CurrentBalance);

        // Customer mappings
        config.NewConfig<Customer, CustomerDto>()
            .Ignore(dest => dest.Tags);

        // Supplier mappings
        config.NewConfig<Supplier, SupplierDto>()
            .Ignore(dest => dest.Tags);

        // Person mappings
        config.NewConfig<Person, PersonDto>()
            .Map(dest => dest.PersonType, src => src.PersonType.ToString())
            .Ignore(dest => dest.Tags);

        // Project mappings
        config.NewConfig<Project, ProjectDto>()
            .Map(dest => dest.ProjectCode, src => src.ProjectCode ?? string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Customer, src => src.Customer != null
                ? new CustomerInfo { Id = src.Customer.Id, Name = src.Customer.Name }
                : new CustomerInfo())
            .Ignore(dest => dest.Tags)
            .Ignore(dest => dest.CostBreakdown);

        // Payable mappings
        config.NewConfig<Payable, PayableDto>()
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : (string?)null)
            .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : (string?)null)
            .Map(dest => dest.PersonName, src => src.Person != null ? src.Person.Name : (string?)null)
            .Map(dest => dest.ProjectName, src => src.Project != null ? src.Project.Name : (string?)null)
            .Map(dest => dest.PayableTypeName, src => src.PayableType != null ? src.PayableType.Name : (string?)null)
            .Map(dest => dest.Status, src => src.Status.ToString().ToLowerInvariant())
            .Ignore(dest => dest.Tags);
        config.NewConfig<PayableDetail, PayableDetailDto>();

        // PayableType mappings
        config.NewConfig<PayableType, PayableTypeDto>();

        // Receivable mappings
        config.NewConfig<Receivable, ReceivableDto>()
            .Map(dest => dest.ProjectName, src => src.Project != null ? src.Project.Name : (string?)null)
            .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : (string?)null)
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : (string?)null)
            .Map(dest => dest.PersonName, src => src.Person != null ? src.Person.Name : (string?)null)
            .Map(dest => dest.ReceivableTypeName, src => src.ReceivableType != null ? src.ReceivableType.Name : (string?)null)
            .Map(dest => dest.Status, src => src.Status.ToString().ToLowerInvariant())
            .Ignore(dest => dest.Tags);
        config.NewConfig<ReceivableDetail, ReceivableDetailDto>();

        // ReceivableType mappings
        config.NewConfig<ReceivableType, ReceivableTypeDto>();

        // Transaction mappings - 使用 MapWith 完全自定义，避免 Mapster 自动展开导航属性
        config.NewConfig<Transaction, TransactionDto>()
            .MapWith(src => new TransactionDto
            {
                Id = src.Id,
                BankTransactionId = src.BankTransactionId,
                TransactionDate = src.TransactionDate,
                Amount = src.Amount,
                TransactionType = src.TransactionType.ToString(),
                TransferDirection = TransactionBalanceHelper.ResolveTransferDirection(src).ToString(),
                CategoryId = src.CategoryId,
                AccountId = src.AccountId,
                ProjectId = src.ProjectId,
                CustomerId = src.CustomerId,
                SupplierId = src.SupplierId,
                PersonId = src.PersonId,
                Description = src.Description,
                Status = src.Status.ToString(),
                IsAllocated = src.IsAllocated,
                RelatedTransactionId = src.RelatedTransactionId,
                CreatedAt = src.CreatedAt,
                AllocationStatus = src.AllocationStatus.ToString(),
                AvailableAmount = src.GetAvailableAmount(),
                AccountName = src.Account != null ? src.Account.Name : string.Empty,
                CategoryName = src.Category != null ? src.Category.Name : null,
                ProjectName = src.Project != null ? src.Project.Name : null,
                CustomerName = src.Customer != null ? src.Customer.Name : null,
                SupplierName = src.Supplier != null ? src.Supplier.Name : null,
                PersonName = src.Person != null ? src.Person.Name : null,
                Counterparty = src.BankTransaction != null ? src.BankTransaction.Counterparty : null,
                TransactionTime = src.BankTransaction != null ? src.BankTransaction.TransactionTime : null,
                CounterpartyAccount = src.BankTransaction != null ? src.BankTransaction.CounterpartyAccount : null,
                CounterpartyBank = src.BankTransaction != null ? src.BankTransaction.CounterpartyBank : null,
                TransactionNumber = src.BankTransaction != null ? src.BankTransaction.TransactionNumber : null,
                Memo = src.BankTransaction != null ? src.BankTransaction.Memo : null,
                RelatedAccountName = src.RelatedTransaction != null && src.RelatedTransaction.Account != null ? src.RelatedTransaction.Account.Name : null,
                Allocations = src.Allocations != null
                    ? src.Allocations.Select(a => new TransactionAllocationDto
                    {
                        Id = a.Id,
                        ProjectId = a.ProjectId,
                        ProjectName = a.Project != null ? a.Project.Name : null,
                        PersonId = a.PersonId,
                        PersonName = a.Person != null ? a.Person.Name : null,
                        Amount = a.Amount,
                        AllocationRate = a.AllocationRate,
                        Description = a.Description
                    }).ToList()
                    : new List<TransactionAllocationDto>()
            });

        return config;
    }
}
