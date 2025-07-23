<template>
  <div v-if="(isEditMode && modder) || !isEditMode">
    <v-container max-width="1020px">
      <h1 class="mt-5 mb-5" v-if="!isEditMode">Apply to Become a Modder</h1>
      <h1 class="mt-5 mb-5" v-else>Edit Your Modder Profile</h1>

      <v-form @submit.prevent="isEditMode ? save() : submit()" v-if="user">
        <v-row>
          <!-- Username -->
          <v-col cols="12" sm="4">
            <v-text-field 
              v-model="user.userName"
              label="Username"
              variant="outlined"
              readonly
              outlined
            >
              <template #details>
                Edit your username in your&nbsp;
                <router-link 
                  to="/user-actions"
                  class="offsite unvisitable"
                  target="_blank"
                >
                  user settings
                </router-link>
              </template>
            </v-text-field>
          </v-col>

          <!-- GameBanana ID -->
          <v-col cols="12" sm="5">
            <v-text-field
              v-model.number="modder.gamebananaId"
              label="GameBanana ID"
              type="number"
              :prefix="GB_MEMBER_URL"
            />
          </v-col>

          <!-- Discord -->
          <v-col cols="12" sm="3">
            <v-text-field
              v-model="modder.discordUsername"
              label="Discord username"
              prefix="@"
            />
          </v-col>

          <!-- Bio -->
          <v-col cols="12">
            <v-textarea
              v-model="modder.bio"
              label="Bio"
              rows="3"
              auto-grow
            />
          </v-col>
        </v-row>

        <!-- Submit -->
        <v-row>
          <v-col>
            <v-btn color="primary" type="submit">
              {{ isEditMode ? "Save" : "Apply for Modder" }}
            </v-btn>
          </v-col>
        </v-row>

        <!-- Feedback -->
        <v-row>
          <v-col cols="12">
            <p v-if="success" class="text-green">
              {{ isEditMode ? "Saved successfully!" : "Application submitted!" }}
            </p>
            <p v-if="error" class="text-red">{{ error }}</p>
          </v-col>
        </v-row>
      </v-form>
    </v-container>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'
import { GB_MEMBER_URL } from '@/globals'

const props = defineProps({
  mode: { type: String, default: 'apply' },
})
const isEditMode = computed(() => props.mode === 'edit')

const user = ref(null)
const success = ref(false)
const error = ref(null)

const modder = ref({
  bio: '',
  gamebananaId: null,
  discordUsername: '',
})

const modderId = ref(null)

const fetchUserAndModder = async () => {
  try {
    const userRes = await api.get('/auth/me')
    user.value = userRes.data

    if (isEditMode.value) {
      modderId.value = user.value.modderId ?? user.value.modderIdFuture

      if (!modderId.value) {
        error.value = 'No modder profile or application found.'
        return
      }

      const modderRes = await api.get(`/modders/${modderId.value}`)
      modder.value = {
        bio: modderRes.data.bio || '',
        gamebananaId: modderRes.data.gamebananaId || null,
        discordUsername: modderRes.data.discordUsername || null,
      }
    }
  } catch {
    error.value = 'Failed to load user or modder info.'
  }
}

onMounted(fetchUserAndModder)

const submit = async () => {
  try {
    await api.post('/modders', modder.value)
    success.value = true
    error.value = null
  } catch {
    success.value = false
    error.value = 'Failed to submit application.'
  }
}

const save = async () => {
  try {
    console.log(modder.value.discordUsername);
    await api.put(`/modders/${modderId.value}`, {
      bio: modder.value.bio,
      gamebananaId: modder.value.gamebananaId,
      discordUsername: modder.value.discordUsername,
    })
    success.value = true
    error.value = null
  } catch {
    success.value = false
    error.value = 'Failed to update modder info.'
  }
}
</script>

<style scoped>
:deep(.v-text-field__prefix__text) {
  color: #e4e4e4;
}
</style>
