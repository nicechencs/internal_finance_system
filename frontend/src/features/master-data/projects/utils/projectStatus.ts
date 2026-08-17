import { PROJECT_STATUS } from '@/shared/constants/enums'

export function isCancelledProject(status?: string | null): boolean {
  return status === PROJECT_STATUS.CANCELLED
}

export function canMutateProject(status?: string | null): boolean {
  return !isCancelledProject(status)
}
