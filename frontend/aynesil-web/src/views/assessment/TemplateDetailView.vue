<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useAssessmentTemplateStore()
const refData = useRefDataStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const tpl = computed(() => store.current)
const activeTab = ref<'sections' | 'translations' | 'info'>('info')

const devAreas = ref<RefValueItem[]>([])

onMounted(async () => {
  await store.fetchOne(id.value)
  await refData.getValues('DEVELOPMENT_AREA').then(v => { devAreas.value = v })
})

onUnmounted(() => store.clearCurrent())

// ── Section Management ─────────────────────────────────────────────────────────
const showAddSection = ref(false)
const sectionForm = ref({ code: '', sortOrder: 1, developmentAreaId: '' })
const sectionError = ref('')
const editSectionId = ref<string | null>(null)

function openAddSection() {
  const nextOrder = (tpl.value?.sections.length ?? 0) + 1
  sectionForm.value = { code: '', sortOrder: nextOrder, developmentAreaId: '' }
  sectionError.value = ''
  editSectionId.value = null
  showAddSection.value = true
}

async function submitSection() {
  if (!sectionForm.value.code.trim()) { sectionError.value = t('validation.required', { field: t('assessment.section.code') }); return }
  try {
    if (editSectionId.value) {
      await store.updateSection(editSectionId.value, {
        code: sectionForm.value.code,
        sortOrder: sectionForm.value.sortOrder,
        developmentAreaId: sectionForm.value.developmentAreaId || undefined,
      })
    } else {
      await store.addSection(id.value, {
        code: sectionForm.value.code,
        sortOrder: sectionForm.value.sortOrder,
        developmentAreaId: sectionForm.value.developmentAreaId || undefined,
      })
    }
    showAddSection.value = false
  } catch (e: unknown) {
    sectionError.value = (e as Error).message
  }
}

const deleteSectionTarget = ref<string | null>(null)
async function doDeleteSection() {
  if (!deleteSectionTarget.value) return
  await store.deleteSection(deleteSectionTarget.value)
  deleteSectionTarget.value = null
}

// ── Item Management ────────────────────────────────────────────────────────────
const showAddItem = ref(false)
const addItemSectionId = ref<string | null>(null)
const itemForm = ref({ code: '', prompt: '', responseType: 'numeric', choices: '', weight: 1, sortOrder: 1 })
const itemError = ref('')
const editItemId = ref<string | null>(null)

function openAddItem(sectionId: string, sortOrder: number) {
  addItemSectionId.value = sectionId
  itemForm.value = { code: '', prompt: '', responseType: 'numeric', choices: '', weight: 1, sortOrder }
  itemError.value = ''
  editItemId.value = null
  showAddItem.value = true
}

async function submitItem() {
  if (!itemForm.value.code.trim()) { itemError.value = t('validation.required', { field: t('assessment.item.code') }); return }
  if (!itemForm.value.prompt.trim()) { itemError.value = t('validation.required', { field: t('assessment.item.prompt') }); return }
  try {
    if (editItemId.value) {
      await store.updateItem(editItemId.value, {
        code: itemForm.value.code,
        prompt: itemForm.value.prompt,
        responseType: itemForm.value.responseType,
        choices: itemForm.value.choices || undefined,
        weight: itemForm.value.weight,
        sortOrder: itemForm.value.sortOrder,
      })
    } else if (addItemSectionId.value) {
      await store.addItem(addItemSectionId.value, {
        code: itemForm.value.code,
        prompt: itemForm.value.prompt,
        responseType: itemForm.value.responseType,
        choices: itemForm.value.choices || undefined,
        weight: itemForm.value.weight,
        sortOrder: itemForm.value.sortOrder,
      })
    }
    showAddItem.value = false
  } catch (e: unknown) {
    itemError.value = (e as Error).message
  }
}

const deleteItemTarget = ref<string | null>(null)
async function doDeleteItem() {
  if (!deleteItemTarget.value) return
  await store.deleteItem(deleteItemTarget.value)
  deleteItemTarget.value = null
}

// ── Activate / Version ─────────────────────────────────────────────────────────
async function toggleActive() {
  if (!tpl.value) return
  await store.setActive(tpl.value.id, { isActive: !tpl.value.isActive, rowVersion: tpl.value.rowVersion })
}

async function createVersion() {
  if (!tpl.value) return
  const newTpl = await store.createVersion(tpl.value.id)
  router.push({ name: 'assessment-template-detail', params: { id: newTpl.id } })
}

// ── Translation ────────────────────────────────────────────────────────────────
const showTranslation = ref(false)
const translationForm = ref({ locale: 'tr', name: '', description: '' })
const translationError = ref('')

function openTranslation(locale?: string) {
  const existing = tpl.value?.translations.find(tr => tr.locale === (locale ?? 'tr'))
  translationForm.value = { locale: locale ?? 'tr', name: existing?.name ?? '', description: existing?.description ?? '' }
  translationError.value = ''
  showTranslation.value = true
}

async function submitTranslation() {
  if (!translationForm.value.name.trim()) { translationError.value = t('validation.required', { field: t('assessment.translation.name') }); return }
  try {
    await store.upsertTranslation(id.value, translationForm.value.locale, { name: translationForm.value.name, description: translationForm.value.description || undefined })
    showTranslation.value = false
  } catch (e: unknown) {
    translationError.value = (e as Error).message
  }
}

const responseTypeLabel = (type: string): string => {
  const labels: Record<string, string> = {
    numeric: t('assessment.item.types.numeric'),
    scale: t('assessment.item.types.scale'),
    boolean: t('assessment.item.types.boolean'),
    text: t('assessment.item.types.text'),
    choice: t('assessment.item.types.choice'),
  }
  return labels[type] ?? type
}
</script>

<template>
  <div>
    <div v-if="store.loading && !tpl" class="space-y-3">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <div v-else-if="!tpl && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
    </div>

    <template v-else-if="tpl">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'assessment-templates' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('assessment.template.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ tpl.name }}</h1>
          <div class="flex items-center gap-2 mt-1">
            <span class="text-sm text-muted-foreground font-mono">{{ tpl.code }}</span>
            <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">v{{ tpl.version }}</span>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', tpl.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
              {{ tpl.isActive ? t('common.active') : t('common.passive') }}
            </span>
          </div>
        </div>
        <div class="flex items-center gap-2 flex-wrap">
          <button v-if="can('assessment_template:publish')" @click="toggleActive"
            :class="['px-3 py-2 text-sm rounded-lg border transition-colors', tpl.isActive ? 'border-border hover:bg-accent' : 'border-emerald-300 text-emerald-700 hover:bg-emerald-50']">
            {{ tpl.isActive ? t('common.deactivate') : t('common.activate') }}
          </button>
          <button v-if="can('assessment_template:version')" @click="createVersion"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
            {{ t('assessment.template.createVersion') }}
          </button>
          <button v-if="can('assessment_template:update')"
            @click="router.push({ name: 'assessment-template-edit', params: { id: tpl.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
            {{ t('common.edit') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button v-for="tab in ['info', 'sections', 'translations']" :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']">
            {{ t(`assessment.template.tab.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Info Tab -->
      <div v-if="activeTab === 'info'" class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <dl class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <dt class="text-muted-foreground">{{ t('assessment.template.type') }}</dt>
            <dd class="font-medium text-foreground mt-0.5">{{ tpl.typeName ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-muted-foreground">{{ t('assessment.template.category') }}</dt>
            <dd class="font-medium text-foreground mt-0.5">{{ tpl.categoryName ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-muted-foreground">{{ t('assessment.template.scoringModel') }}</dt>
            <dd class="font-mono text-foreground mt-0.5">{{ tpl.scoringModel ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-muted-foreground">{{ t('assessment.template.version') }}</dt>
            <dd class="font-medium text-foreground mt-0.5">{{ tpl.version }}</dd>
          </div>
          <div>
            <dt class="text-muted-foreground">{{ t('common.createdAt') }}</dt>
            <dd class="font-medium text-foreground mt-0.5">{{ new Date(tpl.createdAt).toLocaleDateString('tr-TR') }}</dd>
          </div>
        </dl>
      </div>

      <!-- Sections Tab -->
      <div v-else-if="activeTab === 'sections'">
        <div class="mb-4 flex items-center justify-between">
          <p class="text-sm text-muted-foreground">{{ tpl.sections.length }} {{ t('assessment.template.sections') }}</p>
          <button v-if="can('assessment_template:update')" @click="openAddSection"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('assessment.section.add') }}
          </button>
        </div>

        <div v-if="tpl.sections.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('assessment.section.none') }}
        </div>
        <div v-else class="space-y-4">
          <div v-for="section in [...tpl.sections].sort((a, b) => a.sortOrder - b.sortOrder)" :key="section.id"
            class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
            <!-- Section header -->
            <div class="flex items-center justify-between px-4 py-3 bg-accent/30 border-b border-border">
              <div>
                <span class="text-sm font-semibold text-foreground font-mono">{{ section.code }}</span>
                <span v-if="section.developmentAreaName" class="ml-2 text-xs text-muted-foreground">· {{ section.developmentAreaName }}</span>
              </div>
              <div class="flex items-center gap-2">
                <button v-if="can('assessment_template:update')" @click="openAddItem(section.id, section.items.length + 1)"
                  class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent transition-colors">
                  + {{ t('assessment.item.add') }}
                </button>
                <button v-if="can('assessment_template:delete')" @click="deleteSectionTarget = section.id"
                  class="p-1.5 rounded-lg hover:bg-red-50 text-red-500 transition-colors">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
            <!-- Section items -->
            <div v-if="section.items.length === 0" class="px-4 py-4 text-sm text-muted-foreground text-center">
              {{ t('assessment.item.none') }}
            </div>
            <div v-else>
              <div v-for="item in [...section.items].sort((a, b) => a.sortOrder - b.sortOrder)" :key="item.id"
                class="flex items-start justify-between px-4 py-3 border-b border-border last:border-0 hover:bg-accent/10">
                <div class="flex-1 min-w-0 pr-4">
                  <p class="text-sm text-foreground">{{ item.prompt }}</p>
                  <p class="text-xs text-muted-foreground mt-0.5">
                    <span class="font-mono">{{ item.code }}</span>
                    · {{ responseTypeLabel(item.responseType) }}
                    · {{ t('assessment.item.weight') }}: {{ item.weight }}
                  </p>
                </div>
                <div v-if="can('assessment_template:delete')" class="flex-none">
                  <button @click="deleteItemTarget = item.id"
                    class="p-1 rounded hover:bg-red-50 text-red-400 transition-colors">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Translations Tab -->
      <div v-else-if="activeTab === 'translations'">
        <div class="mb-4 flex items-center justify-between">
          <p class="text-sm text-muted-foreground">{{ tpl.translations.length }} {{ t('assessment.template.translationsCount') }}</p>
          <button v-if="can('assessment_template:update')" @click="openTranslation()"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
            + {{ t('assessment.translation.add') }}
          </button>
        </div>
        <div v-if="tpl.translations.length === 0" class="text-center py-12 text-muted-foreground">{{ t('assessment.translation.none') }}</div>
        <div v-else class="space-y-2">
          <div v-for="tr in tpl.translations" :key="tr.locale"
            class="flex items-center justify-between rounded-xl border border-border bg-[--color-card] px-4 py-3 shadow-sm">
            <div>
              <span class="text-xs font-mono font-medium text-foreground uppercase">{{ tr.locale }}</span>
              <p class="text-sm text-foreground mt-0.5">{{ tr.name }}</p>
              <p v-if="tr.description" class="text-xs text-muted-foreground">{{ tr.description }}</p>
            </div>
            <button v-if="can('assessment_template:update')" @click="openTranslation(tr.locale)"
              class="p-1.5 rounded-lg hover:bg-accent transition-colors text-muted-foreground">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- Add Section Modal -->
    <FormModal :open="showAddSection" :title="editSectionId ? t('assessment.section.edit') : t('assessment.section.add')" :saving="store.saving" @submit="submitSection" @close="showAddSection = false">
      <div class="space-y-4">
        <p v-if="sectionError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ sectionError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.section.code') }} *</label>
          <input v-model="sectionForm.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.section.sortOrder') }}</label>
            <input v-model.number="sectionForm.sortOrder" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.section.developmentArea') }}</label>
            <select v-model="sectionForm.developmentAreaId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="a in devAreas" :key="a.id" :value="a.id">{{ a.label }}</option>
            </select>
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Add Item Modal -->
    <FormModal :open="showAddItem" :title="editItemId ? t('assessment.item.edit') : t('assessment.item.add')" :saving="store.saving" @submit="submitItem" @close="showAddItem = false">
      <div class="space-y-4">
        <p v-if="itemError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ itemError }}</p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.item.code') }} *</label>
            <input v-model="itemForm.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.item.responseType') }}</label>
            <select v-model="itemForm.responseType" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="numeric">{{ t('assessment.item.types.numeric') }}</option>
              <option value="scale">{{ t('assessment.item.types.scale') }}</option>
              <option value="boolean">{{ t('assessment.item.types.boolean') }}</option>
              <option value="text">{{ t('assessment.item.types.text') }}</option>
              <option value="choice">{{ t('assessment.item.types.choice') }}</option>
            </select>
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.item.prompt') }} *</label>
          <textarea v-model="itemForm.prompt" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div v-if="itemForm.responseType === 'choice'">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.item.choices') }}</label>
          <input v-model="itemForm.choices" type="text" :placeholder="t('assessment.item.choicesHint')"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.item.weight') }}</label>
            <input v-model.number="itemForm.weight" type="number" step="0.1" min="0" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.section.sortOrder') }}</label>
            <input v-model.number="itemForm.sortOrder" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Translation Modal -->
    <FormModal :open="showTranslation" :title="t('assessment.translation.add')" :saving="store.saving" @submit="submitTranslation" @close="showTranslation = false">
      <div class="space-y-4">
        <p v-if="translationError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ translationError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.translation.locale') }}</label>
          <input v-model="translationForm.locale" type="text" maxlength="5" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.translation.name') }} *</label>
          <input v-model="translationForm.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.translation.description') }}</label>
          <textarea v-model="translationForm.description" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete Section Confirm -->
    <ConfirmModal :open="!!deleteSectionTarget" :title="t('assessment.section.deleteTitle')" :message="t('assessment.section.deleteMessage')"
      :confirm-label="t('common.delete')" :loading="store.saving" @confirm="doDeleteSection" @cancel="deleteSectionTarget = null" />

    <!-- Delete Item Confirm -->
    <ConfirmModal :open="!!deleteItemTarget" :title="t('assessment.item.deleteTitle')" :message="t('assessment.item.deleteMessage')"
      :confirm-label="t('common.delete')" :loading="store.saving" @confirm="doDeleteItem" @cancel="deleteItemTarget = null" />
  </div>
</template>
