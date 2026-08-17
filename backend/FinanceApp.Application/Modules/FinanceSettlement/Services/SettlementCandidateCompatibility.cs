using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

/// <summary>
/// 待分配交易与未结清应收/应付的可核销兼容规则。
/// 对手方冲突、客户/供应商互斥、已绑定同一单据均为硬过滤。
/// </summary>
public static class SettlementCandidateCompatibility
{
    public static bool IsReceivableCompatible(Transaction transaction, Receivable receivable)
    {
        if (receivable.Status == ReceivableStatus.Settled || receivable.RemainingAmount <= 0)
        {
            return false;
        }

        if (HasExistingBinding(receivable.Details, transaction.Id))
        {
            return false;
        }

        return IsCounterpartyCompatible(
            transaction.CustomerId,
            transaction.SupplierId,
            receivable.CustomerId,
            receivable.SupplierId);
    }

    public static bool IsPayableCompatible(Transaction transaction, Payable payable)
    {
        if (payable.Status == PayableStatus.Settled || payable.RemainingAmount <= 0)
        {
            return false;
        }

        if (HasExistingBinding(payable.Details, transaction.Id))
        {
            return false;
        }

        return IsCounterpartyCompatible(
            transaction.CustomerId,
            transaction.SupplierId,
            payable.CustomerId,
            payable.SupplierId);
    }

    public static bool IsCounterpartyCompatible(
        long? transactionCustomerId,
        long? transactionSupplierId,
        long? settlementCustomerId,
        long? settlementSupplierId)
    {
        if (transactionCustomerId.HasValue && settlementSupplierId.HasValue)
        {
            return false;
        }

        if (transactionSupplierId.HasValue && settlementCustomerId.HasValue)
        {
            return false;
        }

        if (transactionCustomerId.HasValue &&
            settlementCustomerId.HasValue &&
            transactionCustomerId.Value != settlementCustomerId.Value)
        {
            return false;
        }

        if (transactionSupplierId.HasValue &&
            settlementSupplierId.HasValue &&
            transactionSupplierId.Value != settlementSupplierId.Value)
        {
            return false;
        }

        return true;
    }

    public static int ScoreReceivable(Transaction transaction, Receivable receivable)
    {
        return ScoreMatch(
            transaction.ProjectId,
            receivable.ProjectId,
            transaction.CustomerId,
            receivable.CustomerId,
            transaction.SupplierId,
            receivable.SupplierId,
            transaction.PersonId,
            receivable.PersonId);
    }

    public static int ScorePayable(Transaction transaction, Payable payable)
    {
        return ScoreMatch(
            transaction.ProjectId,
            payable.ProjectId,
            transaction.CustomerId,
            payable.CustomerId,
            transaction.SupplierId,
            payable.SupplierId,
            transaction.PersonId,
            payable.PersonId);
    }

    public static bool IsPreferredReceivable(Transaction transaction, Receivable receivable)
    {
        return IsPreferred(
            transaction.ProjectId,
            receivable.ProjectId,
            transaction.CustomerId,
            receivable.CustomerId,
            transaction.SupplierId,
            receivable.SupplierId,
            transaction.PersonId,
            receivable.PersonId);
    }

    public static bool IsPreferredPayable(Transaction transaction, Payable payable)
    {
        return IsPreferred(
            transaction.ProjectId,
            payable.ProjectId,
            transaction.CustomerId,
            payable.CustomerId,
            transaction.SupplierId,
            payable.SupplierId,
            transaction.PersonId,
            payable.PersonId);
    }

    private static bool HasExistingBinding(IEnumerable<ReceivableDetail>? details, long transactionId)
        => details?.Any(d => !d.IsDeleted && d.TransactionId == transactionId) == true;

    private static bool HasExistingBinding(IEnumerable<PayableDetail>? details, long transactionId)
        => details?.Any(d => !d.IsDeleted && d.TransactionId == transactionId) == true;

    private static int ScoreMatch(
        long? transactionProjectId,
        long? settlementProjectId,
        long? transactionCustomerId,
        long? settlementCustomerId,
        long? transactionSupplierId,
        long? settlementSupplierId,
        long? transactionPersonId,
        long? settlementPersonId)
    {
        var score = 0;
        if (transactionProjectId.HasValue && transactionProjectId == settlementProjectId)
        {
            score += 2;
        }

        if (transactionCustomerId.HasValue && transactionCustomerId == settlementCustomerId)
        {
            score += 3;
        }
        else if (transactionSupplierId.HasValue && transactionSupplierId == settlementSupplierId)
        {
            score += 3;
        }
        else if (transactionPersonId.HasValue && transactionPersonId == settlementPersonId)
        {
            score += 3;
        }

        return score;
    }

    private static bool IsPreferred(
        long? transactionProjectId,
        long? settlementProjectId,
        long? transactionCustomerId,
        long? settlementCustomerId,
        long? transactionSupplierId,
        long? settlementSupplierId,
        long? transactionPersonId,
        long? settlementPersonId)
    {
        var projectOk = !transactionProjectId.HasValue || transactionProjectId == settlementProjectId;
        var counterpartOk =
            (transactionCustomerId.HasValue && transactionCustomerId == settlementCustomerId) ||
            (transactionSupplierId.HasValue && transactionSupplierId == settlementSupplierId) ||
            (transactionPersonId.HasValue && transactionPersonId == settlementPersonId) ||
            (!transactionCustomerId.HasValue && !transactionSupplierId.HasValue && !transactionPersonId.HasValue);

        return projectOk && counterpartOk;
    }
}
