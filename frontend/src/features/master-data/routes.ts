import type { RouteRecordRaw } from 'vue-router'
import { PermissionGroups } from '@/shared/constants/permissions'

// ── Accounts ──
const accountRoutes: RouteRecordRaw[] = [
  {
    path: 'accounts',
    name: 'Accounts',
    component: () => import('@/features/master-data/accounts/pages/AccountListPage.vue'),
    meta: { title: '账户管理', roles: PermissionGroups.ALL, icon: 'Wallet', group: '基础数据', order: 5 }
  },
  {
    path: 'accounts/:id',
    name: 'AccountDetail',
    component: () => import('@/features/master-data/accounts/pages/AccountDetailPage.vue'),
    meta: { title: '账户详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/accounts' }
  }
]

// ── Fixed Deposits ──
const fixedDepositRoutes: RouteRecordRaw[] = [
  {
    path: 'fixed-deposits',
    name: 'FixedDeposits',
    component: () => import('@/features/master-data/fixed-deposits/pages/FixedDepositListPage.vue'),
    meta: { title: '定期存款台账', roles: PermissionGroups.ALL, icon: 'Clock', group: '财务管理', order: 3.5 }
  }
]

const categoryRoutes: RouteRecordRaw[] = [
  {
    path: 'categories',
    name: 'Categories',
    component: () => import('@/features/master-data/categories/pages/CategoryListPage.vue'),
    meta: { title: '分类管理', roles: PermissionGroups.ADMIN_ONLY, icon: 'FolderOpened', group: '基础数据', order: 10 }
  }
]

// ── Projects ──
const projectRoutes: RouteRecordRaw[] = [
  {
    path: 'projects',
    name: 'ProjectList',
    component: () => import('@/features/master-data/projects/pages/ProjectListPage.vue'),
    meta: { title: '项目管理', roles: PermissionGroups.ALL, icon: 'Briefcase', group: '基础数据', order: 6 }
  },
  {
    path: 'projects/create',
    name: 'ProjectCreate',
    component: () => import('@/features/master-data/projects/pages/ProjectFormPage.vue'),
    meta: { title: '新建项目', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/projects' }
  },
  {
    path: 'projects/:id/edit',
    name: 'ProjectEdit',
    component: () => import('@/features/master-data/projects/pages/ProjectFormPage.vue'),
    meta: { title: '编辑项目', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/projects' }
  },
  {
    path: 'projects/:id',
    name: 'ProjectDetail',
    component: () => import('@/features/master-data/projects/pages/ProjectDetailPage.vue'),
    meta: { title: '项目详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/projects' }
  }
]

// ── Customers ──
const customerRoutes: RouteRecordRaw[] = [
  {
    path: 'customers',
    name: 'CustomerList',
    component: () => import('@/features/master-data/customers/pages/CustomerListPage.vue'),
    meta: { title: '客户管理', roles: PermissionGroups.ALL, icon: 'User', group: '基础数据', order: 7 }
  },
  {
    path: 'customers/create',
    name: 'CustomerCreate',
    component: () => import('@/features/master-data/customers/pages/CustomerFormPage.vue'),
    meta: { title: '新建客户', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/customers' }
  },
  {
    path: 'customers/:id/edit',
    name: 'CustomerEdit',
    component: () => import('@/features/master-data/customers/pages/CustomerFormPage.vue'),
    meta: { title: '编辑客户', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/customers' }
  },
  {
    path: 'customers/:id',
    name: 'CustomerDetail',
    component: () => import('@/features/master-data/customers/pages/CustomerDetailPage.vue'),
    meta: { title: '客户详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/customers' }
  }
]

// ── Suppliers ──
const supplierRoutes: RouteRecordRaw[] = [
  {
    path: 'suppliers',
    name: 'SupplierList',
    component: () => import('@/features/master-data/suppliers/pages/SupplierListPage.vue'),
    meta: { title: '供应商管理', roles: PermissionGroups.ALL, icon: 'Shop', group: '基础数据', order: 8 }
  },
  {
    path: 'suppliers/create',
    name: 'SupplierCreate',
    component: () => import('@/features/master-data/suppliers/pages/SupplierFormPage.vue'),
    meta: { title: '新建供应商', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/suppliers' }
  },
  {
    path: 'suppliers/:id/edit',
    name: 'SupplierEdit',
    component: () => import('@/features/master-data/suppliers/pages/SupplierFormPage.vue'),
    meta: { title: '编辑供应商', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/suppliers' }
  },
  {
    path: 'suppliers/:id',
    name: 'SupplierDetail',
    component: () => import('@/features/master-data/suppliers/pages/SupplierDetailPage.vue'),
    meta: { title: '供应商详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/suppliers' }
  }
]

// ── Persons ──
const personRoutes: RouteRecordRaw[] = [
  {
    path: 'persons',
    name: 'PersonList',
    component: () => import('@/features/master-data/persons/pages/PersonListPage.vue'),
    meta: { title: '人员管理', roles: PermissionGroups.ALL, icon: 'UserFilled', group: '基础数据', order: 9 }
  },
  {
    path: 'persons/create',
    name: 'PersonCreate',
    component: () => import('@/features/master-data/persons/pages/PersonFormPage.vue'),
    meta: { title: '新建人员', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/persons' }
  },
  {
    path: 'persons/:id/edit',
    name: 'PersonEdit',
    component: () => import('@/features/master-data/persons/pages/PersonFormPage.vue'),
    meta: { title: '编辑人员', roles: PermissionGroups.EDIT, hidden: true, activeMenu: '/persons' }
  },
  {
    path: 'persons/:id',
    name: 'PersonDetail',
    component: () => import('@/features/master-data/persons/pages/PersonDetailPage.vue'),
    meta: { title: '人员详情', roles: PermissionGroups.ALL, hidden: true, activeMenu: '/persons' }
  }
]

// ── Tags ──
const tagRoutes: RouteRecordRaw[] = [
  {
    path: 'tags',
    name: 'TagManagement',
    component: () => import('@/features/master-data/tags/pages/TagManagementPage.vue'),
    meta: { title: '标签管理', roles: PermissionGroups.ALL, icon: 'PriceTag', group: '基础数据', order: 99 }
  },
  {
    path: 'tag-analytics',
    name: 'TagAnalytics',
    component: () => import('@/features/master-data/tags/pages/TagAnalyticsPage.vue'),
    meta: { title: '标签分析', roles: PermissionGroups.ALL, icon: 'TrendCharts', group: '基础数据', order: 100 }
  }
]

/** 基础数据模块所有路由（作为 MainLayout children 挂载） */
export const masterDataRoutes: RouteRecordRaw[] = [
  ...accountRoutes,
  ...fixedDepositRoutes,
  ...categoryRoutes,
  ...projectRoutes,
  ...customerRoutes,
  ...supplierRoutes,
  ...personRoutes,
  ...tagRoutes
]
