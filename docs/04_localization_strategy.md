# 04 — Localization Strategy

## Goals

- Per-**user** language, per-**corporation** default language.
- A single, consistent translation mechanism (no ad-hoc `name_tr` / `name_en` columns).
- Deterministic **fallback** so the UI never shows a blank label.
- Adding a new language = inserting one `ref.locale` row + translations (no schema change).

## Locale model

`ref.locale` stores BCP-47 codes (`tr`, `en`, `en-US`, …) with display names and direction. It is **system reference data** (shared, not tenant-scoped).

Language preference resolution order (most specific wins):

```
1. explicit request parameter (e.g. ?lang=en)         -- per request
2. user_account.preferred_locale                       -- per user
3. corporation.default_locale                           -- per tenant   (Akran => 'tr')
4. platform default locale                              -- system setting (=> 'tr')
```

## What gets translated, and how

| Content kind | Mechanism | Example |
|---|---|---|
| Reference values (the big one) | `ref.ref_value_translation` | "Bireysel" / "Individual" |
| First-class entity content | per-entity `*_translation` table | `program_translation`, `goal_template_translation`, `assessment_template_translation`, `contract_template_translation`, `consent_template_translation`, `notification_template_translation`, `menu_item_translation` |
| Static UI / system strings | `ref.i18n_message(namespace, msg_key, locale, value)` | button labels, validation messages, email subjects |
| User-authored free text (notes, reports) | **not translated** — stored in the author's language | a therapist's session note |

Rationale: translatable, *curated* content (catalogs, templates, menus) uses **structured per-entity translation tables** for referential integrity and indexability. Volatile UI copy uses the **key/value `i18n_message`** catalog so it can be managed without touching entity tables.

## Fallback behavior (implemented, not just documented)

`ref.value_label(ref_value_id, locale)` resolves a label as:

```
requested locale  ->  English ('en')  ->  the value's code
```

and `locale` itself defaults to the current corporation's `default_locale` when not passed. The same fallback pattern is applied (in views / resolver functions) to every `*_translation` table. The principle: **always return *something* human-readable**, and surface "missing translation" to admins via a report rather than to end users as a blank.

## Tenant + locale interaction

Reference values can be **system/global** (shared translations) or **tenant-specific** (the tenant supplies translations for their own values). Because translations hang off `ref_value_id`, a tenant's custom value (e.g. Akran's `hydrotherapy`) carries its own `tr`/`en` labels without affecting other tenants.

## Operational guidance

- Seed the platform with `tr` + `en`; Akran's corporation default is `tr`.
- A nightly "untranslated values" report (over `ref_value` left-joined to `ref_value_translation`) flags gaps per locale.
- Right-to-left support is already modeled (`locale.direction`) for future Arabic/Hebrew tenants.
- Number/date/currency formatting is a **presentation** concern (driven by locale + `corporation.default_currency`/`timezone`), not stored per string.
