<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useRoleStore } from '@/stores/role.store'
import { usePermissionStore } from '@/stores/permission.store'
import { usePermission } from '@/composables/usePermission'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { PermissionListItemDto } from '@/types/permission.types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const store = useRoleStore()
const permStore = usePermissionStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const activeTab = ref<'info' | 'permissions'>('info')

onMounted(async () => {
  await store.fetchOne(id.value)
  await permStore.loadCatalog()
})

// ── Edit role ─────────────────────────────────────────────────────────────────
const showEdit = ref(false)
const editForm = reactive({ name: '', description: '' })
const editErrors = reactive<Record<string, string>>({})

function openEdit() {
  if (!store.current) return
  Object.assign(editForm, { name: store.current.name, description: store.current.description ?? '' })
  Object.keys(editErrors).forEach((k) => delete editErrors[k])
  showEdit.value = true
}

async function submitEdit() {
  Object.keys(editErrors).forEach((k) => delete editErrors[k])
  if (!editForm.name.trim()) { editErrors.name = t('validation.required', { field: t('role.name') }); return }
  try {
    await store.update(id.value, { name: editForm.name, description: editForm.description || undefined, rowVersion: store.current!.rowVersion })
    await store.fetchOne(id.value)
    showEdit.value = false
  } catch (err: unknown) {
    editErrors.general = (err as Error).message
  }
}

// ── Permission matrix ──────────────────────────────────────────────────────────
const permSearchQuery = ref('')
const savingPermissions = ref(false)
const permError = ref('')

const assignedPermissionIds = computed(() => new Set(store.current?.permissions.map((p) => p.id) ?? []))

const filteredCatalog = computed(() => {
  const q = permSearchQuery.value.toLowerCase()
  if (!q) return permStore.catalogByResource
  const filtered: Record<string, PermissionListItemDto[]> = {}
  for (const [resource, perms] of Object.entries(permStore.catalogByResource)) {
    const matched = perms.filter(
      (p) => p.code.toLowerCase().includes(q) || p.action.toLowerCase().includes(q) || resource.toLowerCase().includes(q)
    )
    if (matched.length) filtered[resource] = matched
  }
  return filtered
})

function isAssigned(permId: string): boolean {
  return assignedPermissionIds.value.has(permId)
}

async function togglePermission(perm: PermissionListItemDto) {
  if (!can('role:assign_permission')) return
  permError.value = ''
  savingPermissions.value = true
  try {
    if (isAssigned(perm.id)) {
      await store.removePermission(id.value, perm.id)
    } else {
      await store.assignPermission(id.value, { permissionId: perm.id })
    }
  } catch (err: unknown) {
    permError.value = (err as Error).message
  } finally {
    savingPermissions.value = false
  }
}

async function toggleResource(resource: string) {
  if (!can('role:assign_permission')) return
  const perms = permStore.catalogByResource[resource] ?? []
  const allAssigned = perms.every((p) => isAssigned(p.id))
  savingPermissions.value = true
  permError.value = ''
  try {
    if (allAssigned) {
      for (const p of perms.filter((p) => isAssigned(p.id))) {
        await store.removePermission(id.value, p.id)
      }
    } else {
      for (const p of perms.filter((p) => !isAssigned(p.id))) {
        await store.assignPermission(id.value, { permissionId: p.id })
      }
    }
  } catch (err: unknown) {
    permError.value = (err as Error).message
  } finally {
    savingPermissions.value = false
  }
}

function resourceAllAssigned(resource: string): boolean {
  return (permStore.catalogByResource[resource] ?? []).every((p) => isAssigned(p.id))
}

function resourceSomeAssigned(resource: string): boolean {
  return (permStore.catalogByResource[resource] ?? []).some((p) => isAssigned(p.id))
}
</script>

<template>
  <div>
    <!-- Header -->
    <div class="mb-6 flex items-center gap-3">
      <button @click="router.push({ name: 'roles' })"
        class="flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent text-muted-foreground transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <div class="flex-1">
        <div class="flex items-center gap-2">
          <h1 class="text-xl font-bold text-foreground">{{ store.current?.name }}</h1>
          <span v-if="store.current?.isSystem" class="inline-flex items-center rounded-md bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-600/20">
            {{ t('role.system') }}
          </span>
        </div>
        <p class="text-sm text-muted-foreground">{{ store.current?.code }}</p>
      </div>
      <button v-if="can('role:update') && store.current && !store.current.isSystem"
        @click="openEdit" class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
        {{ t('common.edit') }}
      </button>
    </div>

    <!-- Tabs -->
    <div class="border-b border-border mb-6">
      <nav class="-mb-px flex gap-4">
        <button v-for="tab in ['info', 'permissions']" :key="tab"
          @click="activeTab = tab as 'info' | 'permissions'"
          :class="['pb-3 text-sm font-medium transition-colors border-b-2', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']">
          {{ t(`role.tab.${tab}`) }}
          <span v-if="tab === 'permissions' && store.current" class="ml-1 text-xs text-muted-foreground">
            ({{ store.current.permissions.length }})
          </span>
        </button>
      </nav>
    </div>

    <!-- Tab: Info -->
    <div v-if="activeTab === 'info'" class="space-y-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('role.code') }}</p>
          <p class="text-sm font-semibold text-foreground font-mono">{{ store.current?.code }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('role.permissionCount') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current?.permissions.length ?? 0 }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('common.createdAt') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current ? new Date(store.current.createdAt).toLocaleDateString('tr-TR') : '-' }}</p>
        </div>
      </div>
      <div v-if="store.current?.description" class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('role.description') }}</p>
        <p class="text-sm text-foreground">{{ store.current.description }}</p>
      </div>
    </div>

    <!-- Tab: Permissions Matrix -->
    <div v-if="activeTab === 'permissions'">
      <!-- Search + status -->
      <div class="mb-4 flex items-center gap-3">
        <div class="relative flex-1 max-w-xs">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="permSearchQuery" type="search" :placeholder="t('permission.search')"
            class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div v-if="savingPermissions" class="flex items-center gap-1 text-xs text-muted-foreground">
          <svg class="animate-spin w-3.5 h-3.5" viewBox="0 0 24 24" fill="none">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          {{ t('common.saving') }}
        </div>
      </div>

      <p v-if="permError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ permError }}</p>

      <div v-if="permStore.catalogLoading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="bg-[--color-card] rounded-xl p-4 border border-border animate-pulse">
          <div class="h-4 w-24 bg-accent rounded mb-3" />
          <div class="flex gap-2 flex-wrap">
            <div v-for="j in 4" :key="j" class="h-6 w-20 bg-accent rounded" />
          </div>
        </div>
      </div>

      <div v-else class="space-y-2">
        <div v-for="(perms, resource) in filteredCatalog" :key="resource"
          class="bg-[--color-card] rounded-xl border border-border shadow-sm overflow-hidden">
          <!-- Resource header -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-border bg-accent/30">
            <div class="flex items-center gap-2">
              <input
                type="checkbox"
                :checked="resourceAllAssigned(resource)"
                :indeterminate="!resourceAllAssigned(resource) && resourceSomeAssigned(resource)"
                :disabled="!can('role:assign_permission') || store.current?.isSystem"
                @change="toggleResource(resource)"
                class="rounded border-border text-primary focus:ring-primary disabled:opacity-50"
              />
              <span class="text-sm font-semibold text-foreground capitalize">{{ resource }}</span>
              <span class="text-xs text-muted-foreground">({{ perms.filter((p) => isAssigned(p.id)).length }}/{{ perms.length }})</span>
            </div>
          </div>
          <!-- Permission checkboxes -->
          <div class="px-4 py-3 flex flex-wrap gap-2">
            <label
              v-for="perm in perms"
              :key="perm.id"
              class="flex items-center gap-1.5 cursor-pointer select-none"
              :class="(!can('role:assign_permission') || store.current?.isSystem) ? 'opacity-50 cursor-default' : ''"
            >
              <input
                type="checkbox"
                :checked="isAssigned(perm.id)"
                :disabled="!can('role:assign_permission') || store.current?.isSystem || savingPermissions"
                @change="togglePermission(perm)"
                class="rounded border-border text-primary focus:ring-primary disabled:opacity-50"
              />
              <span class="text-xs font-medium text-foreground">{{ perm.action }}</span>
            </label>
          </div>
        </div>

        <div v-if="Object.keys(filteredCatalog).length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('permission.noResults') }}
        </div>
      </div>
    </div>

    <!-- Edit modal -->
    <FormModal :open="showEdit" :title="t('role.edit')" :saving="store.saving" @submit="submitEdit" @close="showEdit = false">
      <div class="space-y-4">
        <p v-if="editErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ editErrors.general }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('role.name') }} *</label>
          <input v-model="editForm.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="editErrors.name ? 'border-red-400' : 'border-border'" />
          <p v-if="editErrors.name" class="mt-1 text-xs text-red-600">{{ editErrors.name }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('role.description') }}</label>
          <textarea v-model="editForm.description" rows="3"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
