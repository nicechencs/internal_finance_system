import { computed, type Ref } from 'vue'
import type { RuleRerunCandidateDto } from '@/features/transactions/types/link'

export function useRuleRerunChangeCandidates(candidates: Ref<RuleRerunCandidateDto[]>) {
  const changeCandidates = computed(() => candidates.value.filter(candidate => candidate.willChange))

  return {
    changeCandidates
  }
}
