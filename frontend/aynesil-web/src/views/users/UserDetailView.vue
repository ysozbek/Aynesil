<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user.store'
import { useRoleStore } from '@/stores/role.store'
import { useBranchStore } from '@/stores/branch.store'
import { usePermission } from '@/composables/usePermission'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { UserRoleDto } from '@/types/user.types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const store = useUserStore()
const roleStore = useRoleStore()
const branchStore = useBranchStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const activeTab = ref<'info' | 'roles'>('info')

onMounted(async () => {
  await store.fetchOne(id.value)
  await store.fetchRoles(id.value)
  if (!roleStore.list.items.length) await roleStore.fetchList({ pageSize: 200 })
  if (!branchStore.list.items.length) await branchStore.fetchList({ pageSize: 200 })
})

// ── Edit info ─────────────────────────────────────────────────────────────────
const showEdit = ref(false)
const editForm = reactive({ fullName: '', email: '', phone: '', preferredLocale: 'tr', primaryCampusId: '' })
const editErrors = reactive<Record<string, string>>({})

function openEdit() {
  if (!store.current) return
  Object.assign(editForm, {
    fullName: store.current.fullName,
    email: store.current.email ?? '',
    phone: store.current.phone ?? '',
    preferredLocale: store.current.preferredLocale ?? 'tr',
    primaryCampusId: store.current.primaryCampusId ?? '',
  })
  Object.keys(editErrors).forEach((k) => delete editErrors[k])
  showEdit.value = true
}

async function submitEdit() {
  Object.keys(editErrors).forEach((k) => delete editErrors[k])
  if (!editForm.fullName.trim()) { editErrors.fullName = t('validation.required', { field: t('user.fullName') }); return }
  try {
    await store.update(id.value, {
      fullName: editForm.fullName,
      email: editForm.email || undefined,
      phone: editForm.phone || undefined,
      preferredLocale: editForm.preferredLocale || undefined,
      primaryCampusId: editForm.primaryCampusId || undefined,
      rowVersion: store.current!.rowVersion,
    })
    showEdit.value = false
  } catch (err: unknown) {
    editErrors.general = (err as Error).message
  }
}

// ── Status ─────────────────────────────────────────────────────────────────────
async function toggleStatus() {
  if (!store.current) return
  if (store.current.status === 'Active') await store.suspend(id.value)
  else await store.activate(id.value)
}

// ── Assign role ────────────────────────────────────────────────────────────────
const showAssignRole = ref(false)
const assignForm = reactive({ roleId: '', campusId: '', validFrom: '', validTo: '' })
const assignErrors = reactive<Record<string, string>>({})

function openAssignRole() {
  Object.assign(assignForm, { roleId: '', campusId: '', validFrom: '', validTo: '' })
  Object.keys(assignErrors).forEach((k) => delete assignErrors[k])
  showAssignRole.value = true
}

async function submitAssignRole() {
  Object.keys(assignErrors).forEach((k) => delete assignErrors[k])
  if (!assignForm.roleId) { assignErrors.roleId = t('validation.required', { field: t('role.title') }); return }
  try {
    await store.assignRole(id.value, {
      roleId: assignForm.roleId,
      campusId: assignForm.campusId || undefined,
      validFrom: assignForm.validFrom || undefined,
      validTo: assignForm.validTo || undefined,
    })
    showAssignRole.value = false
  } catch (err: unknown) {
    assignErrors.general = (err as Error).message
  }
}

// ── Remove role ────────────────────────────────────────────────────────────────
const removeRoleTarget = ref<UserRoleDto | null>(null)
const removeRoleLoading = ref(false)

async function doRemoveRole() {
  if (!removeRoleTarget.value) return
  removeRoleLoading.value = true
  try {
    await store.removeRole(id.value, removeRoleTarget.value.id)
    removeRoleTarget.value = null
  } finally {
    removeRoleLoading.value = false
  }
}

function formatDate(val?: string) {
  if (!val) return '-'
  return new Date(val).toLocaleDateString('tr-TR')
}

function getBranchName(campusId?: string) {
  if (!campusId) return '-'
  return branchStore.list.items.find((c) => c.id === campusId)?.name ?? campusId
}
</script>

<template>
  <div>
    <!-- Header -->
    <div class="mb-6 flex items-center gap-3">
      <button @click="router.push({ name: 'users' })"
        class="flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent text-muted-foreground transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <div class="flex-1" v-if="store.current">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-full bg-primary flex items-center justify-center text-primary-foreground font-bold text-sm shrink-0">
            {{ store.current.fullName.charAt(0).toUpperCase() }}
          </div>
          <div>
            <div class="flex items-center gap-2">
              <h1 class="text-xl font-bold text-foreground">{{ store.current.fullName }}</h1>
              <StatusBadge :value="store.current.status" />
            </div>
            <p class="text-sm text-muted-foreground">@{{ store.current.username }}</p>
          </div>
        </div>
      </div>
      <div class="flex items-center gap-2" v-if="store.current && can('user:update')">
        <button @click="openEdit" class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
          {{ t('common.edit') }}
        </button>
        <button @click="toggleStatus"
          :class="['px-3 py-2 text-sm rounded-lg border transition-colors', store.current.status === 'Active' ? 'border-amber-300 text-amber-700 hover:bg-amber-50' : 'border-emerald-300 text-emerald-700 hover:bg-emerald-50']">
          {{ store.current.status === 'Active' ? t('user.suspend') : t('user.activate') }}
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="border-b border-border mb-6">
      <nav class="-mb-px flex gap-4">
        <button v-for="tab in ['info', 'roles']" :key="tab"
          @click="activeTab = tab as 'info' | 'roles'"
          :class="['pb-3 text-sm font-medium transition-colors border-b-2', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']">
          {{ t(`user.tab.${tab}`) }}
        </button>
      </nav>
    </div>

    <!-- Tab: Info -->
    <div v-if="activeTab === 'info'">
      <div v-if="store.loading" class="grid grid-cols-2 gap-4 animate-pulse">
        <div v-for="i in 6" :key="i" class="bg-[--color-card] rounded-xl p-4 border border-border">
          <div class="h-3 w-20 bg-accent rounded mb-2" />
          <div class="h-4 w-36 bg-accent rounded" />
        </div>
      </div>
      <div v-else-if="store.current" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('user.email') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.email || '-' }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('user.phone') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.phone || '-' }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('user.locale') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.preferredLocale || 'tr' }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('user.primaryCampus') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ getBranchName(store.current.primaryCampusId) }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('user.lastLogin') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ formatDate(store.current.lastLoginAt) }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('common.createdAt') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ formatDate(store.current.createdAt) }}</p>
        </div>
      </div>
    </div>

    <!-- Tab: Roles -->
    <div v-if="activeTab === 'roles'">
      <div class="mb-3 flex items-center justify-between">
        <h2 class="text-sm font-semibold text-foreground">{{ t('user.assignedRoles') }}</h2>
        <button v-if="can('user:update')" @click="openAssignRole"
          class="flex items-center gap-1 px-3 py-1.5 bg-primary text-primary-foreground rounded-lg text-xs font-medium hover:opacity-90">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('user.assignRole') }}
        </button>
      </div>

      <div v-if="!store.currentRoles.length" class="text-center py-12 text-muted-foreground text-sm">
        {{ t('user.noRoles') }}
      </div>

      <div class="space-y-2">
        <div v-for="ur in store.currentRoles" :key="ur.id"
          class="flex items-center justify-between bg-[--color-card] rounded-xl border border-border px-4 py-3">
          <div>
            <p class="text-sm font-semibold text-foreground">{{ ur.roleName }}</p>
            <p class="text-xs text-muted-foreground">
              {{ ur.roleCode }}
              <span v-if="ur.campusId"> · {{ getBranchName(ur.campusId) }}</span>
              <span v-if="ur.validFrom"> · {{ formatDate(ur.validFrom) }} → {{ formatDate(ur.validTo) }}</span>
            </p>
          </div>
          <button v-if="can('user:update')" @click="removeRoleTarget = ur"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors" :title="t('user.removeRole')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Edit info modal -->
    <FormModal :open="showEdit" :title="t('user.edit')" :saving="store.saving" @submit="submitEdit" @close="showEdit = false">
      <div class="space-y-4">
        <p v-if="editErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ editErrors.general }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.fullName') }} *</label>
          <input v-model="editForm.fullName" type="text" class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="editErrors.fullName ? 'border-red-400' : 'border-border'" />
          <p v-if="editErrors.fullName" class="mt-1 text-xs text-red-600">{{ editErrors.fullName }}</p>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.email') }}</label>
            <input v-model="editForm.email" type="email" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.phone') }}</label>
            <input v-model="editForm.phone" type="tel" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.locale') }}</label>
            <select v-model="editForm.preferredLocale" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="tr">Türkçe</option>
              <option value="en">English</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.primaryCampus') }}</label>
            <select v-model="editForm.primaryCampusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.none') }}</option>
              <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Assign role modal -->
    <FormModal :open="showAssignRole" :title="t('user.assignRole')" @submit="submitAssignRole" @close="showAssignRole = false">
      <div class="space-y-4">
        <p v-if="assignErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ assignErrors.general }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('role.title') }} *</label>
          <select v-model="assignForm.roleId" class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="assignErrors.roleId ? 'border-red-400' : 'border-border'">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="r in roleStore.list.items" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
          <p v-if="assignErrors.roleId" class="mt-1 text-xs text-red-600">{{ assignErrors.roleId }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.title') }}</label>
          <select v-model="assignForm.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.allCampuses') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.validFrom') }}</label>
            <input v-model="assignForm.validFrom" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.validTo') }}</label>
            <input v-model="assignForm.validTo" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Remove role confirm -->
    <ConfirmModal :open="!!removeRoleTarget" :title="t('user.removeRoleTitle')"
      :message="t('user.removeRoleMessage', { role: removeRoleTarget?.roleName })"
      :confirm-label="t('common.delete')" :loading="removeRoleLoading" @confirm="doRemoveRole" @cancel="removeRoleTarget = null" />
  </div>
</template>
