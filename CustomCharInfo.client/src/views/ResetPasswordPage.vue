<template>
  <PageShell title="Reset password" tier="narrow">
    <div v-if="!linkComplete" class="note note--err" role="alert">
      This reset link is incomplete. Copy the whole link again, or see
      <router-link :to="{ name: 'ForgotPasswordPage' }">Forgot password</router-link>.
    </div>

    <div v-else-if="success" class="note note--ok">
      Password reset. You can now
      <router-link :to="{ name: 'UserActions' }">log in</router-link>.
    </div>

    <div v-if="errors.length" class="note note--err" role="alert">
      <p v-for="(msg, index) in errors" :key="index">{{ msg }}</p>
    </div>

    <form v-if="linkComplete && !success" class="panel reset-form" @submit.prevent="submit">
      <LabeledField label="New password" required for-id="reset-password">
        <v-text-field
          id="reset-password"
          v-model="password"
          type="password"
          autocomplete="new-password"
        />
      </LabeledField>
      <LabeledField label="Confirm password" required for-id="reset-confirm">
        <v-text-field
          id="reset-confirm"
          v-model="confirm"
          type="password"
          autocomplete="new-password"
        />
      </LabeledField>
      <AppButton type="submit" variant="primary" icon="mdi-lock-reset" :busy="loading">
        Reset password
      </AppButton>
    </form>
  </PageShell>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/services/api'
import { extractErrorMessages } from '@/services/apiErrors'
import PageShell from '@/components/PageShell.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

const route = useRoute()

const password = ref('')
const confirm = ref('')
const errors = ref([])
const success = ref(false)
const loading = ref(false)

const linkComplete = computed(() => Boolean(route.query.userId && route.query.token))

const submit = async () => {
  errors.value = []

  if (password.value !== confirm.value) {
    errors.value = ['Passwords do not match']
    return
  }

  loading.value = true

  try {
    await api.post('/auth/reset-password', {
      userId: route.query.userId,
      token: route.query.token,
      newPassword: password.value,
    })

    success.value = true
  } catch (err) {
    errors.value = extractErrorMessages(err)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.reset-form {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 16px;
  max-width: 480px;
}

.reset-form > .field {
  align-self: stretch;
}

.note {
  margin: 0 0 16px;
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

.note p {
  margin: 0;
}

.note p + p {
  margin-top: 4px;
}

.note a {
  color: var(--white);
  text-decoration: underline;
}
</style>
