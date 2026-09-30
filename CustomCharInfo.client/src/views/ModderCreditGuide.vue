<template>
  <PageShell title="Who should I include?" tier="narrow">
    <div class="prose">
      <h2>Primary creators only</h2>
      <p>
        Only include modders who made significant, direct contributions to the moveset itself. Aim
        for no more than three or four credits total. A single main author is the most common case.
      </p>

      <h2>Examples of who to include</h2>
      <p>
        The modder who did the majority of the work (moveset design, coding, etc). If the coding and
        animations were handled by two different people, both should be credited. Co-leads on a
        collaborative project both belong here.
      </p>

      <h2>Examples of who to leave out</h2>
      <p>
        Someone who gave feedback, tested the moveset, or contributed a single asset. Anyone whose
        involvement was minor or indirect shouldn't be credited on UMC. Leave that for the mod page.
        While their contributions are appreciated, including only the main authors helps keep UMC
        clean and concise.
      </p>

      <h2>Roles</h2>
      <p>
        Each credited modder can also have roles. Pick every role that applies; a solo author often has
        several. Roles are optional and appear under the name on the moveset page and on the modder's profile.
      </p>
      <p>
        "Show on card" controls the small creator line on moveset cards in lists. Turn it off for a
        credit that shouldn't take up that space. At least one modder has to stay on the card.
      </p>

      <h2>Adding as an editor</h2>
      <p>
        When editing a moveset, you can also add users as editors, which allows them to edit the
        moveset while not being listed as a modder.
      </p>

      <p class="prose-closing">
        If you're unsure, ask yourself: would this moveset look or function meaningfully different
        at first glance without their work? If not, they probably don't need to be listed.
      </p>
    </div>
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
</script>
