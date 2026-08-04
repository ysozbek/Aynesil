<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { usePackageStore } from '@/stores/package.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = usePackageStore()
const refData = useRefDataStore()

const packageId = route.params.id as string | undefined
const isEdit = !!packageId
const corporationId = computed(() => auth.user?.corporationId ?? '')

const packageTypes = ref<RefValueItem[]>([])

const form = reactive({
  code: '',
  name: '',
  packageTypeId: '',
  programId: '',
  totalCredits: 10,
  validityDays: 365,
  listPrice: 0,
  currency: 'TRY',
  description: '',
})

const errors = reactive<Record<string, string>>({})

onMounted(async () => {
  await refData.getValues('PACKAGE_TYPE').then(v => { packageTypes.value = v })
  if (isEdit && packageId) {
    await store.fetchDefinition(packageId)
    const d = store.currentDefinition
    if (d) {
      form.code = d.code
      form.name = d.name
      form.packageTypeId = d.packageTypeId
      form.programId = d.programId ?? ''
      form.totalCredits = d.totalCredits
      form.validityDays = d.validityDays
      form.listPrice = d.listPrice
      form.currency = d.currency
      form.description = d.description ?? ''
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete (errors as Record<string, string>)[k])
  if (!form.name.trim()) errors.name = t('validation.required', { field: t('finance.package.name') })
  if (!form.packageTypeId) errors.packageTypeId = t('validation.required', { field: t('finance.package.type') })
  if (form.totalCredits < 1) errors.totalCredits = t('validation.numeric', { field: t('finance.package.totalCredits') })
  if (form.listPrice < 0) errors.listPrice = t('validation.numeric', { field: t('finance.package.listPrice') })
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    if (isEdit && packageId) {
      await store.updateDefinition(packageId, {
        name: form.name,
        packageTypeId: form.packageTypeId,
        programId: form.programId || undefined,
        totalCredits: form.totalCredits,
        validityDays: form.validityDays,
        listPrice: form.listPrice,
        currency: form.currency,
        description: form.description || undefined,
        rowVersion: store.currentDefinition!.rowVersion,
      })
      router.push({ name: 'package-detail', params: { id: packageId } })
    } else {
      const created = await store.createDefinition({
        corporationId: corporationId.value,
        code: form.code,
        name: form.name,
        packageTypeId: form.packageTypeId,
        programId: form.programId || undefined,
        totalCredits: form.totalCredits,
        validityDays: form.validityDays,
        listPrice: form.listPrice,
        currency: form.currency,
        description: form.description || undefined,
      })
      router.push({ name: 'package-detail', params: { id: created.id } })
    }
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="isEdit ? t('finance.package.edit') : t('finance.package.create')"
      :description="isEdit ? t('finance.package.editDescription') : t('finance.package.createDescription')"
    />

    <div class="max-w-2xl">
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-5">

        <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
          {{ errors.submit }}
        </div>

        <div v-if="!isEdit">
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.code') }} <span class="text-red-500">*</span></label>
          <input v-model="form.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.name') }} <span class="text-red-500">*</span></label>
          <input v-model="form.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.name ? 'border-red-400' : 'border-border'" />
          <p v-if="errors.name" class="text-xs text-red-500 mt-1">{{ errors.name }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.type') }} <span class="text-red-500">*</span></label>
          <select v-model="form.packageTypeId" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.packageTypeId ? 'border-red-400' : 'border-border'">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="pt in packageTypes" :key="pt.id" :value="pt.id">{{ pt.label }}</option>
          </select>
          <p v-if="errors.packageTypeId" class="text-xs text-red-500 mt-1">{{ errors.packageTypeId }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.totalCredits') }}</label>
            <input v-model.number="form.totalCredits" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.validityDays') }}</label>
            <input v-model.number="form.validityDays" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.listPrice') }}</label>
            <input v-model.number="form.listPrice" type="number" min="0" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.currency') }}</label>
            <select v-model="form.currency" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="TRY">TRY</option>
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.descriptionField') }}</label>
          <textarea v-model="form.description" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button @click="router.back()" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="submit" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
