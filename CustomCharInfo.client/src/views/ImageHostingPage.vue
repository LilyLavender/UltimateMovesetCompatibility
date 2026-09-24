<template>
  <PageShell title="Image hosting" tier="narrow">
    <div class="prose">
      <p>
        UMC used to require users to host images themselves. This requirement has been lifted, but if you still wish to self-host, here are some guidelines:
      </p>
      <ul>
        <li>Make sure you're linking to an image directly and not the page the image is on.</li>
      </ul>

      <h2>Good image hosts</h2>
      <ul>
        <li>
          <a href="https://lensdump.com" target="_blank" rel="noopener">lensdump.com</a>: doesn't
          require sign-in, doesn't compress images.
        </li>
        <li>
          <a href="https://postimage.io" target="_blank" rel="noopener">postimage.io</a>: doesn't
          require sign-in, doesn't compress images.
        </li>
        <li>
          <a href="https://catbox.moe" target="_blank" rel="noopener">catbox.moe</a>: doesn't
          require sign-in, doesn't compress images.
        </li>
        <li>
          <strong>GameBanana Bitpit</strong>: works just as well as any other, but needs to be
          unlocked. Great option if already unlocked.
        </li>
        <li><strong>GitHub Pages</strong>: good for developers.</li>
        <li>
          <strong>Google Drive</strong>: requires you to get the direct link to the image (it should
          contain "drive-viewer"), which can be hard to access. Also needs to be shared. If the
          image isn't embedding, one of these things is incorrect.
        </li>
      </ul>

      <h2>Image hosts to avoid</h2>
      <ul>
        <li>
          Discord: images uploaded to Discord can only be embedded for three days after upload.
        </li>
        <li>Imgur: compresses images.</li>
        <li>Tinypic: compresses images.</li>
      </ul>

      <p class="prose-closing">
        If you have suggestions for image hosts to add, feel free to message Lily.
      </p>
    </div>
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
</script>
