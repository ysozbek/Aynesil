<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useProgramStore } from '@/stores/program.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type {
  ProgramServiceDto,
  AddProgramServicePayload,
  SetTranslationPayload,
} from '@/types/program.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useProgramStore()
const refData = useRefDataStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const program = computed(() => store.currentProgram)
const activeTab = ref<'overview' | 'services' | 'translations'>('overview')

const serviceTypes = ref<RefValueItem[]>([])

onMounted(async () => {
  await store.fetchProgram(id.value)
  await refData.getValues('SERVICE_TYPE').then(v => { serviceTypes.value = v })
})

onUnmounted(() => {
  store.clearCurrent()
})

// ── Service Modal ─────────────────────────────────────────────────────────
const showServiceModal = ref(false)
const serviceTarget = ref<ProgramServiceDto | null>(null)
const serviceForm = reactive<AddProgramServicePayload>({
  name: '',
  serviceTypeId: null,
  defaultDurationMinutes: null,
  defaultSessionsPerWeek: null,
  sortOrder: 0,
})
const serviceError = ref('')

function openAddService() {
  serviceTarget.value = null
  serviceForm.name = ''
  serviceForm.serviceTypeId = null
  serviceForm.defaultDurationMinutes = null
  serviceForm.defaultSessionsPerWeek = null
  serviceForm.sortOrder = (program.value?.services.length ?? 0) + 1
  serviceError.value = ''
  showServiceModal.value = true
}

function openEditService(svc: ProgramServiceDto) {
  serviceTarget.value = svc
  serviceForm.name = svc.name
  serviceForm.serviceTypeId = svc.serviceTypeId
  serviceForm.defaultDurationMinutes = svc.defaultDurationMinutes
  serviceForm.defaultSessionsPerWeek = svc.defaultSessionsPerWeek
  serviceForm.sortOrder = svc.sortOrder
  serviceError.value = ''
  showServiceModal.value = true
}

async function submitService() {
  if (!serviceForm.name.trim()) { serviceError.value = t('validation.required', { field: t('program.service.name') }); return }
  serviceError.value = ''
  try {
    if (serviceTarget.value) {
      await store.updateService(id.value, serviceTarget.value.id, {
        name: serviceForm.name,
        serviceTypeId: serviceForm.serviceTypeId,
        defaultDurationMinutes: serviceForm.defaultDurationMinutes,
        defaultSessionsPerWeek: serviceForm.defaultSessionsPerWeek,
        sortOrder: serviceForm.sortOrder,
      })
    } else {
      await store.addService(id.value, {
        name: serviceForm.name,
        serviceTypeId: serviceForm.serviceTypeId,
        defaultDurationMinutes: serviceForm.defaultDurationMinutes,
        defaultSessionsPerWeek: serviceForm.defaultSessionsPerWeek,
        sortOrder: serviceForm.sortOrder,
      })
    }
    showServiceModal.value = false
  } catch (e: unknown) {
    serviceError.value = (e as Error).message
  }
}

const deleteServiceTarget = ref<string | null>(null)
const deleteServiceLoading = ref(false)

async function doDeleteService() {
  if (!deleteServiceTarget.value) return
  deleteServiceLoading.value = true
  try {
    await store.deleteService(id.value, deleteServiceTarget.value)
    deleteServiceTarget.value = null
  } finally {
    deleteServiceLoading.value = false
  }
}

// ── Translation Modal ─────────────────────────────────────────────────────
const showTranslationModal = ref(false)
const translationLocale = ref('')
const translationForm = reactive<SetTranslationPayload>({ name: '', description: null })
const translationError = ref('')

function openTranslationModal(locale?: string, name?: string, description?: string | null) {
  translationLocale.value = locale ?? ''
  translationForm.name = name ?? ''
  translationForm.description = description ?? null
  translationError.value = ''
  showTranslationModal.value = true
}

async function submitTranslation() {
  if (!translationLocale.value.trim()) { translationError.value = t('validation.required', { field: t('program.translation.locale') }); return }
  if (!translationForm.name.trim()) { translationError.value = t('validation.required', { field: t('program.name') }); return }
  translationError.value = ''
  try {
    await store.setTranslation(id.value, translationLocale.value, {
      name: translationForm.name,
      description: translationForm.description,
    })
    showTranslationModal.value = false
  } catch (e: unknown) {
    translationError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="store.loading && !program" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Error / Not found -->
    <div v-else-if="store.error && !program" class="text-center py-24">
      <p class="text-muted-foreground">{{ store.error }}</p>
      <button @click="router.push({ name: 'programs' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('program.backToList') }}
      </button>
    </div>
    <div v-else-if="!program && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'programs' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('program.backToList') }}
      </button>
    </div>

    <template v-else-if="program">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'programs' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('program.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ program.name }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700 font-mono">
              {{ program.code }}
            </span>
            <span v-if="program.programTypeLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
              {{ program.programTypeLabel }}
            </span>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', program.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
              {{ program.isActive ? t('common.active') : t('common.inactive') }}
            </span>
          </div>
        </div>
        <button
          v-if="can('program:update')"
          @click="router.push({ name: 'program-edit', params: { id: program.id } })"
          class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
        >
          {{ t('common.edit') }}
        </button>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button
            v-for="tab in ['overview', 'services', 'translations']"
            :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ t(`program.tab.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 space-y-3 shadow-sm">
          <h3 class="font-semibold text-foreground">{{ t('program.title') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('program.code') }}</dt>
              <dd class="font-mono font-medium text-foreground">{{ program.code }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('program.name') }}</dt>
              <dd class="font-medium text-foreground">{{ program.name }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('program.programType') }}</dt>
              <dd class="font-medium text-foreground">{{ program.programTypeLabel ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('program.isActive') }}</dt>
              <dd>
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', program.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
                  {{ program.isActive ? t('common.active') : t('common.inactive') }}
                </span>
              </dd>
            </div>
          </dl>
        </div>
        <div v-if="program.description" class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
          <h3 class="font-semibold text-foreground mb-2">{{ t('program.description2') }}</h3>
          <p class="text-sm text-muted-foreground">{{ program.description }}</p>
        </div>
      </div>

      <!-- Services Tab -->
      <div v-else-if="activeTab === 'services'">
        <div class="flex justify-end mb-3">
          <button
            v-if="can('program:update')"
            @click="openAddService"
            class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('program.service.add') }}
          </button>
        </div>
        <div v-if="program.services.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="svc in program.services.slice().sort((a, b) => a.sortOrder - b.sortOrder)"
            :key="svc.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between">
              <div class="space-y-1">
                <p class="text-sm font-semibold text-foreground">{{ svc.name }}</p>
                <p class="text-xs text-muted-foreground">{{ svc.serviceTypeLabel ?? '—' }}</p>
                <div class="flex gap-4 text-xs text-muted-foreground">
                  <span v-if="svc.defaultDurationMinutes">{{ t('program.service.duration') }}: {{ svc.defaultDurationMinutes }}</span>
                  <span v-if="svc.defaultSessionsPerWeek">{{ t('program.service.sessionsPerWeek') }}: {{ svc.defaultSessionsPerWeek }}</span>
                  <span>{{ t('program.service.sortOrder') }}: {{ svc.sortOrder }}</span>
                </div>
              </div>
              <div v-if="can('program:update')" class="flex gap-2">
                <button @click="openEditService(svc)"
                  class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent transition-colors">
                  {{ t('program.service.edit') }}
                </button>
                <button @click="deleteServiceTarget = svc.id"
                  class="px-2 py-1 text-xs rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors">
                  {{ t('common.delete') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Translations Tab -->
      <div v-else-if="activeTab === 'translations'">
        <div class="flex justify-end mb-3">
          <button
            v-if="can('program:update')"
            @click="openTranslationModal()"
            class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('program.translation.add') }}
          </button>
        </div>
        <div v-if="program.translations.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="tr in program.translations"
            :key="tr.locale"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm flex items-start justify-between"
          >
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 rounded text-xs font-mono font-medium bg-gray-100 text-gray-700 uppercase">{{ tr.locale }}</span>
                <p class="text-sm font-semibold text-foreground">{{ tr.name }}</p>
              </div>
              <p v-if="tr.description" class="text-xs text-muted-foreground">{{ tr.description }}</p>
            </div>
            <button
              v-if="can('program:update')"
              @click="openTranslationModal(tr.locale, tr.name, tr.description)"
              class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent transition-colors"
            >
              {{ t('common.edit') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- Service Modal -->
    <FormModal :open="showServiceModal" :title="serviceTarget ? t('program.service.edit') : t('program.service.add')" :saving="store.saving" @submit="submitService" @close="showServiceModal = false">
      <div class="space-y-4">
        <p v-if="serviceError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ serviceError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.service.name') }} *</label>
          <input v-model="serviceForm.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.service.serviceType') }}</label>
          <select v-model="serviceForm.serviceTypeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option :value="null">{{ t('common.select') }}</option>
            <option v-for="st in serviceTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
          </select>
        </div>
        <div class="grid grid-cols-3 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.service.duration') }}</label>
            <input v-model.number="serviceForm.defaultDurationMinutes" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.service.sessionsPerWeek') }}</label>
            <input v-model.number="serviceForm.defaultSessionsPerWeek" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.service.sortOrder') }}</label>
            <input v-model.number="serviceForm.sortOrder" type="number" min="0" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Translation Modal -->
    <FormModal :open="showTranslationModal" :title="t('program.translation.add')" :saving="store.saving" @submit="submitTranslation" @close="showTranslationModal = false">
      <div class="space-y-4">
        <p v-if="translationError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ translationError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.translation.locale') }} *</label>
          <input v-model="translationLocale" type="text" placeholder="tr, en, de..." class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.name') }} *</label>
          <input v-model="translationForm.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('program.description2') }}</label>
          <textarea v-model="translationForm.description" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete Service Confirm -->
    <ConfirmModal
      :open="!!deleteServiceTarget"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.confirmAction')"
      :confirm-label="t('common.delete')"
      :loading="deleteServiceLoading"
      @confirm="doDeleteService"
      @cancel="deleteServiceTarget = null"
    />
  </div>
</template>
