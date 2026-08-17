using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Common;

public static class TransactionBalanceHelper
{
    public static TransferDirection ResolveTransferDirection(Transaction transaction)
        => ResolveTransferDirection(
            transaction.TransactionType,
            transaction.TransferDirection,
            transaction.Description,
            transaction.Id,
            transaction.RelatedTransactionId);

    public static TransferDirection ResolveTransferDirection(
        TransactionType transactionType,
        TransferDirection transferDirection,
        string? description,
        long transactionId,
        long? relatedTransactionId)
    {
        if (transactionType != TransactionType.Transfer)
        {
            return TransferDirection.None;
        }

        if (transferDirection != TransferDirection.None)
        {
            return transferDirection;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            if (description.Contains("转账至"))
            {
                return TransferDirection.Out;
            }

            if (description.Contains("转账自"))
            {
                return TransferDirection.In;
            }
        }

        if (relatedTransactionId.HasValue)
        {
            return transactionId < relatedTransactionId.Value
                ? TransferDirection.Out
                : TransferDirection.In;
        }

        return TransferDirection.None;
    }

    public static decimal GetSignedAmount(Transaction transaction)
        => GetSignedAmount(
            transaction.TransactionType,
            ResolveTransferDirection(transaction),
            transaction.Amount);

    public static decimal GetSignedAmount(
        TransactionType transactionType,
        TransferDirection transferDirection,
        decimal amount)
    {
        return transactionType switch
        {
            TransactionType.Income => amount,
            TransactionType.Expense => -amount,
            TransactionType.Transfer when transferDirection == TransferDirection.In => amount,
            TransactionType.Transfer when transferDirection == TransferDirection.Out => -amount,
            _ => 0m
        };
    }

    public static TransferDirection GetDirectionForTransactionType(TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.Expense => TransferDirection.Out,
            TransactionType.Income => TransferDirection.In,
            _ => TransferDirection.None
        };
    }

    public static string BuildDefaultTransferDescription(Account counterpartAccount, TransferDirection direction)
    {
        return direction == TransferDirection.Out
            ? $"转账至{counterpartAccount.Name}"
            : $"转账自{counterpartAccount.Name}";
    }
}
