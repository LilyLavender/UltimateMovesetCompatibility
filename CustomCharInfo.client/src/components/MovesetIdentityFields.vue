<template>
  <!-- Internal ID -->
  <v-col cols="12" sm="4">
    <v-checkbox v-model="showSeparateIds" label="Slotted ID and Replacement ID are different" />
  </v-col>
  <v-col v-if="!showSeparateIds" cols="12" sm="4">
    <v-text-field v-model="slottedId" variant="outlined" label="Internal ID">
      <template #label>Internal ID <span class="required-asterisk">*</span></template>
    </v-text-field>
  </v-col>
  <v-col v-else cols="12" sm="8" class="two-of-them">
    <v-col cols="6">
      <v-text-field v-model="slottedId" variant="outlined" label="Slotted ID">
        <template #label>Slotted ID <span class="required-asterisk">*</span></template>
      </v-text-field>
    </v-col>
    <v-col cols="6">
      <v-text-field v-model="replacementId" variant="outlined" label="Replacement ID" />
    </v-col>
  </v-col>

  <!-- Vanilla Character -->
  <v-col cols="12" sm="4">
    <v-select
      v-model="vanillaCharInternalName"
      variant="outlined"
      :items="vanillaChars"
      item-title="displayName"
      item-value="vanillaCharInternalName"
      label="Vanilla Character"
    >
      <template #label>Vanilla Character <span class="required-asterisk">*</span></template>
      <template #item="{ props: itemProps, item }">
        <v-list-item v-bind="itemProps" class="remove-bound-props">
          <div class="filter-option">
            <div>
              <v-img
                :src="stockIconUrl(item.raw.vanillaCharInternalName)"
                class="stock-icon-small"
              />
            </div>
            <v-list-item-title>{{ item.raw.displayName }}</v-list-item-title>
          </div>
        </v-list-item>
      </template>
      <template #selection="{ item }">
        <div class="filter-option d-flex align-center">
          <v-avatar class="me-2" size="26">
            <v-img :src="stockIconUrl(item.raw.vanillaCharInternalName)" />
          </v-avatar>
          <span>{{ item.raw.displayName }}</span>
        </div>
      </template>
    </v-select>
  </v-col>

  <!-- Slots -->
  <v-col cols="12" sm="2" class="left-merged-input-container">
    <v-text-field
      :model-value="slotsStart"
      variant="outlined"
      label="Start Slot"
      :min="8"
      :max="255"
      prefix="c"
      class="left-merged-input"
      @update:model-value="(v) => (slotsStart = digitsOnly(v))"
      @blur="checkSlotAlignment"
    >
      <template #label>Start Slot <span class="required-asterisk">*</span></template>
    </v-text-field>
  </v-col>
  <v-col cols="12" sm="2" class="right-merged-input-container">
    <v-text-field
      :model-value="slotsEnd"
      variant="outlined"
      label="End Slot"
      :min="8"
      :max="255"
      prefix="c"
      class="right-merged-input"
      @update:model-value="(v) => (slotsEnd = digitsOnly(v))"
      @blur="checkSlotAlignment"
    >
      <template #label>End Slot <span class="required-asterisk">*</span></template>
    </v-text-field>
  </v-col>

  <!-- Slot alignment warning -->
  <v-dialog v-model="slotWarningDialog" max-width="480">
    <v-card color="#2e2e2e">
      <v-card-title>
        <v-icon>mdi-alert</v-icon>
        Unusual Slot Range
      </v-card-title>
      <v-card-text>
        Slots c{{ slotsStart }} through c{{ slotsEnd }} aren't a standard 8-slot-aligned range (e.g.
        c08-c15, c120-c127). Please double-check this is intentional before saving.
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="dismissSlotWarning">Dismiss</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, watch } from 'vue'

// The identity block of the moveset form: internal IDs, vanilla character, and costume slot range.
// Renders as a fragment of v-cols so the parent's v-row keeps its layout.
defineProps({
  vanillaChars: { type: Array, default: () => [] },
})

const slottedId = defineModel('slottedId', { type: String, default: null })
const replacementId = defineModel('replacementId', { type: String, default: null })
const vanillaCharInternalName = defineModel('vanillaCharInternalName', {
  type: String,
  default: '',
})
const slotsStart = defineModel('slotsStart', { type: [Number, String], default: null })
const slotsEnd = defineModel('slotsEnd', { type: [Number, String], default: null })

const stockIconUrl = (internalName) =>
  `/UltimateMovesetCompatibility/vanilla-stock-icons/chara_2_${internalName}.png`

// Most movesets use one ID for both; the checkbox reveals the second field.
// Loaded data with two different IDs switches it on automatically.
const showSeparateIds = ref(false)
watch(
  [slottedId, replacementId],
  ([slotted, replacement]) => {
    if (replacement != null && replacement !== slotted) showSeparateIds.value = true
  },
  { immediate: true }
)
watch(slottedId, (val) => {
  if (!showSeparateIds.value) replacementId.value = val
})

// defineModel refs only update once the parent re-renders, so handlers must use the event's value, not the ref.
const digitsOnly = (value) => (value == null ? value : String(value).replace(/\D+/g, ''))

// Slot ranges are normally 8-aligned (c08-c15, c120-c127); anything else is probably a typo, so warn once per range.
const slotWarningDialog = ref(false)
let dismissedSlotRange = null

const isSlotRangeClean = (start, end) => start % 8 === 0 && (end - start + 1) % 8 === 0

const checkSlotAlignment = () => {
  const start = parseInt(slotsStart.value)
  const end = parseInt(slotsEnd.value)
  if (isNaN(start) || isNaN(end)) return
  if (isSlotRangeClean(start, end)) return
  if (dismissedSlotRange && dismissedSlotRange[0] === start && dismissedSlotRange[1] === end) return
  slotWarningDialog.value = true
}

const dismissSlotWarning = () => {
  dismissedSlotRange = [parseInt(slotsStart.value), parseInt(slotsEnd.value)]
  slotWarningDialog.value = false
}
</script>

<style scoped>
.required-asterisk {
  color: #cf6679;
}

/* Fix for showing/hiding extra ID input */
.two-of-them {
  display: flex;
}
.two-of-them > .v-col {
  padding-top: 0;
  padding-bottom: 0;
}
.two-of-them > .v-col:first-of-type {
  padding-left: 0;
}
.two-of-them > .v-col:last-of-type {
  padding-right: 0;
}

/* Dropdown display */
.stock-icon-small {
  width: 20px;
}
.filter-option {
  display: flex;
}
:deep(.filter-option div) {
  margin-right: 4px;
}
.remove-bound-props :deep(.v-list-item-title:not(.filter-option .v-list-item-title)) {
  display: none;
}
.v-avatar {
  background: transparent;
}

/* Merge the two slot inputs into one control */
.left-merged-input-container {
  padding-right: 0;
}
.right-merged-input-container {
  padding-left: 0;
}
.right-merged-input :deep(.v-field) {
  border-top-left-radius: 0;
  border-bottom-left-radius: 0;
}
.left-merged-input :deep(.v-field) {
  border-top-right-radius: 0;
  border-bottom-right-radius: 0;
}
.right-merged-input :deep(.v-field__outline__start) {
  border-left: none;
}
.left-merged-input :deep(.v-field__outline__end) {
  border-right: none;
}
:deep(.v-text-field__prefix__text) {
  color: #e4e4e4;
}
</style>
