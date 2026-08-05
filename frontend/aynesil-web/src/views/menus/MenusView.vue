<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMenuAdminActions } from '@/stores/menu.store'
import { usePermissionStore } from '@/stores/permission.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import MenuTreeLevel, { type TreeNode } from './MenuTreeLevel.vue'
import type { MenuItemListItemDto, MenuItemTranslationDto } from '@/types/menu-admin.types'

const { t } = useI18n()
const { adminTree, adminLoading, loadAdminTree, createItem, updateItem, removeItem, setTranslations, activateItem, deactivateItem } =
  useMenuAdminActions()
const permStore = usePermissionStore()
const { can } = usePermission()

onMounted(async () => {
  await loadAdminTree()
  await permStore.loadCatalog()
})

const treeWithChildren = computed((): TreeNode[] => {
  const map = new Map<string, TreeNode>()
  const roots: TreeNode[] = []

  adminTree.value.forEach((item) => map.set(item.id, { ...item, children: [] }))
  map.forEach((node) => {
    if (node.parentId && map.has(node.parentId)) {
      map.get(node.parentId)!.children.push(node)
    } else {
      roots.push(node)
    }
  })

  const sort = (nodes: TreeNode[]) => {
    nodes.sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code))
    nodes.forEach((n) => sort(n.children))
  }
  sort(roots)
  return roots
})

// ── Expanded state ─────────────────────────────────────────────────────────────
const expanded = ref<Set<string>>(new Set())
function toggleExpand(id: string) {
  if (expanded.value.has(id)) expanded.value.delete(id)
  else expanded.value.add(id)
}

function getLabel(item: MenuItemListItemDto, locale = 'tr') {
  return item.translations?.find((tr) => tr.locale === locale)?.label ?? item.code
}

// ── Create modal ───────────────────────────────────────────────────────────────
const showCreate = ref(false)
const createParentId = ref<string | undefined>(undefined)

const createForm = reactive({
  code: '',
  route: '',
  icon: '',
  sortOrder: 100,
  requiredPermissionId: '',
  featureFlag: '',
  labelTr: '',
  labelEn: '',
})
const createErrors = reactive<Record<string, string>>({})

function openCreate(parentId?: string) {
  createParentId.value = parentId
  Object.assign(createForm, { code: '', route: '', icon: '', sortOrder: 100, requiredPermissionId: '', featureFlag: '', labelTr: '', labelEn: '' })
  Object.keys(createErrors).forEach((k) => delete createErrors[k])
  showCreate.value = true
}

async function submitCreate() {
  Object.keys(createErrors).forEach((k) => delete createErrors[k])
  if (!createForm.code.trim()) { createErrors.code = t('validation.required', { field: t('menu.code') }); return }
  if (!createForm.labelTr.trim()) { createErrors.labelTr = t('validation.required', { field: 'Türkçe etiket' }); return }

  const translations: MenuItemTranslationDto[] = [{ locale: 'tr', label: createForm.labelTr }]
  if (createForm.labelEn.trim()) translations.push({ locale: 'en', label: createForm.labelEn })

  try {
    await createItem({
      parentId: createParentId.value,
      code: createForm.code,
      route: createForm.route || undefined,
      icon: createForm.icon || undefined,
      sortOrder: createForm.sortOrder,
      requiredPermissionId: createForm.requiredPermissionId || undefined,
      featureFlag: createForm.featureFlag || undefined,
      translations,
    })
    showCreate.value = false
  } catch (err: unknown) {
    createErrors.general = (err as Error).message
  }
}

// ── Edit modal ─────────────────────────────────────────────────────────────────
const showEdit = ref(false)
const editTargetId = ref<string | null>(null)

const editForm = reactive({
  parentId: '',
  route: '',
  icon: '',
  sortOrder: 100,
  requiredPermissionId: '',
  featureFlag: '',
  rowVersion: 0,
})
const editErrors = reactive<Record<string, string>>({})

function openEdit(item: MenuItemListItemDto, e: Event) {
  e.stopPropagation()
  editTargetId.value = item.id
  Object.assign(editForm, {
    parentId: item.parentId ?? '',
    route: item.route ?? '',
    icon: item.icon ?? '',
    sortOrder: item.sortOrder,
    requiredPermissionId: item.requiredPermissionId ?? '',
    featureFlag: item.featureFlag ?? '',
    rowVersion: item.rowVersion,
  })
  Object.keys(editErrors).forEach((k) => delete editErrors[k])
  showEdit.value = true
}

async function submitEdit() {
  if (!editTargetId.value) return
  try {
    await updateItem(editTargetId.value, {
      parentId: editForm.parentId || undefined,
      route: editForm.route || undefined,
      icon: editForm.icon || undefined,
      sortOrder: editForm.sortOrder,
      requiredPermissionId: editForm.requiredPermissionId || undefined,
      featureFlag: editForm.featureFlag || undefined,
      rowVersion: editForm.rowVersion,
    })
    showEdit.value = false
  } catch (err: unknown) {
    editErrors.general = (err as Error).message
  }
}

// ── Translations modal ─────────────────────────────────────────────────────────
const showTranslations = ref(false)
const translationsTargetId = ref<string | null>(null)
const translationForms = ref<{ locale: string; label: string }[]>([{ locale: 'tr', label: '' }, { locale: 'en', label: '' }])
const translationErrors = reactive<Record<string, string>>({})

function openTranslations(item: MenuItemListItemDto, e: Event) {
  e.stopPropagation()
  translationsTargetId.value = item.id
  translationForms.value = [
    { locale: 'tr', label: item.translations.find((t) => t.locale === 'tr')?.label ?? '' },
    { locale: 'en', label: item.translations.find((t) => t.locale === 'en')?.label ?? '' },
  ]
  Object.keys(translationErrors).forEach((k) => delete translationErrors[k])
  showTranslations.value = true
}

async function submitTranslations() {
  if (!translationsTargetId.value) return
  const translations = translationForms.value.filter((f) => f.label.trim())
  if (!translations.length) { translationErrors.general = 'En az bir çeviri gereklidir.'; return }
  try {
    await setTranslations(translationsTargetId.value, { translations })
    showTranslations.value = false
  } catch (err: unknown) {
    translationErrors.general = (err as Error).message
  }
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<MenuItemListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(item: MenuItemListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = item
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await removeItem(deleteTarget.value.id)
    deleteTarget.value = null
  } finally {
    deleteLoading.value = false
  }
}

// ── Toggle active ──────────────────────────────────────────────────────────────
async function toggleActive(item: MenuItemListItemDto, e: Event) {
  e.stopPropagation()
  if (item.isActive) await deactivateItem(item.id)
  else await activateItem(item.id)
}

// ── Reorder ────────────────────────────────────────────────────────────────────
async function moveUp(item: MenuItemListItemDto, siblings: TreeNode[], e: Event) {
  e.stopPropagation()
  const idx = siblings.findIndex((s) => s.id === item.id)
  if (idx === 0) return
  const prev = siblings[idx - 1]
  await updateItem(item.id, {
    parentId: item.parentId,
    route: item.route,
    icon: item.icon,
    sortOrder: prev.sortOrder - 1,
    requiredPermissionId: item.requiredPermissionId,
    featureFlag: item.featureFlag,
    rowVersion: item.rowVersion,
  })
}

async function moveDown(item: MenuItemListItemDto, siblings: TreeNode[], e: Event) {
  e.stopPropagation()
  const idx = siblings.findIndex((s) => s.id === item.id)
  if (idx === siblings.length - 1) return
  const next = siblings[idx + 1]
  await updateItem(item.id, {
    parentId: item.parentId,
    route: item.route,
    icon: item.icon,
    sortOrder: next.sortOrder + 1,
    requiredPermissionId: item.requiredPermissionId,
    featureFlag: item.featureFlag,
    rowVersion: item.rowVersion,
  })
}

const allPermissions = computed(() => permStore.catalog)
</script>

<template>
  <div>
    <PageHeader :title="t('menu.title')" :description="t('menu.description')">
      <button v-if="can('menu:manage')" @click="openCreate(undefined)"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('menu.create') }}
      </button>
    </PageHeader>

    <!-- Loading -->
    <div v-if="adminLoading" class="space-y-2">
      <div v-for="i in 6" :key="i" class="bg-[--color-card] rounded-xl h-14 border border-border animate-pulse" />
    </div>

    <!-- Empty -->
    <div v-else-if="!treeWithChildren.length" class="text-center py-16 text-muted-foreground text-sm">
      {{ t('menu.empty') }}
    </div>

    <!-- Tree -->
    <div v-else class="bg-[--color-card] rounded-xl border border-border shadow-sm overflow-hidden">
      <MenuTreeLevel :nodes="treeWithChildren" :depth="0"
        @create-child="openCreate" @edit="openEdit" @translate="openTranslations"
        @toggle-active="toggleActive" @delete="confirmDelete"
        @move-up="moveUp" @move-down="moveDown"
        :get-label="getLabel" :expanded="expanded" @toggle-expand="toggleExpand"
        :can-manage="can('menu:manage')" />
    </div>
  </div>

  <!-- Create modal -->
  <FormModal :open="showCreate" :title="createParentId ? t('menu.createChild') : t('menu.create')" :saving="adminLoading"
    @submit="submitCreate" @close="showCreate = false">
    <div class="space-y-4">
      <p v-if="createErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ createErrors.general }}</p>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.code') }} *</label>
        <input v-model="createForm.code" type="text" placeholder="dashboard, users, settings..."
          class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary font-mono"
          :class="createErrors.code ? 'border-red-400' : 'border-border'" />
        <p v-if="createErrors.code" class="mt-1 text-xs text-red-600">{{ createErrors.code }}</p>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.labelTr') }} *</label>
        <input v-model="createForm.labelTr" type="text"
          class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
          :class="createErrors.labelTr ? 'border-red-400' : 'border-border'" />
        <p v-if="createErrors.labelTr" class="mt-1 text-xs text-red-600">{{ createErrors.labelTr }}</p>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.labelEn') }}</label>
        <input v-model="createForm.labelEn" type="text"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.route') }}</label>
          <input v-model="createForm.route" type="text" placeholder="/users, /settings..."
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary font-mono" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.icon') }}</label>
          <input v-model="createForm.icon" type="text" placeholder="ki-users ki-outline"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary font-mono" />
        </div>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.sortOrder') }}</label>
          <input v-model.number="createForm.sortOrder" type="number"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.requiredPermission') }}</label>
          <select v-model="createForm.requiredPermissionId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.none') }}</option>
            <option v-for="p in allPermissions" :key="p.id" :value="p.id">{{ p.code }}</option>
          </select>
        </div>
      </div>
    </div>
  </FormModal>

  <!-- Edit modal -->
  <FormModal :open="showEdit" :title="t('menu.edit')" :saving="adminLoading"
    @submit="submitEdit" @close="showEdit = false">
    <div class="space-y-4">
      <p v-if="editErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ editErrors.general }}</p>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.route') }}</label>
          <input v-model="editForm.route" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary font-mono" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.icon') }}</label>
          <input v-model="editForm.icon" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary font-mono" />
        </div>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.sortOrder') }}</label>
          <input v-model.number="editForm.sortOrder" type="number" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.requiredPermission') }}</label>
          <select v-model="editForm.requiredPermissionId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.none') }}</option>
            <option v-for="p in allPermissions" :key="p.id" :value="p.id">{{ p.code }}</option>
          </select>
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('menu.featureFlag') }}</label>
        <input v-model="editForm.featureFlag" type="text" placeholder="consultancy, kpi..."
          class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary font-mono" />
      </div>
    </div>
  </FormModal>

  <!-- Translations modal -->
  <FormModal :open="showTranslations" :title="t('menu.translations')" :saving="adminLoading"
    @submit="submitTranslations" @close="showTranslations = false">
    <div class="space-y-3">
      <p v-if="translationErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ translationErrors.general }}</p>
      <div v-for="tf in translationForms" :key="tf.locale">
        <label class="block text-sm font-medium text-foreground mb-1">
          {{ tf.locale === 'tr' ? '🇹🇷 Türkçe' : '🇬🇧 English' }}
          <span v-if="tf.locale === 'tr'" class="text-red-500 ml-0.5">*</span>
        </label>
        <input v-model="tf.label" type="text"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>
    </div>
  </FormModal>

  <!-- Delete confirm -->
  <ConfirmModal :open="!!deleteTarget" :title="t('menu.deleteTitle')"
    :message="t('menu.deleteMessage', { code: deleteTarget?.code })"
    :confirm-label="t('common.delete')" :loading="deleteLoading" @confirm="doDelete" @cancel="deleteTarget = null" />
</template>
