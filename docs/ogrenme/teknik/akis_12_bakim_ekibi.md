# Akış 12 (teknik) — Bakım ekibi (ABAC) — **frontend yok**

Router/views/services/stores'ta bakım ekibi yok. Menü seed'inde `students-care-team` (`/students/care-team`, `care_team:assign`) var ama rota yok.

## Uçlar (`CareTeamController`, kök `api`)
`GET students/{id}/care-team`, `GET …/{assignmentId}`, `POST students/{id}/care-team` (`care_team:assign`), `PUT …/{assignmentId}`, `DELETE …/{assignmentId}`, `GET care-team/my-students` (`care_team:read`).

## Kurallar (Messages.resx)
- Rol/yetki türü ref değer olmalı: "{0} geçerli bir bakım ekibi rolü değil." / "… yetki türü değil." (ref türleri `care_team_role`: primary_therapist, secondary_therapist, coordinator, psychologist, consultant, observer, supervisor; `care_team_grant_type`: permanent, …)
- `emergency`/`delegated` yetki türü için `Reason` zorunlu: "'{0}' yetki türü için gerekçe zorunludur."
- "Bu öğrenci için zaten bir birincil bakım ekibi ataması var. …" (kısmi benzersiz indeks + komut kontrolü)
- "Atama zaten sonlandırılmış." · eşzamanlılık `rowVersion`.

## ABAC mekanizması (V15/V16, `docs/ABAC_CARE_TEAM_DESIGN.md`)
- Tablo `students.student_care_assignment`; `students.user_can_access_student()`; GUC `app.care_team_bypass` (JWT `perm` içinde `care_team:bypass` varsa `TenantConnectionInterceptor` set eder).
- Ayar `care_team_abac_enabled` (kurum bazlı, **varsayılan false** → yalnızca RBAC). Açıkken klinik tablolarda RESTRICTIVE RLS devrede.
- Kimlik köprüsü: `educators.educator.user_id → iam.user_account` (bağ kurma API'si yok; Akış 15).
- Kod: `Application/Common/CareTeam`, `Features/CareTeam/`, `src/Aynesil.Api/Controllers/CareTeamController.cs`.

## [doğrulanacak]
Hangi tabloların RESTRICTIVE politikaya bağlı olduğu (V16 içinden); demo'da ayarın açık olup olmadığı (README bakım ekibi anmıyor).
