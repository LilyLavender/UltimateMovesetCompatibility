<template>
  <PageShell title="Forgot password?" tier="narrow">
    <div class="prose">
      <p>UMC doesn't use an automated email service, so password resets are handled by hand.</p>
      <p>
        Send an admin a message and they'll clear your account and get you a reset link as soon as
        they can.
      </p>
    </div>
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
</script>

<style scoped>
.prose {
  color: var(--tx-2);
  line-height: 1.6;
}

.prose p {
  margin: 0 0 0.75em;
}
</style>
