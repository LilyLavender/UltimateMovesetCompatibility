<template>
  <div v-if="(isEditMode && modder) || !isEditMode">
    <v-container max-width="1020px">
      <h1 class="mt-5 mb-5" v-if="!isEditMode">Apply to Become a Modder</h1>
      <h1 class="mt-5 mb-5" v-else>Edit Your Modder Profile</h1>

      <v-form @submit.prevent="isEditMode ? save() : submit()" v-if="user">
        <!-- Row 1: PFP preview + Username + GB ID + Discord -->
        <v-row align="center" class="mb-0">
          <!-- PFP preview -->
          <v-col cols="auto">
            <div class="pfp-preview-wrap">
              <img
                v-if="pfpPreviewUrl"
                :src="pfpPreviewUrl"
                class="pfp-preview"
                alt="PFP preview"
              />
              <v-icon v-else size="48" class="pfp-preview-placeholder">mdi-account</v-icon>
            </div>
          </v-col>

          <!-- Username -->
          <v-col cols="12" sm>
            <v-text-field
              v-model="user.userName"
              label="Username"
              variant="outlined"
              class="disabled"
              readonly
            >
              <template #details>
                <span>
                  Edit your username in&nbsp;
                  <router-link
                    to="/user-actions"
                    class="offsite unvisitable"
                    target="_blank"
                  >
                    user settings
                  </router-link>
                  <span v-if="!isEditMode">
                    This must be done before submitting a modder application.
                  </span>
                </span>
              </template>
            </v-text-field>
          </v-col>

          <!-- GameBanana ID -->
          <v-col cols="12" sm="4">
            <v-text-field
              variant="outlined"
              v-model.number="modder.gamebananaId"
              label="GameBanana ID"
              @input="digitsOnly('gamebananaId')"
              :prefix="GB_MEMBER_URL"
            >
              <template #label>
                <img src="https://images.gamebanana.com/img/ico/games/banana.gif" class="field-platform-icon" alt="" />
                GameBanana ID
              </template>
            </v-text-field>
          </v-col>

          <!-- Discord -->
          <v-col cols="12" sm="3">
            <v-text-field
              variant="outlined"
              v-model="modder.discordUsername"
              label="Discord"
              prefix="@"
            >
              <template #label>
                <img src="https://cdn.simpleicons.org/discord/5865F2" class="field-platform-icon" alt="" />
                Discord
              </template>
            </v-text-field>
          </v-col>
        </v-row>

        <!-- Row 2: Twitter, Bluesky, GitHub -->
        <v-row class="mb-0">
          <v-col cols="12" sm="4">
            <v-text-field
              variant="outlined"
              v-model="modder.twitterUsername"
              label="Twitter / X"
              prefix="x.com/"
            >
              <template #label>
                <img src="https://cdn.simpleicons.org/x/ffffff" class="field-platform-icon" alt="" />
                Twitter
              </template>
            </v-text-field>
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              variant="outlined"
              v-model="modder.blueskyHandle"
              label="Bluesky"
              prefix="bsky.app/profile/"
            >
              <template #label>
                <img src="https://cdn.simpleicons.org/bluesky/0085FF" class="field-platform-icon" alt="" />
                Bluesky
              </template>
            </v-text-field>
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              variant="outlined"
              v-model="modder.githubUsername"
              label="GitHub"
              prefix="github.com/"
            >
              <template #label>
                <img src="https://cdn.simpleicons.org/github/ffffff" class="field-platform-icon" alt="" />
                GitHub
              </template>
            </v-text-field>
          </v-col>
        </v-row>

        <!-- Row 3: PFP URL -->
        <v-row class="mb-0">
          <v-col cols="12">
            <v-text-field
              variant="outlined"
              v-model="modder.pfpUrl"
              label="Profile Picture URL"
              placeholder="Any square image URL (jpg, png, gif…)"
              clearable
            >
              <template #details>
                <span class="hint-text">Falls back to GameBanana avatar if left empty.</span>
              </template>
            </v-text-field>
          </v-col>
        </v-row>

        <!-- Row 4: Bio -->
        <v-row class="mb-3">
          <v-col cols="12">
            <v-textarea
              variant="outlined"
              v-model="modder.bio"
              label="Bio"
              placeholder="Displayed on your modder profile page."
              rows="3"
              auto-grow hide-details
            />
          </v-col>
        </v-row>

        <!-- Notes + Submit -->
        <div class="d-flex align-start ga-3 justify-end mt-3">
          <v-textarea
            variant="outlined"
            density="compact"
            v-model="modder.notes"
            :label="isEditMode ? 'Editing notes' : 'Submission notes'"
            placeholder="Optional, shown to admins only."
            rows="1"
            auto-grow
            hide-details
            class="notes-field"
          />
          <v-btn class="btn mt-1" type="submit">
            {{ isEditMode ? "Save Profile" : "Apply for Modder" }}
          </v-btn>
        </div>

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
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
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
  pfpUrl: '',
  twitterUsername: '',
  blueskyHandle: '',
  githubUsername: '',
  notes: '',
})

const router = useRouter()
const modderId = ref(null)
const gbPfpUrl = ref(null)

const pfpPreviewUrl = computed(() => modder.value.pfpUrl || gbPfpUrl.value || null)

const fetchGbPfp = async (gbId) => {
  if (!gbId) { gbPfpUrl.value = null; return }
  try {
    const res = await axios.get(
      `https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid=${gbId}&fields=Url().sHdAvatarUrl(),Url().sAvatarUrl()`
    )
    gbPfpUrl.value = res.data[0] || res.data[1] || null
  } catch {
    gbPfpUrl.value = null
  }
}

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
        discordUsername: modderRes.data.discordUsername || '',
        pfpUrl: modderRes.data.pfpUrl || '',
        twitterUsername: modderRes.data.twitterUsername || '',
        blueskyHandle: modderRes.data.blueskyHandle || '',
        githubUsername: modderRes.data.githubUsername || '',
      }
    }
  } catch {
    error.value = 'Failed to load user or modder info.'
  }
}

onMounted(async () => {
  await fetchUserAndModder()
  fetchGbPfp(modder.value.gamebananaId)
})

watch(() => modder.value.gamebananaId, fetchGbPfp)

const digitsOnly = (field) => {
  if (form[field] == null) return
  form[field] = String(form[field]).replace(/\D+/g, '')
}

const submit = async () => {
  try {
    await api.post('/modders', {
      ...modder.value,
      twitterUsername: modder.value.twitterUsername || null,
      blueskyHandle: modder.value.blueskyHandle || null,
      githubUsername: modder.value.githubUsername || null,
    })
    success.value = true
    error.value = null
    router.push('/user-actions')
  } catch {
    success.value = false
    error.value = 'Failed to submit application.'
  }
}

const save = async () => {
  try {
    await api.put(`/modders/${modderId.value}`, {
      bio: modder.value.bio,
      gamebananaId: modder.value.gamebananaId,
      discordUsername: modder.value.discordUsername,
      pfpUrl: modder.value.pfpUrl || null,
      twitterUsername: modder.value.twitterUsername || null,
      blueskyHandle: modder.value.blueskyHandle || null,
      githubUsername: modder.value.githubUsername || null,
      notes: modder.value.notes,
    })
    success.value = true
    error.value = null
    router.push(`/modder/${modderId.value}`)
  } catch {
    success.value = false
    error.value = 'Failed to update modder info.'
  }
}
</script>

<style scoped>
h1 {
  font-size: 3.25em;
}

section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}

.btn {
  text-transform: unset;
  background-color: #2e2e2e;
  color: #e2e2e2;
}

.disabled :deep(input) {
  color: #484848;
}

:deep(.v-text-field__prefix__text) {
  font-size: 0.85em;
  color: #484848;
}

.pfp-preview-wrap {
  width: 80px;
  height: 80px;
  border-radius: 8px;
  overflow: hidden;
  background-color: #2e2e2e;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.pfp-preview {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.pfp-preview-placeholder {
  color: #555;
}

.field-platform-icon {
  width: 14px;
  height: 14px;
  vertical-align: middle;
  margin-right: 3px;
  margin-bottom: 2px;
  opacity: 0.85;
}

.hint-text {
  font-size: 0.78rem;
  color: #777;
}

.notes-field {
  max-width: 400px;
}
.notes-field :deep(.v-field__input) {
  font-size: 0.85rem;
  padding-top: 6px;
  padding-bottom: 6px;
}
.notes-field :deep(.v-label) {
  font-style: italic;
  color: #6e6e6e !important;
}
</style>
