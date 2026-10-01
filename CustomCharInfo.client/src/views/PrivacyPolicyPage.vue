<template>
  <PageShell title="Privacy policy" tier="narrow">
    <div class="prose">
      <p class="prose-meta">Last updated: September 11, 2026</p>

      <h2>What we collect</h2>
      <p>
        When you create an account, we store your email address and a salted and hashed password
        (meaning your actual password never touches our database). Your email address is used only
        for account-related purposes and is never shared with third parties or displayed publicly.
      </p>
      <p>
        We also record the IP address you log in from to help restrict abusive accounts. IP
        addresses are never displayed publicly and are not shared with third parties.
      </p>

      <h2>What we don't collect</h2>
      <p>
        UMC does not use tracking cookies, analytics services, or advertising. We do not track your
        browsing behavior or device information.
      </p>

      <h2>Third-party services</h2>
      <p>
        Modder profile pages may display profile pictures fetched from the GameBanana API using a
        modder's GameBanana ID, if they have provided one. This request is made client-side by your
        browser; UMC does not proxy or store any data returned from GameBanana.
      </p>

      <h2>Data visibility</h2>
      <p>
        Your username is publicly visible on any moveset, series, or hook you are credited on. Your
        email address is never displayed publicly. Modder profiles may optionally display a Discord
        username, which is provided voluntarily.
      </p>

      <h2>Account deletion</h2>
      <p>
        If you would like your account or submitted content removed, please contact Lily on Discord.
      </p>

      <h2>Contact</h2>
      <p>For any privacy-related questions or requests, please contact Lily on Discord.</p>
    </div>
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
</script>
