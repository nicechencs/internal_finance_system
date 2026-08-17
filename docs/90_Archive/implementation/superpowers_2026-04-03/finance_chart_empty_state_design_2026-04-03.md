# Finance Chart Empty State Design

## Background

`docs/DOCUMENTATION_OPEN_QUESTIONS.md` confirms that the finance dashboard charts are still not closed-loop for empty-data scenarios. The current implementation in `frontend/src/features/finance/pages/FinanceManagementPage.vue` always renders `v-chart`, even when trend and aging APIs return empty buckets or all-zero values.

This causes the UI to present a chart frame for what is effectively "no business data", which does not satisfy the documented expectation of an explicit empty state.

## Goal

Make the receivable/payable trend and aging cards show a clear empty state when the underlying finance data is absent, while continuing to render charts normally when at least one series contains meaningful values.

## Non-Goals

- Do not change backend API contracts.
- Do not redesign chart visuals for non-empty scenarios.
- Do not alter request failure handling in this iteration.
- Do not address other unresolved items such as `voucher_no` or list sorting.

## Chosen Approach

Use frontend conditional rendering in `FinanceManagementPage.vue`.

Each chart card will keep its header and layout, but the content area will switch between:

- `v-chart` when the card has meaningful data
- `el-empty` when the card has no meaningful data

This is preferred over ECharts `graphic` placeholders because it is simpler, more accessible, easier to reason about, and easier to verify in component tests.

## Data Semantics

### Trend chart

The trend chart is considered empty when either:

- `months.length === 0`, or
- both receivable and payable arrays contain only `0`

The trend chart is considered non-empty when any receivable or payable point is non-zero.

### Aging chart

The aging chart is considered empty when either:

- `categories.length === 0`, or
- both receivable and payable arrays contain only `0`

The aging chart is considered non-empty when any receivable or payable bucket is non-zero.

## UX Details

- Keep the trend date-range picker visible even when the chart is empty.
- Show `el-empty` with:
  - `暂无趋势数据` for the trend card
  - `暂无账龄数据` for the aging card
- Preserve the existing card shell, spacing, and responsive layout.

## Testing Strategy

Add or extend component tests in `frontend/tests/views/finance/FinanceManagement.test.ts` to verify:

1. Empty API payloads show both empty states and do not render chart mocks.
2. All-zero payloads also show empty states.
3. Non-zero payloads render chart mocks and hide the empty states.

## Risks

- The distinction between "empty data" and "valid zero-valued buckets" is product-defined. This design intentionally treats all-zero series as empty because the documented issue expects "no business data" to show an empty state.
- Existing tests currently mock `vue-echarts`; assertions must distinguish between chart mock presence and empty-state text reliably.

## Files In Scope

- `frontend/src/features/finance/pages/FinanceManagementPage.vue`
- `frontend/tests/views/finance/FinanceManagement.test.ts`
