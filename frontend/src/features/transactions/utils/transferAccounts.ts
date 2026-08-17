import type { Account } from '@/features/master-data/accounts/types/account'

export const mergeTransferAccounts = (...accountGroups: Account[][]): Account[] => {
  const mergedAccounts = new Map<number, Account>()

  for (const accounts of accountGroups) {
    for (const account of accounts) {
      if (!account) continue

      const current = mergedAccounts.get(account.id)
      mergedAccounts.set(account.id, current ? { ...account, ...current } : account)
    }
  }

  return Array.from(mergedAccounts.values())
}

export const formatTransferAccountLabel = (account: Account) => {
  return account.accountType === 'FixedDeposit'
    ? `${account.name} · 定期存款`
    : account.name
}
