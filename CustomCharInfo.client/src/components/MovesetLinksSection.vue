<template>
  <section class="links-section">
    <h2>Links</h2>
    <v-row>
      <!-- Mod Page -->
      <v-col cols="12">
        <v-text-field
          v-model="modPageUrl"
          variant="outlined"
          label="Mod Page"
          type="url"
          :placeholder="`${GB_PAGE_URL}12345`"
        />
      </v-col>
      <!-- GameBanana WIP -->
      <v-col cols="12">
        <v-text-field
          :model-value="gamebananaWipId"
          variant="outlined"
          label="GameBanana WIP Page"
          :prefix="GB_WIP_URL"
          @update:model-value="(v) => (gamebananaWipId = digitsOnly(v))"
        />
      </v-col>
      <!-- Mods Wiki -->
      <v-col cols="12">
        <v-text-field
          v-model="modsWikiLink"
          variant="outlined"
          label="SSBU Mods Wiki Page"
          :prefix="MODS_WIKI_URL"
        />
      </v-col>
      <!-- Source Code -->
      <v-col cols="12">
        <v-text-field v-model="sourceCode" variant="outlined" label="Source Code URL" type="url">
          <template #details>
            <router-link
              to="/open-source"
              class="offsite unvisitable text-decoration-none"
              target="_blank"
              >Why should I open-source my movesets?</router-link
            >
          </template>
        </v-text-field>
      </v-col>
    </v-row>
  </section>
</template>

<script setup>
import { GB_PAGE_URL, GB_WIP_URL, MODS_WIKI_URL } from '@/globals'

const modPageUrl = defineModel('modPageUrl', { type: String, default: '' })
const gamebananaWipId = defineModel('gamebananaWipId', { type: [Number, String], default: null })
const modsWikiLink = defineModel('modsWikiLink', { type: String, default: '' })
const sourceCode = defineModel('sourceCode', { type: String, default: '' })

const digitsOnly = (value) => (value == null ? value : String(value).replace(/\D+/g, ''))
</script>

<style scoped>
h2 {
  font-size: 2.25em;
  margin-bottom: 10px;
}
/* The URL prefixes are hints, so they sit dimmer than typed text */
:deep(.v-text-field__prefix__text) {
  color: #9b9b9b;
}
</style>
