// Bildirim şablonları/tetikleyiciler (API) + örnek bildirim kutusu (SQL) + anketler ve yanıtlar (API, veli portalı kullanıcısıyla).
import { step, info, idOf, count, sqlStep, q, Client, fx } from '../lib.mjs'

export async function setupExtras(ctx) {
  const { c, code, t, ids } = ctx
  const pk = code.slice(-1).toLowerCase()
  // ── bildirim şablonları + tetikleyiciler ──
  if ((await count(c, '/notification-templates')) === 0) {
    const mk = (tc, typeCode, cat, subject, body) => step(`bildirim şablonu ${tc}`, async () => c.post('/notification-templates', { code: `${tc}_${pk}`, categoryId: await ctx.ref('notification_category', cat), typeId: await ctx.ref('notification_type', typeCode), translations: [{ locale: 'tr', subject, body }] }))
    const a = await mk('seans_hatirlatma', 'session_reminder', 'sessions', 'Yarınki seansınızı hatırlatırız', '{{studentName}} için seans {{startsAt}} tarihinde. (Demo)')
    await mk('paket_bitiyor', 'package_expiring', 'finance', 'Paketinizin süresi doluyor', '{{studentName}} paketi {{expiresOn}} tarihinde sona eriyor. (Demo)')
    await mk('toplanti_hatirlatma', 'meeting_reminder', 'meetings', 'Toplantı hatırlatması', '{{title}} toplantısı yaklaşıyor. (Demo)')
    if (a?.id) await step('bildirim tetikleyici', async () => c.put('/notification-triggers', { triggerCode: 'session_reminder', templateId: a.id, offsetMinutes: -1440, isActive: true, channelIds: [await ctx.ref('notification_channel', 'in_app'), await ctx.ref('notification_channel', 'email')] }))
  } else info('bildirim şablonları zaten var — atlandı')
  // ── örnek bildirim kutusu: API yalnızca dahili olarak üretir → SQL ──
  if (sql0(`select count(*) from core.notification n join core.corporation c on c.id=n.corporation_id where c.code=${q(code)}`) === '0') {
    const rows = [[ids.users.coordinator, 'Yeni aday kaydı', 'Web sitesinden yeni bir ön başvuru geldi.', 'sent'], [ids.users.coordinator, 'Gecikmiş takip görevi', '"Dil terapisi seans planını güncelle" görevi gecikti.', 'sent'],
      [ids.users.admin, 'Paket süresi doluyor', 'Bir öğrenci paketi 30 gün içinde sona erecek.', 'sent'], [ids.users.educator, 'Seans hatırlatması', 'Yarın 3 seansınız var.', 'sent'],
      [ids.users.guardian1, 'Seans notu eklendi', 'Çocuğunuzun son seansına bir not eklendi.', 'sent'], [ids.users.guardian1, 'Yeni BEP yayınlandı', 'Bireysel eğitim planı paylaşıldı.', 'read'],
      [ids.users.guardian2, 'Seans hatırlatması', 'Yarınki seansınızı hatırlatırız.', 'sent']]
    for (const [uid, subj, body, status] of rows) if (uid) sqlStep('core.notification (örnek bildirim kutusu; API bildirimi yalnızca sistem olayıyla üretir)',
      `insert into core.notification(corporation_id, recipient_user_id, subject, body, status, read_at) select id, ${q(uid)}, ${q(subj)}, ${q(body)}, ${q(status)}, ${status === 'read' ? 'now()' : 'null'} from core.corporation where code=${q(code)}`)
  }
  // ── anketler ──
  if ((await count(c, '/surveys')) === 0) {
    const sv = await step('anket', async () => c.post('/surveys', { title: 'Veli Memnuniyet Anketi (Demo)', target: 'guardian', typeId: await ctx.ref('survey_type', 'satisfaction_survey'), description: 'Dönem sonu memnuniyet ölçümü.',
      questions: [{ questionText: 'Genel memnuniyetiniz nedir?', questionType: 'rating', isRequired: true, sortOrder: 1, answerOptions: [] },
                  { questionText: 'Eğitmenle iletişimden memnun musunuz?', questionType: 'multiple_choice', isRequired: true, sortOrder: 2, answerOptions: [{ optionText: 'Evet', optionValue: 'yes', sortOrder: 1 }, { optionText: 'Kısmen', optionValue: 'partial', sortOrder: 2 }, { optionText: 'Hayır', optionValue: 'no', sortOrder: 3 }] },
                  { questionText: 'Önerileriniz', questionType: 'text', isRequired: false, sortOrder: 3, answerOptions: [] }] }))
    await step('anket 2', async () => c.post('/surveys', { title: 'Seans Geri Bildirimi (Demo)', target: 'guardian', typeId: await ctx.ref('survey_type', 'session_feedback'), description: null, questions: [{ questionText: 'Seans süresi yeterli miydi?', questionType: 'multiple_choice', isRequired: true, sortOrder: 1, answerOptions: [{ optionText: 'Evet', optionValue: 'yes', sortOrder: 1 }, { optionText: 'Hayır', optionValue: 'no', sortOrder: 2 }] }] }))
    // yanıtlar: veli portalı kullanıcısıyla
    const p1 = ids.portal?.guardian1
    if (sv?.id && p1) await step('anket yanıtı', async () => {
      const gc = new Client('veli1'); await gc.login(`${fx.users.guardian1.local}@${t.domain}`, fx.password)
      const full = await gc.get(`/surveys/${sv.id}`).catch(() => c.get(`/surveys/${sv.id}`))
      const qs = full.questions ?? []
      await gc.post('/surveys/responses', { surveyId: sv.id, guardianId: p1.guardianId, studentId: p1.studentIds[0], sessionId: null,
        answers: qs.map((q) => q.questionType === 'rating' ? { questionId: q.id, answerText: null, answerOptionId: null, numericValue: 5 } : q.questionType === 'multiple_choice' ? { questionId: q.id, answerText: null, answerOptionId: q.answerOptions?.[0]?.id ?? null, numericValue: null } : { questionId: q.id, answerText: 'Çok memnunuz, teşekkürler. (demo)', answerOptionId: null, numericValue: null }) })
    })
    await step('veli geri bildirimi', async () => {
      const gc = new Client('veli1'); await gc.login(`${fx.users.guardian1.local}@${t.domain}`, fx.password)
      await gc.post('/surveys/parent-feedback', { guardianId: p1.guardianId, educatorId: ids.educators[0].id, sessionId: null, rating: 5, comment: 'Eğitmenimiz çok ilgili, teşekkür ederiz. (demo)' })
    })
  } else info('anketler zaten var — atlandı')
  info('bildirim şablonu/tetikleyici, 7 örnek bildirim, 2 anket, yanıt ve geri bildirim')
}
function sql0(q) { return sqlRun(q) }
import { sql as sqlRun } from '../lib.mjs'
