# Settlement TransactionId Required Design

**Goal:** Close issue 3 from `docs/FIXES_SUMMARY.md` by making settlement transaction binding mandatory across backend contracts, persistence, and frontend flows.

## Problem

The current codebase already rejects missing settlement transaction bindings at service runtime, but the contract is still inconsistent:

- Backend DTOs still allow `TransactionId` to be nullable.
- `ReceivableDetail.TransactionId` and `PayableDetail.TransactionId` are still nullable in the domain model and EF configuration.
- The frontend request/detail types still model `transactionId` as optional.
- The payable detail page still allows clearing the selected transaction.
- Database schema may still contain nullable `transaction_id` columns and potentially historical null data.

This leaves the system in a half-fixed state where type-level and schema-level guarantees do not match runtime behavior.

## Recommended Approach

Use a full-stack “safe closure” approach:

1. Make `TransactionId` required in backend request DTOs and settlement detail entities.
2. Update validation logic to treat `0`/missing values as invalid and fail with a clear validation message.
3. Align frontend request/detail types and form state so users cannot submit or clear an empty transaction selection.
4. Add a database migration that first checks for existing null settlement bindings and aborts if dirty data exists, then makes the columns `NOT NULL`.

## Why This Approach

- It matches the existing business rule already enforced by services.
- It prevents new nullable data from entering through compile-time types, UI behavior, and schema constraints together.
- It avoids silent data mutation during migration. If production has historical nulls, the migration fails loudly and preserves data for explicit cleanup.

## Affected Areas

- Backend DTOs:
  `ReceivePaymentRequest`, `PayPaymentRequest`
- Backend domain/entities:
  `ReceivableDetail`, `PayableDetail`
- Backend validation/services:
  `ISettlementTransactionBindingService`, `SettlementTransactionBindingService`, `ReceivableService`, `PayableService`
- EF configuration/migrations:
  `ReceivableDetailConfiguration`, `PayableDetailConfiguration`, `AppDbContextModelSnapshot`, new migration
- Frontend:
  finance request/detail types, receivable/payable detail pages/components
- Documentation:
  `docs/FIXES_SUMMARY.md`, `docs/PENDING_FIXES.md`

## Risks

- Production may already contain null `transaction_id` rows, which would make a direct `NOT NULL` migration unsafe.
- Frontend forms currently use “unselected” states that need a numeric sentinel while preserving validation UX.

## Risk Handling

- The migration will perform a pre-check for null data and throw with an explicit remediation message if any exists.
- Frontend forms will use `0` as the local unselected sentinel and validate `transactionId >= 1` before submission.

## Verification

- Application tests cover `TransactionId = 0` being rejected for receivable/payable settlement.
- Targeted backend test suite passes after the contract change.
- Frontend type-check passes after request/detail type updates.
- EF migration is generated successfully against the updated model.
