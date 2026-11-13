# Akış 10 (teknik) — KPI ve raporlar

**Menü:** `kpi` (rota `/kpi`, etiket "KPI & Performans", V27); alt `kpi.definitions` (`kpi:read`), `kpi.snapshots` (`kpi_snapshot:read`). Eski `performance`/`reports` kök satırları (V8, `report:read`) ekransız; V37 eski performans menüsünü pasifleştirdi.
**Router:** `/kpi` (`KpiDashboardView`, `kpi_dashboard:read`), `/kpi/definitions[/:id]` (`kpi:read`), `/kpi/snapshots` (`kpi_snapshot:read`).

## Uçlar (`PerformanceKpiController`, `api/performance-kpi`)
`categories`, `definitions[/…/activate|deactivate]` (`kpi:read|manage`), `kpi-values`, `educators/{id}/kpi-values`, `snapshots` (`kpi_snapshot:read`), `educators/{id}/compute`, `compute/bulk` (`kpi:compute`), `parent-feedback` (GET/POST `parent_feedback:read|submit`), `dashboards/educator/{id}|manager|executive` (`kpi_dashboard:read`), `trends/educator/{id}|corporation`, `ranking`, `reports/kpi` (`kpi_report:read`).

## Hesaplama (`Features/PerformanceKpi/Commands/PerformanceSnapshotCommands.cs`)
Girdi: eğitmen + dönem. Çıktı: `ops.educator_performance_snapshot` (upsert) + `core.kpi_value` (kpi_id+özne+dönem upsert; kodlar `educator.session_volume`, `educator.attendance_rate`, `educator.goal_achievement`, `educator.parent_satisfaction`, …).
- Devam: (`present`+`late`) / dönemdeki tamamlanmış seansların yoklama kayıtları ×100
- Hedef başarı: ≥1 `session_goal` olan tamamlanmış seans / tamamlanmış seans ×100
- Memnuniyet: `parent_feedback.rating` ortalaması (dönem içinde, eğitmene ait)
- Kullanım: completed / (completed+cancelled+no_show+rescheduled) ×100
- Program tamamlama: `student_program` `completed` oranı
- Dönem tipi: monthly/quarterly/annual. `Validation.MustBeAfter` (bitiş > başlangıç).
`PerformanceSnapshotListView` toplu hesaplamayı store yerine doğrudan servisle çağırır (kod yorumu). Demo seed `compute/bulk` kullanır.

## Dosyalar
Vue: `views/kpi/*`, `views/DashboardView.vue` (29 satır, veri yok) · Store: `kpi.store.ts`, `performance.store.ts` · Servis: `kpi.service.ts` · Controller: `PerformanceKpiController.cs` · Application: `Features/PerformanceKpi/` (Commands, Queries/DashboardQueries, TrendAndRankingQueries, PerformanceReportQueries) · DB: `core.kpi_definition`, `core.kpi_value`, `ops.educator_performance_snapshot`, `ops.parent_feedback`

## Diğer pano/rapor uçları
CRM `leads/reports/conversion`; Goals `analytics/*`, plans `reports/*`; Finance `payments/reports/revenue|packages|credit-usage` (ekransız); Leave `reports/usage|trends`; Legal `reporting/*`; Consultancy `reporting/*`; Camps `performance`, `enrollment-summary`; scheduling `students/{id}/attendance/summary`.
## [doğrulanacak]
Finans panosunun hangi sayıları gösterdiği; `core.report_definition/report_run/report_schedule` tablolarını kullanan bir özellik var mı (şema'da var, kodda ekran görülmedi).
