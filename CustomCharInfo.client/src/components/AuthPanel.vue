<template>
  <v-card class="pa-4" max-width="420px">
    <!-- Sign in/out/up -->
    <div>
      <!-- Sign in/up -->
      <div v-if="!token">
        <v-text-field v-model="email" label="Email" />
        <v-text-field v-model="password" label="Password" type="password" />

        <v-btn class="multibtn" @click="register">Register</v-btn>
        <v-btn class="multibtn" @click="login">Login</v-btn>

        <!-- Errors -->
        <div v-if="errorMsgs.length" class="error">
          <h3>Error:</h3>
          <p v-for="(msg, index) in errorMsgs" :key="index">{{ msg }}</p>
        </div>
      </div>

      <!-- Sign out -->
      <div v-else-if="user">
        <!-- Username -->
        <div class="d-flex mb-2">
          <p>You are logged in as <code>{{ user.userName }}</code></p>
          <v-icon
            class="rotate-toggle"
            :class="{ rotated: editProfileForm }"
            @click="editProfileForm = !editProfileForm"
          >
            mdi-cog
          </v-icon>
        </div>

        <!-- Edit username -->
        <v-expand-transition>
          <div v-show="editProfileForm">
            <div class="d-flex mb-2">
              <v-text-field 
                v-model="editedUsername"
                label="New Username"
                class="mr-3 w-75"
                variant="outlined"
                hide-details
              />
              <v-btn 
                @click="updateUsername"
                class="ml-3 w-25 h-auto"
              >
                Update
              </v-btn>
            </div>
          </div>
        </v-expand-transition>

        <!-- Logout button -->
        <v-btn @click="logout" class="mb-2">Logout</v-btn>
      </div>
    </div>

    <!-- Signed-in -->
    <div v-if="user" class="user-links">
      <!-- Signed-in (USER) actions -->
      <div v-if="!user?.modderId && !pendingApproval">
        <!-- apply for modder -->
        <router-link
          :to="{ name: 'ApplyModder' }"
          class="unvisitable user-link"
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
          class="unvisitable user-link"
        >
          <v-icon>mdi-account-eye</v-icon>
          View my profile
        </router-link>

        <!-- Edit profile -->
        <router-link
          :to="{ name: 'EditModder', params: { id: user.modderId } }"
          class="unvisitable user-link"
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
          class="unvisitable user-link"
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
      <div class="d-flex mb--1 ml-4">
        <h2>Actions</h2>
      </div>
      <ActionLogList :userId="user.id" />
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

.v-btn {
  background-color: #2e2e2e;
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

.mb--1 {
  margin-bottom: -1em;
}

.ml-4 {
  margin-left: 4em !important;
}

.user-links {
  margin: 0 auto;
  width: fit-content;
}

.user-links > * {
  display: flex;
  justify-content: center;
}

.user-links .user-link {
  background-color: #151515;
  padding: 0.4em 0.75em;
  margin: 0.25em 1em;
  border-radius: 6px;
  text-decoration: none;
}

.user-link .v-icon {
  margin-top: -4px;
}
</style>