// Her çalıştırmada: referans değer haritası, kampüs listesi, roller (+izinler).
import { step, info, idOf } from '../lib.mjs'

const ROLE_RULES = {
  coordinator: {
    name: 'Koordinatör', desc: 'Demo: operasyon koordinatörü',
    allow: (c) => !/^(user|role|corporation|settings|integration|audit|menu|ref_data|refdata|finance|invoice|refund|credit_ledger|package_definition|student_package|scholarship|promotion|discount|report|kpi_snapshot|kpi:manage|permission)/.test(c),
  },
  educator: {
    name: 'Eğitmen', desc: 'Demo: eğitmen',
    allow: (c) => /^(student:read|guardian:read|session:read|session_note:|attendance:|student_goal:|goal_progress:|goal_template:read|goal_library:read|education_plan:(read|add_review|approve)|educator:read|room:read|campus:read|meeting:read|leave_request:(submit|read|cancel)|leave_balance:read|kpi_dashboard:read|case_note:(create|read|update)|assessment_session:read|makeup_request:(read|request)|camp:read|camp_attendance:|camp_participation:|notification:read|parent_feedback:read|enrollment:read|program:read)/.test(c),
  },
  finance: {
    name: 'Muhasebe', desc: 'Demo: muhasebe / finans',
    allow: (c) => /^(finance|invoice|refund|credit_ledger|package_definition|student_package|scholarship|promotion|discount|finance_payment|finance_report|student:read|guardian:read|campus:read|enrollment:read|program:read|report:|notification:read)/.test(c),
  },
  guardian: {
    name: 'Veli (portal)', desc: 'Demo: veli portalı',
    allow: (c) => /^(portal:access|parent_feedback:(submit|create)|survey:respond|notification:read)/.test(c),
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
      const have = new Set((await c.list(`/roles/${role.id}/permissions`)).map((p) => p.permissionId ?? p.id))
      const ids = perms.filter((p) => rule.allow(p.code) && !have.has(p.id)).map((p) => p.id)
      if (!ids.length) return
      for (let i = 0; i < ids.length; i += 10)
        await Promise.all(ids.slice(i, i + 10).map((permissionId) => c.post(`/roles/${role.id}/permissions`, { permissionId })))
      info(`rol ${code}: ${ids.length} izin`)
    })
  }
}
