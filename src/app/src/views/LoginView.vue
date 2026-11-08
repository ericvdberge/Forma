<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { TriangleAlert } from 'lucide-vue-next'
import { FButton, FCard, FInput, FLogo } from '@forma/design-system'
import { useAuth } from '@/composables/useAuth'

const { login } = useAuth()
const router = useRouter()
const route = useRoute()

const userName = ref('')
const password = ref('')
const submitting = ref(false)
const error = ref('')

/** Only follow redirects back into this app, never to another site. */
function redirectTarget() {
  const target = route.query.redirect
  return typeof target === 'string' && target.startsWith('/') && !target.startsWith('//') ? target : '/'
}

async function submit() {
  if (submitting.value) return
  error.value = ''
  submitting.value = true
  try {
    const result = await login(userName.value.trim(), password.value)
    if (result === 'ok') {
      await router.replace(redirectTarget())
    } else if (result === 'rate-limited') {
      error.value = 'Too many sign-in attempts. Wait a minute, then try again.'
    } else {
      error.value = 'That username and password don’t match.'
      password.value = ''
    }
  } catch {
    error.value = 'We couldn’t sign you in. Check that the Forma API is running, then try again.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="flex min-h-dvh items-center justify-center bg-[#f7f7f5] px-4 py-10">
    <div class="w-full max-w-sm">
      <div class="mb-8 flex justify-center">
        <FLogo size="lg" accent-dot />
      </div>
      <FCard padding="lg">
        <h1 class="font-display text-2xl font-bold tracking-[-0.03em]">Sign in</h1>
        <p class="mt-1 text-sm text-muted">Welcome back. Sign in to manage your websites.</p>

        <form class="mt-6 flex flex-col gap-4" novalidate @submit.prevent="submit">
          <FInput v-model="userName" label="Username" autocomplete="username" required />
          <FInput v-model="password" label="Password" type="password" autocomplete="current-password" required />

          <p v-if="error" role="alert" class="flex items-start gap-2 text-sm text-danger">
            <TriangleAlert class="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            {{ error }}
          </p>

          <FButton type="submit" block :disabled="submitting || !userName || !password">
            {{ submitting ? 'Signing in…' : 'Sign in' }}
          </FButton>
        </form>
      </FCard>
    </div>
  </main>
</template>
