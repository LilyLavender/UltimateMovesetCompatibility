<template>
  <footer class="site-footer">
    <nav class="site-footer__links" aria-label="Site">
      <router-link v-for="link in footerLinks" :key="link.label" :to="link.to">
        {{ link.label }}
      </router-link>
    </nav>
    <p class="site-footer__copy no-select">
      &copy; {{ new Date().getFullYear() }}
      <a
        href="https://github.com/LilyLavender/UltimateMovesetCompatibility?tab=readme-ov-file#credits"
        class="offsite"
        target="_blank"
        rel="noopener"
        >UMC Development Team</a
      >
    </p>
  </footer>
</template>

<script setup>
import { footerLinks } from '@/navigation'
</script>

<style scoped>
.site-footer {
  position: relative;
  z-index: 1;
  margin-top: 56px;
  padding: 18px var(--gutter) 22px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  color: var(--tx-2);
  font-size: 13px;
  text-align: center;
}

.site-footer__links {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px 28px;
}

.site-footer__links a {
  color: var(--tx-2);
  text-decoration: none;
}

.site-footer__links a:hover {
  color: var(--white);
}

.site-footer__copy {
  margin: 0;
  color: var(--tx-3);
}

.site-footer__copy a {
  color: var(--tx-3);
}
</style>
