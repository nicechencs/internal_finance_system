# Finance Chart Empty State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show explicit empty states for finance trend and aging charts when APIs return empty or all-zero business data.

**Architecture:** Keep backend contracts unchanged and implement the behavior entirely inside the finance page. Add small computed guards for chart visibility, then switch the chart area between `v-chart` and `el-empty`. Protect the behavior with focused component tests covering empty, all-zero, and non-empty payloads.

**Tech Stack:** Vue 3, Element Plus, vue-echarts, Vitest, Vue Test Utils

---

### Task 1: Add Failing Finance Chart Empty-State Tests

**Files:**
- Modify: `frontend/tests/views/finance/FinanceManagement.test.ts`
- Test: `frontend/tests/views/finance/FinanceManagement.test.ts`

- [ ] **Step 1: Write the failing tests**

```ts
  it('空数组图表数据时应显示两个空状态而不是图表', async () => {
    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.text()).toContain('暂无趋势数据')
    expect(wrapper.text()).toContain('暂无账龄数据')
    expect(wrapper.findAll('.v-chart-mock')).toHaveLength(0)
  })

  it('图表数据全为 0 时也应显示空状态', async () => {
    vi.mocked(receivableApi.getReceivableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [0, 0] } })
    )
    vi.mocked(payableApi.getPayableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [0, 0] } })
    )
    vi.mocked(receivableApi.getReceivableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [0, 0] } })
    )
    vi.mocked(payableApi.getPayableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [0, 0] } })
    )

    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.text()).toContain('暂无趋势数据')
    expect(wrapper.text()).toContain('暂无账龄数据')
    expect(wrapper.findAll('.v-chart-mock')).toHaveLength(0)
  })

  it('存在非零图表数据时应继续渲染图表', async () => {
    vi.mocked(receivableApi.getReceivableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [1200, 0] } })
    )
    vi.mocked(payableApi.getPayableTrend).mockResolvedValue(
      mockAxiosResponse({ data: { months: ['2026-01', '2026-02'], amounts: [0, 300] } })
    )
    vi.mocked(receivableApi.getReceivableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [500, 0] } })
    )
    vi.mocked(payableApi.getPayableAging).mockResolvedValue(
      mockAxiosResponse({ data: { categories: ['未到期', '1-30天'], amounts: [0, 200] } })
    )

    const wrapper = mountWithPlugins(FinanceManagement, { stubs: defaultStubs })
    await flushPromises()

    expect(wrapper.findAll('.v-chart-mock')).toHaveLength(2)
    expect(wrapper.text()).not.toContain('暂无趋势数据')
    expect(wrapper.text()).not.toContain('暂无账龄数据')
  })
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- tests/views/finance/FinanceManagement.test.ts`
Expected: FAIL because the page still renders chart mocks for empty and all-zero payloads.

### Task 2: Implement Empty-State Rendering

**Files:**
- Modify: `frontend/src/features/finance/pages/FinanceManagementPage.vue`
- Test: `frontend/tests/views/finance/FinanceManagement.test.ts`

- [ ] **Step 1: Add minimal visibility guards**

```ts
const hasAnyNonZeroValue = (values: number[]) => values.some(value => toSafeNumber(value) !== 0)

const hasTrendData = computed(() =>
  trendData.value.months.length > 0
  && (hasAnyNonZeroValue(trendData.value.receivable) || hasAnyNonZeroValue(trendData.value.payable))
)

const hasAgingData = computed(() =>
  agingData.value.categories.length > 0
  && (hasAnyNonZeroValue(agingData.value.receivable) || hasAnyNonZeroValue(agingData.value.payable))
)
```

- [ ] **Step 2: Switch chart card bodies to conditional rendering**

```vue
<v-chart v-if="hasTrendData" :option="trendChartOption" :autoresize="true" class="chart" />
<div v-else class="chart-empty-state">
  <el-empty description="暂无趋势数据" :image-size="72" />
</div>

<v-chart v-if="hasAgingData" :option="agingChartOption" :autoresize="true" class="chart" />
<div v-else class="chart-empty-state">
  <el-empty description="暂无账龄数据" :image-size="72" />
</div>
```

- [ ] **Step 3: Add minimal styling for the empty-state container**

```css
.chart-empty-state {
  min-height: 320px;
  display: flex;
  align-items: center;
  justify-content: center;
}
```

- [ ] **Step 4: Run targeted tests to verify they pass**

Run: `npm test -- tests/views/finance/FinanceManagement.test.ts`
Expected: PASS

### Task 3: Regression Verification

**Files:**
- Verify: `frontend/tests/views/finance/FinanceManagement.test.ts`

- [ ] **Step 1: Run the focused finance test suite**

Run: `npm test -- tests/views/finance/FinanceManagement.test.ts`
Expected: PASS with the new empty-state coverage included.

- [ ] **Step 2: Run one adjacent finance list suite to catch obvious collateral regressions**

Run: `npm test -- tests/views/receivables/ReceivableList.test.ts`
Expected: PASS

- [ ] **Step 3: Run one adjacent payable suite to catch obvious collateral regressions**

Run: `npm test -- tests/views/payables/PayableList.test.ts`
Expected: PASS
