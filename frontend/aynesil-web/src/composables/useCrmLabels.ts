import { ref } from 'vue'
import { useRefDataStore } from '@/stores/refdata.store'

export type CrmRefType = 'lead_source' | 'lead_status' | 'pipeline_stage' | 'activity_type'

/**
 * CRM referans veri kodlarını (sourceCode, statusCode, pipelineStageCode, activityTypeCode)
 * ref-data etiketlerine çevirir. Etiket bulunamazsa kodun kendisi döner.
 */
export function useCrmLabels() {
  const refData = useRefDataStore()
  const labels = ref<Record<string, Record<string, string>>>({})

  async function load(...types: CrmRefType[]) {
    await Promise.all(
      types.map(async (type) => {
        const values = await refData.getValues(type, false)
        labels.value[type] = Object.fromEntries(values.map((v) => [v.code, v.label]))
      }),
    )
  }

  function label(type: CrmRefType, code: string | null | undefined): string | null {
    if (!code) return null
    return labels.value[type]?.[code] ?? code
  }

  return { load, label }
}
