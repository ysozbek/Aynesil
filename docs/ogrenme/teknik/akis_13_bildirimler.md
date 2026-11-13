# Akış 13 (teknik) — Bildirimler

**Menü (seed):** `notifications` (`/notifications`, `notification:read`); alt `notifications-inbox`, `notifications-templates` (`notification_template:read`); `notifications-triggers` (`notification_trigger:manage`) **pasif** (V31).
**Router:** `/notifications` (pano), `/notifications/inbox[/:id]`, `/notifications/templates[/new|/:id/edit]`, `/notifications/preferences` (yalnızca `requiresAuth`). Portal: `/portal/notifications` (`portal:access`).

## Uçlar
- `NotificationsController` (`api/notifications`): `GET` (benim bildirimlerim), `unread-count`, `PATCH {id}/read`, `PATCH mark-all-read`, `GET/PUT preferences` (`notification:read`)
- `NotificationTemplatesController` (`api/notification-templates`): CRUD (`notification_template:*`)
- `NotificationTriggersController` (`api/notification-triggers`): `GET`, `PUT` (upsert), `DELETE` — **ekran yok**
- `GET api/portal/notifications` (veli)

## Mimari
- `INotificationService` (`Application/Common/Interfaces`), tek kayıt: `LoggingNotificationService` — bildirim göndermez, şablon kodu + alıcıyı loglar (CLAUDE.md "deliberately deferred").
- `SendAsync` çağıranlar yalnızca: `RegisterCommand` (şablon `user.email_verification`), `RequestPasswordResetCommand` (`user.password_reset`).
- Domain event altyapısı var (`DomainEventInterceptor`, `InProcessEventBus`) ama bildirim üreten bir handler bulunamadı. `NotificationTriggerConfig` yalnızca CRUD'da kullanılıyor.
- Tablolar: `core.notification`, `notification_delivery`, `notification_preference`, `notification_template(+_translation)`, `notification_trigger_config`, `notification_trigger_channel`, `outbox_event`.
- Demo: 7 örnek bildirim doğrudan SQL ile yazılır (README: "Bildirimi yalnızca sistem olayları üretiyor, oluşturma ucu yok").

## [doğrulanacak]
Şablon silme korumaları; tercih kategorilerinin kaynağı (`defaultCategory: 'Genel'`); güncel menüde "Tercihler" satırı olup olmadığı.
