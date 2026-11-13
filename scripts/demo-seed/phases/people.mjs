// Kullanıcılar, derslikler, eğitmenler, veliler, öğrenciler, programlar/kayıtlar.
import { step, info, idOf, rng, pick, trAscii, dayStr, fx } from '../lib.mjs'
import { POOLS } from '../data.mjs'
import { ensureRoles } from './identity.mjs'

export async function setupPeople(ctx) {
  const { c, code, t } = ctx
  const P = POOLS[code]
  await ensureRoles(ctx)
  const ids = ctx.ids

  // ── Derslikler: kampüs başına 3 (biri sanal) ──
  const rooms = await c.list('/scheduling/rooms')
  ids.rooms = []
  for (let ci = 0; ci < ctx.campuses.length; ci++) {
    const camp = ctx.campuses[ci]
    const names = ci === 0 ? P.rooms : P.rooms2
    for (let ri = 0; ri < 3; ri++) {
      const rc = `${code.slice(-1).toUpperCase()}${ci + 1}-R${ri + 1}`
      const virtual = ri === 2
      const found = rooms.find((r) => r.code === rc)
      const id = found?.id ?? await step(`derslik ${rc}`, async () => idOf(await c.post('/scheduling/rooms', {
        campusId: virtual ? null : camp.id, code: rc, name: virtual ? `Çevrimiçi Oda ${ci + 1}` : `${names[ri]} Odası`, capacity: virtual ? 20 : 4, isVirtual: virtual,
        roomTypeId: await ctx.ref('room_type', virtual ? 'online_room' : 'therapy_room'), meetingUrl: virtual ? `https://meet.demo.test/${code}-${ci + 1}` : null })))
      if (id) ids.rooms.push({ id, campusId: virtual ? null : camp.id, homeCampusId: camp.id, virtual, code: rc })
    }
  }

  // ── Eğitmenler ──
  const edList = await c.list('/educators')
  ids.educators = []
  for (let i = 0; i < P.educator.length; i++) {
    const [fn, ln, title, spec] = P.educator[i]
    const found = edList.find((e) => e.firstName === fn && e.lastName === ln)
    let id = found?.id
    if (!id) id = await step(`eğitmen ${fn}`, async () => idOf(await c.post('/educators', {
      firstName: fn, lastName: ln, titleId: await ctx.ref('educator_title', title), email: `${trAscii(fn)}.${trAscii(ln)}@${t.domain}`,
      phone: null, employmentType: i === 5 ? 'part_time' : 'full_time', hireDate: `202${1 + (i % 4)}-0${1 + i}-15`, primaryCampusId: ctx.campuses[i % 2].id })))
    if (id) {
      ids.educators.push({ id, name: `${fn} ${ln}`, campusId: ctx.campuses[i % 2].id, spec })
      if (!found) await step(`uzmanlık ${fn}`, async () => c.post(`/educators/${id}/specialties`, { specialtyId: await ctx.ref('specialty', spec) }))
    }
  }

  // ── Kullanıcılar (rol başına bir + 2 veli) ──
  const users = await c.list('/users')
  ids.users = {}
  for (const [key, u] of Object.entries(fx.users)) {
    const username = `${u.local}@${t.domain}`
    let user = users.find((x) => x.username === username)
    if (!user) {
      const id = await step(`kullanıcı ${username}`, async () => idOf(await c.post('/users', {
        username, fullName: u.fullName[code], email: username, phone: null, password: fx.password, preferredLocale: 'tr',
        primaryCampusId: ctx.campuses[0].id, activateImmediately: true })))
      user = id ? { id, username } : null
      if (id && key !== 'admin') await step(`rol ata ${username}`, () => c.post(`/users/${id}/roles`, { roleId: ctx.roles[u.role].id, campusId: null }))
    }
    if (user) ids.users[key] = user.id
  }
  // Eğitmen kullanıcısı ↔ eğitmen kaydı bağı (aynı kullanıcıyla tekrar bağlamak idempotent)
  if (ids.users.educator && ids.educators[0]) {
    await step('eğitmen ↔ kullanıcı bağı', () => c.put(`/educators/${ids.educators[0].id}/user-account`, { userId: ids.users.educator }))
  }

  // ── Veliler + öğrenciler ──
  const students = await c.list('/students')
  const guardians = await c.list('/guardians')
  const r = rng(code === 'demo_a' ? 11 : 29)
  ids.students = []; ids.guardians = []
  const statusActive = await ctx.ref('student_status', 'active')
  for (let i = 0; i < P.child.length; i++) {
    const [cn, g] = P.child[i]
    const sn = P.surname[i]
    const no = `${t.studentPrefix}-${String(2601 + i)}`
    const campus = ctx.campuses[i % 2]
    let st = students.find((s) => s.studentNo === no)
    let sid = st?.id
    if (!sid) sid = await step(`öğrenci ${no}`, async () => idOf(await c.post('/students', {
      firstName: cn, lastName: sn, studentNo: no, nationalId: null, birthDate: `20${15 + (i % 4)}-${String(1 + (i * 5) % 12).padStart(2, '0')}-${String(3 + (i * 7) % 24).padStart(2, '0')}`,
      gender: g === 'F' ? 'female' : 'male', primaryCampusId: campus.id, statusId: statusActive, leadId: null,
      notes: 'Demo veri — gerçek kişi değildir.' })))
    if (!sid) continue
    const entry = { id: sid, no, name: `${cn} ${sn}`, campusId: campus.id, guardianIds: [], idx: i }
    ids.students.push(entry)
    // veliler: anne her zaman, baba ~yarısında
    const gs = [[P.mother[i], 'mother', true]]
    if (i % 2 === 0) gs.push([P.father[i], 'father', false])
    for (const [gn, rel, primary] of gs) {
      const email = `${trAscii(gn)}.${trAscii(sn)}@veli.${t.domain}`
      let gd = guardians.find((x) => x.email === email)
      let gid = gd?.id
      if (!gid) {
        gid = await step(`veli ${gn} ${sn}`, async () => idOf(await c.post('/guardians', { firstName: gn, lastName: sn, nationalId: null, email, phone: null, occupation: null, addressLine: null })))
        if (gid) guardians.push({ id: gid, email })
        if (gid) await step(`veli bağı ${gn}`, async () => c.post(`/students/${sid}/guardians`, {
          guardianId: gid, relationshipId: await ctx.ref('guardian_relationship', rel), isPrimary: primary, hasCustody: true, portalAccess: false, financialResponsible: primary }))
      }
      if (gid) { entry.guardianIds.push(gid); ids.guardians.push({ id: gid, studentId: sid, email, name: `${gn} ${sn}` }) }
    }
  }
  // Öğrenciyi kampüse kaydet
  for (const s of ids.students) await step(`kampüs kaydı ${s.no}`, async () => {
    const d = await c.get(`/students/${s.id}`)
    if (!(d.campuses ?? d.campusEnrollments ?? []).length) await c.post(`/students/${s.id}/campuses`, { campusId: s.campusId, isPrimary: true, activeFrom: '2026-01-05' })
  })

  // ── Portal veli kullanıcıları: ilk iki veli kaydı ↔ kullanıcı ──
  const portals = [['guardian1', [0, 1]], ['guardian2', [2]]]   // veli1: kardeş iki öğrenci (kamera yetkili); veli2: tek öğrenci
  ids.portal = {}
  for (const [key, studIdx] of portals) {
    const uid = ids.users[key]; if (!uid) continue
    const first = ids.students[studIdx[0]]
    const gid = first?.guardianIds[0]; if (!gid) continue
    await step(`veli ↔ kullanıcı bağı ${key}`, () => c.put(`/guardians/${gid}/user-account`, { userId: uid }))
    ids.portal[key] = { userId: uid, guardianId: gid, studentIds: [] }
    for (const si of studIdx) {
      const s = ids.students[si]; if (!s) continue
      if (si === 1) { // kardeş: aynı veli kaydı ikinci öğrenciye de bağlı olmalı
        await step(`kardeş bağı ${s.no}`, async () => {
          const links = (await c.get(`/students/${s.id}`)).guardians ?? []
          if (!links.some((l) => (l.guardianId ?? l.id) === gid))
            await c.post(`/students/${s.id}/guardians`, { guardianId: gid, relationshipId: await ctx.ref('guardian_relationship', 'mother'), isPrimary: true, hasCustody: true, portalAccess: true, financialResponsible: true })
        })
      }
      await step(`portal erişimi ${s.no}`, async () => c.post(`/guardians/${gid}/students/${s.id}/portal-access`, {
        guardianId: gid, studentId: s.id, canViewSessions: true, canViewAttendance: true, canViewReports: true, canViewPlan: true, canViewFinance: true, canViewCamera: key === 'guardian1' }).catch((e) => { if (!/exist|already|zaten|duplicate|unique/i.test(e.message)) throw e }))
      ids.portal[key].studentIds.push(s.id)
    }
  }
  info(`${ids.rooms.length} derslik, ${ids.educators.length} eğitmen, ${ids.students.length} öğrenci, ${ids.guardians.length} veli`)
}
