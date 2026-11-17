import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import {
  periodPhase, canBulkComplete, periodHasStudents, blocksDeactivation, CampEducatorRoles,
  canRecordForEnrollment, canWriteReport, planBulkSave, attendanceRate, attendanceNeedsReason,
} from '../src/utils/campRules.ts'

const today = '2026-07-10'
const p = (startDate, endDate, enrolledCount = 0, waitlistCount = 0) => ({ startDate, endDate, enrolledCount, waitlistCount })

test('dönem evresi: başlamadı / devam ediyor / bitti (sınır günleri dahil)', () => {
  assert.equal(periodPhase(p('2026-07-11', '2026-07-20'), today), 'upcoming')
  assert.equal(periodPhase(p('2026-07-10', '2026-07-20'), today), 'ongoing')
  assert.equal(periodPhase(p('2026-07-01', '2026-07-10'), today), 'ongoing')
  assert.equal(periodPhase(p('2026-07-01', '2026-07-09'), today), 'ended')
})

test('toplu tamamlama yalnızca başlamış dönemde ve kayıtlı öğrenci varken açık', () => {
  assert.equal(canBulkComplete(p('2026-07-01', '2026-07-09', 3), today), true)
  assert.equal(canBulkComplete(p('2026-07-01', '2026-07-09', 0), today), false)
  assert.equal(canBulkComplete(p('2026-07-11', '2026-07-20', 3), today), false)
})

test('kayıtlı veya bekleyen öğrencisi olan dönem silinemez', () => {
  assert.equal(periodHasStudents(p('a', 'b', 0, 0)), false)
  assert.equal(periodHasStudents(p('a', 'b', 1, 0)), true)
  assert.equal(periodHasStudents(p('a', 'b', 0, 2)), true)
})

test('pasifleştirmeyi yalnızca bitmemiş dönemdeki kayıt/bekleme engeller', () => {
  assert.equal(blocksDeactivation([p('2026-06-01', '2026-06-15', 4)], today), false) // bitmiş dönem geçmişi
  assert.equal(blocksDeactivation([p('2026-07-20', '2026-07-30', 0, 1)], today), true)
  assert.equal(blocksDeactivation([p('2026-07-20', '2026-07-30')], today), false)
})

test('eğitmen rolleri sunucudaki rollerle aynı', () => {
  assert.deepEqual([...CampEducatorRoles], ['lead', 'assistant', 'observer', 'supervisor'])
})

// ── Kaynak denetimi: yeni kamp rotaları/ekranları modül ve izin kurallarına bağlı ───────────────
const router = readFileSync(new URL('../src/router/index.ts', import.meta.url), 'utf8')
test('tüm kamp rotaları meta.module = Modules.Camps taşır', () => {
  const block = router.slice(router.indexOf('// ── Camp Management'))
  const camps = [...block.matchAll(/path: '(camps[^']*)'[\s\S]*?meta: \{([^}]*)\}/g)]
  assert.ok(camps.length >= 6)
  for (const [, path, meta] of camps) assert.match(meta, /module: Modules\.Camps/, path)
})

for (const view of ['CampListView', 'CampDetailView', 'CampEnrollmentView']) {
  test(`${view}: eylemler useModule(...).visible(izin) ile korunur`, () => {
    const src = readFileSync(new URL(`../src/views/camps/${view}.vue`, import.meta.url), 'utf8')
    assert.match(src, /useModule\(Modules\.Camps\)/)
    assert.match(src, /visible\(Permissions\./)
  })
}

test('yoklama/katılım yalnızca kayıtlı öğrenci için; rapor yalnızca tamamlanmış kayıt için', () => {
  assert.equal(canRecordForEnrollment('enrolled'), true)
  for (const s of ['completed', 'withdrawn', 'waitlist']) assert.equal(canRecordForEnrollment(s), false)
  assert.equal(canWriteReport('completed'), true)
  for (const s of ['enrolled', 'withdrawn', 'waitlist']) assert.equal(canWriteReport(s), false)
})

test('toplu kayıt planı: yeni → create, durumu değişen → update, aynı/boş → atla', () => {
  const existing = new Map([
    ['a', { id: 'pa', status: 'present' }],
    ['b', { id: 'pb', status: 'absent' }],
  ])
  const plan = planBulkSave([
    { enrollmentId: 'a', status: 'present' }, // değişmedi
    { enrollmentId: 'b', status: 'excused' }, // düzeltme
    { enrollmentId: 'c', status: 'late' },    // yeni
    { enrollmentId: 'd', status: '' },        // boş
  ], existing)
  assert.deepEqual(plan.create, [{ enrollmentId: 'c', status: 'late', reasonId: undefined }])
  assert.deepEqual(plan.update, [{ id: 'pb', status: 'excused', reasonId: undefined }])
})

test('yoklama nedeni: yalnızca absent/excused için taşınır, değişince düzeltme olur, durum değişince temizlenir', () => {
  assert.equal(attendanceNeedsReason('absent'), true)
  assert.equal(attendanceNeedsReason('excused'), true)
  assert.equal(attendanceNeedsReason('present'), false)
  const existing = new Map([
    ['a', { id: 'pa', status: 'absent', reasonId: 'r1' }],
    ['b', { id: 'pb', status: 'absent', reasonId: 'r1' }],
    ['c', { id: 'pc', status: 'absent', reasonId: 'r1' }],
  ])
  const plan = planBulkSave([
    { enrollmentId: 'a', status: 'absent', reasonId: 'r1' }, // değişmedi
    { enrollmentId: 'b', status: 'absent', reasonId: 'r2' }, // yalnız neden değişti
    { enrollmentId: 'c', status: 'present', reasonId: 'r1' }, // devam → neden temizlenir
    { enrollmentId: 'd', status: 'excused', reasonId: 'r1' }, // yeni, nedenli
    { enrollmentId: 'e', status: 'late', reasonId: 'r1' },    // yeni, neden anlamsız
  ], existing)
  assert.deepEqual(plan.update, [
    { id: 'pb', status: 'absent', reasonId: 'r2' },
    { id: 'pc', status: 'present', reasonId: undefined },
  ])
  assert.deepEqual(plan.create, [
    { enrollmentId: 'd', status: 'excused', reasonId: 'r1' },
    { enrollmentId: 'e', status: 'late', reasonId: undefined },
  ])
})

test('devam oranı 1 ondalığa yuvarlanır, gün yoksa 0', () => {
  assert.equal(attendanceRate(2, 3), 66.7)
  assert.equal(attendanceRate(0, 0), 0)
  assert.equal(attendanceRate(5, 5), 100)
})

test('tekil tamamlama yalnızca başlamış dönemde (D4); dönem bilgisi yoksa kapalı', async () => {
  const { canCompleteEnrollment } = await import('../src/utils/campRules.ts')
  assert.equal(canCompleteEnrollment({ startDate: '2026-07-11' }, today), false)
  assert.equal(canCompleteEnrollment({ startDate: '2026-07-10' }, today), true)
  assert.equal(canCompleteEnrollment({ startDate: '2026-07-01' }, today), true)
  assert.equal(canCompleteEnrollment(null, today), false)
})

test('yeniden kayıt: istenen enrolled, dönen waitlist ise kullanıcıya bildirilir (D3)', async () => {
  const { fellToWaitlist } = await import('../src/utils/campRules.ts')
  assert.equal(fellToWaitlist('enrolled', { status: 'waitlist' }), true)
  assert.equal(fellToWaitlist('enrolled', { status: 'enrolled' }), false)
  assert.equal(fellToWaitlist('waitlist', { status: 'waitlist' }), false)
  assert.equal(fellToWaitlist('enrolled', undefined), false)
})

test('kayıt ekranı: tamamla düğmesi dönem başlamadan gizli, çekilmiş satırda yeniden kaydet', () => {
  const view = readFileSync(new URL('../src/views/camps/CampEnrollmentView.vue', import.meta.url), 'utf8')
  assert.match(view, /CampEnrollmentStatus\.Enrolled && completeEnabled && visible\(Permissions\.CampEnrollments\.Complete\)/)
  assert.match(view, /CampEnrollmentStatus\.Withdrawn && visible\(Permissions\.CampEnrollments\.Enroll\)/)
})
