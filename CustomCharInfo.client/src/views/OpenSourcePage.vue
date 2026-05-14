<template>
  <v-container max-width="700px" class="page-container">
    <h1 class="mb-1 page-title no-select">Why Open-Source Your Movesets?</h1>

    <div class="content">
      <h2>More examples means everyone gets better</h2>
      <p>Moveset modding has a steep learning curve.</p>
      <p>Good examples of real, working code are one of the best ways to get better at it. Seeing how someone actually handled giving functionality to a move teaches you things that are hard to explain in any tutorial.</p>
      <p>When modders share their source, that knowledge sticks around ungated. It stops living in one person's head and becomes something the whole community can learn from.</p>

      <h2>GitHub is worth it even if you work alone</h2>
      <p>Having a full history of every change you've made is extremely useful. You can see exactly what you changed, when, and why.</p>
      <p>If you ever want to collaborate, it's easy. Other modders can contribute directly or suggest fixes without anyone passing files back and forth.</p>
      <p>And even if you never collaborate with anyone, a remote backup means your work survives a hard drive failure.</p>

      <h2>You don't have to give up control</h2>
      <p>Open-source doesn't mean "anyone can do anything with this." You can pick a license that requires credit, restricts redistribution, or limits other uses.</p>
      <p>Publishing the source is about visibility and learning, not handing ownership over to anyone.</p>

      <p class="closing">
        Nobody is required to share anything. But if you're on the fence, think about the movesets you learned from.
        Someone made the call to share those.
      </p>
      <p>Happy modding.</p>

    </div>
  </v-container>
</template>

<style scoped>
.page-container {
  padding-bottom: 4rem;
}
.page-title {
  font-size: 3em;
}
.content {
  font-size: 18px;
}
h2 {
  font-size: 1.5em;
  margin-top: 1.5em;
  margin-bottom: 0.5em;
}
p {
  opacity: 0.5;
  line-height: 1.6;
  margin-bottom: 0.85em;
}
.closing {
  margin-top: 2em;
}
</style>
