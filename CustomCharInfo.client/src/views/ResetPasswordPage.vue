<template>
  <v-container max-width="1020px">
    <h1 class="mb-4 page-title no-select">Reset Password</h1>

    <v-alert v-if="error" type="error" class="mb-4">
      {{ error }}
    </v-alert>

    <v-alert v-if="success" type="success" class="mb-4">
      Password successfully reset. You can now log in.
    </v-alert>

    <v-form v-if="!success" @submit.prevent="submit">
      <v-row>
        <!-- Password -->
        <v-col cols="12" sm="6">
          <v-text-field
            v-model="password"
            variant="outlined"
            label="New Password"
            type="password"
            required
          />
        </v-col>

        <!-- Confirm -->
        <v-col cols="12" sm="6">
          <v-text-field
            v-model="confirm"
            variant="outlined"
            label="Confirm Password"
            type="password"
            required
          />
        </v-col>

        <!-- Button -->
        <v-col cols="12" sm="2">
          <v-btn
            color="primary"
            block
            :loading="loading"
            type="submit"
            class="btn"
          >
            Reset Password
          </v-btn>
        </v-col>
      </v-row>
    </v-form>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/services/api'

const route = useRoute()

const password = ref('')
const confirm = ref('')
const error = ref('')
const success = ref(false)
const loading = ref(false)

const submit = async () => {
  error.value = ''

  if (password.value !== confirm.value) {
    error.value = 'Passwords do not match'
    return
  }

  loading.value = true

  try {
    await api.post('/auth/reset-password', {
      userId: route.query.userId,
      token: route.query.token,
      newPassword: password.value
    })

    success.value = true
  } catch (err) {
    error.value = err.response?.data ?? 'Invalid or expired reset link'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.page-title {
  font-size: 4em;
  margin-top: 0.5em;
}

.btn {
  margin-top: -1em;
}
</style>