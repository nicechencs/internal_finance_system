<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { useSiteBrandStore } from '@/features/system/stores/siteBrand'

const brandStore = useSiteBrandStore()

onMounted(() => {
  void brandStore.load()
})
</script>

<template>
  <RouterView />
</template>

<style>
#app {
  font-family: var(--font-family-base);
  width: 100%;
  height: 100%;
}
</style>
