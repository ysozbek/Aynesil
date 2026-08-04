<template>
  <div class="space-y-4">
    <div v-if="store.loading" class="flex justify-center py-8">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <template v-else>
      <!-- Documents list -->
      <div v-if="store.documentList.items.length" class="overflow-x-auto">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>{{ $t('portal.documents.name') }}</th>
              <th>{{ $t('portal.documents.purpose') }}</th>
              <th>{{ $t('portal.documents.size') }}</th>
              <th>{{ $t('common.createdAt') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="doc in store.documentList.items" :key="doc.fileId">
              <td>
                <div class="flex items-center gap-2">
                  <span>{{ fileIcon(doc.mimeType) }}</span>
                  <span class="truncate max-w-xs">{{ doc.originalName }}</span>
                </div>
              </td>
              <td>{{ doc.purpose ?? '-' }}</td>
              <td>{{ formatSize(doc.byteSize) }}</td>
              <td>{{ formatDate(doc.createdAt) }}</td>
            </tr>
          </tbody>
        </table>

        <div v-if="store.documentList.totalPages > 1" class="flex justify-center mt-4 gap-2">
          <button class="btn btn-sm" :disabled="!store.documentList.hasPreviousPage" @click="load(store.documentList.page - 1)">«</button>
          <span class="btn btn-sm btn-disabled">{{ store.documentList.page }} / {{ store.documentList.totalPages }}</span>
          <button class="btn btn-sm" :disabled="!store.documentList.hasNextPage" @click="load(store.documentList.page + 1)">»</button>
        </div>
      </div>

      <div v-else class="card bg-base-100 shadow">
        <div class="card-body items-center text-center py-10">
          <div class="text-4xl mb-2">📄</div>
          <p class="text-gray-500">{{ $t('portal.documents.noDocuments') }}</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}
function formatSize(bytes?: number): string {
  if (!bytes) return '-'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1048576).toFixed(1)} MB`
}
function fileIcon(mime?: string): string {
  if (!mime) return '📄'
  if (mime.includes('pdf')) return '📕'
  if (mime.includes('image')) return '🖼️'
  if (mime.includes('word') || mime.includes('doc')) return '📝'
  if (mime.includes('sheet') || mime.includes('excel')) return '📊'
  return '📄'
}

function load(page = 1) {
  store.fetchDocuments({ studentId: props.studentId, page, pageSize: 20 })
}

onMounted(() => load())
</script>
