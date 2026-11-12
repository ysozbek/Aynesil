import { ref } from 'vue'
import { useRefDataStore } from '@/stores/refdata.store'

/**
 * Referans veri (ref.ref_value) kodlarını ve kimliklerini etikete çevirir.
 * - `label(type, code)`: backend kod döndürüyorsa (ör. CRM `statusCode`)
 * - `labelById(type, id)`: backend ref_value kimliği döndürüyorsa (ör. `paymentMethodId`)
 * Etiket bulunamazsa kod döner; kimlik için bulunamazsa null döner.
 */
export function useRefLabels<T extends string = string>() {
  const refData = useRefDataStore()
  const byCode = ref<Record<string, Record<string, string>>>({})
  const byId = ref<Record<string, Record<string, string>>>({})

  async function load(...types: T[]) {
    await Promise.all(
      types.map(async (type) => {
        const values = await refData.getValues(type, false)
        byCode.value[type] = Object.fromEntries(values.map((v) => [v.code, v.label]))
        byId.value[type] = Object.fromEntries(values.map((v) => [v.id, v.label]))
      }),
    )
  }

  function label(type: T, code: string | null | undefined): string | null {
    if (!code) return null
    return byCode.value[type]?.[code] ?? code
  }

  function labelById(type: T, id: string | null | undefined): string | null {
    if (!id) return null
    return byId.value[type]?.[id] ?? null
  }

  return { load, label, labelById }
}
