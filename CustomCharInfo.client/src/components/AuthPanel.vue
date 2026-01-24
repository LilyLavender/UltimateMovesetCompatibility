<template>
  <v-card class="pa-4" max-width="420px">
    <!-- Sign in/out/up -->
    <div>
      <!-- Sign in/up -->
      <div v-if="!token">
        <!-- Email/password -->
        <v-text-field v-model="email" label="Email" variant="outlined" />
        <v-text-field v-model="password" label="Password" type="password" variant="outlined">
          <template #details>
            <router-link 
            to="/forgot-password"
            class="offsite unvisitable"
            target="_blank"
            >Forgot Password?</router-link>
          </template>
        </v-text-field>

        <!-- Register/login buttons -->
        <div>
          <v-btn class="multibtn user-link" @click="register">
            <v-icon>mdi-account-plus</v-icon>
            Register
          </v-btn>
          <v-btn class="multibtn user-link" @click="login">
            <v-icon>mdi-login</v-icon>
            Log in
          </v-btn>
        </div>

        <!-- Errors -->
        <div v-if="errorMsgs.length" class="error">
          <h3>Error:</h3>
          <p v-for="(msg, index) in errorMsgs" :key="index">{{ msg }}</p>
        </div>
      </div>

      <!-- Username -->
      <div v-else-if="user">
        <div class="d-flex mb-2">
          <p>You are logged in as <code>{{ user.userName }}</code></p>
        </div>
      </div>
    </div>

    <!-- Signed-in -->
    <div v-if="user" class="user-links">
      <!-- Signed-in (ANY) actions -->
      <div>
        <!-- Logout button -->
        <v-btn @click="logout" class="user-link">
          <v-icon>mdi-logout</v-icon>
          Log out
        </v-btn>

        <!-- Change username -->
        <v-btn @click="editProfileForm = !editProfileForm" class="user-link">
          <v-icon
            class="rotate-toggle"
            :class="{ rotated: editProfileForm }"
          >
            mdi-cog
          </v-icon>
          Change username
        </v-btn>
      </div>

      <!-- Change username form -->
      <v-expand-transition class="no-user-link-styling">
        <div v-show="editProfileForm">
          <div class="d-flex mb-2 align-center">
            <!-- Text -->
            <v-text-field 
              v-model="editedUsername"
              label="New Username"
              class="mr-3 w-75"
              variant="outlined"
              hide-details
            />

            <!-- Button -->
            <v-btn 
              @click="updateUsername"
              class="user-link"
            >
              <v-icon>mdi-account-check</v-icon>
              Update
            </v-btn>
          </div>
        </div>
      </v-expand-transition>

      <!-- Signed-in (USER) actions -->
      <div v-if="!user?.modderId && !pendingApproval">
        <!-- apply for modder -->
        <router-link
          :to="{ name: 'ApplyModder' }"
          class="router-link unvisitable user-link"
        >
          <v-icon>mdi-account-plus</v-icon>
          Apply for modder
        </router-link>
      </div>

      <!-- Signed-in (LIMBO) actions -->
      <div v-if="pendingApproval">
        <p>
          <v-icon>mdi-account-clock</v-icon>
          Awaiting approval of your modder application. Sit tight!
        </p>
      </div>

      <!-- Signed-in (MODDER) actions -->
      <div v-if="user?.modderId">
        <!-- View profile -->
        <router-link
          :to="{ name: 'ModderDetail', params: { id: user.modderId } }"
          class="router-link unvisitable user-link"
        >
          <v-icon>mdi-account-eye</v-icon>
          View my profile
        </router-link>

        <!-- Edit profile -->
        <router-link
          :to="{ name: 'EditModder', params: { id: user.modderId } }"
          class="router-link unvisitable user-link"
        >
          <v-icon>mdi-account-edit</v-icon>
          Edit my profile
        </router-link>
      </div>

      <!-- Signed-in (ADMIN) actions -->
      <div v-if="user?.userTypeId == 3">
        <!-- Admin Portal -->
        <router-link
          :to="{ name: 'AdminPortal' }"
          class="router-link unvisitable user-link"
        >
          <v-icon>mdi-shield-account</v-icon>
          Admin Portal
        </router-link>
      </div>
    </div>
  </v-card>
  <v-card class="pa-4" width="1020px" v-if="user">
    <!-- Signed-in (ANY) actions -->
    <div>
      <!-- Action Log -->
      <ActionLogList />
    </div>
  </v-card>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { jwtDecode } from "jwt-decode"
import ActionLogList from '@/components/ActionLogList.vue'

const email = ref('')
const password = ref('')
const token = ref(localStorage.getItem('token'))
const user = ref(null)
const editProfileForm = ref(false)
const errorMsgs = ref([])
const editedUsername = ref('')

const register = async () => {
  try {
    errorMsgs.value = []
    await api.post('/auth/register', { email: email.value, password: password.value })
    alert('Registered! You can now log in.')
  } catch (err) {
    console.error("Register Failed:", err)
    errorMsgs.value = extractErrorMessages(err)
  }
}

const login = async () => {
  try {
    errorMsgs.value = []
    const res = await api.post('/auth/login', { email: email.value, password: password.value })
    token.value = res.data.token
    localStorage.setItem('token', token.value)
    api.defaults.headers.common['Authorization'] = `Bearer ${token.value}`
    getCurrentUserDetails()
  } catch (err) {
    console.error("Login Failed:", err)
    errorMsgs.value = extractErrorMessages(err)
  }
}

const updateUsername = async () => {
  try {
    await api.put(`/auth/edit-username`, {
      newUserName: editedUsername.value
    })
    alert('Username updated successfully!')
    await getCurrentUserDetails()
    editProfileForm.value = false
  } catch (err) {
    console.error("Failed to update username:", err)
    alert('Failed to update username.')
  }
}

const logout = () => {
  token.value = null
  email.value = null
  password.value = null
  user.value = null
  localStorage.removeItem('token')
  delete api.defaults.headers.common['Authorization']
}

function extractErrorMessages(err) {
  // ASP.NET Identity validation errors (array)
  if (Array.isArray(err.response?.data)) {
    return err.response.data.map(e => e.description || e.message || String(e))
  }

  // ASP.NET ProblemDetails / custom object
  if (typeof err.response?.data === 'object' && err.response?.data !== null) {
    if (err.response.data.message) {
      return [err.response.data.message]
    }

    if (err.response.data.title) {
      return [err.response.data.title]
    }
  }

  // Axios error
  if (err.message) {
    return [err.message]
  }

  // Fallback
  return ['An error occurred.']
}

function isTokenExpired(token) {
  if (!token) return true;
  const { exp } = jwtDecode(token);
  return Date.now() >= exp * 1000;
}

const pendingApproval = ref(false)

async function getCurrentUserDetails() {
  const res = await api.get('/auth/me')
  user.value = res.data

  try {
    const logsRes = await api.get('/logs', {
      params: { userId: user.value.id }
    })
    const logs = logsRes.data.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    const latestLog = logs.find(log => log.itemType.itemTypeId === 2)

    if (latestLog && [2, 4].includes(latestLog.acceptanceState.acceptanceStateId)) {
      pendingApproval.value = true
    }
  } catch (err) {
    console.error("Failed to fetch action logs:", err)
  }
}

onMounted(async () => {
  const token = localStorage.getItem('token')
  if (token) {
    if (isTokenExpired(token)) {
      localStorage.removeItem('token')
      delete api.defaults.headers.common['Authorization']
    }
    api.defaults.headers.common['Authorization'] = `Bearer ${token}`
    getCurrentUserDetails()
  }
})
</script>

<style scoped>
.v-card {
  margin: 0 auto;
  background-color: #1e1e1e;
  color: #dedede;
}

.multibtn:not(:last-of-type) {
  margin-right: 1em;
}

.error {
  margin-top: 1em;
  color: #ff6b6b;
}

.rotate-toggle::before {
  transition: transform 250ms ease-in-out;
}

.rotate-toggle.rotated::before {
  transform: rotate(90deg);
}

.user-links {
  margin: 1.25em auto -0.5em auto;
  width: 90%;
}

.user-links > *:not(.no-user-link-styling) {
  display: flex;
  justify-content: center;
  gap: 0.6em;
  margin-bottom: 0.6em;
}

.user-link {
  background-color: #151515;
  padding: 0.4em 0.75em;
  margin: 0;
  border-radius: 6px;
  text-decoration: none;
  text-transform: unset;
  letter-spacing: 0.1px;
  font-size: 15px;
  box-shadow: none;

  transition: background-color 200ms ease-in-out;
}

.user-link:hover {
  background-color: #191919 !important;
}

.user-link > .v-btn__overlay {
  background-color: unset;
}

.user-link.router-link .v-icon {
  margin-top: -4px;
}

.user-link:not(.router-link) .v-icon {
  margin-right: 3px;
}
</style>