<template>
  <div class="p-6 space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ $t('notification.templates.title') }}</h1>
        <p class="text-sm text-gray-500">{{ $t('notification.templates.subtitle') }}</p>
      </div>
      <router-link :to="{ name: 'notification-template-new' }" class="btn btn-primary btn-sm">
        + {{ $t('notification.templates.create') }}
      </router-link>
    </div>

    <!-- Filters -->
    <div class="flex flex-wrap gap-3 items-end">
      <div>
        <label class="label label-text text-xs">{{ $t('common.status') }}</label>
        <select v-model="filters.isActive" class="select select-sm select-bordered" @change="load(1)">
          <option value="">{{ $t('common.allStatuses') }}</option>
          <option value="true">{{ $t('common.active') }}</option>
          <option value="false">{{ $t('common.passive') }}</option>
        </select>
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('common.search') }}</label>
        <input v-model="filters.search" type="text" class="input input-sm input-bordered" :placeholder="$t('common.search')" @keyup.enter="load(1)" />
      </div>
      <button class="btn btn-primary btn-sm mt-5" @click="load(1)">{{ $t('common.filter') }}</button>
    </div>

    <!-- Table -->
    <div class="card bg-base-100 shadow overflow-hidden">
      <div v-if="store.loading" class="flex justify-center py-10">
        <span class="loading loading-spinner text-primary"></span>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>{{ $t('notification.templates.code') }}</th>
              <th>{{ $t('notification.templates.category') }}</th>
              <th>{{ $t('notification.templates.type') }}</th>
              <th>{{ $t('common.status') }}</th>
              <th>{{ $t('common.updatedAt') }}</th>
              <th>{{ $t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!store.templateList.items.length">
              <td colspan="6" class="text-center py-8 text-gray-400">{{ $t('common.noData') }}</td>
            </tr>
            <tr v-for="tpl in store.templateList.items" :key="tpl.id">
              <td class="font-mono text-sm">{{ tpl.code }}</td>
              <td>{{ tpl.categoryCode ?? '-' }}</td>
              <td>{{ tpl.typeCode ?? '-' }}</td>
              <td>
                <span :class="['badge badge-sm', tpl.isActive ? 'badge-success' : 'badge-ghost']">
                  {{ tpl.isActive ? $t('common.active') : $t('common.passive') }}
                </span>
              </td>
              <td>{{ formatDate(tpl.updatedAt) }}</td>
              <td>
                <div class="flex gap-1">
                  <router-link :to="{ name: 'notification-template-edit', params: { id: tpl.id } }" class="btn btn-ghost btn-xs">
                    {{ $t('common.edit') }}
                  </router-link>
                  <button class="btn btn-ghost btn-xs text-error" @click="confirmDelete(tpl.id)">
                    {{ $t('common.delete') }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="store.templateList.totalPages > 1" class="flex justify-center gap-2">
      <button class="btn btn-sm" :disabled="!store.templateList.hasPreviousPage" @click="load(store.templateList.page - 1)">«</button>
      <span class="btn btn-sm btn-disabled">{{ store.templateList.page }} / {{ store.templateList.totalPages }}</span>
      <button class="btn btn-sm" :disabled="!store.templateList.hasNextPage" @click="load(store.templateList.page + 1)">»</button>
    </div>

    <!-- Delete confirm modal -->
    <dialog ref="deleteModal" class="modal">
      <div class="modal-box">
        <h3 class="font-bold text-lg">{{ $t('notification.templates.deleteTitle') }}</h3>
        <p class="py-4">{{ $t('notification.templates.deleteMessage') }}</p>
        <div class="modal-action">
          <button class="btn btn-ghost" @click="closeModal">{{ $t('common.cancel') }}</button>
          <button class="btn btn-error" :disabled="store.saving" @click="doDelete">{{ $t('common.delete') }}</button>
        </div>
      </div>
    </dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useNotificationTemplateStore } from '@/stores/notificationTemplate.store'

const store = useNotificationTemplateStore()
const deleteModal = ref<HTMLDialogElement | null>(null)
const deleteId = ref<string | null>(null)

const filters = ref({ isActive: '', search: '' })

function load(page = 1) {
  store.fetchTemplates({
    page,
    pageSize: 20,
    isActive: filters.value.isActive === '' ? undefined : filters.value.isActive === 'true',
    search: filters.value.search || undefined,
  })
}

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}

function confirmDelete(id: string) {
  deleteId.value = id
  deleteModal.value?.showModal()
}
function closeModal() {
  deleteModal.value?.close()
  deleteId.value = null
}
async function doDelete() {
  if (!deleteId.value) return
  await store.deleteTemplate(deleteId.value)
  closeModal()
  load(store.templateList.page)
}

onMounted(() => load())
</script>
