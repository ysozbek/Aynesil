import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import {
  periodPhase, canBulkComplete, periodHasStudents, blocksDeactivation, CampEducatorRoles,
  canRecordForEnrollment, canWriteReport, planBulkSave, attendanceRate,
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
  assert.deepEqual(plan.create, [{ enrollmentId: 'c', status: 'late' }])
  assert.deepEqual(plan.update, [{ id: 'pb', status: 'excused' }])
})

test('devam oranı 1 ondalığa yuvarlanır, gün yoksa 0', () => {
  assert.equal(attendanceRate(2, 3), 66.7)
  assert.equal(attendanceRate(0, 0), 0)
  assert.equal(attendanceRate(5, 5), 100)
})
