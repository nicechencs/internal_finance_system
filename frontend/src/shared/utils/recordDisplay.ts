export function getCounterpartyLabel(row: {
  customerName?: string
  supplierName?: string
  personName?: string
  counterparty?: string
  counterpartyName?: string
}): string {
  return row.customerName
    || row.supplierName
    || row.personName
    || row.counterpartyName
    || row.counterparty
    || ''
}

export function joinMeta(...parts: Array<string | undefined | null>): string {
  return parts.filter((part) => Boolean(part && part !== '-')).join(' · ')
}
