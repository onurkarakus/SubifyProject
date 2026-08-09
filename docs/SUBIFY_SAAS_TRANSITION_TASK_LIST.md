# Subify SaaS Geçiş — Detaylı Task Listesi

| Alan | Değer |
| ---- | ----- |
| **Sürüm** | 1.2 |
| **Durum** | Plan — **şimdi implementasyon yok**; OS tamamlandıktan / karar verildikten sonra |
| **Son güncelleme** | 2026-08-02 |
| **Kilit (D5)** | **SMTP + AI yalnızca platform** — müşteriden key/SMTP **istenmez** |
| **Kilit (D11–D13)** | AI **1 canlı / gün / org**; aynı gün tekrar = **DB cache replay** |
| **PRD** | [SUBIFY_SAAS_TRANSITION_PRD.md](./SUBIFY_SAAS_TRANSITION_PRD.md) |
| **Ödeme ref** | [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md) |
| **Kullanım** | Grok/dev’e görev: `S3.2.1` veya `SaaS-S3.2.1` |

### Kurallar

1. Bu liste **Cloud / SaaS** içindir. OS manifesto “ödeme yok” kuralı burada **bilinçli olarak aşılır**.
2. OS task listesine billing task **yazma** — drift olmasın.
3. Her task: numara, açıklama, öncelik (P0–P3), bağımlılık, kabul kriteri.
4. Tamamlanınca `[x]` + tarih + kısa Not.
5. PRD’deki **D1–D10 kararları** kilitlenmeden P0 coding’e girme (S0).

### Öncelik

| Kod | Anlam |
| --- | ---- |
| **P0** | Launch blocker |
| **P1** | Launch’a çok yakın / ilk ay |
| **P2** | Önemli, ertelenebilir |
| **P3** | Nice-to-have |

---

## S0 — Karar, envanter, hazırlık

### S0.1 Ürün

- [ ] **S0.1.1** PRD kilit kararları (D1–D10) imzala  
  **Açıklama:** Paylaşım modeli, org-billed, free plan, e-posta confirm, multi-org, dual-host, region.  
  **D5 kilitli:** SMTP + AI = **platform-only** (müşteri BYOK yok).  
  **Öncelik:** P0 · **Bağımlı:** —  
  **Kabul:** PRD §12 tablo dolu; tarihli.

- [ ] **S0.1.2** Plan matrisi netleştir (Free/Plus/Pro/Lifetime)  
  **Açıklama:** maxSubs, maxMembers, AI daily live (v1: 0 veya 1), feature flags. Fiyat TRY/USD taslak.  
  **Not:** Aylık “20 AI call” yerine birincil abuse kontrolü **günlük 1 canlı + cache** (PRD §3.3.1).  
  **Öncelik:** P0 · **Bağımlı:** S0.1.1  
  **Kabul:** Tablo PRD veya bu dosyada; RC product ID taslağı.

- [ ] **S0.1.3** Legal iskelet listesi  
  **Açıklama:** ToS, Privacy, DPA, cookie, AI disclosure checklist (içerik sonra).  
  **Öncelik:** P1 · **Bağımlı:** S0.1.1

### S0.2 Teknik envanter

- [ ] **S0.2.1** Entity → OrganizationId gereklilik matrisi  
  **Açıklama:** Her DbSet için: required / nullable / global.  
  **Öncelik:** P0 · **Bağımlı:** S0.1.1  
  **Kabul:** Spreadsheet veya markdown tablo; Subscription, Category, Activity, AI log, Provider, Invite…

- [ ] **S0.2.2** Handler envanteri (tenant filter eklenecekler)  
  **Açıklama:** Application Features klasörü listesi; her biri org-scope mi?  
  **Öncelik:** P0 · **Bağımlı:** S0.2.1

- [ ] **S0.2.3** Dual-host repo layout taslağı  
  **Açıklama:** `Subify.Cloud.Api` / billing projeleri; OS Api dokunulmaz kalacak mı?  
  **Öncelik:** P0 · **Bağımlı:** S0.1.1 D7  
  **Kabul:** ADR kısa not (`docs/ADR.md` maddesi veya bu dosyada).

- [ ] **S0.2.4** RevenueCat + Stripe hesapları (sandbox)  
  **Açıklama:** Project, apps (web/iOS/Android), products, entitlements isimleri.  
  **Öncelik:** P0 · **Bağımlı:** S0.1.2  
  **Kabul:** Sandbox keys vault’ta; [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md) Cloud için güncellenmiş draft.

- [ ] **S0.2.5** Tehdit modeli (tenant isolation)  
  **Açıklama:** IDOR senaryoları, impersonation, webhook spoof.  
  **Öncelik:** P1 · **Bağımlı:** S0.2.1

### S0.3 Ürün / OS ilişki

- [ ] **S0.3.1** OS feature freeze veya “shared-core only” kuralı  
  **Açıklama:** Cloud S1 sırasında OS’e hangi PR’lar girer?  
  **Öncelik:** P1 · **Bağımlı:** S0.1.1 D8

- [ ] **S0.3.2** Marka: Cloud domain, e-posta from, destek adresi  
  **Öncelik:** P2 · **Bağımlı:** —

---

## S1 — Tenancy foundation (P0)

### S1.1 Domain & persistence

- [ ] **S1.1.1** `Organization` entity + status enum  
  **Açıklama:** Id, Name, Slug, Status, CreatedAt, OwnerUserId (denormalize opsiyonel).  
  **Öncelik:** P0 · **Bağımlı:** S0.2.1  
  **Kabul:** Unit test create/normalize slug.

- [ ] **S1.1.2** `OrganizationMember` entity + roles  
  **Açıklama:** Owner/Admin/Member; unique (Org, User).  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1

- [ ] **S1.1.3** `OrganizationSettings` entity  
  **Açıklama:** locale, currency, theme defaults (OS SystemSettings’ten ayrıştır).  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1

- [ ] **S1.1.4** `PlatformSettings` entity (managed SMTP + AI)  
  **Açıklama:** **Tek** platform AI key (encrypted), SMTP veya SES/SendGrid config, FromName/FromEmail, AiProvider/Model/BaseUrl, MailEnabled/AiEnabled, maintenance.  
  Müşteri/org tablosu **değil**; org settings’te SMTP/AI alanı **yok**.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1  
  **Kabul:** Secret maskeleme; plain key API response’ta dönmez.

- [ ] **S1.1.5** Migration: add org tables  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1–S1.1.4

- [ ] **S1.1.6** Migration: add `OrganizationId` to tenant tables  
  **Açıklama:** Subscription, UserCategory, ActivityLog, AiSuggestionLog, … PRD §5.2.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.5, S0.2.1  
  **Kabul:** Snapshot yeşil; backfill strategy documented.

- [ ] **S1.1.7** Provider: `OrganizationId` nullable (null = global)  
  **Öncelik:** P0 · **Bağımlı:** S1.1.6

- [ ] **S1.1.8** In-place backfill job (single-tenant → one Default org)  
  **Açıklama:** Mevcut OS DB’yi Cloud mode’a yükseltme script/handler.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.6  
  **Kabul:** SuperAdmin → Owner; tüm satırlar org’a bağlı; test harness.

### S1.2 Runtime tenancy

- [ ] **S1.2.1** `ITenantContext` / `ICurrentOrganization`  
  **Açıklama:** JWT `org_id` → scoped service.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.x

- [ ] **S1.2.2** EF global query filters per tenant entity  
  **Öncelik:** P0 · **Bağımlı:** S1.2.1  
  **Kabul:** Integration test: User A cannot read User B org data.

- [ ] **S1.2.3** JWT claims: org_id, org_role  
  **Açıklama:** Login/refresh/register token’a ekle.  
  **Öncelik:** P0 · **Bağımlı:** S1.2.1

- [ ] **S1.2.4** Org switch endpoint (v1 skip if single-org)  
  **Açıklama:** Multi-org D6=no ise task iptal/ defer.  
  **Öncelik:** P2 · **Bağımlı:** S0.1.1 D6

### S1.3 Auth akışları Cloud

- [ ] **S1.3.1** Register: User + Organization + Owner member + settings  
  **Öncelik:** P0 · **Bağımlı:** S1.2.3  
  **Kabul:** Tek transaction; e-posta unique.

- [ ] **S1.3.2** Cloud e-posta confirm (D4)  
  **Açıklama:** OS’te yoktu; Cloud’da token mail + gate.  
  **Öncelik:** P0 · **Bağımlı:** S1.3.1, platform mail  
  **Kabul:** Confirm olmadan hassas aksiyonlar kilitli (politikaya göre).

- [ ] **S1.3.3** Setup wizard devre dışı (Cloud host)  
  **Açıklama:** SetupGateMiddleware Cloud’da kapalı; platform seed. Müşteri setup’ta SMTP/AI **adımı yok**.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.4

- [ ] **S1.3.4** Seed PlatformOwner  
  **Açıklama:** Env ile ilk platform admin.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.4

- [ ] **S1.3.5** Cloud’da müşteri SMTP/AI yazma API’lerini kaldır veya 410  
  **Açıklama:** OS `PUT /admin/settings` AI/SMTP alanları müşteri rolüne **kapalı**. Yalnız PlatformAdmin `PlatformSettings`.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.4, S6.1.2  
  **Kabul:** OrgAdmin AI key POST ederse 403/404; dokümantasyon net.

### S1.4 Application handlers — org scope (dalga)

> S0.2.2 envanterine göre parçala; örnek gruplar:

- [ ] **S1.4.1** Subscriptions CQRS org filter + CreatedByUserId  
  **Öncelik:** P0 · **Bağımlı:** S1.2.2

- [ ] **S1.4.2** Categories org filter  
  **Öncelik:** P0 · **Bağımlı:** S1.2.2

- [ ] **S1.4.3** Reports org filter  
  **Öncelik:** P0 · **Bağımlı:** S1.4.1

- [ ] **S1.4.4** AI org filter + usage counter hook (limit sonra S3)  
  **Açıklama:** `IAiClient` her zaman **PlatformSettings** key ile; request body’de apiKey yok.  
  **Öncelik:** P0 · **Bağımlı:** S1.2.2, S1.1.4

- [ ] **S1.4.5** Activity org filter  
  **Öncelik:** P1 · **Bağımlı:** S1.2.2

- [ ] **S1.4.6** Profile: org-agnostic alanlar vs org settings ayrımı  
  **Öncelik:** P1 · **Bağımlı:** S1.1.3

### S1.5 Test

- [ ] **S1.5.1** Tenant isolation integration suite  
  **Açıklama:** En az: list/get/update/delete subscription cross-org 404.  
  **Öncelik:** P0 · **Bağımlı:** S1.4.1  
  **Kabul:** CI’da koşar.

- [ ] **S1.5.2** Register creates org smoke  
  **Öncelik:** P0 · **Bağımlı:** S1.3.1

---

## S2 — Members, invites, shared ledger

### S2.1 Domain

- [ ] **S2.1.1** `OrganizationInvite` (token hash, role, expiry)  
  **Açıklama:** OS UserInvite’den ayır veya adapt et.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.2

- [ ] **S2.1.2** Membership rules  
  **Açıklama:** Son Owner silinemez; Owner transfer.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.2

### S2.2 API

- [ ] **S2.2.1** `POST /api/org/invites`  
  **Öncelik:** P0 · **Bağımlı:** S2.1.1, mail

- [ ] **S2.2.2** `POST /api/auth/accept-org-invite`  
  **Öncelik:** P0 · **Bağımlı:** S2.2.1

- [ ] **S2.2.3** `GET/PATCH/DELETE /api/org/members`  
  **Öncelik:** P0 · **Bağımlı:** S1.1.2

- [ ] **S2.2.4** Member limit pre-check (entitlement gelince sertleştir)  
  **Öncelik:** P1 · **Bağımlı:** S3.x

### S2.3 Paylaşım modeli A (shared ledger)

- [ ] **S2.3.1** Subscription visibility rules by role  
  **Açıklama:** Member tüm org subs görür mü, yoksa sadece kendi CreatedBy? PRD D1.  
  **Öncelik:** P0 · **Bağımlı:** S0.1.1 D1, S1.4.1  
  **Kabul:** Yazılı kural + test.

- [ ] **S2.3.2** Permission: who can archive/edit  
  **Öncelik:** P0 · **Bağımlı:** S2.3.1

### S2.4 Web

- [ ] **S2.4.1** Members page (list, role, remove)  
  **Öncelik:** P0 · **Bağımlı:** S2.2.3

- [ ] **S2.4.2** Invite UI + accept-invite page org-aware  
  **Öncelik:** P0 · **Bağımlı:** S2.2.2

---

## S3 — Entitlements & RevenueCat (P0 billing)

### S3.1 Data & domain

- [ ] **S3.1.1** `CustomerEntitlement` table (org-scoped)  
  **Açıklama:** EntitlementId, ProductId, ExpiresAt, WillRenew, Source.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1, S0.1.2

- [ ] **S3.1.2** `RevenueCatWebhookEvent` idempotency table  
  **Öncelik:** P0 · **Bağımlı:** S3.1.1

- [ ] **S3.1.3** `UsageCounter` (AiLiveCalls daily/monthly, EmailSummary, …)  
  **Açıklama:** Live LLM sayacı; cache replay **artırmaz**.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1

- [ ] **S3.1.3b** `DailyAiSuggestion` (veya AiSuggestionLog daily unique)  
  **Açıklama:** `(OrganizationId, Day, Kind)` unique; ResponsePayload; SourceLogId; timezone dayKey (D11).  
  Kind: `Analyze`, `ReportCommentary`.  
  **Öncelik:** P0 · **Bağımlı:** S1.1.6, S0.1.1 D11–D13  
  **Kabul:** Migration + index; unit test day boundary.

- [ ] **S3.1.4** Plan configuration source  
  **Açıklama:** Code constants v1; later DB. Map entitlement → limits (incl. `aiLivePerDay`).  
  **Öncelik:** P0 · **Bağımlı:** S0.1.2

### S3.2 RevenueCat integration

- [ ] **S3.2.1** RC project products/entitlements (sandbox) finalize  
  **Açıklama:** Update [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md) for Cloud (plus/pro, not only premium).  
  **Öncelik:** P0 · **Bağımlı:** S0.2.4

- [ ] **S3.2.2** `POST /api/billing/revenuecat/webhook`  
  **Açıklama:** Authorization (shared secret), parse event, upsert entitlement, idempotent.  
  **Öncelik:** P0 · **Bağımlı:** S3.1.1–S3.1.2, S3.2.1  
  **Kabul:** Fixture events ile unit/integration test; duplicate event no-op.

- [ ] **S3.2.3** Webhook event mapping matrix  
  **Açıklama:** INITIAL_PURCHASE, RENEWAL, CANCELLATION, EXPIRATION, BILLING_ISSUE, PRODUCT_CHANGE…  
  **Öncelik:** P0 · **Bağımlı:** S3.2.2

- [ ] **S3.2.4** `app_user_id` = OrganizationId (org-billed)  
  **Açıklama:** Document + enforce; login response includes rcAppUserId.  
  **Öncelik:** P0 · **Bağımlı:** S0.1.1 D2

- [ ] **S3.2.5** Stripe ↔ RC web linkage  
  **Açıklama:** Web checkout path (RC Stripe Billing veya Stripe Checkout + RC sync — seçim ADR).  
  **Öncelik:** P0 · **Bağımlı:** S3.2.1  
  **Kabul:** Sandbox’ta satın alma → webhook → entitlement Active.

- [ ] **S3.2.6** Customer portal / manage subscription (web)  
  **Öncelik:** P1 · **Bağımlı:** S3.2.5

### S3.3 Enforcement

- [ ] **S3.3.1** `IEntitlementService` (current org limits)  
  **Öncelik:** P0 · **Bağımlı:** S3.1.1, S3.1.4

- [ ] **S3.3.2** Gate: create subscription count  
  **Açıklama:** Error code `BILL_001` (define in ERROR_CODES Cloud section).  
  **Öncelik:** P0 · **Bağımlı:** S3.3.1, S1.4.1

- [ ] **S3.3.3** Gate: invite member count  
  **Öncelik:** P0 · **Bağımlı:** S3.3.1, S2.2.1

- [ ] **S3.3.4** AI analyze: daily live + same-day cache replay  
  **Açıklama:**  
  1) Bugün `DailyAiSuggestion` var → return cached (`fromCache=true`), **LLM yok**, counter yok.  
  2) Yok + plan AI kapalı → `BILL_004`.  
  3) Yok + plan açık → LLM (platform key) → persist daily row + log → `fromCache=false`.  
  Scope: **per Organization** (D12). Timezone: D11.  
  **Öncelik:** P0 · **Bağımlı:** S3.1.3b, S3.3.1, S1.4.4  
  **Kabul:** İki peş peşe POST aynı gün = 1 LLM mock call; ertesi gün 2. call.

- [ ] **S3.3.4b** AI report-commentary: ayrı daily kind  
  **Açıklama:** Analyze ile aynı mekanizma; `Kind=ReportCommentary`.  
  **Öncelik:** P1 · **Bağımlı:** S3.3.4

- [ ] **S3.3.4c** Response DTO: `fromCache`, `nextLiveAvailableAt`, `suggestionDay`  
  **Öncelik:** P0 · **Bağımlı:** S3.3.4  
  **Kabul:** Web bu alanlarla “yarın yenilenir” mesajı gösterir.

- [ ] **S3.3.5** Gate: report email summary feature  
  **Açıklama:** Gönderim platform SMTP; org SMTP ayarı yok. MailEnabled platform flag.  
  **Öncelik:** P1 · **Bağımlı:** S3.3.1, S1.1.4

- [ ] **S3.3.6** Grace period / billing issue behavior  
  **Açıklama:** Read-only mode vs soft banner; PRD kararı.  
  **Öncelik:** P1 · **Bağımlı:** S3.2.3

- [ ] **S3.3.7** Free tier defaults on register  
  **Açıklama:** Entitlement row or implicit free limits without RC.  
  **Öncelik:** P0 · **Bağımlı:** S1.3.1, S3.1.4

### S3.4 API read models

- [ ] **S3.4.1** `GET /api/billing/entitlements`  
  **Öncelik:** P0 · **Bağımlı:** S3.3.1

- [ ] **S3.4.2** `POST /api/billing/checkout` (web)  
  **Açıklama:** Returns URL / RC package identifier.  
  **Öncelik:** P0 · **Bağımlı:** S3.2.5

### S3.5 Tests

- [ ] **S3.5.1** Webhook signature reject test  
  **Öncelik:** P0 · **Bağımlı:** S3.2.2

- [ ] **S3.5.2** Limit enforcement tests (subs, members, AI daily cache)  
  **Açıklama:** (1) ilk analyze live (2) ikinci aynı gün cache (3) gün değişince live (4) free deny.  
  **Öncelik:** P0 · **Bağımlı:** S3.3.2–S3.3.4c

- [ ] **S3.5.3** Expiration removes access test  
  **Öncelik:** P0 · **Bağımlı:** S3.2.3

---

## S4 — Web Cloud UX (billing + shell)

### S4.1 Marketing / auth

- [ ] **S4.1.1** Landing + pricing page  
  **Öncelik:** P0 · **Bağımlı:** S0.1.2

- [ ] **S4.1.2** Register/login Cloud copy (setup yok)  
  **Öncelik:** P0 · **Bağımlı:** S1.3.1

- [ ] **S4.1.3** E-posta confirm UX  
  **Öncelik:** P0 · **Bağımlı:** S1.3.2

### S4.2 Billing UX

- [ ] **S4.2.1** Plan / billing settings page  
  **Açıklama:** Current plan, renew date, upgrade CTA, portal link.  
  **Öncelik:** P0 · **Bağımlı:** S3.4.1–S3.4.2

- [ ] **S4.2.2** Soft paywall banner component  
  **Öncelik:** P1 · **Bağımlı:** S3.4.1

- [ ] **S4.2.3** Hard paywall modal on BILL_001  
  **Öncelik:** P0 · **Bağımlı:** S3.3.2

- [ ] **S4.2.3b** AI page: cache vs live UX  
  **Açıklama:** `fromCache` ise “Bugünün önerisi · yarın yenilenir”; spam “yeniden üret” butonu yok veya disabled + tooltip.  
  History listesi aynı log’u gösterir.  
  **Öncelik:** P0 · **Bağımlı:** S3.3.4c  
  **Kabul:** i18n TR/EN.

- [ ] **S4.2.4** Checkout success/cancel routes  
  **Öncelik:** P0 · **Bağımlı:** S3.4.2

### S4.3 App shell adjustments

- [ ] **S4.3.1** Remove/hide OS SuperAdmin settings for customers  
  **Açıklama:** Müşteri UI’dan **SMTP ve AI key formları tamamen kaldırılır**. Instance settings yok.  
  Kopya: “E-posta ve AI Subify tarafından yönetilir.”  
  **Öncelik:** P0 · **Bağımlı:** S1.3.3, S1.3.5

- [ ] **S4.3.2** Org switcher UI (if multi-org)  
  **Öncelik:** P2 · **Bağımlı:** S1.2.4

- [ ] **S4.3.3** Members nav entry  
  **Öncelik:** P0 · **Bağımlı:** S2.4.1

### S4.4 i18n

- [ ] **S4.4.1** Billing/paywall + AI daily-cache strings TR/EN  
  **Açıklama:** örn. `aiDailyCached`, `aiNextLiveTomorrow`, `aiLiveQuotaUsed`.  
  **Öncelik:** P0 · **Bağımlı:** S4.2.x

---

## S5 — Provider catalog (global + custom)

### S5.1 Backend

- [ ] **S5.1.1** List providers: global ∪ current org  
  **Öncelik:** P0 · **Bağımlı:** S1.1.7

- [ ] **S5.1.2** OrgAdmin create custom provider  
  **Öncelik:** P1 · **Bağımlı:** S1.1.7

- [ ] **S5.1.3** Platform import JSON (move from SuperAdmin)  
  **Açıklama:** Existing import handler → PlatformAdmin policy.  
  **Öncelik:** P0 · **Bağımlı:** S6.x or temporary Platform role  
  **Kabul:** Sample catalog imports; customers cannot call.

- [ ] **S5.1.4** Subscription create: providerId optional + name required  
  **Öncelik:** P0 · **Bağımlı:** S1.4.1

- [ ] **S5.1.5** Slug uniqueness: global vs per-org  
  **Öncelik:** P0 · **Bağımlı:** S5.1.2

### S5.2 Web

- [ ] **S5.2.1** Provider picker: search global + “add custom”  
  **Öncelik:** P0 · **Bağımlı:** S5.1.1

- [ ] **S5.2.2** Custom provider mini-form  
  **Öncelik:** P1 · **Bağımlı:** S5.1.2

---

## S6 — Platform console (sen)

### S6.1 AuthZ

- [ ] **S6.1.1** `PlatformAdmin` / `PlatformSupport` roles  
  **Öncelik:** P0 · **Bağımlı:** S1.3.4

- [ ] **S6.1.2** Policy: PlatformOnly endpoints  
  **Öncelik:** P0 · **Bağımlı:** S6.1.1

### S6.2 Features

- [ ] **S6.2.1** Tenant list + search + status  
  **Öncelik:** P0 · **Bağımlı:** S6.1.2

- [ ] **S6.2.2** Suspend / unsuspend tenant  
  **Açıklama:** Login blocked; data retained.  
  **Öncelik:** P0 · **Bağımlı:** S6.2.1

- [ ] **S6.2.3** Impersonation (Support) + audit  
  **Öncelik:** P1 · **Bağımlı:** S6.1.1  
  **Kabul:** Every act logged; time-boxed token.

- [ ] **S6.2.4** Global provider admin UI + import  
  **Öncelik:** P0 · **Bağımlı:** S5.1.3

- [ ] **S6.2.5** Entitlement override (support)  
  **Açıklama:** Comp access; reason required; expiry.  
  **Öncelik:** P1 · **Bağımlı:** S3.1.1

- [ ] **S6.2.6** Platform health dashboard  
  **Açıklama:** FX, mail, jobs, error rate — OS ops tab’ın platform hali.  
  **Öncelik:** P1 · **Bağımlı:** —

- [ ] **S6.2.7** Platform settings UI: managed SMTP + AI (sen)  
  **Açıklama:** Platform console’da: SMTP/SES ayarı, test mail; AI provider/model/base URL/key (maskeli), test AI.  
  Env override dokümantasyonu (`Platform__AiApiKey`, `Platform__Smtp__…`).  
  **Öncelik:** P0 · **Bağımlı:** S1.1.4, S6.3.1  
  **Kabul:** Key rotasyonu; audit log “settings updated” secrets’sız.

- [ ] **S6.2.8** Platform mail + AI health indicators  
  **Açıklama:** Health: last mail success/fail, AI configured yes/no (key var mı — değer değil).  
  **Öncelik:** P1 · **Bağımlı:** S6.2.6, S6.2.7

### S6.3 Web platform app

- [ ] **S6.3.1** `/platform` layout + nav  
  **Öncelik:** P0 · **Bağımlı:** S6.1.2

- [ ] **S6.3.2** Pages: tenants, providers, health, settings  
  **Öncelik:** P0 · **Bağımlı:** S6.2.x

---

## S7 — Mobile (Flutter) + RevenueCat client

> OS Flutter foundation (13.x) ile hizala; Cloud’da RC şart.

- [ ] **S7.1.1** Flutter RC SDK init  
  **Öncelik:** P1 · **Bağımlı:** S3.2.1, mobile foundation

- [ ] **S7.1.2** Login: logIn(appUserId=orgId)  
  **Öncelik:** P1 · **Bağımlı:** S3.2.4, S7.1.1

- [ ] **S7.1.3** Offerings + purchase package  
  **Öncelik:** P1 · **Bağımlı:** S7.1.2

- [ ] **S7.1.4** Paywall screen (legacy mockup)  
  **Öncelik:** P1 · **Bağımlı:** S7.1.3

- [ ] **S7.1.5** Restore purchases  
  **Öncelik:** P1 · **Bağımlı:** S7.1.2

- [ ] **S7.1.6** Customer center / manage (store)  
  **Öncelik:** P2 · **Bağımlı:** S7.1.3

- [ ] **S7.1.7** Entitlement-aware API errors on mobile  
  **Öncelik:** P1 · **Bağımlı:** S3.3.x

---

## S8 — Hardening, compliance, launch

### S8.1 Security

- [ ] **S8.1.1** Penetration checklist: IDOR, webhook, JWT org mismatch  
  **Öncelik:** P0 · **Bağımlı:** S1.5.1, S3.5.x

- [ ] **S8.1.2** Rate limits: register, login, invite, AI, checkout  
  **Öncelik:** P0 · **Bağımlı:** —

- [ ] **S8.1.3** Captcha on register (Cloudflare Turnstile/hCaptcha)  
  **Öncelik:** P1 · **Bağımlı:** S1.3.1

- [ ] **S8.1.4** Security headers + CORS prod origins  
  **Öncelik:** P0 · **Bağımlı:** —

- [ ] **S8.1.5** Secret scan CI  
  **Öncelik:** P1 · **Bağımlı:** —

### S8.2 Privacy

- [ ] **S8.2.1** Org data export API  
  **Öncelik:** P0 · **Bağımlı:** S1.4.x

- [ ] **S8.2.2** Org delete + grace period job  
  **Öncelik:** P0 · **Bağımlı:** S1.1.1

- [ ] **S8.2.3** Privacy/ToS pages + acceptance on register  
  **Öncelik:** P0 · **Bağımlı:** S0.1.3

- [ ] **S8.2.4** Subprocessors list page  
  **Öncelik:** P1 · **Bağımlı:** S0.1.3

### S8.3 Ops

- [ ] **S8.3.1** Prod deploy pipeline  
  **Öncelik:** P0 · **Bağımlı:** —

- [ ] **S8.3.2** Automated DB backup + restore drill  
  **Öncelik:** P0 · **Bağımlı:** —

- [ ] **S8.3.3** Observability (OTel/Sentry) — Cloud  
  **Açıklama:** OS 16.6.5 opsiyoneldi; Cloud P0’a yakın.  
  **Öncelik:** P0 · **Bağımlı:** —

- [ ] **S8.3.4** On-call runbook  
  **Öncelik:** P1 · **Bağımlı:** S8.3.1

- [ ] **S8.3.5** Cost alerts (LLM, mail, DB)  
  **Öncelik:** P1 · **Bağımlı:** S8.3.3

### S8.4 Billing prod

- [ ] **S8.4.1** RC/Stripe prod keys + webhook URL  
  **Öncelik:** P0 · **Bağımlı:** S3.2.x

- [ ] **S8.4.2** App Store / Play products live  
  **Öncelik:** P1 · **Bağımlı:** S7.x, S3.2.1

- [ ] **S8.4.3** Tax / VAT settings (Stripe Tax opsiyonel)  
  **Öncelik:** P2 · **Bağımlı:** S3.2.5

- [ ] **S8.4.4** Trial policy (14g?) implementation  
  **Öncelik:** P1 · **Bağımlı:** S3.3.7

### S8.5 Launch gate

- [ ] **S8.5.1** Staging full journey checklist  
  **Açıklama:** register → invite → add sub → hit limit → pay → AI.  
  **Öncelik:** P0 · **Bağımlı:** S1–S6

- [ ] **S8.5.2** Load smoke (register + list subs)  
  **Öncelik:** P1 · **Bağımlı:** S8.3.1

- [ ] **S8.5.3** Go-live checklist signed  
  **Öncelik:** P0 · **Bağımlı:** S8.5.1

---

## S9 — Post-launch / v1.1+ (backlog)

- [ ] **S9.1** Multi-org per user  
- [ ] **S9.2** SSO (Google/Microsoft)  
- [ ] **S9.3** MFA  
- [ ] **S9.4** Org-level AI BYOK *(varsayılan kapalı; v1 PRD dışı — D5 platform-only)*  
  **Not:** Açılırsa ayrı güvenlik + ToS; müşteri SMTP BYOK da buraya bağlı değerlendirilir.

- [ ] **S9.11** Pro: N canlı AI / gün veya “force refresh” entitlement  
  **Not:** v1 sabit 1/gün; ücretli ekstra slot sonra.  
- [ ] **S9.5** Shared-with matrix beyond roles  
- [ ] **S9.6** Multi-region data residency  
- [ ] **S9.7** Marketplace / public provider community (moderated)  
- [ ] **S9.8** Annual invoices PDF  
- [ ] **S9.9** Partner/referral  
- [ ] **S9.10** OS→Cloud paid migration concierge  

---

## Error codes (Cloud eklentisi — taslak)

OS kodlarına ek (ayrı namespace veya `BILL_*` / `ORG_*` / `PLT_*`):

| Code | HTTP | Anlam |
| ---- | ---- | ----- |
| `BILL_001` | 402/403 | Plan limit (subs) |
| `BILL_002` | 402/403 | Plan limit (members) |
| `BILL_003` | 402/403 | AI live quota exceeded *(v1 nadir: günlük 1 + cache; aylık cap varsa)* |
| `BILL_003a` | 200 + flag | *(tercihen error değil)* same-day cache — normal 200 `fromCache` |
| `BILL_004` | 403 | Feature requires higher plan |
| `BILL_005` | 402 | Subscription expired / billing issue |
| `BILL_006` | 400 | Webhook invalid |
| `ORG_001` | 404 | Organization not found |
| `ORG_002` | 403 | Not a member |
| `ORG_003` | 403 | Insufficient org role |
| `ORG_004` | 409 | Last owner |
| `ORG_005` | 409 | Invite expired/used |
| `PLT_001` | 403 | Platform admin only |
| `PLT_002` | 403 | Tenant suspended |
| `PLT_003` | 503 | Platform AI not configured (senin key’in eksik) |
| `PLT_004` | 503 | Platform mail not configured |

→ Launch öncesi [ERROR_CODES.md](./ERROR_CODES.md) veya `ERROR_CODES_SAAS.md` dosyasına işlenir (**S8**).

---

## Tahmini efor (grubça, normal tempo)

| Faz | Kabaca |
| --- | ------ |
| S0 | 3–5 gün |
| S1 | 2–4 hafta |
| S2 | 1–2 hafta |
| S3 | 2–3 hafta |
| S4 | 1–2 hafta |
| S5 | 3–7 gün |
| S6 | 1–2 hafta |
| S7 | 2–4 hafta (mobile olgunluğuna bağlı) |
| S8 | 1–2 hafta |
| **Toplam v1 web-first (S7 hafif)** | **~2.5–4 ay** tek deneyimli full-stack |
| **+ mobile store** | **+1–2 ay** |

Bunlar planlama rakamları; takım boyutuna göre değişir.

---

## OS task listesi ile ilişki

| OS | SaaS |
| -- | ---- |
| [SUBIFY_OS_TASK_LIST.md](./SUBIFY_OS_TASK_LIST.md) | Bu dosya |
| Manifesto: ödeme yok | PRD: RC var |
| SuperAdmin setup | PlatformOwner seed |
| 16.6 ops self-host | S6/S8 cloud ops |
| Provider import SuperAdmin | S5/S6 PlatformAdmin |

**Öneri:** OS v1 stabilize → S0 kararları → S1 tenancy. Ortada “yarı OS yarı SaaS” production’a çıkma.

---

## Değişiklik geçmişi

| Tarih | Sürüm | Not |
| ----- | ----- | --- |
| 2026-08-02 | 1.0 | İlk detaylı geçiş task listesi + PRD referansı |
| 2026-08-02 | 1.1 | D5: platform-only SMTP/AI; müşteri BYOK task’ları netleştirildi / S9.4 ertelendi |
| 2026-08-02 | 1.2 | D11–D13: günlük 1 canlı AI + same-day DB cache replay; S3.3.4* / S4.2.3b |

---

*Grok’a örnek: “SaaS S3.2.2 webhook’u implement et — SUBIFY_SAAS_TRANSITION_TASK_LIST ve PRD’ye uy.”*
