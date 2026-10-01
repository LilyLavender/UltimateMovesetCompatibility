<template>
  <div class="links-grid">
    <LabeledField label="Mod page" class="span-2">
      <v-text-field v-model="modPageUrl" type="url" :placeholder="`${GB_PAGE_URL}12345`" />
    </LabeledField>
    <LabeledField label="GameBanana WIP page">
      <v-text-field
        :model-value="gamebananaWipId"
        :prefix="GB_WIP_URL"
        inputmode="numeric"
        @update:model-value="(v) => (gamebananaWipId = digitsOnly(v))"
      />
    </LabeledField>
    <LabeledField label="SSBU Mods Wiki page">
      <v-text-field v-model="modsWikiLink" :prefix="MODS_WIKI_URL" />
    </LabeledField>
    <LabeledField label="Source code" class="span-2">
      <v-text-field v-model="sourceCode" type="url" />
      <router-link to="/open-source" target="_blank" class="field-link">
        Why should I open-source my movesets?
      </router-link>
    </LabeledField>
  </div>
</template>

<script setup>
import { GB_PAGE_URL, GB_WIP_URL, MODS_WIKI_URL } from '@/globals'
import LabeledField from '@/components/LabeledField.vue'

// The moveset form's links block. The parent wraps it in a FormSection.
const modPageUrl = defineModel('modPageUrl', { type: String, default: '' })
const gamebananaWipId = defineModel('gamebananaWipId', { type: [Number, String], default: null })
const modsWikiLink = defineModel('modsWikiLink', { type: String, default: '' })
const sourceCode = defineModel('sourceCode', { type: String, default: '' })

const digitsOnly = (value) => (value == null ? value : String(value).replace(/\D+/g, ''))
</script>

<style scoped>
.links-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  align-items: start;
}

.span-2 {
  grid-column: span 2;
}

.field-link {
  align-self: flex-start;
  margin-top: 4px;
  font-size: 12px;
  color: var(--tx-2);
  text-decoration: underline;
}

/* The URL prefixes are hints, so they sit dimmer than typed text */
:deep(.v-text-field__prefix__text) {
  color: var(--tx-3);
  font-size: 13px;
}

@media (max-width: 959px) {
  .links-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: span 1;
  }
}
</style>
