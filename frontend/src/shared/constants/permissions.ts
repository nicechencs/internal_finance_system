export enum UserRole {
  Admin = 'Admin',
  Accountant = 'Accountant',
  Viewer = 'Viewer'
}

export const PermissionGroups = {
  /** 所有角色 */
  ALL: [UserRole.Admin, UserRole.Accountant, UserRole.Viewer] as string[],
  /** 可编辑（Admin + Accountant） */
  EDIT: [UserRole.Admin, UserRole.Accountant] as string[],
  /** 仅管理员 */
  ADMIN_ONLY: [UserRole.Admin] as string[],
} as const
