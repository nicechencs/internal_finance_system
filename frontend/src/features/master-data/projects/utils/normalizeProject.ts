import type { Project } from '@/features/master-data/projects/types/project'

type ProjectWithNestedCustomer = Project & {
  customer?: {
    id?: number | null
    name?: string | null
  } | null
}

export function normalizeProject<T extends ProjectWithNestedCustomer>(project: T): T {
  return {
    ...project,
    customerId: project.customerId ?? project.customer?.id ?? undefined,
    customerName: project.customerName ?? project.customer?.name ?? undefined
  }
}

export function normalizeProjects<T extends ProjectWithNestedCustomer>(projects: T[]): T[] {
  return projects.map(normalizeProject)
}
