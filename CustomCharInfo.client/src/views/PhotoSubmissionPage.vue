<template>
  <v-container max-width="800px">
    <!-- Page title -->
    <h1 class="mb-1 page-title no-select">Photo Submissions</h1>

    <!-- Main content -->
    <div class="space-between-ps font-size-18">
      <p>So, you'd like to contribute to the scrolling banner?</p>

      <p>
        Great to hear. Here's some basic guidelines to follow when submitting photos.
        <ol>
          <li>Photos should be of a high quality moveset, with some exceptions.</li>
          <li>Photos must either be 1920x1080 or 1280x720.</li>
          <li>Only one photo is allowed per character.</li>
          <li>Photos should only contain the modded character, with some exceptions.</li>
          <li>The modded character should be the subject of the photo.</li>
          <li>Photos should be interesting to look at, effectively using one or many <a href="https://guides.lib.berkeley.edu/design" target="_blank" class="offsite unvisitable">Principles of Art and Design</a>.</li>
          <li>Characters should contrast the background.</li>
          <li>Photos should not use filters, with some exceptions. The "Super Smash Bros. Ultimate" filter is a hard no.</li>
          <li>Photos should be of a character's default costume, with some exceptions usually being alternative designs.</li>
          <li>Try to avoid stages that have already been used on screenshots on the banner.</li>
        </ol>
        More detail on any guideline can be provided upon request.
      </p>

      <p>If you'd like to submit a photo, feel free to reach out to Lily on Discord @LilyLambda for consideration.</p>

    </div>
  </v-container>
</template>

<style scoped>
.space-between-ps > p:not(:last-child) {
  margin-bottom: 2em;
}

.page-title {
  font-size: 5em;
}

.font-size-18 {
  font-size: 18px;
}

ol {
  font-size: 16px;
  margin-left: 2em;
  color: grey;
}
</style>