<template>
  <v-container max-width="700px">
    <h1 class="mb-1 page-title no-select">Who Should I Include?</h1>

    <div class="content">
      <h2>Primary Creators Only</h2>
      <p>
        Only include modders who made significant, direct contributions to the moveset itself.
        Aim for no more than three or four credits total.
        A single main author is the most common case.
      </p>

      <h2>Examples of Who to Include</h2>
      <p>
        The modder who did the majority of the work (moveset design, coding, etc).
        If the coding and animations were handled by two different people, both should be credited.
        Co-leads on a collaborative project both belong here.
      </p>

      <h2>Examples of Who to Leave Out</h2>
      <p>
        Someone who gave feedback, tested the moveset, or contributed a single asset.
        Anyone whose involvement was minor or indirect shouldn't be credited on UMC. Leave that for the mod page.
        While their contributions are appreciated, including only the main authors helps keep UMC clean and concise.
      </p>

      <p class="closing">
        If you're unsure, ask yourself: would this moveset look or function meaningfully different at first glance without their work?
        If not, they probably don't need to be listed.
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
  margin-bottom: 2em;
}
</style>
