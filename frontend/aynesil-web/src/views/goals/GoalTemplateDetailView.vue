<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useGoalStore } from '@/stores/goal.store'
import { usePermission } from '@/composables/usePermission'
import FormModal from '@/components/shared/FormModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useGoalStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const template = computed(() => store.currentTemplate)
const activeTab = ref<'overview' | 'translations'>('overview')

// Translation modal
const showTranslationModal = ref(false)
const editTranslationLocale = ref<string | null>(null)
const translationForm = ref({ locale: '', statement: '', defaultCriteria: '' })
const translationError = ref('')

onMounted(async () => {
  await store.fetchTemplate(id.value)
})

onUnmounted(() => {
  store.clearCurrent()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

function openAddTranslation() {
  editTranslationLocale.value = null
  translationForm.value = { locale: '', statement: '', defaultCriteria: '' }
  translationError.value = ''
  showTranslationModal.value = true
}

function openEditTranslation(locale: string, statement: string, defaultCriteria: string | null) {
  editTranslationLocale.value = locale
  translationForm.value = { locale, statement, defaultCriteria: defaultCriteria ?? '' }
  translationError.value = ''
  showTranslationModal.value = true
}

async function submitTranslation() {
  if (!translationForm.value.locale.trim()) {
    translationError.value = t('validation.required', { field: 'Dil (locale)' })
    return
  }
  if (!translationForm.value.statement.trim()) {
    translationError.value = t('validation.required', { field: t('goal.template.statement') })
    return
  }
  try {
    await store.setTemplateTranslation(id.value, translationForm.value.locale, {
      statement: translationForm.value.statement,
      defaultCriteria: translationForm.value.defaultCriteria || null,
    })
    showTranslationModal.value = false
  } catch (e: unknown) {
    translationError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <!-- Loading skeleton -->
    <div v-if="store.loading && !template" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- 404 -->
    <div v-else-if="!template && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'goal-template-list' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('goal.template.title') }}
      </button>
    </div>

    <template v-else-if="template">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'goal-template-list' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('goal.template.title') }}
          </button>
          <h1 class="text-xl font-bold text-foreground">{{ template.code ? `[${template.code}]` : '' }} {{ template.statement }}</h1>
          <div class="flex items-center gap-2 mt-1">
            <span v-if="template.libraryName" class="text-xs text-muted-foreground">{{ template.libraryName }}</span>
            <span v-if="template.categoryLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-accent text-foreground">{{ template.categoryLabel }}</span>
            <span v-if="template.developmentAreaLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">{{ template.developmentAreaLabel }}</span>
          </div>
        </div>
        <button
          v-if="can('goal_template:update')"
          @click="router.push({ name: 'goal-template-edit', params: { id: template.id } })"
          class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
        >
          {{ t('common.edit') }}
        </button>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button
            v-for="tab in ['overview', 'translations']"
            :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ tab === 'overview' ? t('bep.tab.overview') : 'Çeviriler' }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="space-y-4">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
          <dl class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.template.code') }}</dt>
              <dd class="font-mono font-medium text-foreground">{{ template.code ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.template.library') }}</dt>
              <dd class="font-medium text-foreground">{{ template.libraryName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.template.category') }}</dt>
              <dd class="font-medium text-foreground">{{ template.categoryLabel ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.template.developmentArea') }}</dt>
              <dd class="font-medium text-foreground">{{ template.developmentAreaLabel ?? '—' }}</dd>
            </div>
            <div class="md:col-span-2">
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.template.statement') }}</dt>
              <dd class="text-foreground">{{ template.statement }}</dd>
            </div>
            <div v-if="template.defaultCriteria" class="md:col-span-2">
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.template.defaultCriteria') }}</dt>
              <dd class="text-foreground">{{ template.defaultCriteria }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('common.createdAt') }}</dt>
              <dd class="text-muted-foreground">{{ formatDate(template.createdAt) }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Translations Tab -->
      <div v-else-if="activeTab === 'translations'" class="space-y-4">
        <div class="flex justify-end">
          <button
            v-if="can('goal_template:translate')"
            @click="openAddTranslation"
            class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Çeviri Ekle
          </button>
        </div>
        <div v-if="template.translations.length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="tr in template.translations"
            :key="tr.locale"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between">
              <div class="flex-1">
                <span class="inline-block px-2 py-0.5 rounded text-xs font-mono font-medium bg-accent text-foreground mb-2">
                  {{ tr.locale.toUpperCase() }}
                </span>
                <p class="text-sm text-foreground">{{ tr.statement }}</p>
                <p v-if="tr.defaultCriteria" class="text-xs text-muted-foreground mt-1">{{ tr.defaultCriteria }}</p>
              </div>
              <button
                v-if="can('goal_template:translate')"
                @click="openEditTranslation(tr.locale, tr.statement, tr.defaultCriteria)"
                class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors ml-2"
                :title="t('common.edit')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Translation Modal -->
    <FormModal
      :open="showTranslationModal"
      :title="editTranslationLocale ? 'Çeviriyi Düzenle' : 'Çeviri Ekle'"
      :saving="store.saving"
      @submit="submitTranslation"
      @close="showTranslationModal = false"
    >
      <div class="space-y-4">
        <p v-if="translationError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ translationError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">Dil (locale) *</label>
          <input
            v-model="translationForm.locale"
            type="text"
            :readonly="!!editTranslationLocale"
            placeholder="tr, en, ar..."
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="editTranslationLocale ? 'bg-accent' : ''"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.statement') }} *</label>
          <textarea v-model="translationForm.statement" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.defaultCriteria') }}</label>
          <textarea v-model="translationForm.defaultCriteria" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
