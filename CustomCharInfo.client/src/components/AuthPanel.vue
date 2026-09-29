<template>
  <section class="panel auth-panel">
    <!-- Signed out: log in or register -->
    <template v-if="!authStore.isLoggedIn">
      <h3 class="auth-panel__title">Log in</h3>
      <form class="auth-panel__form" @submit.prevent="login">
        <LabeledField label="Email" for-id="auth-email">
          <v-text-field id="auth-email" v-model="email" type="email" autocomplete="email" />
        </LabeledField>
        <LabeledField label="Password" for-id="auth-password">
          <v-text-field
            id="auth-password"
            v-model="password"
            type="password"
            autocomplete="current-password"
          />
          <router-link to="/forgot-password" class="auth-panel__forgot"
            >Forgot password?</router-link
          >
        </LabeledField>

        <div class="auth-panel__actions">
          <AppButton type="submit" variant="primary" icon="mdi-login" :busy="busy === 'login'">
            Log in
          </AppButton>
          <AppButton
            type="button"
            variant="ghost"
            icon="mdi-account-plus"
            :busy="busy === 'register'"
            @click="register"
          >
            Register
          </AppButton>
        </div>

        <div v-if="errorMsgs.length" class="auth-panel__errors" role="alert">
          <p v-for="(msg, index) in errorMsgs" :key="index">{{ msg }}</p>
        </div>
      </form>
    </template>

    <!-- Signed in -->
    <template v-else-if="user">
      <h3 class="auth-panel__title">Signed in</h3>
      <dl class="auth-panel__facts">
        <dt>Username</dt>
        <dd>
          <strong>{{ user.userName }}</strong>
        </dd>
      </dl>

      <div class="auth-panel__actions">
        <AppButton
          variant="ghost"
          size="sm"
          :icon="editProfileForm ? 'mdi-close' : 'mdi-account-edit'"
          @click="editProfileForm = !editProfileForm"
        >
          {{ editProfileForm ? 'Cancel' : 'Change username' }}
        </AppButton>
        <AppButton variant="danger" size="sm" icon="mdi-logout" @click="logout">Log out</AppButton>
      </div>

      <v-expand-transition>
        <div v-show="editProfileForm" class="auth-panel__reveal">
          <form class="auth-panel__rename" @submit.prevent="updateUsername">
            <LabeledField label="New username" for-id="auth-new-username">
              <v-text-field id="auth-new-username" v-model="editedUsername" density="compact" />
            </LabeledField>
            <AppButton type="submit" size="sm" icon="mdi-check" :busy="busy === 'rename'">
              Save
            </AppButton>
          </form>
        </div>
      </v-expand-transition>

      <!-- Modder application -->
      <div v-if="!user.modderId && !pendingApproval" class="auth-panel__apply">
        <p class="muted small">Make movesets? Apply for a modder page to submit them here.</p>
        <AppButton :to="{ name: 'ApplyModder' }" size="sm" icon="mdi-account-plus">
          Apply for modder
        </AppButton>
      </div>
      <p v-else-if="pendingApproval" class="auth-panel__pending">
        <v-icon size="18">mdi-account-clock</v-icon>
        Awaiting approval of your modder application. Sit tight!
      </p>
    </template>
  </section>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import { extractErrorMessages } from '@/services/apiErrors'
import { useAuthStore } from '@/stores/auth'
import { ItemType, AcceptanceState } from '@/globals'
import { useNotify } from '@/composables/useNotify'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

const notify = useNotify()

const authStore = useAuthStore()
const user = computed(() => authStore.user)

const email = ref('')
const password = ref('')
const editProfileForm = ref(false)
const errorMsgs = ref([])
const editedUsername = ref('')
const busy = ref('')

const register = async () => {
  busy.value = 'register'
  try {
    errorMsgs.value = []
    await authStore.register(email.value, password.value)
    notify.success('Registered! You can now log in.')
  } catch (err) {
    console.error('Register Failed:', err)
    errorMsgs.value = extractErrorMessages(err)
  } finally {
    busy.value = ''
  }
}

const login = async () => {
  busy.value = 'login'
  try {
    errorMsgs.value = []
    await authStore.login(email.value, password.value)
    await checkPendingApproval()
  } catch (err) {
    console.error('Login Failed:', err)
    errorMsgs.value = extractErrorMessages(err)
  } finally {
    busy.value = ''
  }
}

const updateUsername = async () => {
  busy.value = 'rename'
  try {
    await api.put(`/auth/edit-username`, {
      newUserName: editedUsername.value,
    })
    notify.success('Username updated successfully!')
    await authStore.fetchCurrentUser()
    editProfileForm.value = false
  } catch (err) {
    console.error('Failed to update username:', err)
    notify.error('Failed to update username.')
  } finally {
    busy.value = ''
  }
}

const logout = async () => {
  await authStore.logout()
  email.value = ''
  password.value = ''
}

const pendingApproval = ref(false)

async function checkPendingApproval() {
  if (!user.value) return
  pendingApproval.value = false
  try {
    const logsRes = await api.get('/logs', {
      params: { userId: user.value.id },
    })
    const logs = logsRes.data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    const latestLog = logs.find((log) => log.itemType.itemTypeId === ItemType.Modder)

    if (
      latestLog &&
      [AcceptanceState.PendingAdminHard, AcceptanceState.PendingUserHard].includes(
        latestLog.acceptanceState.acceptanceStateId
      )
    ) {
      pendingApproval.value = true
    }
  } catch (err) {
    console.error('Failed to fetch action logs:', err)
  }
}

onMounted(async () => {
  if (authStore.isLoggedIn) {
    await authStore.ensureLoaded()
    await checkPendingApproval()
  }
})
</script>

<style scoped>
.auth-panel__title {
  margin: 0 0 14px;
  font-size: 20px;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.auth-panel__form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.auth-panel__forgot {
  align-self: flex-start;
  margin-top: 4px;
  font-size: 12px;
  color: var(--tx-2);
}

.auth-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.auth-panel__errors {
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--err);
  background: var(--panel-2);
  color: var(--tx);
  font-size: 14px;
}

.auth-panel__errors p {
  margin: 0;
}

.auth-panel__errors p + p {
  margin-top: 4px;
}

.auth-panel__facts {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 6px 16px;
  margin: 0 0 14px;
  font-size: 14px;
}

.auth-panel__facts dt {
  color: var(--tx-3);
}

.auth-panel__facts dd {
  margin: 0;
}

/* The expand transition tweens wrapper's height. Margin on the form inside keeps the tween smooth */
.auth-panel__rename {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  padding-top: 14px;
}

.auth-panel__rename > :first-child {
  flex: 1;
}

.auth-panel__apply {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  margin-top: 18px;
  padding-top: 14px;
  border-top: 1px solid var(--line);
}

.auth-panel__apply p {
  margin: 0;
}

.auth-panel__pending {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 18px 0 0;
  padding-top: 14px;
  border-top: 1px solid var(--line);
  color: var(--tx-2);
  font-size: 14px;
}
</style>
