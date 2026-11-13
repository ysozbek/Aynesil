# Akış 11 (teknik) — Anketler ve veli geri bildirimi — **frontend yok**

Router'da, `views/`'da, `services/`'de, `stores/`'da anket/geri bildirim yok. Menü seed'inde `students-surveys` (`/students/surveys`, `survey:read`) ve `students-feedback` (`/students/feedback`, `parent_feedback:read`) vardı; V31 ile pasifleştirildi.

## Uçlar (`SurveysController`, `api/surveys`)
`GET/POST/PUT/DELETE` anket tanımı (`survey:read|create|update|delete`), `GET responses`, `POST responses` (`survey:respond`), `GET/POST parent-feedback` (`parent_feedback:read|create`). KPI tarafında da aynı iş için `api/performance-kpi/parent-feedback` (`parent_feedback:submit`) var — iki ayrı uç aynı tabloya yazıyor olabilir [doğrulanacak].

## Kurallar
- Hedef: `guardian`, `educator`, `student` (`MustBeOneOf`). En az 1 soru; soru türü geçerli listeden olmalı (`Validation.InvalidQuestionType`); başlık ≤500 karakter.
- Cevap: `Validation.NotPartOfSurvey` ("Bu soru ya da seçenek yanıtlanan ankete ait değil."), `Rule.SurveyAlreadySubmitted` ("Anket yanıtı zaten gönderilmiş.").
- Geri bildirim: `Rating` 1–5 (`Validation.MustBeBetween`), `Rating` veya `Comment`'ten en az biri (`Validation.AtLeastOneOf`); alanlar `GuardianId`, `EducatorId`, `SessionId`, `StudentId`.

## Dosyalar / tablolar
`Application/Features/Surveys/{Commands/SurveyCommands.cs,Queries,Dtos}`; Controller: `SurveysController.cs`; DB: `ops.survey_definition`, `survey_question`, `survey_answer_option`, `survey_response`, `survey_question_response`, `parent_feedback`. Demo: 2 anket + yanıt + geri bildirim; demo veli rolü `survey:respond`, `parent_feedback:(submit|create)`.
