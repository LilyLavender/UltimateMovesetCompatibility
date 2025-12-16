<template>
  <v-container max-width="700px">
    <!-- Page title -->
    <h1 class="mb-1 page-title no-select">About UMC</h1>

    <!-- Main content -->
    <div class="space-between-ps">
      <!-- Intro -->
      <p>Hello all! I'm <a href="https://github.com/LilyLavender" target="_blank" class="unvisitable">LilyLambda</a>, the creator of Ultimate Moveset Compatibility. This site is a passion project of mine to bring a space both for end-users to see which movesets are available and for developers to ensure compatibility between their projects.</p>

      <!-- Ko-Fi -->
      <p class="has-inline-image">
        <span>I would greatly appreciate if you could</span>
        <a class="d-inline" href='https://ko-fi.com/E1E510YMY1' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://storage.ko-fi.com/cdn/kofi1.png?v=6' border='0' alt='buy me a coffee at ko-fi.com' /></a>
        <span>to keep this site alive. UMC took months to create, and hosting it is not free.</span>
      </p>

      <!-- Suggestions -->
      <p>If you have suggestions as to how I can improve UMC, feel free to reach out to me on Discord @LilyLambda or <a href="https://github.com/LilyLavender/UltimateMovesetCompatibility/issues" target="_blank" class="unvisitable">create an issue on GitHub</a>. Any help is appreciated.</p>

      <!-- Outro -->
      <p>Thank you all for the continued support 💚</p>

    </div>
  </v-container>
</template>

<style scoped>
.space-between-ps > p:not(:last-child) {
  margin-bottom: 2em;
}

.has-inline-image > * {
  display: inline;
}

.has-inline-image > a {
  margin-left: 0.5em;
  margin-right: 0.5em;
}

.has-inline-image > a {
  position: relative;
  display: inline-block;
}

.has-inline-image > a > img {
  transform: translateY(12px);
  border-radius: 12px;
}

.has-inline-image > a::after {
  content: "";
  position: absolute;
  inset: 0;
  pointer-events: none;
  height: 36px;
  border-radius: 12px;
  transform: translateY(-9px);

  box-shadow: 0px 0px 6px -2px #e3d6c6;

  opacity: 0.4;
  animation: pulse 3s ease-in-out infinite alternate;
}

@keyframes pulse {
  from {
    opacity: 0.4;
  }
  to {
    opacity: 1;
  }
}
</style>