# Akış 12 (teknik) — Bakım ekibi (ABAC)

## Ekranlar
- Öğrenci detayı → **Bakım Ekibi** sekmesi: `components/students/StudentCareTeamPanel.vue` (liste, ekle/düzenle `FormModal`, çıkar `ConfirmModal`; `StatusBadge entity="careAssignment"`; hata `(e as Error).message`). Sekme `useCareTeamModule().canRead` ile görünür.
- **Benim öğrencilerim:** `views/care-team/MyStudentsView.vue`, rota `care-team/my-students` (`care_team:read`). Menü satırı `students-care-team` (V59: rota `/care-team/my-students`, izin `care_team:read`, etiket "Benim Öğrencilerim"; V49 kapatmıştı).
- **Pano:** `views/students/StudentDashboardView.vue` "Atanmış eğitmenler": önce bakım ekibi; ekip boşsa `GET students/{id}/session-educators` ("seanslardan" etiketi) + "Ekibe ekle" (varsayılan rol = `is_default`/ilk `care_team_role`, başlangıç bugün, birincil değil). Önceki hâli kaynaksız bir "Yakında" yer tutucusuydu.
- **Tekrar eden plan formu:** `views/scheduling/RecurringScheduleFormView.vue`; öğrenci + sorumlu eğitimci seçiliyse ekip okunur, eğitimci ekipte değilse öneri ve "Ekibe ekle" (zorunlu değil).
- Servis/tip: `services/careTeam.service.ts`, `types/careTeam.types.ts`; durum `constants/statuses.ts` `CareAssignmentStatus`.
- **Modül açık mı?** Arayüz bayrağı doğrudan okumaz: `composables/useCareTeamModule.ts` menü ağacında (`/menus/me`, sunucuda bayrak + izinle süzülür) `students-care-team` satırının varlığına bakar.

## Uçlar (`CareTeamController`, kök `api`)
`GET students/{id}/care-team`, `GET …/{assignmentId}`, `POST students/{id}/care-team` (`care_team:assign`), `PUT …/{assignmentId}`, `DELETE …/{assignmentId}`, `GET care-team/my-students` (`care_team:read`). Ek uç (StudentsController, `student:read`): `GET students/{id}/session-educators` — öğrencinin iptal/gelmedi olmayan seanslarındaki eğitmenler (sayı + son seans), erişim vermez. Liste yanıtı (`CareTeamAssignmentListItemDto`) güncelle/kaldır için `rowVersion` ve `reason` da döner; `DELETE` gövdesi `{ rowVersion, reason }`.

## Kurallar (Messages.resx)
- Rol/yetki türü ref değer olmalı: "{0} geçerli bir bakım ekibi rolü değil." / "… yetki türü değil." (ref türleri `care_team_role`: primary_therapist, secondary_therapist, coordinator, psychologist, consultant, observer, supervisor; `care_team_grant_type`: permanent, …)
- `emergency`/`delegated` yetki türü için `Reason` zorunlu: "'{0}' yetki türü için gerekçe zorunludur."
- "Bu öğrenci için zaten bir birincil bakım ekibi ataması var. …" (kısmi benzersiz indeks + komut kontrolü)
- "Atama zaten sonlandırılmış." · eşzamanlılık `rowVersion`.

## ABAC mekanizması (V15/V16, `docs/ABAC_CARE_TEAM_DESIGN.md`)
- Tablo `students.student_care_assignment`; `students.user_can_access_student()`; GUC `app.care_team_bypass` (JWT `perm` içinde `care_team:bypass` varsa `TenantConnectionInterceptor` set eder).
- Ayar `care_team_abac_enabled` (kurum bazlı, **varsayılan false** → yalnızca RBAC). Açıkken klinik tablolarda RESTRICTIVE RLS devrede.
- Kimlik köprüsü: `educators.educator.user_id → iam.user_account` (bağlama ekranı Akış 15).
- **Kısıtı açma:** arayüz/API yok; platform sahip rolüyle `insert into core.setting_value(setting_key, scope_level, corporation_id, value) values ('care_team_abac_enabled','corporation','<kurum-id>','true'::jsonb)` (KURULUM_KILAVUZU B.1.3). Kısıt **klinik tablolardadır** (hedef, vaka notu, rapor, BEP, değerlendirme …); `students.student` listesi kısıtlı değildir.
- Testler: `CareTeamQueryTests` (birim: seanslardan eğitmenler, `rowVersion`), `CareTeamAccessTests` (entegrasyon: kısıt açıkken yalnızca ekipteki öğrencinin klinik kaydı görünür; 6 uç; kısıt kapalıyken gizlenmez), `CareTeamPreFilterTests`. smoke-api: ekle→listele→güncelle→bayat rowVersion→çıkar; demo-seed: ilk 3 öğrencide ekip.
- Kod: `Application/Common/CareTeam`, `Features/CareTeam/`, `src/Aynesil.Api/Controllers/CareTeamController.cs`.

## [doğrulanacak]
Hangi tabloların RESTRICTIVE politikaya bağlı olduğu V16'dan okundu (diagnosis, developmental_profile, medical_report, development_report, external_institution_report, case_note, student_guardian, assessment_*, student_goal, education_plan* , goal_progress …); tam liste için V16'ya bakın.
