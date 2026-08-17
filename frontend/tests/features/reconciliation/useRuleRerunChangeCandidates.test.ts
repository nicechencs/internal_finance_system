import { ref } from 'vue'
import { describe, expect, it } from 'vitest'
import { useRuleRerunChangeCandidates } from '@/features/reconciliation/composables/useRuleRerunChangeCandidates'

describe('useRuleRerunChangeCandidates', () => {
  it('returns a stable filtered list until the source candidates change', () => {
    const candidates = ref([
      {
        transactionId: 1,
        willChange: true
      },
      {
        transactionId: 2,
        willChange: false
      }
    ])

    const { changeCandidates } = useRuleRerunChangeCandidates(candidates)

    const firstResult = changeCandidates.value
    const secondResult = changeCandidates.value

    expect(firstResult).toBe(secondResult)
    expect(firstResult.map(item => item.transactionId)).toEqual([1])

    candidates.value = [
      ...candidates.value,
      {
        transactionId: 3,
        willChange: true
      }
    ]

    const thirdResult = changeCandidates.value

    expect(thirdResult).not.toBe(firstResult)
    expect(thirdResult.map(item => item.transactionId)).toEqual([1, 3])
  })
})
