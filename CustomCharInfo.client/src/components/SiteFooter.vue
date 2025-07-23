<template>
  <v-footer>
    <div class="no-select">
      &copy; {{ new Date().getFullYear() }}
      <a 
        href="https://github.com/LilyLavender/UltimateMovesetCompatibility?tab=readme-ov-file#credits"
        class="offsite unvisitable"
        target="_blank" rel="noopener"
      >Ultimate Moveset Compatibility Development Team</a>
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
</style>