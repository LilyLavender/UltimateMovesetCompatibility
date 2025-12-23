<template>
  <HookForm mode="edit" :hookId="parseInt(route.params.hookId)" />
</template>

<script setup>
import HookForm from '@/components/HookForm.vue'
import { useRoute } from 'vue-router'

const route = useRoute()
</script>
