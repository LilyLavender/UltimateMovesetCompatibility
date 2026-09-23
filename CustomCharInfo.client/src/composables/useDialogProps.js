import { computed } from 'vue'
import { useDisplay } from 'vuetify'

// Every dialog binds these so mobile gets a full-screen sheet and desktop gets the centered card.
// Usage: const dialogProps = useDialogProps() then <v-dialog v-bind="dialogProps" ...>
export function useDialogProps() {
  const { smAndDown } = useDisplay()
  return computed(() => ({
    fullscreen: smAndDown.value,
    transition: smAndDown.value ? 'dialog-bottom-transition' : 'fade-transition',
  }))
}
