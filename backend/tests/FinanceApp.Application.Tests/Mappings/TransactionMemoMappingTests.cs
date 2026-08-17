using FluentAssertions;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Tests.Mappings;

public class TransactionMemoMappingTests : TestBase
{
    [Fact]
    public void Should_Map_BankTransaction_Memo_To_TransactionDto()
    {
        var transaction = new Transaction
        {
            Id = 1,
            TransactionDate = new DateTime(2026, 4, 1),
            TransactionType = TransactionType.Expense,
            Amount = 1200m,
            AccountId = 7,
            Account = new Account { Id = 7, Name = "Main Account" },
            BankTransaction = new BankTransaction
            {
                Id = 9,
                AccountId = 7,
                TransactionDate = new DateTime(2026, 4, 1),
                Direction = BankTransactionDirection.Out,
                Amount = 1200m,
                UniqueHash = "tx-hash-1",
                Memo = "Bank memo for reconciliation"
            }
        };

        var result = Mapper.Map<TransactionDto>(transaction);

        result.Memo.Should().Be("Bank memo for reconciliation");
    }
}
