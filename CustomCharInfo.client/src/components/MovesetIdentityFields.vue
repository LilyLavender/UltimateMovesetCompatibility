<template>
  <!-- Internal IDs -->
  <div class="check-item span-3">
    <v-checkbox
      v-model="showSeparateIds"
      label="Slotted ID and Replacement ID are different"
      hide-details
    />
  </div>
  <LabeledField v-if="!showSeparateIds" label="Internal ID" required>
    <v-text-field v-model="slottedId" class="mono-input" />
  </LabeledField>
  <template v-else>
    <LabeledField label="Slotted ID" required>
      <v-text-field v-model="slottedId" class="mono-input" />
    </LabeledField>
    <LabeledField label="Replacement ID">
      <v-text-field v-model="replacementId" class="mono-input" />
    </LabeledField>
  </template>

  <!-- Vanilla Character -->
  <LabeledField label="Vanilla character" required>
    <v-select
      v-model="vanillaCharInternalName"
      :items="vanillaChars"
      item-title="displayName"
      item-value="vanillaCharInternalName"
    >
      <template #item="{ props: itemProps, item }">
        <v-list-item v-bind="itemProps" class="remove-bound-props">
          <div class="filter-option">
            <img :src="stockIconUrl(item.raw.vanillaCharInternalName)" class="stock-icon" alt="" />
            <v-list-item-title>{{ item.raw.displayName }}</v-list-item-title>
          </div>
        </v-list-item>
      </template>
      <template #selection="{ item }">
        <div class="filter-option">
          <img :src="stockIconUrl(item.raw.vanillaCharInternalName)" class="stock-icon" alt="" />
          <span>{{ item.raw.displayName }}</span>
        </div>
      </template>
    </v-select>
  </LabeledField>

  <!-- Slots -->
  <LabeledField label="Slots" required hint="Costume slots the moveset occupies, from 8 to 255.">
    <div class="slot-range">
      <v-text-field
        :model-value="slotsStart"
        placeholder="Start"
        prefix="c"
        class="mono-input"
        inputmode="numeric"
        @update:model-value="(v) => (slotsStart = digitsOnly(v))"
        @blur="checkSlotAlignment"
      />
      <span class="slot-range__to">to</span>
      <v-text-field
        :model-value="slotsEnd"
        placeholder="End"
        prefix="c"
        class="mono-input"
        inputmode="numeric"
        @update:model-value="(v) => (slotsEnd = digitsOnly(v))"
        @blur="checkSlotAlignment"
      />
    </div>
  </LabeledField>

  <!-- Slot alignment warning -->
  <v-dialog v-bind="dialogProps" v-model="slotWarningDialog" max-width="480">
    <v-card>
      <v-card-title class="dialog-title">
        <v-icon>mdi-alert</v-icon>
        Unusual slot range
      </v-card-title>
      <v-card-text>
        Slots {{ formatSlot(slotsStart) }} through {{ formatSlot(slotsEnd) }} aren't a standard
        8-slot-aligned range (e.g. c08-c15, c120-c127). Please double-check this is intentional
        before saving.
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <AppButton variant="ghost" @click="dismissSlotWarning">Dismiss</AppButton>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, watch } from 'vue'
import { formatSlot } from '@/services/slots'
import { useDialogProps } from '@/composables/useDialogProps'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
const dialogProps = useDialogProps()

// The identity block of the moveset form: internal IDs, vanilla character, and costume slot range.
// Renders as a fragment of fields so the parent's form grid keeps its layout.
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
.check-item {
  display: flex;
  align-items: center;
}

.span-3 {
  grid-column: span 3;
}

.mono-input :deep(input),
.mono-input :deep(.v-text-field__prefix__text) {
  font-family: var(--font-mono);
}

.stock-icon {
  width: 22px;
  height: 22px;
  object-fit: contain;
}

.filter-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.remove-bound-props :deep(.v-list-item-title:not(.filter-option .v-list-item-title)) {
  display: none;
}

.slot-range {
  display: flex;
  align-items: center;
  gap: 8px;
}

.slot-range > * {
  flex: 1;
  min-width: 0;
}

.slot-range__to {
  flex: none;
  color: var(--tx-3);
  font-size: 13px;
}

.dialog-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-family: var(--font-condensed);
  font-weight: 700;
  text-transform: uppercase;
}

@media (max-width: 959px) {
  .span-3 {
    grid-column: span 1;
  }
}
</style>
