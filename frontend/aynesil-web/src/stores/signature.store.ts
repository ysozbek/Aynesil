/**
 * Signature tracking store.
 * Wraps signature-related state for the Legal module.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { legalService } from '@/services/legal.service'
import type { SignatureReportItemDto } from '@/types/legal.types'

export const useSignatureStore = defineStore('signature', () => {
  const signatureReport = ref<SignatureReportItemDto[]>([])
  const pendingSignatures = ref<SignatureReportItemDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchSignatureReport(query: { corporationId?: string } & Record<string, unknown>) {
    loading.value = true
    error.value = null
    try {
      const res = await legalService.getSignatureReport(query)
      if (res.success && res.data) {
        signatureReport.value = res.data
        pendingSignatures.value = res.data.filter(r => r.status === 'Sent')
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  return { signatureReport, pendingSignatures, loading, error, fetchSignatureReport }
})
