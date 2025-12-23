<template>
  <div v-if="(isEditMode && hook) || (!isEditMode && form)">
    <v-container max-width="1020px">
      <!-- Header -->
      <h1 v-if="isEditMode">Edit Hook</h1>
      <h1 v-else>Add Hook</h1>

      <!-- Basic Info -->
      <section>
        <v-row>
          <!-- Offset -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="offsetInput"
              label="Offset (hex)"
              placeholder="123ABC"
              prefix="0x"
            />
          </v-col>

          <!-- Hookable Status -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.hookableStatusId"
              :items="hookableStatuses"
              item-title="name"
              item-value="hookableStatusId"
              label="Hookable Status"
            />
          </v-col>
        </v-row>

        <v-row>
          <!-- Description -->
          <v-col cols="12">
            <v-textarea
              v-model="form.description"
              label="Description (what the hook normally handles)"
              auto-grow
              rows="1"
            />
          </v-col>
        </v-row>
      </section>

      <!-- Submit -->
      <div class="d-flex justify-end">
        <v-btn
          class="btn submit-button"
          @click="submit"
        >
          {{ isEditMode ? 'Save' : 'Add Hook' }}
        </v-btn>
      </div>
    </v-container>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api'

const props = defineProps({
  mode: { type: String },
  hookId: { type: Number }
})

const isEditMode = computed(() => props.mode === 'edit')
const router = useRouter()

const hook = ref(null)

const form = ref({
  offset: '',
  description: '',
  hookableStatusId: null
})

const offsetInput = ref('')
const hookableStatuses = ref([])

onMounted(async () => {
  await fetchHookableStatuses()

  if (isEditMode.value && props.hookId) {
    try {
      const res = await api.get(`/hooks/${props.hookId}`)
      hook.value = res.data

      Object.assign(form.value, res.data)

      offsetInput.value = res.data.offset.toString().toUpperCase()
      document.title = `UMC | Editing Hook`
    } catch (err) {
      console.error(err)
      router.replace({ name: 'ErrorPage', query: { http: 404 } })
    }
  }
})

const fetchHookableStatuses = async () => {
  const res = await api.get('/hookablestatuses')
  hookableStatuses.value = res.data
}

watch(offsetInput, (val) => {
  form.value.offset = val.toUpperCase()
})

const submit = async () => {
  if (!form.value.offset) {
    alert('Offset is required')
    return
  }

  const payload = { ...form.value }

  try {
    if (isEditMode.value && props.hookId) {
      await api.put(`/hooks/${props.hookId}`, payload)
    } else {
      await api.post('/hooks', payload)
    }
    router.push('/hooks')
  } catch (err) {
    console.error('Submit failed:', err.response?.data || err.message)
    alert('Failed to save hook.')
  }
}
</script>

<style scoped>
section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}

h1 {
  font-size: 3.25em;
}

.submit-button {
  background-color: #1e1e1e;
  color: #e2e2e2;
}
</style>
