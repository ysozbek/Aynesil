<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRefDataStore } from '@/stores/refdata.store'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const refDataStore = useRefDataStore()

const REF_TYPES = [
  { code: 'session_type',     label: 'Seans Tipleri' },
  { code: 'therapy_type',     label: 'Terapi Tipleri' },
  { code: 'program_type',     label: 'Program Tipleri' },
  { code: 'payment_method',   label: 'Ödeme Yöntemleri' },
  { code: 'student_status',   label: 'Öğrenci Statüleri' },
  { code: 'enrollment_status',label: 'Kayıt Statüleri' },
]

const selected = ref(REF_TYPES[0].code)
const values = ref<{ id: string; code: string; label: string }[]>([])
const loading = ref(false)

async function loadValues(typeCode: string) {
  loading.value = true
  selected.value = typeCode
  try {
    values.value = await refDataStore.getValues(typeCode)
  } finally {
    loading.value = false
  }
}

onMounted(() => loadValues(selected.value))
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">Referans Veri Yönetimi</h1>
      <p class="text-gray-500 mt-1">İş listelerini kod değişikliği yapmadan yapılandırın.</p>
    </div>

    <div class="flex gap-5">
      <!-- Left: type list -->
      <div class="w-56 shrink-0">
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div class="px-4 py-3 border-b border-gray-100 text-xs font-semibold text-gray-400 uppercase tracking-wider">
            Liste Kategorileri
          </div>
          <nav class="p-2">
            <button
              v-for="rt in REF_TYPES"
              :key="rt.code"
              @click="loadValues(rt.code)"
              class="w-full text-left px-3 py-2 rounded-lg text-sm transition-colors"
              :class="selected === rt.code
                ? 'bg-[--kt-primary,#4f46e5] text-white font-medium'
                : 'text-gray-600 hover:bg-gray-50'"
            >
              {{ rt.label }}
            </button>
          </nav>
        </div>
      </div>

      <!-- Right: values -->
      <div class="flex-1">
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div class="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
            <span class="text-sm font-medium text-gray-700">
              {{ REF_TYPES.find(r => r.code === selected)?.label }}
            </span>
            <button class="px-3 py-1.5 text-xs bg-[--kt-primary,#4f46e5] text-white rounded-lg hover:opacity-90 transition-opacity">
              + Değer Ekle
            </button>
          </div>

          <div v-if="loading" class="p-8 text-center text-gray-400 text-sm">
            Yükleniyor...
          </div>

          <div v-else-if="values.length === 0" class="p-8 text-center text-gray-400 text-sm">
            Bu kategoride henüz değer yok.
          </div>

          <table v-else class="w-full text-sm">
            <thead class="bg-gray-50 border-b border-gray-100">
              <tr>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Kod</th>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Etiket</th>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Durum</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-50">
              <tr v-for="v in values" :key="v.id" class="hover:bg-gray-50 transition-colors">
                <td class="px-6 py-3 font-mono text-xs text-gray-500">{{ v.code }}</td>
                <td class="px-6 py-3 text-gray-800">{{ v.label }}</td>
                <td class="px-6 py-3">
                  <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-700">
                    Aktif
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
