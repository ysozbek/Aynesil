import { useRefLabels } from '@/composables/useRefLabels'

export type CrmRefType = 'lead_source' | 'lead_status' | 'pipeline_stage' | 'activity_type'

/**
 * CRM referans veri kodlarını (sourceCode, statusCode, pipelineStageCode, activityTypeCode)
 * ref-data etiketlerine çevirir. Genel mantık `useRefLabels` içindedir.
 */
export function useCrmLabels() {
  return useRefLabels<CrmRefType>()
}
