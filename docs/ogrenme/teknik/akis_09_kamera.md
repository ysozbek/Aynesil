# Akış 9 (teknik) — Kamera

**Menü (seed V20):** `cameras` (`/cameras`, izin `camera:read`); alt: `camera-list` (`/cameras/list`), `camera-authorizations` (`viewing_authorization:read`), `camera-viewing-logs` (seed'de `/cameras/viewing-logs`; DB'de bugün `/cameras/viewing-history` — router ile uyumlu, `viewing_log:read`).
**Router:** `/cameras`, `cameras/list`, `cameras/new`, `cameras/authorizations`, `cameras/viewing-history`, `cameras/:id`, `cameras/:id/edit`. **`/cameras/:id/live` router'da yok** ve akış oynatıcısı bulunmuyor → `CameraDetailView`'daki "Canlı İzle" düğmesi (`/404`'e düşüyordu) kaldırıldı; nedeni şablonda yorum olarak duruyor. Oynatıcı yazılırsa `camera.store.ts` `startViewing`/`endViewing` (`POST cameras/viewing/start|{logId}/end`) oradan çağrılmalı.

## Durum
`ViewingAuthorizationState` (türetilmiş): active / expired / revoked (`isRevoked`, `isCurrentlyValid`). Kamera: `isActive`.

## Uçlar (`CameraController`, `api/cameras`)
- Kamera: `GET/POST/PUT/DELETE`, `PATCH {id}/active` (izin `camera:*`)
- Atama: `GET assignments`, `POST/DELETE {id}/rooms[/{roomId}]`, `POST/DELETE {id}/sessions[/{sessionId}]` (`camera_assignment:*`)
- Yetki: `GET/POST authorizations`, `POST authorizations/{id}/revoke` (`viewing_authorization:*`)
- İzleme: `POST viewing/start`, `POST viewing/{logId}/end` (`viewing_session:start|end`), `GET viewing-logs` (`viewing_log:read`). Ekranlar `startViewing/endViewing`'i çağırmıyor (yalnızca store/servis tanımlı).

## Kurallar
- `GrantViewingAuthorizationCommand`: veli için `GuardianPortalAccess.CanViewCamera` olmalı → `Rule.GuardianNoCameraAccess`; geçerli `student_consent` (kamera türü, `ValidUntil` boş ya da gelecekte) → `Rule.CameraConsentRequired`; `ValidTo > ValidFrom` (`Validation.MustBeAfter`); `ValidTo` zorunlu.
- `Rule.CameraConsentWithdrawn` ("Kamera izleme onayı geri çekilmiş. Erişim reddedildi.") izleme başlatılırken denetlenir [doğrulanacak: çağrı yeri].
- `Rule.ViewingAuthAlreadyRevoked`, `Rule.ViewingSessionEnded`; `Rule.CameraNotInRoom` / `Rule.CameraNotInSession` ("{0} kamerası {1} odasına/seansına atanmamış.").
- Her izleme `media.viewing_log`'a yazılır (bölümlü tablo; IP sütunu V29/V42 ile düzeltildi).

## Dosyalar
- Vue: `views/cameras/*` (`CameraDashboardView`, `CameraListView`, `CameraFormView`, `CameraDetailView`, `ViewingAuthorizationsView`, `ViewingHistoryView`) · Store: `camera.store.ts`, `cameraAccess.store.ts` · Servis: `camera.service.ts`
- Controller: `CameraController.cs` · Application: `Features/Cameras/Commands/ViewingCommands.cs` ve komşuları · Domain: `Domain/Modules/Media/Entities/`
- DB: `media.camera`, `room_camera`, `session_camera`, `viewing_authorization`, `viewing_log`; rıza: `legal.student_consent`; portal: `students.guardian_portal_access`

## Demo
2 kamera + derslik ataması + `veli1@demo-a.test` yetkisi (README). Kamera rızası: bir tanesi geri çekilmiş.
