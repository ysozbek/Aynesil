<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMenuAdminActions } from '@/stores/menu.store'
import { usePermissionStore } from '@/stores/permission.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { MenuItemListItemDto, MenuItemTranslationDto } from '@/types/menu-admin.types'

const { t } = useI18n()
const menuAdmin = useMenuAdminActions()
const permStore = usePermissionStore()
const { can } = usePermission()

onMounted(async () => {
  await menuAdmin.loadAdminTree()
  await permStore.loadCatalog()
})

// ── Tree helpers ───────────────────────────────────────────────────────────────
type TreeNode = MenuItemListItemDto & { children: TreeNode[] }

const treeWithChildren = computed((): TreeNode[] => {
  const map = new Map<string, TreeNode>()
  const roots: TreeNode[] = []
  const flat = menuAdmin.adminTree.value

  flat.forEach((item) => map.set(item.id, { ...item, children: [] }))
  map.forEach((node) => {
    if (node.parentId && map.has(node.parentId)) {
      map.get(node.parentId)!.children.push(node)
    } else {
      roots.push(node)
    }
  })

  const sort = (nodes: TreeNode[]) => {
    nodes.sort((a, b) => a.sortOrder - b.sortOrder)
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
  return item.translations.find((t) => t.locale === locale)?.label ?? item.code
}

// ── Selected item ──────────────────────────────────────────────────────────────
const selectedId = ref<string | null>(null)
const selectedItem = computed(() =>
  menuAdmin.adminTree.value.find((i) => i.id === selectedId.value) ?? null
)

function selectItem(item: MenuItemListItemDto) {
  selectedId.value = selectedId.value === item.id ? null : item.id
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
    await menuAdmin.createItem({
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
    rowVersion: (item as { rowVersion?: number }).rowVersion ?? 0,
  })
  Object.keys(editErrors).forEach((k) => delete editErrors[k])
  showEdit.value = true
}

async function submitEdit() {
  if (!editTargetId.value) return
  try {
    await menuAdmin.updateItem(editTargetId.value, {
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
    await menuAdmin.setTranslations(translationsTargetId.value, { translations })
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
    await menuAdmin.removeItem(deleteTarget.value.id)
    deleteTarget.value = null
  } finally {
    deleteLoading.value = false
  }
}

// ── Toggle active ──────────────────────────────────────────────────────────────
async function toggleActive(item: MenuItemListItemDto, e: Event) {
  e.stopPropagation()
  if (item.isActive) await menuAdmin.deactivateItem(item.id)
  else await menuAdmin.activateItem(item.id)
}

// ── Reorder ────────────────────────────────────────────────────────────────────
async function moveUp(item: MenuItemListItemDto, siblings: TreeNode[], e: Event) {
  e.stopPropagation()
  const idx = siblings.findIndex((s) => s.id === item.id)
  if (idx === 0) return
  const prev = siblings[idx - 1]
  await menuAdmin.updateItem(item.id, {
    parentId: item.parentId,
    route: item.route,
    icon: item.icon,
    sortOrder: prev.sortOrder - 1,
    requiredPermissionId: item.requiredPermissionId,
    featureFlag: item.featureFlag,
    rowVersion: (item as { rowVersion?: number }).rowVersion ?? 0,
  })
}

async function moveDown(item: MenuItemListItemDto, siblings: TreeNode[], e: Event) {
  e.stopPropagation()
  const idx = siblings.findIndex((s) => s.id === item.id)
  if (idx === siblings.length - 1) return
  const next = siblings[idx + 1]
  await menuAdmin.updateItem(item.id, {
    parentId: item.parentId,
    route: item.route,
    icon: item.icon,
    sortOrder: next.sortOrder + 1,
    requiredPermissionId: item.requiredPermissionId,
    featureFlag: item.featureFlag,
    rowVersion: (item as { rowVersion?: number }).rowVersion ?? 0,
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
    <div v-if="menuAdmin.adminLoading.value" class="space-y-2">
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
  <FormModal :open="showCreate" :title="createParentId ? t('menu.createChild') : t('menu.create')" :saving="menuAdmin.adminLoading.value"
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
  <FormModal :open="showEdit" :title="t('menu.edit')" :saving="menuAdmin.adminLoading.value"
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
  <FormModal :open="showTranslations" :title="t('menu.translations')" :saving="menuAdmin.adminLoading.value"
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

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { MenuItemListItemDto } from '@/types/menu-admin.types'

// Recursive tree level component
export const MenuTreeLevel = defineComponent({
  name: 'MenuTreeLevel',
  props: {
    nodes: { type: Array as PropType<(MenuItemListItemDto & { children: unknown[] })[]>, required: true },
    depth: { type: Number, default: 0 },
    expanded: { type: Object as PropType<Set<string>>, required: true },
    getLabel: { type: Function as PropType<(item: MenuItemListItemDto, locale?: string) => string>, required: true },
    canManage: { type: Boolean, default: false },
  },
  emits: ['create-child', 'edit', 'translate', 'toggle-active', 'delete', 'move-up', 'move-down', 'toggle-expand'],
  template: `
    <div>
      <template v-for="(node, idx) in nodes" :key="node.id">
        <div
          class="flex items-center gap-2 px-4 py-2.5 border-b border-border last:border-0 hover:bg-accent/20 transition-colors"
          :style="{ paddingLeft: (depth * 20 + 16) + 'px' }"
          :class="!node.isActive ? 'opacity-60' : ''"
        >
          <!-- Expand toggle -->
          <button v-if="node.children?.length" @click="$emit('toggle-expand', node.id)"
            class="w-5 h-5 flex items-center justify-center text-muted-foreground hover:text-foreground transition-colors shrink-0">
            <svg class="w-3.5 h-3.5 transition-transform" :class="expanded.has(node.id) ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
          <div v-else class="w-5 h-5 shrink-0" />

          <!-- Icon -->
          <i v-if="node.icon" :class="node.icon" class="ki-outline w-4 text-center text-muted-foreground shrink-0" />
          <div v-else class="w-4 h-4 rounded border border-border shrink-0" />

          <!-- Label + code -->
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <span class="text-sm font-medium text-foreground truncate">{{ getLabel(node) }}</span>
              <span class="text-xs text-muted-foreground font-mono">{{ node.code }}</span>
              <span v-if="node.requiredPermissionCode" class="text-xs text-blue-600 font-mono bg-blue-50 px-1 rounded">{{ node.requiredPermissionCode }}</span>
            </div>
            <p v-if="node.route" class="text-xs text-muted-foreground font-mono">{{ node.route }}</p>
          </div>

          <!-- Sort order -->
          <span class="text-xs text-muted-foreground w-10 text-right shrink-0">{{ node.sortOrder }}</span>

          <!-- Actions -->
          <div v-if="canManage" class="flex items-center gap-0.5 shrink-0">
            <button @click="$emit('move-up', node, nodes, $event)" :disabled="idx === 0" class="p-1 rounded hover:bg-accent text-muted-foreground disabled:opacity-30" title="Yukarı">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" /></svg>
            </button>
            <button @click="$emit('move-down', node, nodes, $event)" :disabled="idx === nodes.length - 1" class="p-1 rounded hover:bg-accent text-muted-foreground disabled:opacity-30" title="Aşağı">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
            </button>
            <button @click="$emit('create-child', node.id)" class="p-1 rounded hover:bg-accent text-muted-foreground" title="Alt öğe ekle">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
            </button>
            <button @click="$emit('translate', node, $event)" class="p-1 rounded hover:bg-accent text-muted-foreground" title="Çeviriler">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129" /></svg>
            </button>
            <button @click="$emit('edit', node, $event)" class="p-1 rounded hover:bg-accent text-muted-foreground" title="Düzenle">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
            </button>
            <button @click="$emit('toggle-active', node, $event)" :class="node.isActive ? 'text-amber-600' : 'text-emerald-600'" class="p-1 rounded hover:bg-accent" :title="node.isActive ? 'Devre dışı bırak' : 'Aktif et'">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.636 5.636a9 9 0 1012.728 12.728M9 9l6 6" /></svg>
            </button>
            <button @click="$emit('delete', node, $event)" class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600" title="Sil">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
            </button>
          </div>
        </div>

        <!-- Recursive children -->
        <MenuTreeLevel v-if="node.children?.length && expanded.has(node.id)"
          :nodes="node.children" :depth="depth + 1" :expanded="expanded" :get-label="getLabel" :can-manage="canManage"
          @create-child="$emit('create-child', $event)"
          @edit="(item, e) => $emit('edit', item, e)"
          @translate="(item, e) => $emit('translate', item, e)"
          @toggle-active="(item, e) => $emit('toggle-active', item, e)"
          @delete="(item, e) => $emit('delete', item, e)"
          @move-up="(item, siblings, e) => $emit('move-up', item, siblings, e)"
          @move-down="(item, siblings, e) => $emit('move-down', item, siblings, e)"
          @toggle-expand="$emit('toggle-expand', $event)"
        />
      </template>
    </div>
  `,
})
</script>
