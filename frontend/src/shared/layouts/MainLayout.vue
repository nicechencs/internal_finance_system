<template>
  <el-container class="main-layout" :class="{ 'is-mobile': isMobile }">
    <el-aside v-show="!isMobile" width="220px" class="sidebar">
      <div class="logo-area">
        <el-icon :size="28" color="#818CF8"><Coin /></el-icon>
        <span class="logo-text">{{ brandStore.siteName }}</span>
      </div>
      <AppNavMenu :active-menu="activeMenu" @select="handleMenuSelect" />
    </el-aside>

    <el-container class="main-container">
      <el-header class="header">
        <div class="header-left">
          <el-button
            v-if="isMobile"
            class="menu-toggle"
            text
            @click="drawerVisible = true"
          >
            <el-icon :size="22"><Expand /></el-icon>
          </el-button>
          <h1 v-if="isMobile" class="mobile-page-title">{{ currentPageTitle }}</h1>
          <el-breadcrumb v-else separator="/">
            <el-breadcrumb-item
              v-for="item in breadcrumbs"
              :key="item.path"
              :to="item.path"
            >
              {{ item.title }}
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>

        <div class="header-right">
          <el-dropdown trigger="click" @command="handleCommand">
            <span class="user-dropdown">
              <el-icon :size="18"><UserFilled /></el-icon>
              <span class="username">{{ userStore.user?.fullName || '用户' }}</span>
              <el-icon :size="14"><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile" :icon="User">
                  个人资料
                </el-dropdown-item>
                <el-dropdown-item command="security" :icon="Setting">
                  账号安全
                </el-dropdown-item>
                <el-dropdown-item command="logout" :icon="SwitchButton">
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="main-content">
        <router-view v-slot="{ Component, route: viewRoute }">
          <transition name="fade-slide" mode="out-in">
            <component :is="Component" :key="viewRoute.fullPath" />
          </transition>
        </router-view>
      </el-main>

      <el-footer v-show="!isMobile" height="48px" class="footer">
        <div class="footer-content">
          <div class="footer-left">
            <span class="copyright">&copy; 2026 {{ brandStore.siteName }}</span>
            <span class="divider">|</span>
            <span class="version">v1.0.0</span>
          </div>
          <div class="footer-right">
            <span class="tech-stack">基于 .NET 8 + Vue 3 构建</span>
          </div>
        </div>
      </el-footer>
    </el-container>

    <el-drawer
      v-model="drawerVisible"
      class="mobile-nav-drawer"
      direction="ltr"
      size="264px"
      :with-header="false"
      :append-to-body="true"
    >
      <div class="sidebar mobile-sidebar">
        <div class="logo-area">
          <el-icon :size="28" color="#818CF8"><Coin /></el-icon>
          <span class="logo-text">{{ brandStore.siteName }}</span>
        </div>
        <AppNavMenu :active-menu="activeMenu" @select="handleMobileMenuSelect" />
      </div>
    </el-drawer>
  </el-container>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowDown,
  Coin,
  Expand,
  Setting,
  SwitchButton,
  User,
  UserFilled
} from '@element-plus/icons-vue'
import { logout as logoutRequest } from '@/features/auth/api/auth'
import { useBreakpoint } from '@/shared/composables/useBreakpoint'
import { useUserStore } from '@/features/auth/stores/user'
import { useSiteBrandStore } from '@/features/system/stores/siteBrand'
import AppNavMenu from '@/shared/layouts/AppNavMenu.vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const brandStore = useSiteBrandStore()
const { isMobile } = useBreakpoint()
const drawerVisible = ref(false)

const activeMenu = computed(() => {
  return (route.meta.activeMenu as string) || route.path
})

const currentPageTitle = computed(() => {
  return (route.meta.title as string) || brandStore.siteName
})

const handleMenuSelect = (index: string) => {
  if (index !== route.path) {
    router.push(index)
  }
}

const handleMobileMenuSelect = (index: string) => {
  handleMenuSelect(index)
  drawerVisible.value = false
}

const breadcrumbs = computed(() => {
  return route.matched
    .filter((item) => item.meta?.title)
    .map((item) => ({
      path: item.path || '/',
      title: item.meta.title as string
    }))
})

const handleCommand = async (command: string) => {
  if (command === 'profile') {
    router.push('/account-profile')
    return
  }

  if (command === 'security') {
    router.push('/account-security')
    return
  }

  if (command === 'logout') {
    try {
      await logoutRequest()
    } catch {
      // Ignore logout API errors and clear local state anyway.
    }

    userStore.logout()
    router.push('/login')
  }
}
</script>

<style scoped>
.main-layout {
  height: 100vh;
  height: 100dvh;
  overflow: hidden;
}

.sidebar {
  background-color: var(--bg-sidebar);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.mobile-sidebar {
  height: 100%;
  padding-top: var(--safe-area-top);
}

.logo-area {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 0 16px;
  border-bottom: 1px solid var(--bg-sidebar-hover);
  flex-shrink: 0;
}

.logo-text {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-placeholder);
  white-space: nowrap;
  letter-spacing: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 150px;
}

.header {
  height: calc(var(--header-height) + var(--safe-area-top)) !important;
  padding-top: var(--safe-area-top);
  background-color: var(--bg-card);
  border-bottom: 1px solid var(--border-base);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-left: 20px;
  padding-right: 20px;
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.menu-toggle {
  width: var(--touch-target-min);
  height: var(--touch-target-min);
  padding: 0;
  color: var(--text-primary);
}

.mobile-page-title {
  margin: 0;
  font-size: var(--font-size-page-title);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-dropdown {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  color: var(--text-primary);
  padding: 6px 12px;
  border-radius: 6px;
  min-height: var(--touch-target-min);
  transition: background-color 0.2s;
}

.user-dropdown:hover {
  background-color: var(--bg-hover);
}

.username {
  font-size: 14px;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.main-container {
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}

.main-content {
  background-color: var(--bg-page);
  padding: var(--layout-page-padding);
  min-width: 0;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
  flex: 1;
}

.footer {
  background-color: var(--bg-card);
  border-top: 1px solid var(--border-base);
  display: flex;
  align-items: center;
  padding: 0 20px;
  flex-shrink: 0;
}

.footer-content {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.footer-left,
.footer-right {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 13px;
  color: var(--text-secondary);
}

.copyright {
  font-weight: 500;
}

.divider {
  color: var(--border-base);
}

.version {
  color: var(--text-secondary);
  font-family: 'Courier New', monospace;
}

.tech-stack {
  color: var(--text-secondary);
}

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(12px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-12px);
}

.is-mobile .header {
  padding-left: 8px;
  padding-right: 12px;
}

.is-mobile .username {
  max-width: 72px;
}

.is-mobile .main-content {
  padding-bottom: calc(var(--layout-page-padding) + var(--safe-area-bottom));
}
</style>
