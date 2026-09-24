<template>
  <PageShell
    v-if="(isEditMode && modder) || !isEditMode"
    :title="isEditMode ? 'Edit your modder profile' : 'Apply to become a modder'"
    :head="false"
  >
    <form v-if="user" @submit.prevent="isEditMode ? save() : submit()">
      <FormLayout>
        <FormSection id="profile" title="Profile">
          <div class="profile-grid">
            <div class="pfp-preview">
              <img v-if="pfpPreviewUrl" :src="pfpPreviewUrl" alt="Profile picture preview" />
              <v-icon v-else size="40">mdi-account</v-icon>
            </div>

            <div class="form-grid form-grid--2">
              <LabeledField label="Username">
                <v-text-field v-model="user.userName" disabled />
                <p class="field-hint">
                  Change your username in
                  <router-link to="/user-actions" target="_blank">account settings</router-link
                  ><template v-if="!isEditMode"> before submitting a modder application</template>.
                </p>
              </LabeledField>

              <LabeledField label="GameBanana ID">
                <template #label>
                  <img
                    src="https://images.gamebanana.com/img/ico/games/banana.gif"
                    class="platform-icon"
                    alt=""
                  />
                  GameBanana ID
                </template>
                <v-text-field
                  v-model.number="modder.gamebananaId"
                  :prefix="GB_MEMBER_URL"
                  inputmode="numeric"
                  @input="digitsOnly('gamebananaId')"
                />
              </LabeledField>

              <LabeledField label="Bio" hint="Shown on your modder page." class="span-2">
                <v-textarea v-model="modder.bio" rows="3" auto-grow />
              </LabeledField>
            </div>
          </div>
        </FormSection>

        <FormSection id="links" title="Links and picture">
          <div class="form-grid">
            <LabeledField label="Discord">
              <template #label>
                <img
                  src="https://cdn.simpleicons.org/discord/5865F2"
                  class="platform-icon"
                  alt=""
                />
                Discord
              </template>
              <v-text-field v-model="modder.discordUsername" prefix="@" />
            </LabeledField>
            <LabeledField label="Twitter">
              <template #label>
                <img src="https://cdn.simpleicons.org/x/ffffff" class="platform-icon" alt="" />
                Twitter
              </template>
              <v-text-field v-model="modder.twitterUsername" prefix="x.com/" />
            </LabeledField>
            <LabeledField label="Bluesky">
              <template #label>
                <img
                  src="https://cdn.simpleicons.org/bluesky/0085FF"
                  class="platform-icon"
                  alt=""
                />
                Bluesky
              </template>
              <v-text-field v-model="modder.blueskyHandle" prefix="bsky.app/profile/" />
            </LabeledField>
            <LabeledField label="GitHub">
              <template #label>
                <img src="https://cdn.simpleicons.org/github/ffffff" class="platform-icon" alt="" />
                GitHub
              </template>
              <v-text-field v-model="modder.githubUsername" prefix="github.com/" />
            </LabeledField>
            <LabeledField
              label="Profile picture URL"
              hint="Any square image link. Falls back to your GameBanana avatar if left empty."
              class="span-2"
            >
              <v-text-field v-model="modder.pfpUrl" clearable />
            </LabeledField>
          </div>
        </FormSection>

        <p v-if="error" class="note note--err">{{ error }}</p>
        <p v-if="success" class="note note--ok">
          {{ isEditMode ? 'Saved successfully.' : 'Application submitted.' }}
        </p>

        <template #savebar>
          <LabeledField
            :label="isEditMode ? 'Editing notes' : 'Submission notes'"
            note="admins only"
            class="savebar-notes"
          >
            <v-textarea v-model="modder.notes" density="compact" rows="1" auto-grow hide-details />
          </LabeledField>
          <span class="savebar-spacer"></span>
          <AppButton type="submit" variant="primary" icon="mdi-check" :busy="saving">
            {{ isEditMode ? 'Save profile' : 'Apply for modder' }}
          </AppButton>
        </template>
      </FormLayout>
    </form>
  </PageShell>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import axios from 'axios'
import api from '@/services/api'
import { GB_MEMBER_URL } from '@/globals'
import PageShell from '@/components/PageShell.vue'
import FormLayout from '@/components/FormLayout.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

const props = defineProps({
  mode: { type: String, default: 'apply' },
})
const isEditMode = computed(() => props.mode === 'edit')

useHead(
  computed(() => ({
    title: isEditMode.value ? 'UMC | Editing modder page' : 'UMC | Apply for modder',
  }))
)

const user = ref(null)
const success = ref(false)
const error = ref(null)
const saving = ref(false)

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
  if (!gbId) {
    gbPfpUrl.value = null
    return
  }
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
        notes: '',
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
  if (modder.value[field] == null) return
  modder.value[field] = String(modder.value[field]).replace(/\D+/g, '')
}

const submit = async () => {
  saving.value = true
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
  } finally {
    saving.value = false
  }
}

const save = async () => {
  saving.value = true
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
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.profile-grid {
  display: grid;
  grid-template-columns: 96px minmax(0, 1fr);
  gap: 20px;
  align-items: start;
}

.pfp-preview {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 96px;
  height: 96px;
  overflow: hidden;
  border: 1px solid var(--line);
  background: var(--panel-2);
  color: var(--tx-3);
}

.pfp-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  align-items: start;
}

.form-grid--2 {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.span-2 {
  grid-column: span 2;
}

.span-3 {
  grid-column: span 3;
}

.platform-icon {
  width: 14px;
  height: 14px;
  object-fit: contain;
  opacity: 0.85;
}

.field-hint {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--tx-3);
}

.field-hint a {
  color: var(--tx-2);
  text-decoration: underline;
}

:deep(.v-text-field__prefix__text) {
  color: var(--tx-3);
  font-size: 13px;
}

.note {
  margin: 0;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--tx-3);
  background: var(--panel);
  color: var(--tx);
  font-size: 14px;
}

.note--err {
  border-left-color: var(--err);
}

.note--ok {
  border-left-color: var(--ok);
}

.savebar-notes {
  flex: 1 1 320px;
  max-width: 480px;
}

.savebar-spacer {
  flex: 1;
}

@media (max-width: 959px) {
  .profile-grid,
  .form-grid,
  .form-grid--2 {
    grid-template-columns: 1fr;
  }

  .span-2,
  .span-3 {
    grid-column: span 1;
  }
}
</style>
