<template>
  <v-footer>
    <div class="footer-flex">
      <div class="no-select">
        &copy; {{ new Date().getFullYear() }}
        <a 
          href="https://github.com/LilyLavender/UltimateMovesetCompatibility?tab=readme-ov-file#credits"
          class="offsite unvisitable"
          target="_blank" rel="noopener"
        >Ultimate Moveset Compatibility Development Team</a>
      </div>
      <div>
        <router-link
          :to="{ name: 'AboutPage' }"
          class="unvisitable question"
        >
          About UMC
        </router-link>
      </div>
      <div>
        <router-link
          :to="{ name: 'PhotoSubmissions' }"
          class="unvisitable"
        >
          Photo Submissions
        </router-link>
      </div>
    </div>
  </v-footer>
</template>

<style scoped>
footer {
  /* Color */
  background-color: black;
  color: #dedede;
  /* Center */
  margin: 0 auto;
  text-align: center;
  /* Display at bottom */
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
}

.footer-flex {
  display: flex;
  gap: 2.5em;
}
</style>