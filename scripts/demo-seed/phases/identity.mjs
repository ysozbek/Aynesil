// Her çalıştırmada: referans değer haritası, kampüs listesi, roller (+izinler).
import { step, info, idOf } from '../lib.mjs'

// Demo rolleri = KURULUM_KILAVUZU B.4'teki önerilen rol seti. Her çalıştırmada eşitlenir: kurala uyan izin eklenir,
// uymayan (eski sürümlerden kalan) izin kaldırılır.

// Koordinatör: günlük operasyonun tamamı; kurum yönetimi, finans ve raporlar hariç.
const COORDINATOR_EXCLUDED_GROUPS = /^(user|role|corporation|settings|integration|audit|menu|ref_data|refdata|finance|invoice|refund|credit_ledger|package_definition|student_package|scholarship|promotion|discount|report|kpi_snapshot|kpi:manage|permission)/
// Gruplar içinden yalnızca yöneticide kalanlar: başkasının seans/vaka notunu düzenleme-silme, ana kayıt silme,
// kurum yapısı (kampüs), bildirim tetikleyicileri. Veli tarafı izinleri (portal, geri bildirim verme, anket yanıtlama)
// personel rolüne ait değildir: koordinatöre portal menüsü açılıyordu.
const COORDINATOR_DENIED = new Set([
  'session_note:delete', 'case_note:delete',
  'student:delete', 'guardian:delete', 'educator:delete',
  'campus:create', 'campus:update', 'campus:delete',
  'notification_trigger:manage',
  'portal:access', 'parent_feedback:create', 'parent_feedback:submit', 'survey:respond',
])
// Gruplar dışından gerekenler: aday formu (atanan) ve değerlendirme oturumu formu (değerlendiren) kullanıcı listesini ister (salt okuma).
const COORDINATOR_EXTRA = new Set(['user:read'])

const ROLE_RULES = {
  coordinator: {
    name: 'Koordinatör', desc: 'Demo: operasyon koordinatörü',
    allow: (c) => COORDINATOR_EXTRA.has(c) || (!COORDINATOR_EXCLUDED_GROUPS.test(c) && !COORDINATOR_DENIED.has(c)),
  },
  educator: {
    name: 'Eğitmen', desc: 'Demo: eğitmen',
    // Seansını yürütür (yoklama, hedef, not, tamamlama); BEP'i okur ve gözden geçirme ekler, onaylamaz (koordinatör).
    // session_note:delete yok: başkalarının notunu düzenleme/silme yönetici yetkisidir. Hedef silemez.
    allow: (c) => /^(student:read|guardian:read|session:(read|complete|manage_goals)|session_note:write|attendance:(read|record)|student_goal:(read|create|update|change_status)|goal_progress:(read|record)|goal_template:read|goal_library:read|education_plan:(read|add_review)|educator:read|room:read|campus:read|meeting:read|leave_request:(submit|read|cancel)|leave_balance:read|kpi_dashboard:read|case_note:(create|read|update)|assessment_session:read|makeup_request:(read|request)|camp:read|camp_attendance:(read|record)|camp_participation:(read|record)|notification:read|parent_feedback:read|enrollment:read|program:read)$/.test(c),
  },
  finance: {
    name: 'Muhasebe', desc: 'Demo: muhasebe / finans',
    allow: (c) => /^(finance|invoice|refund|credit_ledger|package_definition|student_package|scholarship|promotion|discount|finance_payment|finance_report|student:read|guardian:read|campus:read|enrollment:read|program:read|report:|notification:read)/.test(c),
  },
  guardian: {
    name: 'Veli (portal)', desc: 'Demo: veli portalı',
    allow: (c) => /^(portal:access|parent_feedback:(submit|create)|survey:respond|notification:read)$/.test(c),
  },
}

export async function setupIdentity(ctx) {
  const { c } = ctx
  const cache = {}
  ctx.ref = async (type, code) => {
    cache[type] ||= Object.fromEntries((await c.get(`/reference-data/${type}?activeOnly=true`)).map((v) => [v.code, v.id]))
    if (!cache[type][code]) throw new Error(`ref bulunamadı: ${type}/${code}`)
    return cache[type][code]
  }
  ctx.refAll = async (type) => { await ctx.ref(type, Object.keys(cache[type] || {})[0] ?? '').catch(() => {}); return cache[type] || {} }
  ctx.campuses = (await c.list('/campuses')).sort((a, b) => a.code.localeCompare(b.code))
  ctx.roles = {}
  for (const r of await c.list('/roles')) ctx.roles[r.code] = r
}

export async function ensureRoles(ctx) {
  const { c } = ctx
  const perms = await c.list('/permissions')
  for (const [code, rule] of Object.entries(ROLE_RULES)) {
    await step(`rol ${code}`, async () => {
      let role = ctx.roles[code]
      if (!role) { const id = idOf(await c.post('/roles', { code, name: rule.name, description: rule.desc })); role = { id, code, permissionCount: 0 }; ctx.roles[code] = role }
      const current = await c.list(`/roles/${role.id}/permissions`)
      const have = new Set(current.map((p) => p.permissionId ?? p.id))
      const ids = perms.filter((p) => rule.allow(p.code) && !have.has(p.id)).map((p) => p.id)
      const extra = current.filter((p) => !rule.allow(p.code))
      for (let i = 0; i < ids.length; i += 10)
        await Promise.all(ids.slice(i, i + 10).map((permissionId) => c.post(`/roles/${role.id}/permissions`, { permissionId })))
      for (const p of extra) await c.call('DELETE', `/roles/${role.id}/permissions/${p.permissionId ?? p.id}`)
      if (ids.length || extra.length)
        info(`rol ${code}: +${ids.length} izin${extra.length ? `, −${extra.length} (${extra.map((p) => p.code).join(', ')})` : ''}`)
    })
  }
}
