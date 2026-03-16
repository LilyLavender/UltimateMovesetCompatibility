<template>
  <v-container max-width="700px">
    <!-- Page title -->
    <h1 class="mb-1 page-title no-select">Image Hosting</h1>

    <!-- Main content -->
    <div>
      <p class="mb-5">
        Because UMC no longer hosts images itself, users must find a place to host moveset/series images on the site. This is fairly simple, but here's some guidelines:<br>
        <ul>
          <li>Make sure you're linking to an image directly and not the page the image is on.</li>
        </ul>
      </p>
      <p class="mb-5">
        Good image hosts to use include:
        <ul>
          <li><a href="https://lensdump.com" class="unvisitable" target="_blank">lensdump.com</a> - Doesn't require sign-in, doesn't compress images.</li>
          <li><a href="https://postimage.io" class="unvisitable" target="_blank">postimage.io</a> - Doesn't require sign-in, doesn't compress images.</li>
          <li><a href="https://catbox.moe" class="unvisitable" target="_blank">catbox.moe</a> - Doesn't require sign-in, doesn't compress images.</li>
          <li>GitHub Pages - Good for developers.</li>
        </ul>
        Image hosts to avoid:
        <ul>
          <li>Discord - Images uploaded to Discord can only be embedded for three days after upload.</li>
          <li>Imgur - Compresses images</li>
          <li>Tinypic - Compresses images</li>
        </ul>
      </p>

      <p>
        If you have suggestions for image hosts to add, feel free to DM Lily on Discord
      </p>
    
    </div>
  </v-container>
</template>

<style scoped>
ul {
  font-size: 16px;
  margin-left: 2em;
  color: grey;
}
</style>