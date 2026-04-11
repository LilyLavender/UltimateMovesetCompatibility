<template>
  <v-container max-width="700px">
    <h1 class="mb-1 page-title no-select">When to Submit a Moveset</h1>

    <div class="content">
      <h2>Completed Movesets</h2>
      <p>
        Completed movesets are welcome in any form.
        As long as the moveset contains at least a handful of new moves, it qualifies.
        This includes movesets that are modpack-exclusive, bundled with other projects, or similar releases.
      </p>

      <h2>WIP Movesets</h2>
      <p>
        WIP movesets must be actively in progress or have genuine intent to move forward.
        A moveset with zero (or very little) work done should not be submitted.
        A good benchmark is at least a couple days' worth of work already put in before submitting.
        This is a measure put in place to prevent projects that are nothing more than concepts from flooding the site.
      </p>

      <p class="closing">
        If you intend to submit a moveset and do not know whether or not it will be allowed, please message Lily for clarification.
        At the end of the day, what's on UMC is a choice of moveset modders.
        To all parties involved, please be respectful to other creators.
      </p>
    </div>
  </v-container>
</template>

<style scoped>
.page-title {
  font-size: 3em;
}
.content {
  font-size: 18px;
}
h2 {
  font-size: 1.5em;
  margin-top: 1.25em;
  margin-bottom: 0.25em;
}
p:not(.closing) {
  opacity: 0.5;
  line-height: 1.6;
  margin-bottom: 1em;
}
.closing {
  margin-top: 2em;
}
</style>
