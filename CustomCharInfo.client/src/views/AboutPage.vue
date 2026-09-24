<template>
  <PageShell title="About UMC" tier="narrow">
    <div class="prose">
      <p>
        Hello all! I'm
        <a href="https://github.com/LilyLavender" target="_blank" rel="noopener">Lily</a>, the
        creator of Ultimate Moveset Compatibility. This site is a passion project of mine to bring a
        space both for end-users to see which movesets are available and for developers to ensure
        compatibility between their projects.
      </p>

      <p class="kofi-line">
        I would greatly appreciate if you could
        <a href="https://ko-fi.com/E1E510YMY1" target="_blank" rel="noopener" class="kofi">
          <img
            src="https://storage.ko-fi.com/cdn/kofi1.png?v=6"
            alt="buy me a coffee at ko-fi.com"
            height="36"
          />
        </a>
        to keep this site alive. UMC took months to create, and hosting it is not free.
      </p>

      <p>
        If you have suggestions as to how I can improve UMC, feel free to
        <a
          href="https://github.com/LilyLavender/UltimateMovesetCompatibility/issues"
          target="_blank"
          rel="noopener"
          >create an issue on GitHub</a
        >
        UNLESS it's a security issue, in which case you should contact me directly. Any help is
        appreciated.
      </p>

      <p class="prose-closing">Thank you all for the continued support 💚</p>
    </div>
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
</script>

<style scoped>
.kofi-line {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
}

.kofi {
  display: inline-block;
  line-height: 0;
  text-decoration: none;
}

.kofi img {
  height: 36px;
  display: block;
}
</style>
