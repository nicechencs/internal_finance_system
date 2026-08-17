using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.TransactionProcessing.Services;

public class AllocationService : IAllocationService
{
    private readonly IRepository<TransactionAllocation> _allocationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AllocationService> _logger;

    public AllocationService(
        IRepository<TransactionAllocation> allocationRepository,
        IUnitOfWork unitOfWork,
        ILogger<AllocationService> logger)
    {
        _allocationRepository = allocationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public void ValidateAllocations(List<CreateAllocationRequest> allocations, decimal totalAmount)
    {
        _logger.LogDebug("开始验证分摊记录: 分摊数={Count}, 总金额={TotalAmount}",
            allocations.Count, totalAmount);

        if (allocations.Count == 0)
        {
            _logger.LogDebug("无分摊记录，跳过验证");
            return;
        }

        // Check each allocation has either amount or rate
        foreach (var allocation in allocations)
        {
            if (!allocation.Amount.HasValue && !allocation.AllocationRate.HasValue)
            {
                _logger.LogWarning("分摊记录验证失败: 未指定金额或百分比");
                throw new ValidationException("分摊记录必须指定金额或百分比");
            }

            if (allocation.Amount.HasValue && allocation.Amount.Value <= 0)
            {
                _logger.LogWarning("分摊金额验证失败: Amount={Amount}", allocation.Amount.Value);
                throw new ValidationException("分摊金额必须大于0");
            }

            if (allocation.AllocationRate.HasValue && (allocation.AllocationRate.Value <= 0 || allocation.AllocationRate.Value > 100))
            {
                _logger.LogWarning("分摊百分比验证失败: Rate={Rate}", allocation.AllocationRate.Value);
                throw new ValidationException("分摊百分比必须在0-100之间");
            }

            // Check at least one of ProjectId or PersonId is set
            if (!allocation.ProjectId.HasValue && !allocation.PersonId.HasValue)
            {
                _logger.LogWarning("分摊记录验证失败: 未指定项目或人员");
                throw new ValidationException("分摊记录必须指定项目或人员");
            }
        }

        // Calculate total allocation amount
        decimal totalAllocation = 0;
        foreach (var allocation in allocations)
        {
            if (allocation.Amount.HasValue)
            {
                totalAllocation += allocation.Amount.Value;
            }
            else if (allocation.AllocationRate.HasValue)
            {
                totalAllocation += CalculateAmountFromRate(totalAmount, allocation.AllocationRate.Value);
            }
        }

        _logger.LogDebug("分摊金额总和: {TotalAllocation}, 期望: {TotalAmount}",
            totalAllocation, totalAmount);

        // Validate total equals transaction amount (with small tolerance for rounding)
        if (Math.Abs(totalAllocation - totalAmount) > 0.01m)
        {
            _logger.LogWarning(
                "分摊金额总和验证失败: 总和={TotalAllocation}, 期望={TotalAmount}, 差异={Difference}",
                totalAllocation, totalAmount, Math.Abs(totalAllocation - totalAmount));
            throw new ValidationException($"分摊金额总和({totalAllocation})必须等于交易金额({totalAmount})");
        }

        _logger.LogDebug("分摊记录验证通过");
    }

    public decimal CalculateAmountFromRate(decimal totalAmount, decimal rate)
    {
        var calculatedAmount = Math.Round(totalAmount * rate / 100, 2);
        _logger.LogDebug("计算分摊金额: 总金额={TotalAmount}, 百分比={Rate}%, 结果={Amount}",
            totalAmount, rate, calculatedAmount);
        return calculatedAmount;
    }

    public async Task CreateAllocationsAsync(long transactionId, List<CreateAllocationRequest> allocations, decimal totalAmount)
    {
        _logger.LogDebug("创建分摊记录: TransactionId={TransactionId}, 分摊数={Count}",
            transactionId, allocations.Count);

        foreach (var allocationRequest in allocations)
        {
            var allocation = new TransactionAllocation
            {
                TransactionId = transactionId,
                ProjectId = allocationRequest.ProjectId,
                PersonId = allocationRequest.PersonId,
                Amount = allocationRequest.Amount ?? CalculateAmountFromRate(totalAmount, allocationRequest.AllocationRate!.Value),
                AllocationRate = allocationRequest.AllocationRate,
                Description = allocationRequest.Description
            };

            await _allocationRepository.AddAsync(allocation);
        }

        _logger.LogInformation("分摊记录已追踪: TransactionId={TransactionId}, 分摊数={Count}",
            transactionId, allocations.Count);
    }

    public async Task ReplaceAllocationsAsync(Transaction transaction, List<CreateAllocationRequest>? allocations)
    {
        _logger.LogDebug("替换分摊记录: TransactionId={TransactionId}", transaction.Id);

        // 删除旧分摊记录
        var oldAllocations = await _allocationRepository.GetQueryable()
            .Where(a => a.TransactionId == transaction.Id)
            .ToListAsync();

        var oldAllocationCount = oldAllocations.Count;
        foreach (var oldAllocation in oldAllocations)
        {
            _allocationRepository.Delete(oldAllocation);
        }

        if (oldAllocationCount > 0)
        {
            _logger.LogDebug("删除旧分摊记录: {Count}条", oldAllocationCount);
        }

        // 创建新分摊记录
        if (allocations != null && allocations.Count > 0)
        {
            await CreateAllocationsAsync(transaction.Id, allocations, transaction.Amount);
            _logger.LogDebug("创建新分摊记录: {Count}条", allocations.Count);
        }
    }
}
