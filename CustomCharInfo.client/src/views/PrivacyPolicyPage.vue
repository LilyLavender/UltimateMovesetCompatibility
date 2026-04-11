<template>
  <v-container max-width="700px">
    <h1 class="mb-1 page-title no-select">Privacy Policy</h1>

    <div class="content">
      <p class="last-updated">Last updated: April 11, 2026</p>

      <h2>What we collect</h2>
      <p>
        When you create an account, we store your email address and a salted & hashed password.
        Your email address is used only for account-related purposes and is never shared with third parties or displayed publicly.
      </p>

      <h2>What we don't collect</h2>
      <p>
        UMC does not use tracking cookies, analytics services, or advertising.
        We do not collect IP addresses, device information, or browsing behavior.
      </p>

      <h2>Third-party services</h2>
      <p>
        Modder profile pages may display profile pictures fetched from the GameBanana API using a modder's GameBanana ID, if they have provided one.
        This request is made client-side by your browser; UMC does not proxy or store any data returned from GameBanana.
      </p>

      <h2>Data visibility</h2>
      <p>
        Your username is publicly visible on any moveset, series, or hook you are credited on.
        Your email address is never displayed publicly
        Modder profiles may optionally display a Discord username, which is provided voluntarily.
      </p>

      <h2>Account deletion</h2>
      <p>
        If you would like your account or submitted content removed, please contact Lily on Discord.
      </p>

      <h2>Contact</h2>
      <p>
        For any privacy-related questions or requests, contact Lily on Discord.
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
.last-updated {
  font-size: 14px;
  margin-bottom: 1.5em;
}
h2 {
  font-size: 1.4em;
  margin-top: 1.5em;
  margin-bottom: 0.3em;
}
p {
  opacity: 0.5;
  line-height: 1.6;
  margin-bottom: 0.75em;
}
</style>
