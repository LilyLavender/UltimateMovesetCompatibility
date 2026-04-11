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
          <li><span class="underline">GameBanana Bitpit</span> - Works just as well as any other, but needs to be unlocked. Great option if already unlocked.</li>
          <li><span class="underline">GitHub Pages</span> - Good for developers.</li>
          <li><span class="underline">Google Drive</span> - Requires you to get the DIRECT link to the image (should contain "drive-viewer"), which can be hard to access. Also needs to be shared. If the image isn't embedding, one of these things is incorrect.</li>
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
code {
  font-size: 13px;
  background-color: #2a2a2a;
  padding: 1px 5px;
  border-radius: 4px;
  color: #d4d4d4;
}
.underline {
  text-decoration: underline;
}
</style>