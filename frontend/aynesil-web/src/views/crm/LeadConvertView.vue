<script setup lang="ts">
/**
 * Lead Conversion screen.
 * Links an existing lead to a student record to mark it as converted.
 * Backend requires: POST /api/leads/{id}/convert { studentId, rowVersion }
 */
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeadStore } from '@/stores/lead.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useLeadStore()

const id = computed(() => route.params.id as string)
const lead = computed(() => store.current)

const studentId = ref('')
const error = ref('')
const saving = ref(false)

onMounted(async () => {
  await store.fetchOne(id.value)
})

async function submit() {
  if (!studentId.value.trim()) {
    error.value = t('validation.required', { field: t('crm.lead.studentId') })
    return
  }
  if (!lead.value) return
  saving.value = true
  error.value = ''
  try {
    await store.convert(id.value, {
      studentId: studentId.value.trim(),
      rowVersion: lead.value.rowVersion,
    })
    router.push({ name: 'lead-detail', params: { id: id.value } })
  } catch (e: unknown) {
    error.value = (e as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-lg mx-auto">
    <PageHeader :title="t('crm.lead.convertTitle')" :description="t('crm.lead.convertDescription')" />

    <div v-if="store.loading" class="h-32 rounded-xl bg-accent animate-pulse" />
    <div v-else-if="!lead" class="text-center py-12 text-muted-foreground">{{ t('errors.notFound') }}</div>

    <template v-else>
      <!-- Lead summary -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm mb-6">
        <h3 class="font-semibold text-foreground mb-3">{{ t('crm.lead.convertLeadSummary') }}</h3>
        <dl class="space-y-2 text-sm">
          <div class="flex justify-between">
            <dt class="text-muted-foreground">{{ t('crm.lead.contactName') }}</dt>
            <dd class="font-medium text-foreground">{{ lead.contactName }}</dd>
          </div>
          <div v-if="lead.childName" class="flex justify-between">
            <dt class="text-muted-foreground">{{ t('crm.lead.childName') }}</dt>
            <dd class="font-medium text-foreground">{{ lead.childName }}</dd>
          </div>
          <div v-if="lead.statusName" class="flex justify-between">
            <dt class="text-muted-foreground">{{ t('common.status') }}</dt>
            <dd class="font-medium text-foreground">{{ lead.statusName }}</dd>
          </div>
        </dl>
      </div>

      <!-- Already converted -->
      <div v-if="lead.isConverted" class="rounded-xl border border-emerald-200 bg-emerald-50 p-5 text-emerald-800 text-sm">
        {{ t('crm.lead.alreadyConverted') }}
      </div>

      <form v-else @submit.prevent="submit" class="space-y-4">
        <p v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ error }}</p>

        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">
              {{ t('crm.lead.studentId') }} *
            </label>
            <input
              v-model="studentId"
              type="text"
              :placeholder="t('crm.lead.studentIdPlaceholder')"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
            <p class="mt-1 text-xs text-muted-foreground">{{ t('crm.lead.studentIdHint') }}</p>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3">
          <button type="button" @click="router.back()"
            class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
            {{ t('common.cancel') }}
          </button>
          <button type="submit" :disabled="saving"
            class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-60">
            <svg v-if="saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            {{ saving ? t('common.saving') : t('crm.lead.convertConfirm') }}
          </button>
        </div>
      </form>
    </template>
  </div>
</template>
