# Finance Settlement Transaction Binding Phase 2 Design

## Goal

Tighten the binding rules between `receivable_details` / `payable_details` and `transactions` so settlement links become directionally correct, amount-safe, and resistant to later transaction edits that would invalidate existing links.

## Current Problem

Today, `ReceivePaymentAsync` and `PayPaymentAsync` only validate the settlement amount against the receivable/payable remaining amount. If a `TransactionId` is provided, the system does not verify:

- whether the transaction exists and is accessible
- whether the transaction direction matches the settlement side
- whether the transaction is already used by the opposite side
- whether cumulative linked detail amounts exceed the transaction amount

This leaves the system with an inconsistent state:

- entry is loose: almost any transaction can be linked
- exit is strict: linked transactions already cannot be deleted or converted to transfers

## Design Decision

Keep the existing schema and API contract unchanged. Tighten the rules in the application layer.

## Binding Rules

### Receivable Detail -> Transaction

When `ReceivePaymentRequest.TransactionId` is provided:

- the transaction must exist and not be deleted
- the current user must be allowed to edit the transaction
- the transaction must be `Income`
- the transaction must not already be linked by any `PayableDetail`
- cumulative `ReceivableDetail.Amount` for the same transaction, including the new detail, must be `<= transaction.Amount`

### Payable Detail -> Transaction

When `PayPaymentRequest.TransactionId` is provided:

- the transaction must exist and not be deleted
- the current user must be allowed to edit the transaction
- the transaction must be `Expense`
- the transaction must not already be linked by any `ReceivableDetail`
- cumulative `PayableDetail.Amount` for the same transaction, including the new detail, must be `<= transaction.Amount`

## Supported Scenarios To Keep

- a transaction may still bind to multiple details on the same side
- partial settlement against a transaction is still allowed
- we do not require transaction header fields like `ProjectId` / `CustomerId` / `SupplierId` to exactly match the linked receivable/payable, because one transaction can legally settle multiple documents

## Post-Binding Transaction Rules

If a transaction already has linked settlement details:

- linked-to-receivable transactions cannot be changed to a non-`Income` type
- linked-to-payable transactions cannot be changed to a non-`Expense` type
- transaction amount cannot be reduced below the cumulative linked amount on that side

Existing delete and transfer-conversion guards remain in place.

## Implementation Shape

- add a shared validator service in `FinanceSettlement`
- call it from `ReceivableService.ReceivePaymentAsync`
- call it from `PayableService.PayPaymentAsync`
- add update-time protection in `TransactionService.UpdateAsync`
- cover all new rules with unit tests first

## Risks

- existing dirty data may already violate these rules; the new logic will prevent new inconsistencies but does not auto-migrate old records
- update-time restrictions may surface hidden historical inconsistencies earlier than before
