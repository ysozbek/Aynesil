<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useGuardianStore } from '@/stores/guardian.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useGuardianStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  firstName: '',
  lastName: '',
  nationalId: '',
  email: '',
  phone: '',
  occupation: '',
  addressLine: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  if (isEdit.value && id.value) {
    await store.fetchOne(id.value)
    const guardian = store.current
    if (guardian) {
      form.firstName = guardian.firstName
      form.lastName = guardian.lastName
      form.nationalId = guardian.nationalId ?? ''
      form.email = guardian.email ?? ''
      form.phone = guardian.phone ?? ''
      form.occupation = guardian.occupation ?? ''
      form.addressLine = guardian.addressLine ?? ''
      form.rowVersion = guardian.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.firstName.trim()) {
    errors.firstName = t('validation.required', { field: t('guardian.firstName') })
    valid = false
  }
  if (!form.lastName.trim()) {
    errors.lastName = t('validation.required', { field: t('guardian.lastName') })
    valid = false
  }
  return valid
}

async function submit() {
  if (!validate()) return
  saving.value = true
  generalError.value = ''
  try {
    if (isEdit.value && id.value) {
      await store.update(id.value, {
        firstName: form.firstName,
        lastName: form.lastName,
        nationalId: form.nationalId || null,
        email: form.email || null,
        phone: form.phone || null,
        occupation: form.occupation || null,
        addressLine: form.addressLine || null,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'guardian-detail', params: { id: id.value } })
    } else {
      const result = await store.create({
        corporationId: form.corporationId,
        firstName: form.firstName,
        lastName: form.lastName,
        nationalId: form.nationalId || null,
        email: form.email || null,
        phone: form.phone || null,
        occupation: form.occupation || null,
        addressLine: form.addressLine || null,
      })
      router.push({ name: 'guardian-detail', params: { id: result.id } })
    }
  } catch (e: unknown) {
    generalError.value = (e as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto">
    <PageHeader
      :title="isEdit ? t('guardian.edit') : t('guardian.create')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Personal Info -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('guardian.title') }}</h3>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.firstName') }} *</label>
            <input
              v-model="form.firstName"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.firstName ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.firstName" class="mt-1 text-xs text-red-600">{{ errors.firstName }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.lastName') }} *</label>
            <input
              v-model="form.lastName"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.lastName ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.lastName" class="mt-1 text-xs text-red-600">{{ errors.lastName }}</p>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.nationalId') }}</label>
          <input
            v-model="form.nationalId"
            type="text"
            maxlength="11"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.email') }}</label>
            <input
              v-model="form.email"
              type="email"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.phone') }}</label>
            <input
              v-model="form.phone"
              type="tel"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.occupation') }}</label>
          <input
            v-model="form.occupation"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.addressLine') }}</label>
          <textarea
            v-model="form.addressLine"
            rows="2"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none"
          />
        </div>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3">
        <button
          type="button"
          @click="router.back()"
          class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="saving"
          class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60"
        >
          <svg v-if="saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          {{ saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
