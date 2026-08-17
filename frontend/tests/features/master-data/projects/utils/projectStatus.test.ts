import { describe, it, expect } from 'vitest'
import { canMutateProject, isCancelledProject } from '@/features/master-data/projects/utils/projectStatus'

describe('projectStatus', () => {
  it('identifies cancelled projects', () => {
    expect(isCancelledProject('Cancelled')).toBe(true)
    expect(isCancelledProject('Active')).toBe(false)
    expect(isCancelledProject('Completed')).toBe(false)
    expect(isCancelledProject(undefined)).toBe(false)
  })

  it('only allows mutations on non-cancelled projects', () => {
    expect(canMutateProject('Active')).toBe(true)
    expect(canMutateProject('Completed')).toBe(true)
    expect(canMutateProject('Cancelled')).toBe(false)
  })
})
