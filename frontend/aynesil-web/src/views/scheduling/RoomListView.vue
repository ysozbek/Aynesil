<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useSchedulingStore } from '@/stores/scheduling.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { RoomListItemDto } from '@/types/scheduling.types'

const { t } = useI18n()
const auth = useAuthStore()
const store = useSchedulingStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
  campusId: '',
  isActive: '' as '' | 'true' | 'false',
  page: 1,
  pageSize: 20,
  search: '',
})

const showForm = ref(false)
const editTarget = ref<RoomListItemDto | null>(null)
const formData = reactive({ code: '', name: '', campusId: '', capacity: 10, isVirtual: false })
const formErrors = reactive<Record<string, string>>({})

watch(() => [query.campusId, query.isActive, query.page], () => loadList())

onMounted(() => loadList())

async function loadList() {
  await store.fetchRooms({
    ...query,
    corporationId: corporationId.value,
    campusId: query.campusId || undefined,
    isActive: query.isActive === 'true' ? true : query.isActive === 'false' ? false : undefined,
  })
}

const columns: Column<RoomListItemDto>[] = [
  { key: 'code', label: t('scheduling.room.code'), width: '80px' },
  { key: 'name', label: t('scheduling.room.name') },
  { key: 'campusName', label: t('campus.name'), width: '140px' },
  { key: 'capacity', label: t('scheduling.room.capacity'), width: '90px', align: 'center' },
  { key: 'isVirtual', label: t('scheduling.room.isVirtual'), width: '90px', align: 'center' },
  { key: 'isActive', label: t('common.status'), width: '90px' },
]

function openCreate() {
  editTarget.value = null
  Object.assign(formData, { code: '', name: '', campusId: '', capacity: 10, isVirtual: false })
  showForm.value = true
}

function validateForm(): boolean {
  Object.keys(formErrors).forEach(k => delete (formErrors as Record<string, string>)[k])
  if (!formData.code.trim()) formErrors.code = t('validation.required', { field: t('scheduling.room.code') })
  if (!formData.name.trim()) formErrors.name = t('validation.required', { field: t('scheduling.room.name') })
  return Object.keys(formErrors).length === 0
}

async function submitForm() {
  if (!validateForm()) return
  try {
    if (editTarget.value) {
      await store.updateRoom(editTarget.value.id, {
        name: formData.name,
        capacity: formData.capacity,
        isVirtual: formData.isVirtual,
        rowVersion: store.currentRoom?.rowVersion ?? '',
      })
    } else {
      await store.createRoom({
        corporationId: corporationId.value,
        campusId: formData.campusId,
        code: formData.code,
        name: formData.name,
        capacity: formData.capacity,
        isVirtual: formData.isVirtual,
      })
    }
    showForm.value = false
    await loadList()
  } catch (e: unknown) {
    formErrors.submit = (e as Error).message
  }
}

const deleteTarget = ref<RoomListItemDto | null>(null)
const deleteLoading = ref(false)

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteRoom(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.room.title')" :description="t('scheduling.room.description')">
      <button
        v-if="can('room:create')"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('scheduling.room.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3">
      <select v-model="query.isActive" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="true">{{ t('common.active') }}</option>
        <option value="false">{{ t('common.passive') }}</option>
      </select>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.roomList.items"
      :loading="store.loading"
    >
      <template #cell-isVirtual="{ value }">
        <span v-if="value" class="px-1.5 py-0.5 rounded text-xs bg-violet-100 text-violet-700">{{ t('scheduling.room.virtual') }}</span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('room:update')"
            @click="store.fetchRoom(row.id).then(() => { editTarget = row; Object.assign(formData, { code: row.code, name: row.name, campusId: row.campusId, capacity: row.capacity, isVirtual: row.isVirtual }); showForm = true })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('room:update') && row.isActive"
            @click="store.deactivateRoom(row.id).then(() => loadList())"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-amber-600"
            :title="t('common.deactivate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
          </button>
          <button
            v-if="can('room:delete')"
            @click="deleteTarget = row"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
            :title="t('common.delete')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.roomList.page"
        :page-size="store.roomList.pageSize"
        :total-count="store.roomList.totalCount"
        :total-pages="store.roomList.totalPages"
        :has-previous-page="store.roomList.hasPreviousPage"
        :has-next-page="store.roomList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-4">{{ editTarget ? t('scheduling.room.edit') : t('scheduling.room.create') }}</h3>

        <div v-if="formErrors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700 mb-4">
          {{ formErrors.submit }}
        </div>

        <div class="space-y-4">
          <div v-if="!editTarget">
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.room.code') }} *</label>
            <input v-model="formData.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.room.name') }} *</label>
            <input v-model="formData.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.room.capacity') }}</label>
            <input v-model.number="formData.capacity" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <label class="flex items-center gap-2 text-sm">
            <input type="checkbox" v-model="formData.isVirtual" class="rounded" />
            {{ t('scheduling.room.isVirtual') }}
          </label>
        </div>

        <div class="flex justify-end gap-2 mt-5">
          <button @click="showForm = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="submitForm" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('scheduling.room.deleteTitle')"
      :message="t('scheduling.room.deleteMessage', { name: deleteTarget?.name })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
