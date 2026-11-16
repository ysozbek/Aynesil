# Akış 11 (teknik) — Anketler ve veli geri bildirimi

Modül anahtarı `surveys` (`Modules.Surveys`, `[RequireModule]`, rotalarda `meta.module`, menüde `feature_flag`).

## Ön yüz
`services/survey.service.ts`, `stores/survey.store.ts`, `types/survey.types.ts`, `views/surveys/{SurveyListView,SurveyFormView,SurveyResultsView,ParentFeedbackListView}.vue`, portal: `views/portal/{PortalSurveysView,PortalFeedbackView}.vue`. Rotalar: `/surveys`, `/surveys/new`, `/surveys/feedback`, `/surveys/:id/edit`, `/surveys/:id/results`, `/portal/surveys`, `/portal/feedback` (hepsi `module: Modules.Surveys`; `tests/moduleGuard.test.mjs` `expected` listesinde `surveys`, `portal/surveys`, `portal/feedback`). Düğmeler `useModule(Modules.Surveys).visible(izin)`. Menü V61: `students-surveys` (`/surveys`, `survey:read`), `students-feedback` (`/surveys/feedback`, `parent_feedback:read`), portalda `portal-surveys` (`survey:respond`), `portal-feedback` (`parent_feedback:create`).

## Uçlar (`SurveysController`, `api/surveys`, 12 uç)
| Uç | İzin |
|---|---|
| `GET /` · `GET /{id}` · `POST /` · `PUT /{id}` · `DELETE /{id}` | `survey:read|read|create|update|delete` |
| `GET /{id}/results` (soru bazında özet) | `survey:read` |
| `GET /pending` (velinin bekleyen anketleri) | `survey:respond` |
| `GET /responses` · `POST /responses` | `survey:read` · `survey:respond` |
| `GET /parent-feedback` · `GET /parent-feedback/mine` · `POST /parent-feedback` | `parent_feedback:read` · `:create` · `:create` |

KPI tarafında aynı iş için `api/performance-kpi/parent-feedback` (`parent_feedback:submit`) da `ops.parent_feedback` tablosuna yazar; anket modülü ucu portalda kullanılır.

## Kurallar
- **Tanım:** hedef `guardian|educator|student`; en az 1 soru; soru türü `text|rating|yes_no|multiple_choice|scale`; `multiple_choice` en az 2 seçenek (`Validation.ChoiceNeedsOptions`); başlık ≤500.
- **Düzenleme:** `PUT` gövdesindeki `questions` doluysa liste eşitlenir (kimlikli güncelle, kimliksiz ekle, eksik sil; seçenekler de aynı). Yanıt varsa 409 `Rule.SurveyQuestionsLocked`. Kimlik başka ankete aitse 422 `Validation.NotPartOfSurvey`. `questions` yoksa sorulara dokunulmaz. Tüm okuma/yazma `CorporationId` ile süzülür (RLS'e ek).
- **Silme:** yanıt yoksa soft-delete, varsa `Deactivate()`.
- **Yanıt:** veli kimliği oturumdan (`PortalGuardian.ResolveAsync`); çocuk `GuardianPortalAccess` (iptal edilmemiş) ile doğrulanır (`Rule.GuardianNoActiveAccess`); seans verilirse çocuklarından birinin katılımcı olduğu seans olmalı; hedef `guardian` değilse `Rule.SurveyTargetMismatch`; soru/seçenek ankete ait olmalı; zorunlu sorular dolu (`Validation.RequiredQuestionUnanswered`); `rating` 1–5; aynı soruya tek yanıt (`Validation.DuplicateAnswer`); aynı anket × veli × çocuk × seans için ikinci gönderim `Rule.SurveyAlreadySubmitted`.
- **Bekleyen:** aktif, silinmemiş, hedefi `guardian`, sorusu olan anketler × velinin erişimi açık çocukları − gönderilmiş yanıtı (seanssız) olan çiftler. Veli olmayan çağırana boş liste.
- **Geri bildirim:** `Rating` 1–5, puan/yorum'dan biri, yorum ≤2000. Eğitmen/seans kurum içinde var olmalı (404). Veli çağırıyorsa seans çocuklarından birinin seansı, eğitmen de o seanslarda görevli olmalı (403 `GuardianNoActiveAccess`). Yalnız seans verilirse `EducatorId` seansın tek eğitmeni ya da `lead` rolündeki eğitmen olur. Liste (`GET parent-feedback`) veli çağırırsa yalnızca kendi kayıtlarına daraltılır; `mine` veli olmayana boş döner. Listeler veli/eğitmen adını içerir.
- **Sonuçlar:** yalnızca `submitted_at` dolu yanıtlar. `rating`/ölçek (seçeneksiz): ortalama + 1–5 dağılım; `multiple_choice`/seçenekli ölçek: seçenek başına sayı (+ seçenek değerleri sayısalsa ortalama); `yes_no`: `yes/no` dağılımı; `text`: son 50 yazılı yanıt.

## KPI bağı
`ComputePerformanceSnapshotCommand` `parent_feedback_avg`'ı `ops.parent_feedback.rating` (eğitmen + dönem, puanlı kayıtlar) ortalamasından hesaplar; `survey_response` KPI'a girmez. Testler: `SurveyModuleTests.Guardian_feedback_ratings_feed_the_educator_kpi_parent_satisfaction`, smoke-api `kpi.parentFeedbackAvg`.

## Dosyalar / tablolar
`Application/Features/Surveys/{Commands/SurveyCommands.cs,Queries/SurveyQueries.cs,Dtos/SurveyDtos.cs}`; `Api/Controllers/SurveysController.cs`; DB: `ops.survey_definition`, `survey_question`, `survey_answer_option`, `survey_response`, `survey_question_response`, `parent_feedback`. Testler: `tests/Aynesil.Tests/TenantScoping/SurveyModuleTests.cs` (+ `PortalGuardianTests` anket kısmı). Demo: 2 anket (biri velide bekler), 3 yanıt (veli1 iki çocuk, veli2 bir çocuk), 2 veli geri bildirimi. Smoke: `surveyFlows`.
