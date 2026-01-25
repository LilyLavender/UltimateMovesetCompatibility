<template>
  <!-- Mobile warning dialog -->
  <v-dialog v-model="showMobileWarning" max-width="420" persistent>
    <v-card color="#2e2e2e">
      <v-card-title>
        <v-icon>mdi-alert</v-icon>
        Warning
      </v-card-title>

      <v-card-text>
        UMC is not yet optimized for mobile devices. <br>
        you may experience layout or usability issues.<br>
        Usage on desktop is recommended.
      </v-card-text>

      <v-card-actions class="justify-end">
        <v-btn color="red" @click="dismissForSession">
          Don’t show again
        </v-btn>
        <v-btn @click="continueAnyway">
          Continue Anyway
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const showMobileWarning = ref(false)

onMounted(() => {
  const dismissed = localStorage.getItem('dismissedMobileWarning')
  const isMobile = window.matchMedia('(max-width: 768px)').matches

  if (isMobile && !dismissed) {
    showMobileWarning.value = true
  }
})

const continueAnyway = () => {
  showMobileWarning.value = false
}

const dismissForSession = () => {
  localStorage.setItem('dismissedMobileWarning', 'true')
  showMobileWarning.value = false
}
</script>

<style scoped>
.v-btn {
  text-transform: unset;
}
</style>