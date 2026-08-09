# Subify OS — Detaylı Geliştirme Task Listesi

| Alan | Değer |
| ---- | ----- |
| **Sürüm** | 1.4 |
| **Durum** | Aktif — uygulama sırası |
| **Son güncelleme** | 2026-08-02 |
| **Kaynak** | [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md), [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md) |
| **Kullanım** | Grok’a görev verirken **task numarasını** yaz (ör. `3.2.4` veya `T-3.2.4`) |

### SaaS / Cloud geçiş (ayrı hat — bu listeye karıştırma)

İleride self-host OS → kapalı **Subify Cloud** için:

| Doküman | İçerik |
| ------- | ------ |
| [SUBIFY_SAAS_TRANSITION_PRD.md](./SUBIFY_SAAS_TRANSITION_PRD.md) | Ürün/mimari/tenancy/RevenueCat/geçiş PRD |
| [SUBIFY_SAAS_TRANSITION_TASK_LIST.md](./SUBIFY_SAAS_TRANSITION_TASK_LIST.md) | `S0`–`S9` numaralı Cloud task’ları |
| [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md) | Legacy + Cloud ödeme entitlement referansı |

**Kural:** Billing, plan limiti, multi-tenant `OrganizationId` işleri **SaaS task listesine** yazılır. OS manifestosu “ödeme yok” kuralı yalnızca **bu OS listesi** için geçerlidir.

### Yeni scope / task ekleme kuralı (14.3.2)

1. **Manifesto çelişkisi** → ekleme; reddet (freemium, confirm-mail zorunlu, SaaS ödeme, vs.).  
   - Cloud istisnası: [SUBIFY_SAAS_TRANSITION_PRD.md](./SUBIFY_SAAS_TRANSITION_PRD.md) — **ayrı ürün disiplini**.  
2. **Setup / EmailSend** kararlarına uy: SMTP **kayıt şimdi**, **gönderim Faz 15**; confirm **yok (kalıcı)** *(yalnız OS)*.  
3. Yeni task: net numara (`X.Y.Z`), öncelik, bağımlılık, kısa açıklama.  
4. Sprint/PR sonunda ilgili satırları `[x]` + **Tamamlandı** tarihi + kısa **Not** (14.3.1).

---

## Ürün kararları (kapsam & erteleme)

### E-posta modeli (özet — SMTP BYOK)

```
┌─────────────────────────────────────────────────────────────────┐
│  SuperAdmin (setup veya Settings)                               │
│    → Email özelliğini AÇ                                        │
│    → SMTP: Host, Port, User, Password, FromName, FromEmail      │
│    → Kaydet (SystemSettings)                                    │
└───────────────────────────┬─────────────────────────────────────┘
                            │
              SmtpEnabled + host/from dolu mu?
                     │              │
                    Evet           Hayır
                     │              │
              Faz 15 motor    Noop / SET_003
              gerçek mail     (gönderim yok)
```

| Aşama | Ne yapılır | Ne zaman |
| ----- | ---------- | -------- |
| **A — Ayar saklama** | SMTP alanları setup + SuperAdmin Settings | **Şimdi** (3S.5, 7.3) — entity/API |
| **B — Gönderim motoru** | `IEmailSender`, test mail, forgot, invite mail, yenileme maili | **Faz 15** (core bitince) |
| **C — Confirm** | Register e-posta doğrulama | **Yok (iptal)** — SMTP açık olsa bile |

### Şimdi / MVP

| Konu | Karar |
| ---- | ----- |
| **E-posta confirm** | **Yok (kalıcı)** — register sonrası hemen login (`EmailConfirmed = true`). SMTP açılsa bile confirm **zorunlu akış olmaz**. |
| **İlk kurulum (Setup Wizard)** | **Var** — Super Admin → opsiyonel kullanıcılar → **opsiyonel SMTP** → opsiyonel AI → Finish |
| **SMTP ayarları** | Setup’ta veya sonradan SuperAdmin Settings’te girilir/düzenlenir; **sadece kaydedilir** |
| **E-posta gönderim** | `SmtpEnabled` + geçerli SMTP → mail kullanıcının sunucusundan (Faz 15 motor) |
| **Şifre (şimdi)** | `change-password` (oturum) + SuperAdmin `reset-password` (mail yok) |
| **Şifremi unuttum (sonra)** | SMTP açıkken e-posta linki (Faz 15) — kapalıysa yine admin reset |
| **Invite** | Link UI’da her zaman; **mail ile gönderme** SMTP açıksa (Faz 15) |

### Kapsam dışı (iptal) — `[-]`

| Konu | Karar | Neden |
| ---- | ----- | ----- |
| **Confirm-email / resend** | Uygulanmayacak | Self-host: SuperAdmin zaten kullanıcıları bilir; friction + mailbox bağımlılığı istemiyoruz |
| **Freemium / ödeme** | Yok | Manifesto |

> **Confirm ≠ SMTP.** SMTP açılınca “forgot password mail” ve “invite mail” gelir; **hesap e-posta doğrulama (confirm) gelmez**. İleride opsiyonel “mail doğrula” istenir ürün kararı yeniden alınır ve yeni task açılır.

### Ertelenen (Faz 15 — EmailSend, core sonrası) — listede **kalmalı**

Bu task’lar “gereksiz” değil; **şimdi yazılmayacak**, core bitince yapılacak. 3.2’de `[-]` ile unutulmasın diye Faz 15’te `[ ]` olarak dururlar:

| Konu | Not |
| ---- | --- |
| `IEmailSender` + `SmtpEmailSender` | SystemSettings SMTP oku; disabled → noop |
| Test SMTP mail | SuperAdmin “test gönder” |
| Forgot-password + mail token reset | SMTP kapalı → anlamlı hata; açık → mail |
| Invite e-posta | SMTP açıksa; değilse sadece kopyalanabilir link |
| Renewal reminder mail | `daysBeforeRenewal` + SMTP |

**Auth sonucu (şimdi):** Confirm yok. Şifre: change + admin reset. Mail: ayar kaydı setup/settings; **gönderim Faz 15**.

---

## Nasıl kullanılır?

1. Aşağıdaki numaralandırma **hiyerarşiktir**: `Faz.Bölüm.Task` (ör. `3.1.2`).
2. Alt adımlar gerekiyorsa `3.1.2.a`, `3.1.2.b` kullanılır.
3. Durum işaretleri:
   - `[ ]` Yapılmadı
   - `[~]` Kısmen yapıldı / iskelet var
   - `[x]` Tamamlandı
   - `[-]` **İptal / kapsam dışı** — yapılmayacak
4. **Öncelik:** P0 (bloklayıcı) → P1 → P2 → P3 (sonra / opsiyonel).
5. **Bağımlılık:** Bir task’ın “Bağımlı” satırı varsa önce onlar bitmeli.
6. Eski SaaS task listeleri (`SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md`) **geçersizdir**; bu dosya geçerlidir.

---

## İlerleme özeti (yüksek seviye)

| Faz | Konu | Genel durum |
| --- | ---- | ----------- |
| 0 | Repo, dokümantasyon, temiz başlangıç | [x] 0.1 + 0.2 tamam |
| 1 | Core setup (solution, tooling, Scalar) | [~] |
| 2 | Domain, EF, Postgres, seed altyapısı | [x] |
| 3 | Auth, roller, SuperAdmin, şifre, multi-user | [~] |
| 3S | **First-run Setup Wizard (API + Web)** | [~] API P0/P1; web 3S.8 açık |
| 4 | Subscription + finansal motor | [x] 4.1 features + 4.2 API + 4.3 financial |
| 5 | Categories, providers, profile, activity | [x] 5.1–5.4 tamam |
| 6 | Reports, FX, resources/i18n | [x] 6.1–6.3 |
| 7 | Admin panel API (users, settings, invites) | [~] 7.1–7.3 + 7.5 (7.3.3/4, 7.4 deferred) |
| 8 | Background jobs (FX + renewal mail) | [x] 8.1–8.4 (mail = 15.3 ile aynı) |
| 9 | AI (BYOK) | [x] 9.1–9.2 |
| 10 | Web (Next.js) UI + setup UI | [~] 10.1–10.9 MVP shell |
| 11 | Docker, release, ops | [x] 11.1–11.2 |
| 12 | Testler | [ ] |
| 13 | Flutter (en son) | [ ] |
| 14 | Dokümantasyon & polish | [x] 14.1–14.3 |
| **15** | **EmailSend altyapısı** | [x] 15.1–15.4 |
| **16** | **Ürün genişletme (v1+ / docs backlog)** | [ ] FX UX + next features |

---

# FAZ 0 — Dokümantasyon hiyerarşisi ve repo hijyeni

### 0.1 Doküman önceliği

- [x] **0.1.1** README’yi Subify OS odaklı yeniden yaz  
  **Açıklama:** Self-hosted, open source, freemium/RevenueCat yok; `docker compose` vaadi ve linkler (Manifesto, PRD, bu task list).  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22

- [x] **0.1.2** Eski SaaS PRD’ye LEGACY banner ekle  
  **Açıklama:** `Subify.Web.Uygulamasi.v2.PRD.md` en üste “uygulama için kullanma; OS PRD geçerli” uyarısı.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

- [x] **0.1.3** Eski task listesine LEGACY banner ekle  
  **Açıklama:** `SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md` için aynı uyarı.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

- [x] **0.1.4** DATA_MODEL / API_CONTRACTS / ERROR_CODES OS notu  
  **Açıklama:** Billing, plan, premium limit bölümlerine “OS’ta yok” notu veya ayrı OS patch notları.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

- [x] **0.1.5** LICENSE dosyası ekle  
  **Açıklama:** MIT (veya seçilen OSS lisansı) kök dizine.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

### 0.2 Repo hijyeni

- [x] **0.2.1** Kök `.gitignore` gözden geçir  
  **Açıklama:** `bin/`, `obj/`, `.env`, `node_modules/`, IDE, user secrets, log dosyaları.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** Eski dosyada `*.sln` ve `*.launchSettings.json` yanlışlıkla ignore ediliyordu (kaldırıldı). `!.env.example`, logs, TestResults, Flutter/Docker data eklendi.

- [x] **0.2.2** API’de kullanılmayan kalıntıları temizle  
  **Açıklama:** `WeatherForecast`, boş Controllers klasörü, örnek dosyalar, çakışan legacy HTTP testleri.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** `LoginRequest` (kullanılmıyordu), boş `Repositories`/`RequestEntities` klasörleri, `bin\Debug` artifact kaldırıldı; `.http` host `5240` yapıldı.

- [x] **0.2.3** `Subify.Api.http` dosyasını güncelle  
  **Açıklama:** Scalar ile uyumlu login/register örnek istekleri.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22  
  **Not:** host `5240`, Scalar/OpenAPI GET’leri, register/login (+ duplicate/invalid), named requests, token variable zinciri.

---

# FAZ 1 — Core setup ve API host sertleştirme

### 1.1 Solution ve katmanlar

- [x] **1.1.1** Clean Architecture solution iskeleti  
  **Açıklama:** Domain, Application, Infrastructure, Api projeleri ve referanslar.  
  **Durum:** Mevcut.

- [x] **1.1.2** MediatR + DI registration  
  **Açıklama:** `AddApplicationServices` / `AddInfrastructureServices`.  
  **Durum:** Mevcut (pipeline behavior eksik olabilir → 1.2.x).

- [x] **1.1.3** Result / Error / DomainErrors altyapısı  
  **Açıklama:** `Result<T>`, `Error`, error kodları.  
  **Durum:** Mevcut; OS temizliği **1.2.4** ile yapıldı.

- [x] **1.1.4** Minimal API endpoint discovery  
  **Açıklama:** `IEndpoint`, `AddEndpoints`, `MapEndpoints`.  
  **Durum:** Mevcut.

- [x] **1.1.5** Scalar / OpenAPI UI  
  **Açıklama:** Development’ta `/scalar/v1`, `/openapi/v1.json`, root redirect.  
  **Durum:** Mevcut.

### 1.2 Cross-cutting API pipeline

- [x] **1.2.1** FluentValidation MediatR pipeline behavior  
  **Açıklama:** Tüm `IRequest` öncesi validator çalışsın; hatalar `VAL_*` + ProblemDetails.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** `ValidationBehavior<,>` + `AddValidatorsFromAssembly` + FluentValidation 12.1.1; handler öncesi short-circuit → `ValidationResult`/`ValidationResult<T>`.

- [x] **1.2.2** Validation exception → ProblemDetails middleware/map  
  **Açıklama:** Pipeline failure’ların HTTP 400 ile RFC 7807 dönmesi.  
  **Öncelik:** P0 · **Bağımlı:** 1.2.1 · **Tamamlandı:** 2026-03-22  
  **Not:** `ToFailureHttpResult` / validation ProblemDetails (`VAL_001` + `errors`); `ValidationExceptionHandler`; `AddProblemDetails` + `UseExceptionHandler`.

- [x] **1.2.3** Global exception handler  
  **Açıklama:** Beklenmeyen exception → `SYS_001`, traceId; development’ta detay opsiyonel.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** `GlobalExceptionHandler` — 500 + SYS_001 + traceId; Dev’de message/exceptionType; client abort → 499.

- [x] **1.2.4** DomainErrors OS temizliği  
  **Açıklama:** Premium/limit kodlarını kaldır veya yeniden adlandır (`AI_KEY_MISSING` vb.); `SUBS_001` limit kalksın.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** Limit/premium/PAY kaldırıldı; `AI_KEY_MISSING`, `SET_*`, auth invite/reg disabled eklendi; SUB kodları yeniden numaralandı.

- [x] **1.2.5** CORS policy  
  **Açıklama:** Web origin (`localhost:3000` + env); production’da bilinen origin.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** `Cors:AllowedOrigins` config; Dev default localhost:3000; prod boş = cross-origin kapalı; credentials + headers/methods.

- [x] **1.2.6** Rate limiting (login/register/AI)  
  **Açıklama:** ASP.NET rate limiter; brute-force ve AI abuse koruması (plan limiti değil). Forgot-password endpoint yok.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** Policies `auth-login` (10/dk), `auth-register` (5/dk), `ai-analyze` (5/dk, hazır); 429 + `SYS_004` ProblemDetails; config `RateLimiting`.

- [x] **1.2.7** `GET /health` (liveness)  
  **Açıklama:** Basit 200 OK; container healthcheck için.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** `GET /health` → `{ status, timestamp }`; anonymous, rate-limit disabled; DB kontrolü yok (1.2.8 readiness).

- [x] **1.2.8** `GET /health/ready` (readiness)  
  **Açıklama:** Postgres bağlantı kontrolü.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** EF `AddDbContextCheck` → `/health/ready`; 200 Healthy / 503 Unhealthy + JSON (database status).

- [x] **1.2.9** OpenAPI JWT Bearer security scheme  
  **Açıklama:** Scalar’da Authorize ile Bearer token girebilme.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** `BearerSecuritySchemeTransformer` + Scalar `AddPreferredSecuritySchemes("Bearer")`.

- [x] **1.2.10** ProblemDetails status code map doğrulama  
  **Açıklama:** `ResultExtensions` tüm `ErrorType` için doğru HTTP kodu.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** `ProblemDetailsStatusMapper` tek kaynak; `GatewayTimeout`→504; `Subify.Api.Tests` 14 test.

- [x] **1.2.11** Request logging / Serilog temel kurulum  
  **Açıklama:** Console + structured log; secret loglanmaz.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22  
  **Not:** Serilog console+file; UseSerilogRequestLogging (body/auth header yok); /health → Debug.

- [x] **1.2.12** `ICurrentUserService`  
  **Açıklama:** JWT’den `UserId`, email, roller; handler’larda tekrar parse yok.  
  **Öncelik:** P0 · **Bağımlı:** 3.1.x · **Tamamlandı:** 2026-03-22  
  **Not:** `ICurrentUserService` + `CurrentUserService`; JWT `MapInboundClaims=false`; `GetRequiredUserId()`.

---

# FAZ 2 — Domain, EF Core, PostgreSQL, seed

### 2.1 Domain model düzeltmeleri

- [x] **2.1.1** `ApplicationUser.Locate` → `Locale` rename  
  **Açıklama:** Property, migration, TokenService claim, tüm referanslar.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** Property + TokenService; migration `RenameLocateToLocale` (RenameColumn AspNetUsers).

- [x] **2.1.2** ApplicationUser profil alanlarını PRD ile hizala  
  **Açıklama:** FullName, Locale, MainCurrency, MonthlyBudget, ApplicationThemeColor, DarkTheme, audit. Plan alanı **eklenmeyecek**.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** Constants (locale/currency/theme); EF max-length + decimal(10,2); `ApplyRegistrationProfile` / `UpdateProfile`; migration AlignApplicationUserProfileFields.

- [x] **2.1.3** Subscription domain metodları güçlendir  
  **Açıklama:** Factory/create kuralları, `UserShare` computed, archive/reactivate, Category XOR UserCategory invariant.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** `Create`/`Update` Result; UserShare + monthly/yearly; Archive/Reactivate; XOR kategori; ProviderId nullable; BillingCycle Monthly|Yearly; domain tests 8.

- [x] **2.1.4** Provider `Logout` → `LogoUrl` (veya doğru alan adı)  
  **Açıklama:** Typo/isim düzeltmesi + migration.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** `Provider.LogoUrl`; migration `RenameProviderLogoutToLogoUrl` (RenameColumn).

- [x] **2.1.5** SystemSettings singleton / instance config modeli  
  **Açıklama:** Tek satır (veya key-value) instance ayarları. Alanlar:  
  - Setup: `IsSetupComplete`, `SetupCompletedAt`, `InstanceName`  
  - Locale defaults: `DefaultLocale`, `DefaultCurrency`, `TimeZoneId` (opsiyonel)  
  - AI: `AiProvider` (örn. OpenAI), `AiApiKey` (secret), `AiModel` (opsiyonel)  
  - SMTP (saklanır, **gönderim Faz 15**): Host, Port, User, Password, FromName, FromEmail, `SmtpEnabled`  
  - Public reg: `AllowPublicRegistration` (setup sonrası genelde false)  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** Entity + methods (`CreateDefault`, UpdateInstance/Ai/Smtp, MarkSetupComplete); EF config; migration ExpandSystemSettingsInstanceModel.

- [x] **2.1.6** RefreshToken entity rotation alanları  
  **Açıklama:** RevokedAt, ReplacedByToken, ReasonRevoked, IsActive helper.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** Create/Revoke/MarkReplaced; IsActive/IsExpired; column renames; LoginHandler Create; DB update applied.

- [x] **2.1.7** Invite token entity (yeni)  
  **Açıklama:** `UserInvite`: token hash, email, expires, createdBy, usedAt.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** Entity + Create/TryMarkUsed; EF config; migration AddUserInviteEntity; DB applied.

- [x] **2.1.8** Device token entity (opsiyonel / sonra)  
  **Açıklama:** Push için; Flutter fazına kadar ertele.  
  **Öncelik:** P3 · **Tamamlandı:** 2026-03-22  
  **Not:** `UserDeviceToken` + `DevicePlatform`; Create/Touch/Deactivate; migration + DB applied. Endpoint Flutter fazında.

- [x] **2.1.9** Soft delete global query filter stratejisi  
  **Açıklama:** `ISoftDeletable` için EF filter (opsiyonel ama tutarlı).  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22  
  **Not:** Global `DeletedAt == null` filter; hard Delete → soft-delete (Subscription.Archive); admin için `IgnoreQueryFilters()`.

- [x] **2.1.10** BaseEntity Id generation politikası  
  **Açıklama:** Postgres uyumlu UUID (v4/v7 veya `gen_random_uuid()`); dokümante et.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** UUID v7 via `GuidGenerator.NewId()`; SaveChanges empty-Id fill; EF ValueGeneratedNever; ADR-010 güncellendi.

### 2.2 EF Core configurations

- [x] **2.2.1** `IEntityTypeConfiguration<>` klasör yapısı  
  **Açıklama:** Infrastructure/Persistence/Configurations.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** 15 config + README; assembly scan; migration CompleteEntityTypeConfigurations (DB applied).

- [x] **2.2.2** Subscription configuration  
  **Açıklama:** Index `(UserId, Archived, NextRenewalDate)`, FK, precision decimal.  
  **Öncelik:** P0 · **Tamamlandı:** SubscriptionConfiguration

- [x] **2.2.3** Category / UserCategory / Provider configuration  
  **Açıklama:** Unique slug, indexes, soft delete (global filter).  
  **Öncelik:** P0 · **Tamamlandı:** Category/UserCategory/ProviderConfiguration

- [x] **2.2.4** Resource unique index  
  **Açıklama:** `(PageName, Name, LanguageCode)` unique.  
  **Öncelik:** P1 · **Tamamlandı:** ResourceConfiguration

- [x] **2.2.5** RefreshToken configuration  
  **Açıklama:** Index user+token hash; uzunluk limitleri.  
  **Öncelik:** P0 · **Tamamlandı:** RefreshTokenConfiguration

- [x] **2.2.6** ActivityLog / AiSuggestionLog configuration  
  **Açıklama:** Index `(UserId, CreatedAt)`.  
  **Öncelik:** P1 · **Tamamlandı:** ActivityLog + AiSuggestionLog configs

- [x] **2.2.7** EmailTemplates unique (Name, LanguageCode)  
  **Açıklama:** EF unique index hazır; seed Faz 15.  
  **Öncelik:** P3 · **Tamamlandı:** EmailTemplatesConfiguration

- [x] **2.2.8** ExchangeRateSnapshot index  
  **Açıklama:** `(Base, Target, FetchedAt)`.  
  **Öncelik:** P1 · **Tamamlandı:** ExchangeRateSnapshotConfiguration

- [x] **2.2.9** ApplicationUser / Identity table naming  
  **Açıklama:** Postgres naming convention (snake_case opsiyonel); tutarlılık.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** PascalCase kararlaştırıldı (ADR-011). `AspNetUsers` + Identity `AspNet*` tables explicit config; snake_case / `EFCore.NamingConventions` **yok**. Migration gerekmedi (isimler zaten aynı).

- [x] **2.2.10** SystemSettings configuration  
  **Açıklama:** Instance fields + max lengths; singleton seed app-level (2.3.9).  
  **Öncelik:** P1 · **Tamamlandı:** SystemSettingsConfiguration

### 2.3 DbContext, migrate, seed runtime

- [x] **2.3.1** SubifyDbContext DbSet’ler  
  **Açıklama:** Tüm OS entity’ler; billing yok.  
  **Durum:** Tamam — UserInvite, UserDeviceToken dahil.

- [x] **2.3.2** Startup auto-migrate  
  **Açıklama:** API ayağa kalkarken `Database.Migrate()` + retry Postgres ready.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-03-22  
  **Not:** `DatabaseMigrator.MigrateAsync` — pending migrations + 15 deneme / 2 sn; Program start’ta traffic öncesi.

- [x] **2.3.3** `IDataSeeder` / `DbInitializer` arayüzü  
  **Açıklama:** Idempotent seed pipeline.  
  **Öncelik:** P0 · **Bağımlı:** 2.3.2 · **Tamamlandı:** 2026-07-22  
  **Not:** `IDataSeeder` + `DatabaseSeeder` + `DatabaseInitializer` (migrate→seed); assembly auto-register; concrete seeders 2.3.4+.

- [x] **2.3.4** Role seed  
  **Açıklama:** `SuperAdmin`, `Admin`, `User` Identity rolleri.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `AppRoles` + `RolesDataSeeder` (Order 10); RoleManager; UUID v7 Id; idempotent RoleExists.

- [x] **2.3.5** Category seed (10 sistem kategorisi)  
  **Açıklama:** streaming, music, productivity, gaming, shopping, utilities, education, health, cloud, other.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `SystemCategories` + `Category.CreateSystem` + `CategoriesDataSeeder` (Order 20); slug-idempotent (IgnoreQueryFilters).

- [x] **2.3.6** Provider seed (başlangıç listesi)  
  **Açıklama:** Netflix, Spotify vb. TR/global; LogoUrl opsiyonel.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `SystemProviders` (28) + `Provider.CreateCatalog` + `ProvidersDataSeeder` (Order 30); LogoUrl null (self-host); slug-idempotent.

- [x] **2.3.7** Resource seed (TR/EN temel metinler)  
  **Açıklama:** Common, Category, Dashboard, Subscription, Error (paywall metinleri yok).  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `SystemResources` + `Resource.Create` + `ResourcesDataSeeder` (Order 40); Paywall/freemium yok; key-idempotent.

- [x] **2.3.8** Email template seed  
  **Açıklama:** ResetPassword, RenewalReminder, Invite (VerifyEmail yok).  
  **Öncelik:** P3 · **Tamamlandı:** 2026-07-22  
  **Not:** `SystemEmailTemplates` + `EmailTemplatesDataSeeder` (Order 60); 6 satır TR/EN; **SMTP send hâlâ Faz 15**.

- [x] **2.3.9** SystemSettings initial empty row  
  **Açıklama:** Singleton boş kayıt oluştur.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `SystemSettingsDataSeeder` (Order 50) + `CreateDefault()`; tablo boşsa 1 satır; secrets yok; `IsSetupComplete=false`.

- [x] **2.3.10** Seed sadece boş tabloya  
  **Açıklama:** Idempotent; ikinci start duplicate üretmesin.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Tüm seeder’lar key/table-empty stratejisi; `SeedIdempotencyTests` double-run; mevcut satır overwrite yok.

- [x] **2.3.11** Development connection string / docker-compose hizası  
  **Açıklama:** appsettings ile `docker/docker-compose.yaml` kullanıcı/şifre/db aynı.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `appsettings` + `appsettings.Development` + compose defaults + `.env.example` → `subify_db` / `subify_admin` / `SecretPassword123!` / `5432`; healthcheck; `docker/README.md`.

- [x] **2.3.12** Migration baseline gözden geçir  
  **Açıklama:** Rename/alan değişikliklerinden sonra yeni migration; gerekirse squash dokümantasyonu.  
  **Öncelik:** P0 · **Bağımlı:** 2.1.x, 2.2.x · **Tamamlandı:** 2026-07-22  
  **Not:** 11 migration; model drift yok; tip=`CompleteEntityTypeConfigurations`; squash **ertelendi** (v1.0); `Migrations/README.md` + `MigrationBaselineTests`.

### 2.4 ISubifyDbContext genişletme

- [x] **2.4.1** DbSet’leri interface’e taşı (gerekli olanlar)  
  **Açıklama:** Handler’lar concrete context’e bağımlı olmasın.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `ISubifyDbContext` → tüm OS `DbSet<>` + `Users` + `SaveChangesAsync`; `AddRefreshTokenAsync` korundu; `ISubifyDbContextContractTests`.

- [x] **2.4.2** Unit of Work / SaveChanges tek giriş  
  **Açıklama:** Handler sonunda tutarlı save.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `IUnitOfWork` + `ISubifyDbContext : IUnitOfWork`; `PrepareChangesForSave` tek pipeline; DI aynı scope instance; Persistence README.

---

# FAZ 3 — Auth, JWT, SuperAdmin, multi-user temel

### 3.1 JWT ve token servisi

- [x] **3.1.1** Access token üretimi  
  **Açıklama:** Sub, email, jti, roles, locale claims.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `AccessTokenClaimsFactory` + `AppClaimTypes`; jti UUID v7; locale normalize; iat/nbf/exp; `CurrentUserService` hizalı; claim roundtrip testleri.

- [x] **3.1.2** Refresh token üretimi + hash saklama  
  **Açıklama:** SHA256 hash DB; plain sadece response.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `RefreshTokenHasher` (SHA-256 hex); `RefreshTokenMaterial`; `JwtOptions.RefreshTokenExpirationDays` (7); Login yalnızca hash persist; lookup API `HashRefreshToken`.

- [x] **3.1.3** Refresh token rotation implementasyonu  
  **Açıklama:** Eski revoke + yeni token; reuse detection (`theft_detected`).  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `RefreshHandler` + `POST /api/auth/refresh`; rotate→`replaced`; reuse→`AUTH_016` + tüm session revoke; rotation tests.

- [x] **3.1.4** Token expiry config  
  **Açıklama:** Access (ör. 15–60 dk) ve refresh (ör. 7 gün) appsettings.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `JwtOptions` resolve/clamp (access 5–1440, refresh 1–90); appsettings + Development; `Authentication/README.md`; `JwtOptionsExpiryTests`.

- [x] **3.1.5** JWT validation clock skew  
  **Açıklama:** TokenValidationParameters.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** `ClockSkewSeconds` (default 30, max 300); `JwtTokenValidation.CreateParameters`; bearer uses resolved skew (ASP.NET 5dk default yok).

### 3.2 Auth endpoint’leri

- [x] **3.2.1** `POST /api/auth/register`  
  **Açıklama:** FullName, Email, Password; validation; duplicate email 409. Register’da `EmailConfirmed = true` (confirm yok).  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** User rolü atanır; SuperAdmin setup’ta (3.3/3S); FullName max 200; AUTH_008 Conflict; `RegisterHandlerTests`.

- [x] **3.2.2** `POST /api/auth/login`  
  **Açıklama:** Email/password; tokens; lockout. **EmailConfirmed kontrolü yapılmaz / her zaman geçer.**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Anti-enumeration AUTH_001; lockout 5/15dk AUTH_005; refresh hash persist; `LoginHandlerTests`.

- [x] **3.2.3** `POST /api/auth/refresh-token`  
  **Açıklama:** Body refreshToken → yeni access+refresh.  
  **Öncelik:** P0 · **Bağımlı:** 3.1.3 · **Tamamlandı:** 2026-07-22  
  **Not:** `POST /api/auth/refresh-token` (+ alias `/refresh`); `RefreshHandler` rotation; `.http` örnekleri; tests 3.1.3.

- [x] **3.2.4** `POST /api/auth/logout`  
  **Açıklama:** Refresh revoke; reason `logout`.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `{ refreshToken }` ve/veya `allSessions`; reason `logout`; idempotent.

- [-] **3.2.5** `GET /api/auth/confirm-email`  
  **Açıklama:** ~~userId + code; Identity confirm~~  
  **Durum:** **İptal** — e-posta confirm uygulama kapsamı dışında.

- [-] **3.2.6** `POST /api/auth/resend-confirmation`  
  **Açıklama:** ~~Rate limited confirm mail~~  
  **Durum:** **İptal** — e-posta gönderimi yok.

- [x] **3.2.7** `POST /api/auth/forgot-password`  
  **Açıklama:** Enumeration-safe; SMTP açıksa reset mail.  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01 · **Not:** Faz 15.2.1

- [x] **3.2.8** `POST /api/auth/reset-password` (mail token)  
  **Açıklama:** Email + code/token + newPassword.  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01 · **Not:** Faz 15.2.1

- [x] **3.2.9** EmailConfirmed / confirm engelini kaldır  
  **Açıklama:** Register’da `EmailConfirmed = true`. LoginHandler’daki `EmailNotConfirmed` kontrolünü **kaldır**.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Register zaten confirmed; Login’de confirm kontrolü yok; Identity `RequireConfirmedEmail=false`.

- [x] **3.2.10** Login response’a user özeti ekle  
  **Açıklama:** id, email, fullName, locale, roles (plan yok); opsiyonel `isSetupComplete`.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `LoginUserSummary` + roles + `isSetupComplete`.

- [x] **3.2.11** Register sonrası otomatik NotificationSettings satırı  
  **Açıklama:** defaults: `emailEnabled=false` (mail motoru yokken), `daysBeforeRenewal` in-app için.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `NotificationSetting.CreateDefaults`; email=false, days=3.

- [x] **3.2.12** Auth endpoint OpenAPI örnekleri / Produces düzelt  
  **Açıklama:** Status kodları doğru.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** Login/register/refresh/logout/change-password/admin-reset Produces + descriptions.

- [x] **3.2.13** Public registration flag  
  **Açıklama:** SystemSettings `AllowPublicRegistration` (setup’ta seçilir; env override opsiyonel). Setup tamamlanmadan public reg kapalı (sadece setup admin oluşturur).  
  **Öncelik:** P0 · **Bağımlı:** 3S.1, 3.3.1 · **Tamamlandı:** 2026-07-22  
  **Not:** Setup incomplete → sadece ilk kullanıcı; setup complete → `AllowPublicRegistration` zorunlu.

- [x] **3.2.14** `POST /api/auth/change-password` (oturum açık)  
  **Açıklama:** currentPassword + newPassword; kendi şifresini değiştirir.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Auth required; tüm refresh session revoke.

- [x] **3.2.15** `POST /api/admin/users/{id}/reset-password` (SuperAdmin)  
  **Açıklama:** Admin başka kullanıcının şifresini yeni şifre ile set eder (mail gerekmez — self-host unutma senaryosu).  
  **Öncelik:** P0 · **Bağımlı:** 3.3.3 · **Tamamlandı:** 2026-07-22  
  **Not:** Policy `RequireSuperAdmin`; target session revoke; mail yok.

### 3.3 SuperAdmin bootstrap ve roller

- [x] **3.3.1** İlk kullanıcı = SuperAdmin  
  **Açıklama:** Transaction + “herhangi SuperAdmin var mı?”; race-safe.  
  **Öncelik:** P0 · **Bağımlı:** 2.3.4 · **Tamamlandı:** 2026-07-22  
  **Not:** `SuperAdminBootstrap.TryAssignFirstSuperAdminAsync` + `POST /api/setup/admin`; concurrent demote → AUTH_019.

- [x] **3.3.2** Sonraki public register = User rolü  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 3.3.1 · **Tamamlandı:** 2026-07-22  
  **Not:** `RegisterHandler` her zaman `AppRoles.User` (setup complete + AllowPublicRegistration).

- [x] **3.3.3** Authorization policies  
  **Açıklama:** `RequireSuperAdmin`, `RequireAdminOrAbove`, `RequireAuthenticatedUser`.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `AuthPolicies` + DI `AddAuthorization(AuthPolicies.Configure)`.

- [x] **3.3.4** `[Authorize]` / `.RequireAuthorization()` endpoint’lerde  
  **Açıklama:** Auth public; diğerleri protected.  
  **Öncelik:** P0 · **Bağımlı:** 3.3.3 · **Tamamlandı:** 2026-07-22  
  **Not:** `FallbackPolicy = RequireAuthenticatedUser`; public: auth/setup/health/docs `AllowAnonymous`.

- [x] **3.3.5** SuperAdmin transfer (opsiyonel)  
  **Açıklama:** v1 dışı bırakılabilir; dokümante et.  
  **Öncelik:** P3 · **Tamamlandı:** 2026-07-22  
  **Not:** v1 dışı — `Authorization/README.md`; implement yok.

- [x] **3.3.6** İlk kullanıcı yalnızca Setup üzerinden  
  **Açıklama:** `IsSetupComplete == false` iken normal `/register` kapalı veya setup’a yönlendir; SuperAdmin sadece `POST /api/setup/admin`.  
  **Öncelik:** P0 · **Bağımlı:** 3S.2 · **Tamamlandı:** 2026-07-22  
  **Not:** Register → AUTH_017 SetupRequired; `GET /api/setup/status` + `POST /api/setup/admin`.

### 3.4 Identity güvenlik ayarları

- [x] **3.4.1** Password policy  
  **Açıklama:** Min 8, upper/lower/digit (mevcut); dokümante.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `IdentitySecurityDefaults` + `PasswordRuleBuilder` + Identity options; special char zorunlu değil.

- [x] **3.4.2** Lockout ayarları  
  **Açıklama:** Max failed attempts, lockout süresi; AUTH_005.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** 5 fail / 15 dk; `IdentityOptionsConfiguration`; login AUTH_005.

- [x] **3.4.3** Unique email enforce  
  **Açıklama:** Identity + DB.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `RequireUniqueEmail`; pre-check + Identity Duplicate* → AUTH_008; tests.

---

# FAZ 3S — First-run Setup Wizard (ilk ayağa kalkış)

> E-ticaret “kurulum sihirbazı” benzeri. Docker/API ilk açıldığında setup tamamlanmadıysa web kullanıcıyı setup’a alır.  
> **Akış:** Welcome → Super Admin → Instance defaults → (opsiyonel) ek kullanıcılar → (opsiyonel) SMTP → (opsiyonel) AI → Finish.

### 3S.1 Setup state & güvenlik

- [x] **3S.1.1** `IsSetupComplete` persistence  
  **Açıklama:** SystemSettings (veya ayrı `setup_state`) flag; seed sonrası default `false`.  
  **Öncelik:** P0 · **Bağımlı:** 2.1.5 · **Tamamlandı:** 2026-07-22  
  **Not:** `SystemSettings.CreateDefault` + seeder; `MarkSetupComplete`.

- [x] **3S.1.2** `GET /api/setup/status` (public)  
  **Açıklama:** `{ isSetupComplete, currentStep?, version }` — web yönlendirme için. Secret yok.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `canCreateAdmin`, `suggestedNextStep`, locale/currency, `hasSmtpConfigured`/`hasAiConfigured` (secret yok).

- [x] **3S.1.3** Setup endpoint’leri setup tamamlanınca kilit  
  **Açıklama:** `IsSetupComplete == true` iken `POST /api/setup/*` → 409/403.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Handler’lar `SETUP_001`; complete sonrası admin/instance/smtp/ai kilitli.

- [x] **3S.1.4** Setup tamamlanmadan app API’leri  
  **Açıklama:** Subscriptions vb. auth ister; setup incomplete iken login sadece SuperAdmin (ilk user) veya setup token — pratikte: setup bitmeden sadece setup + status.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `SetupGateMiddleware` — incomplete iken allowlist: setup/auth/health/docs; diğerleri `AUTH_017`.

- [x] **3S.1.5** Health/readiness’ta setup bilgisi (opsiyonel)  
  **Açıklama:** `GET /health` veya `/health/ready` → `setupRequired: true/false`.  
  **Öncelik:** P2 · **Bağımlı:** 1.2.7 · **Tamamlandı:** 2026-07-22  
  **Not:** `GET /health` → `setupRequired` (DB erişilebilirse).

### 3S.2 Adım 1 — Super Admin oluştur

- [x] **3S.2.1** `POST /api/setup/admin`  
  **Açıklama:** fullName, email, password → SuperAdmin + EmailConfirmed=true. Sadece `IsSetupComplete == false` ve henüz SuperAdmin yokken.  
  **Öncelik:** P0 · **Bağımlı:** 3.3.1 mantığı buraya taşınır/paylaşılır · **Tamamlandı:** 2026-07-22  
  **Not:** `CreateSetupAdmin` + `SuperAdminBootstrap`; race → AUTH_018/019.

- [x] **3S.2.2** Setup admin sonrası otomatik login token (opsiyonel)  
  **Açıklama:** Response’ta access+refresh; wizard devamı için oturum.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** Access + refresh (hash DB) wizard oturumu için.

### 3S.3 Adım 2 — Instance varsayılanları

- [x] **3S.3.1** `PUT /api/setup/instance`  
  **Açıklama:** `InstanceName`, `DefaultLocale` (tr/en), `DefaultCurrency` (TRY/USD/…), `TimeZoneId` (opsiyonel), `AllowPublicRegistration` (default false).  
  **Öncelik:** P0 · **Bağımlı:** SuperAdmin oturumu veya setup session · **Tamamlandı:** 2026-07-22  
  **Not:** SuperAdmin only; setup incomplete.

- [x] **3S.3.2** Theme default (opsiyonel)  
  **Açıklama:** Instance default accent / dark preference (kullanıcı profili sonra override eder).  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `DefaultApplicationThemeColor` + `DefaultDarkTheme`; setup instance + admin settings; applied on new users.

### 3S.4 Adım 3 — Ek kullanıcılar (opsiyonel, skip edilebilir)

- [x] **3S.4.1** Setup sırasında kullanıcı ekleme  
  **Açıklama:** `POST /api/setup/users` veya mevcut admin users API (setup auth ile). Email + temp password veya invite link response.  
  **Öncelik:** P1 · **Bağımlı:** 7.1.2 veya 7.2.1 · **Tamamlandı:** 2026-08-01  
  **Not:** `POST /api/setup/users` SuperAdmin + setup incomplete; User|Admin; wizard form + list.

- [x] **3S.4.2** Setup UI’da “Atla”  
  **Açıklama:** Kullanıcı eklemeden sonraki adıma geçiş.  
  **Öncelik:** P0 (web) · **Tamamlandı:** 2026-08-01

### 3S.5 Adım 4 — SMTP (opsiyonel, skip; gönderim Faz 15)

- [x] **3S.5.1** `PUT /api/setup/smtp`  
  **Açıklama:** Host, Port, User, Password, FromName, FromEmail, enabled flag. **Sadece kaydet**; test-send ve gerçek mail **Faz 15**.  
  **Öncelik:** P1 · **Bağımlı:** 2.1.5 · **Tamamlandı:** 2026-07-22  
  **Not:** Persist only; no send.

- [x] **3S.5.2** Setup SMTP “Atla”  
  **Açıklama:**  
  **Öncelik:** P0 (web) · **Tamamlandı:** 2026-08-01

- [x] **3S.5.3** Admin Settings’ten SMTP sonradan düzenleme  
  **Açıklama:** Setup sonrası `PUT /api/admin/settings` ile SMTP alanları.  
  **Öncelik:** P1 · **Bağımlı:** 7.3.2 · **Tamamlandı:** 2026-07-31 · **Not:** Admin settings SMTP form.

### 3S.6 Adım 5 — AI (opsiyonel, skip)

- [x] **3S.6.1** `PUT /api/setup/ai`  
  **Açıklama:** Provider (OpenAI / compatible), API key, model (opsiyonel). Secret mask.  
  **Öncelik:** P1 · **Bağımlı:** 2.1.5 · **Tamamlandı:** 2026-07-22  
  **Not:** BYOK key stored; status only exposes `hasAiConfigured`.

- [x] **3S.6.2** Setup AI “Atla”  
  **Açıklama:** AI key yoksa AI endpoint’ler `AI_KEY_MISSING`.  
  **Öncelik:** P0 (web) · **Tamamlandı:** 2026-08-01

- [x] **3S.6.3** Setup sırasında AI test (opsiyonel)  
  **Açıklama:** Mini ping; yoksa Faz 7.3.4 / 9.x.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01 · **Not:** Wizard AI step → test-ai after save key.

### 3S.7 Adım 6 — Finish

- [x] **3S.7.1** `POST /api/setup/complete`  
  **Açıklama:** Validasyon: SuperAdmin var mı? → `IsSetupComplete = true`. Idempotent değil (tekrar 409).  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** SuperAdmin required; tekrar → SETUP_001.

- [x] **3S.7.2** Setup complete sonrası yönlendirme  
  **Açıklama:** Web → login veya dashboard.  
  **Öncelik:** P0 (web) · **Tamamlandı:** 2026-08-01 · **Not:** Finish → `/dashboard`.

### 3S.8 Setup Web UI

- [x] **3S.8.1** Setup layout (wizard steps indicator)  
  **Açıklama:** Manifesto light/dark; adım çubuğu.  
  **Öncelik:** P0 · **Bağımlı:** 10.1.x · **Tamamlandı:** 2026-08-01  
  **Not:** `WizardSteps` + `/setup` page.

- [x] **3S.8.2** Step: Welcome  
  **Açıklama:** Subify OS tanıtım, dil seçimi (opsiyonel).  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.3** Step: Create Super Admin form  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 3S.2.1 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.4** Step: Instance defaults form  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 3S.3.1 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.5** Step: Add users (skip)  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01 · **Not:** Skip; add after complete in Admin.

- [x] **3S.8.6** Step: SMTP config (skip)  
  **Açıklama:** “E-posta gönderimi sonraki sürümde; ayarları şimdiden kaydedebilirsiniz.”  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.7** Step: AI config (skip)  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.8** Step: Finish / success  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 3S.7.1 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.9** Root redirect: setupRequired → `/setup`  
  **Açıklama:** `GET /api/setup/status` ile; complete ise app’e.  
  **Öncelik:** P0 · **Bağımlı:** 3S.1.2, 10.1.5 · **Tamamlandı:** 2026-08-01

- [x] **3S.8.10** Setup tamamlanmışken `/setup` engeli  
  **Açıklama:** Login’e yönlendir.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-08-01

---

# FAZ 4 — Subscription CRUD ve finansal motor

### 4.1 Application layer — Subscription features

- [x] **4.1.1** CreateSubscription command/handler/validator  
  **Açıklama:** Name, price>0, currency, cycle, share≥1, category XOR, provider optional, nextRenewal. **Limit yok.**  
  **Öncelik:** P0 · **Bağımlı:** 1.2.12, 3.3.4 · **Tamamlandı:** 2026-07-22  
  **Not:** `Features/Subscriptions/CreateSubscription/*`; current user; provider/category/userCategory ref check; response includes userShare.

- [x] **4.1.2** Create sonrası ActivityLog  
  **Açıklama:** `subscription.created`.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** Create UoW içinde `ActivityLog` + `NewValues` snapshot; `ActivityLogConstants`.
- [x] **4.1.3** GetSubscriptionById query  
  **Açıklama:** Ownership check; 404/403.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `IgnoreQueryFilters` (archived ok); foreign → SUB_002; missing → SUB_001; shared `SubscriptionResponse`.

- [x] **4.1.4** ListSubscriptions query  
  **Açıklama:** includeArchived, category filter, pagination, search.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** slug/id/userCategory filter; search name+notes; page/pageSize; summary → 4.1.5.

- [x] **4.1.5** List response summary  
  **Açıklama:** monthlyTotal, yearlyTotal, currency (mainCurrency).  
  **Öncelik:** P0 · **Bağımlı:** 4.3.x · **Tamamlandı:** 2026-07-22  
  **Not:** `SubscriptionListSummary`; aktif+filtre; MainCurrency only (FX → 4.3.4); `SubscriptionMath`.

- [x] **4.1.6** UpdateSubscription command  
  **Açıklama:** Ownership; old/new values activity.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `subscription.updated` + OldValues/NewValues snapshot; provider/category ref check.

- [x] **4.1.7** ArchiveSubscription (DELETE soft)  
  **Açıklama:** Archived=true; activity archived.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `Archive()` + DeletedAt; `subscription.archived`; idempotent second call.

- [x] **4.1.8** ReactivateSubscription (opsiyonel endpoint)  
  **Açıklama:** Archive geri alma.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** `Reactivate()`; `subscription.reactivated`; aktifken idempotent (activity yok).

- [x] **4.1.9** UpcomingSubscriptions query  
  **Açıklama:** `days` query; daysUntilRenewal; overdue ayrı işaret.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** aktif only; overdue + window; `IsOverdue`/`IsUpcoming`; total MainCurrency.

- [x] **4.1.10** DTO’lar (SubscriptionResponse vb.)  
  **Açıklama:** userShare, category, provider nested.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `SubscriptionCategoryRef` / `SubscriptionProviderRef`; IncludeDetails on get/list/create/update/archive/reactivate.

- [x] **4.1.11** Provider aktif değilse create reject  
  **Açıklama:** SUB provider not active.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `SubscriptionReferenceValidator` → null/inactive → `SUB_006`; create + update.

- [x] **4.1.12** Category / UserCategory varlık ve ownership doğrulama  
  **Açıklama:** UserCategory başka kullanıcıya ait olamaz.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** System cat missing/inactive → `SUB_008`; foreign user cat → `SUB_002`; create+update via `SubscriptionReferenceValidator`.

### 4.2 API endpoints — Subscriptions

- [x] **4.2.1** `GET /api/subscriptions`  
  **Açıklama:**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** List + filters + pagination + summary.

- [x] **4.2.2** `GET /api/subscriptions/{id}`  
  **Açıklama:**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22

- [x] **4.2.3** `POST /api/subscriptions`  
  **Açıklama:**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** 201 Created + Location.

- [x] **4.2.4** `PUT /api/subscriptions/{id}`  
  **Açıklama:**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22

- [x] **4.2.5** `DELETE /api/subscriptions/{id}`  
  **Açıklama:** Soft archive.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22

- [x] **4.2.6** `GET /api/subscriptions/upcoming`  
  **Açıklama:**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** + `POST /{id}/reactivate` (4.1.8).

### 4.3 Finansal hesaplama

- [x] **4.3.1** UserShare pure function / domain property  
  **Açıklama:** `Price / SharedWithCount`.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Entity property + `SubscriptionMath.UserShare`.

- [x] **4.3.2** MonthlyEquivalent / YearlyEquivalent  
  **Açıklama:** monthly as-is; yearly/12 ve tersi.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Entity + `SubscriptionMath.Monthly/YearlyEquivalent`.

- [x] **4.3.3** DashboardTotals service  
  **Açıklama:** Aktif non-archived toplamları.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** `SubscriptionMath.SumInCurrency` (+ list summary kullanımı).

- [x] **4.3.4** Multi-currency convert (basit)  
  **Açıklama:** Snapshot rate ile mainCurrency’ye çevir; rate yoksa orijinal + warning.  
  **Öncelik:** P1 · **Bağımlı:** 6.2.x · **Tamamlandı:** 2026-07-22  
  **Not:** `CurrencyConversion` + `IExchangeRateLookup`; list/upcoming summary convert; no rate → warning, excluded from main total.

- [x] **4.3.5** Budget exceeded flag  
  **Açıklama:** monthlyTotal > monthlyBudget → response flag.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `BudgetRules.IsExceeded`; list summary `monthlyBudget` + `isBudgetExceeded`.

- [x] **4.3.6** Unit testler finansal motor  
  **Açıklama:** share, monthly/yearly, budget.  
  **Öncelik:** P1 · **Bağımlı:** 12.1.x · **Tamamlandı:** 2026-07-22  
  **Not:** `FinancialMotorTests` (pipeline) + `SubscriptionMathTests` / `CurrencyConversionTests` / `BudgetRulesTests` / entity `SubscriptionTests`.

---

# FAZ 5 — Categories, providers, profile, activity

### 5.1 Categories

- [x] **5.1.1** `GET /api/categories`  
  **Açıklama:** Sistem kategorileri; Accept-Language veya user locale ile name.  
  **Öncelik:** P0 · **Bağımlı:** 2.3.5 · **Tamamlandı:** 2026-07-22  
  **Not:** `GetSystemCategories` + `LocaleResolver` + `ICategoryNameLookup`; `?locale=` override.

- [x] **5.1.2** `GET /api/categories/user`  
  **Açıklama:** Kullanıcının özel kategorileri.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Own only; soft-deleted excluded; ordered by name.

- [x] **5.1.3** `POST /api/categories/user`  
  **Açıklama:** name, icon, color.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Duplicate name (case-insensitive) → UCAT_004; 201 + Location.

- [x] **5.1.4** `PUT /api/categories/user/{id}`  
  **Açıklama:** Ownership.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** UCAT_001/002/004; same-name self ok.

- [x] **5.1.5** `DELETE /api/categories/user/{id}`  
  **Açıklama:** Aktif subscription varsa conflict.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** Soft-delete; active (non-archived) sub → UCAT_003; archived-only OK.

- [x] **5.1.6** Category name resource lookup helper  
  **Açıklama:** slug → localized name; fallback slug.  
  **Öncelik:** P1 · **Bağımlı:** 6.3.x · **Tamamlandı:** 2026-07-22  
  **Not:** `CategoryNameLookup` (Resource Page=Category); missing → slug.

### 5.2 Providers

- [x] **5.2.1** `GET /api/providers`  
  **Açıklama:** isActive=true; search query opsiyonel.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.6 · **Tamamlandı:** 2026-07-22  
  **Not:** search name/slug; optional region (+ GLOBAL); order by name.

- [x] **5.2.2** `GET /api/providers/{id}`  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** Active detail; missing/soft-deleted → PROV_001.

- [x] **5.2.3** Admin provider CRUD (opsiyonel v1)  
  **Açıklama:** SuperAdmin manage catalog.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** POST/PUT/DELETE `/api/admin/providers`; soft-delete; active sub → PROV_005.

### 5.3 Profile

- [x] **5.3.1** `GET /api/profile`  
  **Açıklama:** Tercihler + email.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** No plan fields; includes roles, budget, theme.

- [x] **5.3.2** `PUT /api/profile`  
  **Açıklama:** fullName, locale, mainCurrency, budget, theme, darkTheme.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-22  
  **Not:** Returns full ProfileResponse; null monthlyBudget clears budget.

- [x] **5.3.3** Theme color whitelist validation  
  **Açıklama:** Preset listesi.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `ThemeColors.IsSupported` → PRO_004.

- [x] **5.3.4** Currency validation (ISO 4217 basit set)  
  **Açıklama:** TRY, USD, EUR, GBP…  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `SupportedCurrencies` → PRO_003.

- [x] **5.3.5** `PUT /api/profile/notifications`  
  **Açıklama:** `daysBeforeRenewal` (in-app uyarı için). `emailEnabled` gerekmez veya her zaman false — **mail gönderimi yok**.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** days 0–30; email always false; GET companion; creates row if missing.

- [x] **5.3.6** Profile update activity log  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** `profile.updated` + OldValues/NewValues snapshot on PUT /api/profile.

### 5.4 Activity

- [x] **5.4.1** ActivityLog writer service  
  **Açıklama:** Merkezi `IActivityLogger`.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** `IActivityLogger` / `ActivityLogger`; IP+UA; LogAsync + LogAndSaveAsync; used by subs + profile.

- [x] **5.4.2** `GET /api/activity`  
  **Açıklama:** Pagination, entityType filter; sadece kendi logları.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-22  
  **Not:** Own only; newest first; entityType case-insensitive; page/pageSize.

- [x] **5.4.3** Login/logout activity (opsiyonel)  
  **Açıklama:** auth entity.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-22  
  **Not:** `Auth` / `auth.login` / `auth.logout` via IActivityLogger; failed login not logged.

---

# FAZ 6 — Reports, döviz, resources

### 6.1 Reports

- [x] **6.1.1** `GET /api/reports/monthly-spend`  
  **Açıklama:** Son N ay; premium yok.  
  **Öncelik:** P1 · **Bağımlı:** 4.x · **Tamamlandı:** 2026-07-31  
  **Not:** months 1–36 (default 12); MainCurrency / `?currency=`; FX snapshot; history via CreatedAt/archive.

- [x] **6.1.2** `GET /api/reports/category-breakdown`  
  **Açıklama:** total, percentage, count, color.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** Active only; system slug + localized name; user:`id`; uncategorized bucket.

- [x] **6.1.3** `GET /api/reports/currency-distribution`  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-31  
  **Not:** Group by original currency; converted totals/percentage in MainCurrency.

- [x] **6.1.4** Yetersiz veri empty-state response  
  **Açıklama:** Boş array + message; crash yok.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** `data: []` + REP_002 description message; HTTP 200 (no crash).

### 6.2 Exchange rates

- [x] **6.2.1** Exchange rate provider abstraction  
  **Açıklama:** `IExchangeRateClient` (HTTP).  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** `HttpExchangeRateClient` — OpenErApi (no key) / ExchangeRateApi (key).

- [x] **6.2.2** Snapshot persist  
  **Açıklama:** Background veya on-demand fetch → DB.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** `ExchangeRateSyncService` → `ExchangeRateSnapshots` (supported pairs only).

- [x] **6.2.3** `GET /api/exchange-rates?base=`  
  **Açıklama:** Son snapshot / cache.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** Auth; default MainCurrency; IMemoryCache; empty DB → one on-demand sync.

- [x] **6.2.4** Background sync job (saatlik)  
  **Açıklama:** HostedService; API key env.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-31  
  **Not:** `ExchangeRateSyncBackgroundService`; `EXCHANGE_RATE_API_KEY` / `ExchangeRates:*`.

- [x] **6.2.5** Fallback last-known rate  
  **Açıklama:** API down.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** Sync keeps prior rows; GET serves DB; fail only if no snapshot at all.

### 6.3 Resources / i18n API

- [x] **6.3.1** `GET /api/resources?lang=&since=`  
  **Açıklama:** Delta sync.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.7 · **Tamamlandı:** 2026-07-31  
  **Not:** Auth; lang resolve; since → delta; empty delta → HTTP 304.

- [x] **6.3.2** Resource cache (memory)  
  **Açıklama:** Redis zorunlu değil; IMemoryCache.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-31  
  **Not:** Full pack key `resources:full:{lang}` TTL 1h; admin writes invalidate.

- [x] **6.3.3** Admin resource CRUD (opsiyonel)  
  **Açıklama:**  
  **Öncelik:** P3 · **Tamamlandı:** 2026-07-31  
  **Not:** SuperAdmin `/api/admin/resources` list/create/update/delete.

---

# FAZ 7 — Admin: users, invites, SystemSettings API

### 7.1 Users admin

- [x] **7.1.1** `GET /api/admin/users`  
  **Açıklama:** Sayfalı liste, arama; SuperAdmin/Admin.  
  **Öncelik:** P0 · **Bağımlı:** 3.3.3 · **Tamamlandı:** 2026-07-31  
  **Not:** search email/fullName; page/pageSize; activeSubscriptionCount only.

- [x] **7.1.2** `POST /api/admin/users`  
  **Açıklama:** Manuel kullanıcı oluştur (email, temp password veya force change).  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-31  
  **Not:** role User|Admin (Admin requires SuperAdmin caller); never SuperAdmin.

- [x] **7.1.3** `PATCH /api/admin/users/{id}`  
  **Açıklama:** Lock/unlock, rol Admin/User (SuperAdmin korunur).  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** SuperAdmin-only patch; SuperAdmin targets protected (USER_004).

- [x] **7.1.4** Admin başka kullanıcının subscription’ını **görmez** (v1)  
  **Açıklama:** Explicit non-goal enforce; test.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-31  
  **Not:** ListSubscriptions always current-user; admin list exposes count only.

- [x] **7.1.5** Soft disable user  
  **Açıklama:** Login engeli.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** `IsDisabled`/`DisabledAt` + migration; login USER_006; sessions revoked.

### 7.2 Invites

- [x] **7.2.1** `POST /api/admin/invites`  
  **Açıklama:** Email + expiry; token üret; **response’ta invite link/token** (mail yok — admin kopyalar).  
  **Öncelik:** P1 · **Bağımlı:** 2.1.7 · **Tamamlandı:** 2026-07-31  
  **Not:** Admin+; plain token + inviteUrl once; hash stored; supersedes prior pending for email.

- [x] **7.2.2** `GET /api/admin/invites`  
  **Açıklama:** Pending list.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-31  
  **Not:** Default pending only; `includeExpired`; never returns plain token.

- [x] **7.2.3** `POST /api/auth/accept-invite`  
  **Açıklama:** Token + password + fullName → User.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** Public; works when public reg off; assigns User; setup must be complete.

- [x] **7.2.4** Invite e-posta gönderimi  
  **Açıklama:** SMTP doluysa mail; değilse sadece link (zaten response’ta).  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01 · **Not:** Faz 15.2.2

- [x] **7.2.5** Invite single-use + expiry enforce  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-07-31  
  **Not:** TryMarkUsed + IsPending; used/expired → AUTH_015.

### 7.3 SystemSettings API

- [x] **7.3.1** `GET /api/admin/settings`  
  **Açıklama:** Instance + AI + SMTP (secret maskeli: AI key, SMTP password).  
  **Öncelik:** P0 · **Bağımlı:** 2.1.5, 3.3.3 · **Tamamlandı:** 2026-07-31  
  **Not:** SuperAdmin; `hasApiKey`/`hasPassword` + `••••••••` mask; plain secrets never returned.

- [x] **7.3.2** `PUT /api/admin/settings`  
  **Açıklama:** Instance defaults, AI, SMTP partial update (boş secret = değiştirme).  
  **Öncelik:** P0 · **Tamamlandı:** 2026-07-31  
  **Not:** null/omit = keep; empty string = clear; non-empty = set.

- [x] **7.3.3** `POST /api/admin/settings/test-smtp`  
  **Açıklama:** Test mail SuperAdmin adresine.  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01 · **Not:** Faz 15.3.3

- [x] **7.3.4** `POST /api/admin/settings/test-ai`  
  **Açıklama:** Minimal model ping.  
  **Öncelik:** P2 · **Bağımlı:** 9.x · **Tamamlandı:** 2026-08-01  
  **Not:** SuperAdmin; BYOK resolve → tiny chat; returns model/latency/preview; web Test AI button.

- [x] **7.3.5** Settings change audit log  
  **Açıklama:** Secret değer loglanmaz.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-07-31  
  **Not:** `settings.updated` activity; snapshot flags only (HasAiApiKey/HasSmtpPassword).

### 7.4 Email templates admin

- [x] **7.4.1** List/get/update email templates  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01  
  **Not:** `GET/PUT /api/admin/email-templates`; SuperAdmin; subject/body only.

- [x] **7.4.2** Template preview / test send  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01  
  **Not:** `POST …/preview`, `POST …/test-send`; web `/admin/email-templates`.

### 7.5 Admin şifre reset UI/API köprüsü

- [x] **7.5.1** Admin users tablosunda “Şifre sıfırla”  
  **Açıklama:** Yeni şifre girişi; `3.2.15` çağrısı.  
  **Öncelik:** P0 · **Bağımlı:** 3.2.15, 10.9.1 · **Tamamlandı:** 2026-07-31  
  **Not:** API `POST /api/admin/users/{id}/reset-password` on Admin · Users; sessions revoked; lockout cleared; audit without password. UI table action → Faz 10.9.1.

---

# FAZ 8 — Background jobs (FX + mail)

> FX: 8.4 + 6.2.4. Mail renewal: 8.1–8.3 implemente (**Faz 15.3** ile aynı kod).  
> UI upcoming hâlâ dashboard’da; e-posta job SMTP + kullanıcı `EmailEnabled` ister.

### 8.1 Renewal reminder e-posta job

- [x] **8.1.1** `RenewalReminderBackgroundService`  
  **Açıklama:** Periyodik tarama; `daysBeforeRenewal` penceresi + `EmailEnabled`.  
  **Öncelik:** P1 · **Bağımlı:** 15.1, 4.x, 5.3.5 · **Tamamlandı:** 2026-08-01  
  **Not:** `EmailJobs:RenewalReminderInterval` (default `6h`); logic `IRenewalReminderService`.

- [x] **8.1.2** SMTP kapalıyken no-op  
  **Açıklama:** Job çökmez; debug log.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `IsConfiguredAsync` false → return 0.

- [x] **8.1.3** Manuel tetik (ops)  
  **Açıklama:** SuperAdmin `POST /api/admin/jobs/renewal-reminders/run`.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `{ processedCount }`; aynı dedupe kuralları.

### 8.2 Duplicate send koruması

- [x] **8.2.1** Yenileme başına tek mail  
  **Açıklama:** Aynı abonelik + `nextRenewalDate` için tekrar gönderme.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01 · **Not:** 15.3.2 `EmailSendLog.DedupeKey`.

### 8.3 Mail job güvenilirliği / izolasyon

- [x] **8.3.1** Iterasyon hata izolasyonu  
  **Açıklama:** Bir send/iteration fail host loop’u öldürmesin.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `IsolatedPeriodicBackgroundService` (8.4.3 ile paylaşılır).

- [x] **8.3.2** Master switch  
  **Açıklama:** `BackgroundJobs:Enabled` ve `EmailJobs:RenewalRemindersEnabled`.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01

### 8.4 Background host (non-mail jobs)

- [x] **8.4.1** HostedService vs Hangfire kararı implement  
  **Açıklama:** v1 için `BackgroundService` (ör. FX sync).  
  **Öncelik:** P2 · **Bağımlı:** 6.2.4 (opsiyonel) · **Tamamlandı:** 2026-08-01  
  **Not:** `IsolatedPeriodicBackgroundService`; Hangfire deferred; see `Infrastructure/Background/README.md`.

- [x] **8.4.2** Job schedule configuration  
  **Açıklama:** Cron benzeri env (ör. FX hourly).  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `IntervalParser` (`1h`/`30m`/`90s`); `ExchangeRates:SyncInterval` / `BACKGROUND_FX_INTERVAL`; `BackgroundJobs:Enabled`.

- [x] **8.4.3** Job hata izolasyonu  
  **Açıklama:** Bir iterasyon fail tüm job’u öldürmesin.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** Base catch+continue; FX per-base try/catch in `SyncAllAsync`.

---

# FAZ 9 — AI (BYOK)

### 9.1 AI altyapı

- [x] **9.1.1** `IAiClient` OpenAI-compatible  
  **Açıklama:** Chat completions HTTP.  
  **Öncelik:** P2 · **Bağımlı:** 7.3.2 · **Tamamlandı:** 2026-08-01  
  **Not:** `OpenAiCompatibleClient` → `/chat/completions` + `json_object`.

- [x] **9.1.2** Key SystemSettings’ten resolve  
  **Açıklama:** Yoksa `AI_KEY_MISSING` anlamlı hata.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `AiSettingsResolver`; model/baseUrl defaults from `Ai:*` config.

- [x] **9.1.3** Prompt builder (server-side)  
  **Açıklama:** Kullanıcı abonelik özeti; PII minimize.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `AiPromptBuilder` — no email; names + financial fields only.

- [x] **9.1.4** Response parse → tips DTO  
  **Açıklama:** unused, duplicate, yearly, general + savings.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `AiResponseParser` (+ markdown fence strip).

### 9.2 AI endpoints

- [x] **9.2.1** `POST /api/ai/analyze`  
  **Açıklama:** Auth user; rate limit; log request/response.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01

- [x] **9.2.2** `GET /api/ai/history`  
  **Açıklama:** Pagination.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01

- [x] **9.2.3** Insufficient data (<1 subscription)  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01 · **Not:** AI_006

- [x] **9.2.4** AI rate limit (5/min, 20/day öneri)  
  **Açıklama:** Stabilite; plan değil.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** ASP.NET Ai policy 5/min + app daily via log count (`Ai:DailyLimit`).

- [x] **9.2.5** AiSuggestionLog persist  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01

- [x] **9.2.6** Activity log ai_suggestion  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01 · **Not:** `ai.analyze` on `AiSuggestion`.

---

# FAZ 10 — Web (Next.js)

### 10.1 Web foundation

- [x] **10.1.1** Next.js App Router + TS + Tailwind iskelet  
  **Tamamlandı:** 2026-08-01 · Next 16 + Tailwind 4

- [x] **10.1.2** Design tokens (manifesto colors)  
  **Tamamlandı:** 2026-08-01 · CSS vars light/dark (primary violet, success/warning/danger)

- [x] **10.1.3** Dark mode (`class` strategy)  
  **Tamamlandı:** 2026-08-01 · system + user toggle

- [x] **10.1.4** Inter font  
  **Tamamlandı:** 2026-08-01 · next/font

- [x] **10.1.5** API client (fetch/axios) + base URL env  
  **Tamamlandı:** 2026-08-01 · `lib/api/client.ts` + `NEXT_PUBLIC_API_URL`

- [x] **10.1.6** Auth token storage stratejisi  
  **Tamamlandı:** 2026-08-01 · sessionStorage access+refresh (documented in web/README)

- [x] **10.1.7** Auth context / session provider  
  **Tamamlandı:** 2026-08-01 · AuthProvider + refresh-on-401

- [x] **10.1.8** Protected route middleware/layout  
  **Tamamlandı:** 2026-08-01 · AppShell client guard → /login

- [x] **10.1.9** shadcn/ui veya temel component set  
  **Tamamlandı:** 2026-08-01 · Button/Input/Card/Badge/Toast(sonner)/Empty/Spinner

- [x] **10.1.10** i18n (TR/EN) web  
  **Tamamlandı:** 2026-08-01 · lightweight messages + locale switcher

- [x] **10.1.11** Error toast / ProblemDetails handler  
  **Tamamlandı:** 2026-08-01 · ApiError + sonner

- [x] **10.1.12** Loading ve empty states  
  **Tamamlandı:** 2026-08-01 · PageLoader + EmptyState

### 10.2 Auth sayfaları

- [x] **10.2.1** Login sayfası  
  **Açıklama:** Setup incomplete ise `/setup`’a yönlendir.  
  **Öncelik:** P0 · **Bağımlı:** 3.2.2, 10.1.5, 3S.1.2

- [x] **10.2.2** Register sayfası (public; setup sonrası flag açıksa)  
  **Açıklama:** İlk kullanıcı **setup wizard** ile; public reg kapalıysa CTA yok.  
  **Öncelik:** P1 · **Bağımlı:** 3.2.13

- [x] **10.2.3** Change password sayfası/modal (oturum içi)  
  **Açıklama:** Profile veya settings; `3.2.14`.  
  **Öncelik:** P0 · **Bağımlı:** 3.2.14

- [x] **10.2.3b** Forgot password sayfaları  
  **Açıklama:** “Şifremi unuttum” + e-posta token reset UI. SMTP yoksa bilgilendirme.  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01 · **Not:** Faz 15.4.1

- [x] **10.2.4** Accept invite sayfası  
  **Açıklama:** Token query/path ile; mail gerekmez (link paylaşımı manuel).  
  **Öncelik:** P1 · **Bağımlı:** 7.2.3

- [x] **10.2.5** Logout  
  **Açıklama:**  
  **Öncelik:** P0

### 10.3 App shell

- [x] **10.3.1** App layout (sidebar/topnav)  
  **Açıklama:** Dashboard, Subscriptions, Reports, AI, Profile, Admin.  
  **Öncelik:** P0

- [x] **10.3.2** Responsive mobile nav  
  **Açıklama:**  
  **Öncelik:** P1

- [x] **10.3.3** Theme toggle  
  **Açıklama:**  
  **Öncelik:** P1

- [x] **10.3.4** User menu (email, logout)  
  **Açıklama:**  
  **Öncelik:** P0

### 10.4 Dashboard UI

- [x] **10.4.1** Summary cards (monthly/yearly)  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 4.2.1

- [x] **10.4.2** Budget progress bar  
  **Açıklama:**  
  **Öncelik:** P1

- [x] **10.4.3** Upcoming payments list  
  **Açıklama:**  
  **Öncelik:** P0

- [x] **10.4.4** Recent activity list  
  **Açıklama:**  
  **Öncelik:** P1 · **Bağımlı:** 5.4.2

- [x] **10.4.5** Budget exceeded warning UI  
  **Açıklama:**  
  **Öncelik:** P1

### 10.5 Subscriptions UI

- [x] **10.5.1** Subscription list/grid  
  **Açıklama:**  
  **Öncelik:** P0

- [x] **10.5.2** Card states: Yakında / Gecikmiş / Normal  
  **Açıklama:** Manifesto border + badge + dark amber glow.  
  **Öncelik:** P0

- [x] **10.5.3** Create subscription form/modal  
  **Açıklama:** Provider autocomplete, category, share, dates.  
  **Öncelik:** P0

- [x] **10.5.4** Edit subscription  
  **Açıklama:**  
  **Öncelik:** P0

- [x] **10.5.5** Archive confirmation  
  **Açıklama:**  
  **Öncelik:** P0

- [x] **10.5.6** Filters (category, archived, search)  
  **Açıklama:**  
  **Öncelik:** P1

- [x] **10.5.7** UserShare display  
  **Açıklama:** “Sizin payınız”.  
  **Öncelik:** P0

### 10.6 Reports UI

- [x] **10.6.1** Category breakdown chart  
  **Açıklama:**  
  **Öncelik:** P1 · **Bağımlı:** 6.1.2

- [x] **10.6.2** Monthly spend chart  
  **Açıklama:**  
  **Öncelik:** P1

- [x] **10.6.3** Empty/error states  
  **Açıklama:**  
  **Öncelik:** P1

### 10.7 AI UI

- [x] **10.7.1** Analyze CTA + loading  
  **Açıklama:**  
  **Öncelik:** P2 · **Bağımlı:** 9.2.1

- [x] **10.7.2** Tips cards  
  **Açıklama:**  
  **Öncelik:** P2

- [x] **10.7.3** Key missing admin guidance message  
  **Açıklama:** “SuperAdmin AI key girmeli”.  
  **Öncelik:** P2

- [x] **10.7.4** History list  
  **Açıklama:**  
  **Öncelik:** P2

### 10.8 Profile UI

- [x] **10.8.1** Profile form  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 5.3.x

- [x] **10.8.2** Notification preferences form  
  **Açıklama:** In-app tercihler (ör. days before renewal). **E-posta toggle yok / disabled.**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** Profile card → GET/PUT `/api/profile/notifications`; days 0–30, push toggle, email disabled (Faz 15).

- [x] **10.8.3** Theme color picker  
  **Açıklama:**  
  **Öncelik:** P1

### 10.9 Admin UI

- [x] **10.9.1** Users table  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 7.1.x · Sadece SuperAdmin/Admin

- [x] **10.9.2** Create user / invite UI  
  **Açıklama:**  
  **Öncelik:** P1

- [x] **10.9.3** SystemSettings form (Instance + SMTP + AI)  
  **Açıklama:** Instance name/locale/currency; SMTP alanları (kayıt); AI key (maskeli); test-AI. Test-SMTP → Faz 15.  
  **Öncelik:** P0 · **Bağımlı:** 7.3.x

- [x] **10.9.4** Admin nav visibility by role  
  **Açıklama:**  
  **Öncelik:** P0

### 10.10 Landing (opsiyonel)

- [x] **10.10.1** Minimal self-host landing  
  **Açıklama:** Login/Register CTA.  
  **Öncelik:** P2

---

# FAZ 11 — Docker, release, ops

### 11.1 Docker artifacts

- [x] **11.1.1** API Dockerfile  
  **Açıklama:** multi-stage build, non-root opsiyonel.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.2 · **Tamamlandı:** 2026-08-01  
  **Not:** `api/Subify.Api/Dockerfile` — sdk→aspnet, non-root, curl healthcheck.

- [x] **11.1.2** Web Dockerfile  
  **Açıklama:** Next standalone output önerilir.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `web/Dockerfile` + `output: "standalone"`; `NEXT_PUBLIC_API_URL` build-arg.

- [x] **11.1.3** docker-compose full stack  
  **Açıklama:** postgres + api + web; volume; env sample.  
  **Öncelik:** P0 · **Bağımlı:** 11.1.1, 11.1.2 · **Tamamlandı:** 2026-08-01  
  **Not:** `docker/docker-compose.yaml`; DB-only: `docker-compose.db.yaml`.

- [x] **11.1.4** `.env.example`  
  **Açıklama:** Connection string, JWT secret, URLs, flags.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-08-01  
  **Not:** `docker/.env.example` + root `.env.example`.

- [x] **11.1.5** Reverse proxy örneği (Caddy/Nginx)  
  **Açıklama:** `/` → web, `/api` → api; TLS notları.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `docker/Caddyfile`, `docker/nginx.conf.example`.

- [x] **11.1.6** Healthcheck compose  
  **Açıklama:** api `/health`.  
  **Öncelik:** P1 · **Bağımlı:** 1.2.7 · **Tamamlandı:** 2026-08-01  
  **Not:** postgres healthy → api health → web health.

- [x] **11.1.7** Auto-migrate compose path doğrula  
  **Açıklama:** Cold start empty volume.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-08-01  
  **Not:** API `DatabaseInitializer` on start (retry until Postgres ready).

### 11.2 Ops docs

- [x] **11.2.1** README install (one command)  
  **Açıklama:**  
  **Öncelik:** P0 · **Tamamlandı:** 2026-08-01 · **Not:** root README + docker/README.

- [x] **11.2.2** Backup/restore Postgres prosedürü  
  **Açıklama:** pg_dump örnekleri.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01 · **Not:** `docs/OPS.md`.

- [x] **11.2.3** Upgrade / migration notları  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01 · **Not:** `docs/OPS.md`.

- [x] **11.2.4** Troubleshooting (port, JWT, setup, AI key; SMTP Faz 15)  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01 · **Not:** `docs/OPS.md`.

---

# FAZ 12 — Testler

> **Durum (2026-08-01):** Backend unit + integration yeşil.  
> Çalıştır: `dotnet test api/Subify.slnx`  
> (~91 Domain + ~16 Application + ~272 Api incl. Integration).  
> Web E2E: Playwright scaffold (P2 smoke; stack gerekir).  
> **Not:** Minimal API `bool` query param zorunlu sayılıyordu → `bool?` + `?? false`
> (`includeArchived`, `includeExpired`).

### 12.1 Backend unit

- [x] **12.1.1** Test projesi `Subify.Domain.Tests` / `Application.Tests`  
  **Açıklama:** xUnit.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `api/Subify.Domain.Tests`, `api/Subify.Application.Tests`, `api/Subify.Api.Tests` (handler harness).

- [x] **12.1.2** UserShare / totals unit tests  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `SubscriptionMathTests`, `FinancialMotorTests`, `CurrencyConversionTests`, list summary harness.

- [x] **12.1.3** First SuperAdmin race/logic tests  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `SuperAdminBootstrapTests` (ikinci create → SuperAdminAlreadyExists); setup complete lock.

- [x] **12.1.4** Validators unit tests  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `Subify.Application.Tests` — Login + CreateSubscription validators (incl. category XOR).

- [x] **12.1.5** Category XOR rule tests  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `CategoryXorRuleTests` + create validator XOR cases.

### 12.2 Integration

- [x] **12.2.1** WebApplicationFactory setup  
  **Açıklama:** Testcontainers Postgres önerilir.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** SQLite shared in-memory `SubifyWebApplicationFactory`; env `Testing` skips migrate; hosted jobs stripped.

- [x] **12.2.2** Auth flow integration  
  **Açıklama:** register → login → refresh → logout.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `AuthFlowIntegrationTests` — setup admin → complete → login → refresh → logout.

- [x] **12.2.3** Subscription isolation test  
  **Açıklama:** User A User B verisini göremez.  
  **Öncelik:** P0 · **Tamamlandı:** 2026-08-01  
  **Not:** `SubscriptionIntegrationTests.User_cannot_see_other_users_subscriptions`.

- [x] **12.2.4** Admin authorization tests  
  **Açıklama:** User settings’e 403.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `User_cannot_access_admin_settings` → 403.

- [x] **12.2.5** No subscription limit test  
  **Açıklama:** 4+ create 403 değil.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** 5 create + list totalItems ≥ 5 (freemium yok).

### 12.3 Web E2E

- [x] **12.3.1** Playwright setup  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `web/playwright.config.ts` + `@playwright/test`; `npx playwright install` gerekir.

- [x] **12.3.2** E2E: first admin + create subscription  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** Scaffold smoke (`e2e/smoke.spec.ts` landing + login). Full admin+sub E2E optional when stack up.

---

# FAZ 13 — Flutter (en son)

### 13.1 Mobile foundation

- [ ] **13.1.1** Flutter proje oluştur  
  **Açıklama:** `mobile/`  
  **Öncelik:** P3 · **Bağımlı:** API+Web stabilize

- [ ] **13.1.2** Dinamik API base URL onboarding  
  **Açıklama:** Self-host zorunlu.  
  **Öncelik:** P3

- [ ] **13.1.3** Dio + JWT interceptor + secure storage  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.1.4** Riverpod (veya seçilen state) + GoRouter  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.1.5** Light/Dark theme manifesto  
  **Açıklama:**  
  **Öncelik:** P3

### 13.2 Mobile screens

- [ ] **13.2.1** Login / Register  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.2** Dashboard  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.3** Subscriptions list + form  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.4** Reports  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.5** AI  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.6** Profile  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.7** Push notifications (opsiyonel)  
  **Açıklama:** FCM; premium yok.  
  **Öncelik:** P3

---

# FAZ 14 — Polish, güvenlik, dokümantasyon kapanışı

> **Durum (2026-08-01):** Security headers, CORS fail-closed prod, Serilog secret destructure,  
> OpenAPI info, OpenApi package patch, ERROR_CODES_OS, OPS notes.

### 14.1 Güvenlik checklist

- [x] **14.1.1** Secret masking audit (settings GET)  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** `SystemSettingsMapper` + `SystemSettingsHandlerTests`; audit snapshot flags only.

- [x] **14.1.2** Log redaction (passwords, tokens, API keys)  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** Request log no body; `SensitiveDataDestructuringPolicy` for structured `@` logs.

- [x] **14.1.3** CORS production tight  
  **Açıklama:**  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** Prod empty origins → deny all; Dev/Testing localhost default; normalize http(s) only.

- [x] **14.1.4** Security headers (reverse proxy notları)  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `SecurityHeadersMiddleware` + Caddy/Nginx examples + OPS §security.

- [x] **14.1.5** Dependency vulnerability scan  
  **Açıklama:** Örn. Microsoft.OpenApi uyarısı.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `Microsoft.OpenApi` → 2.11.0 (GHSA-v5pm-xwqc-g5wc); AspNetCore.OpenApi 10.0.10.

### 14.2 API dokümantasyon

- [x] **14.2.1** OpenAPI title/version/description  
  **Açıklama:** Subify OS.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `OpenApiInfoTransformer` — Subify OS API 1.0.0.

- [x] **14.2.2** Endpoint summary/description audit  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** All `*EndPoints.cs` groups carry `.WithSummary` / descriptions for main routes.

- [x] **14.2.3** ERROR_CODES OS revizyonu  
  **Açıklama:**  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `docs/ERROR_CODES_OS.md` canonical; legacy `ERROR_CODES.md` points here.

### 14.3 Task list bakımı

- [x] **14.3.1** Tamamlanan task’ları `[x]` yap  
  **Açıklama:** Her PR/sprint sonunda.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** Process: mark done + date + short Not each sprint.

- [x] **14.3.2** Yeni scope task ekleme kuralı  
  **Açıklama:** Manifesto çelişkisi yoksa ekle; çelişki varsa reddet. Setup / EmailSend kararlarına uy.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-08-01  
  **Not:** Rule block at top of this file.

---

# FAZ 15 — EmailSend altyapısı (core ürün bittikten sonra)

> **Durum (2026-08-01):** Motor + forgot/reset + invite mail + renewal job + test-smtp + web.  
> **Confirm yok (kalıcı).** SMTP: `HasSmtpConfigured` = enabled + host + port + from.  
> Migration: `AddEmailSendLog`.

### 15.1 Motor

- [x] **15.1.1** `IEmailSender` abstraction  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `IEmailSender` + `EmailMessage`; `IEmailDeliveryService` + templates.

- [x] **15.1.2** `SmtpEmailSender` (SystemSettings’ten oku)  
  **Açıklama:** Runtime secret; factory/refresh.  
  **Öncelik:** P2 · **Bağımlı:** 2.1.5, 7.3.2 · **Tamamlandı:** 2026-08-01  
  **Not:** `SmtpEmailSender` live reads SystemSettings each send.

- [x] **15.1.3** Noop sender when SMTP empty/disabled  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** Unconfigured → `SET_003` (not silent success except forgot enumeration path).

- [x] **15.1.4** Template renderer + `email_templates` seed  
  **Açıklama:** ResetPassword, RenewalReminder, Invite (VerifyEmail **yok** — confirm yok).  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `EmailTemplateRenderer` + seed catalog; DB first then catalog fallback.

- [x] **15.1.5** Locale’e göre template  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** tr/en; fallback Default locale.

### 15.2 Auth mailleri

- [x] **15.2.1** Forgot-password e-posta + token  
  **Açıklama:** `3.2.7` / `3.2.8` aktif hale gelir.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `POST /api/auth/forgot-password` + `reset-password`; Identity reset token.

- [x] **15.2.2** Invite e-posta (opsiyonel; link hâlâ UI’da)  
  **Öncelik:** P3 · **Tamamlandı:** 2026-08-01  
  **Not:** CreateInvite best-effort send when SMTP on; UI token always returned.

### 15.3 Operasyonel mailler

- [x] **15.3.1** Renewal reminder background job  
  **Açıklama:** `daysBeforeRenewal` + SMTP enabled.  
  **Öncelik:** P2 · **Bağımlı:** 8.4, 4.x · **Tamamlandı:** 2026-08-01  
  **Not:** `IRenewalReminderService` + host; EmailEnabled + window; tests in `RenewalReminderServiceTests`.

- [x] **15.3.2** Duplicate send koruması  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `EmailSendLog.DedupeKey` unique success; `renewal:{subId}:{date}`.

- [x] **15.3.3** `POST /api/admin/settings/test-smtp`  
  **Açıklama:** `7.3.3` implement.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** SuperAdmin; optional `toEmail`; web field + button.

### 15.4 Web

- [x] **15.4.1** Forgot / reset password sayfaları  
  **Açıklama:** `10.2.3b`  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** `/forgot-password`, `/reset-password` (link token hidden); setup-gate allowlist.

- [x] **15.4.2** Settings “Test SMTP” butonu  
  **Öncelik:** P2 · **Tamamlandı:** 2026-08-01  
  **Not:** Admin settings optional recipient; profile email toggle for reminders.

---

# FAZ 16 — Ürün genişletme (v1+ / docs backlog)

> **Amaç:** Core (1–15) üzerine manifesto/PRD ile uyumlu, ilgi çekici özellikler.  
> **Freemium / ödeme / confirm zorunlu / open banking yok.**  
> **Döviz gösterim kuralı (16.1 — ürün kararı):**
>
> 1. **Ana para** = kullanıcının `MainCurrency` (TR kullanıcıda genelde TRY; asla hardcode “sadece TL” değil).
> 2. Abonelik **orijinal para ≠ MainCurrency** ise:
>    - **Birincil satır:** main-currency karşılığı (FX snapshot; rate yoksa uyarı + sadece orijinal).
>    - **İkincil:** orijinal tutar parantezde — örn. `1.250,00 TRY` + muted `(49,99 USD)`.
>    - Aynı para ise parantez **gösterme**.
> 3. Rapor / dashboard **toplam ve grafikler** zaten MainCurrency; satır detayında (2) kuralı.
> 4. Sol menü altı: **son snapshot** kurları (MainCurrency bazlı popüler çaprazlar + `asOf` / “güncel değil” ipucu).

### 16.1 Çoklu para birimi & kur UX

- [x] **16.1.1** FX display kuralı (shared web helper)  
  **Açıklama:** `formatMoneyDual(amount, currency, mainCurrency, rates?)` → primary main + optional `(original)`. Rate missing → primary original + “kur yok” badge/tooltip.  
  **Öncelik:** P1 · **Bağımlı:** 6.2.x, 4.3.x · **Tamamlandı:** 2026-08-02  
  **Not:** `web/src/lib/fx/money-dual.ts` (`convertCurrency`, `formatMoneyDual`, `toFxRatesSnapshot`); re-export `@/lib/utils`; `ExchangeRatesResponse` type; i18n `fxRateMissing*`; smoke `money-dual.test.ts`.

- [x] **16.1.2** Abonelik listesi / kart / detay: main karşılık  
  **Açıklama:** `SubscriptionItem` zaten `userShare` + main alanları taşıyorsa UI’da dual; yoksa API’ye `userShareMain` / `monthlyEquivalentMain` netleştir.  
  **Öncelik:** P1 · **Bağımlı:** 16.1.1, 10.5.x · **Tamamlandı:** 2026-08-02  
  **Not:** Client FX via `useFxRates` + `MoneyDual` on list cards (price + share) and detail summary (price, share, monthly eq). MainCurrency from list summary / profile.

- [x] **16.1.3** Dashboard upcoming / stat kartlarında dual tutar  
  **Açıklama:** Yabancı kur aboneliklerde main + (orijinal).  
  **Öncelik:** P1 · **Bağımlı:** 16.1.1 · **Tamamlandı:** 2026-08-02  
  **Not:** Upcoming rows `MoneyDual` (userShare); KPI totals stay MainCurrency; `fxUnconvertedWarning` banner; 30d upcoming total footer.

- [x] **16.1.4** Raporlar: satır ve tooltip dual display  
  **Açıklama:** Overview / abonelikler tablosu / cashflow satırları MainCurrency; orijinal farklıysa `()`. Currency sekmesi zaten dağılım — orada “orijinal toplam + main toplam” net etiket. Print/CSV: main kolon + original kolon.  
  **Öncelik:** P1 · **Bağımlı:** 16.1.1, 6.1.x, reports web · **Tamamlandı:** 2026-08-02  
  **Not:** MoneyDual on subs table, cashflow timeline, top costs, budget hints; currency panel main+(original); CSV dual columns; print dual text.

- [x] **16.1.5** Sol menü (shell) altı: son döviz kurları  
  **Açıklama:** App sidebar footer: `GET /api/exchange-rates?base={mainCurrency}` (veya mevcut endpoint); 3–6 önemli hedef kur (USD, EUR, GBP, … main değilse); `asOf` + yenileme ipucu; rate yoksa “Kur yüklenemedi”. Mobil dar menüde collapse/okunabilir.  
  **Öncelik:** P1 · **Bağımlı:** 6.2.3, web shell · **Tamamlandı:** 2026-08-02  
  **Not:** `SidebarFxRates` above user block (desktop + mobile drawer); 1 USD = X main; collapse + refresh; `useFxRates.refetch`.

- [x] **16.1.6** Kur stale / missing UX  
  **Açıklama:** Snapshot yaşı > N saat → “kur eski olabilir”; missing conversion sayacı (liste/rapor). SuperAdmin’e FX sync durumu linki (opsiyonel).  
  **Öncelik:** P2 · **Bağımlı:** 16.1.5, 8.4 · **Tamamlandı:** 2026-08-02  
  **Not:** `FxStatusBanner` + `countMissingConversions` / `isFxSnapshotStale` (6h); dashboard, subscriptions, reports; SuperAdmin → settings link; MoneyDual stacked rate-missing tip.

### 16.2 Kullanım & AI aksiyonları

- [ ] **16.2.1** `LastUsedAt` UI (abonelik detay / hızlı aksiyon) — **İPTAL**  
  **Açıklama:** “Bugün kullandım” / tarih seç; liste ve AI unused için girdi.  
  **Öncelik:** P2 · **Bağımlı:** 4.x entity · **İptal:** 2026-08-02  
  **Not:** Kaldırıldı — domain `LastUsedAt`, API/DTO, form, list/detail quick action, `usage.ts`; migration `DropSubscriptionLastUsedAt`.

- [ ] **16.2.2** “Kullanılmıyor” görünümü — **İPTAL**  
  **Açıklama:** N gündür `lastUsed` yok / eski → filtre veya dashboard kartı (AI olmadan da).  
  **Öncelik:** P2 · **Bağımlı:** 16.2.1 · **İptal:** 2026-08-02  
  **Not:** 16.2.1 ile birlikte kaldırıldı (filtre, badge, dashboard banner).

- [x] **16.2.3** AI tip → tek tık aksiyon  
  **Açıklama:** unused → arşiv önerisi; yearly → not; tip `subscriptionId` ile detaya link / arşiv confirm.  
  **Öncelik:** P2 · **Bağımlı:** 9.2.x, 10.x AI page · **Tamamlandı:** 2026-08-02  
  **Not:** Tip actions: open sub, archive (unused tip type), review yearly link. LastUsedAt sinyali yok; AI tip tipi kalır.

### 16.3 Takvim, import / export genişletme

- [x] **16.3.1** Yenileme takvimi ICS export  
  **Açıklama:** Upcoming → `text/calendar` dosya veya `GET /api/subscriptions/upcoming.ics` (auth).  
  **Öncelik:** P2 · **Bağımlı:** 4.2 upcoming · **Tamamlandı:** 2026-08-02  
  **Not:** Client `buildUpcomingIcs` / download; Reports → Cashflow button.

- [x] **16.3.2** CSV/JSON abonelik import  
  **Açıklama:** Export kolonlarıyla uyumlu; dry-run + hata satırları; MainCurrency dışı para korunur.  
  **Öncelik:** P2 · **Bağımlı:** 4.1 create, export CSV · **Tamamlandı:** 2026-08-02  
  **Not:** `parseSubscriptionCsv` dry-run + POST create loop; template download; subscriptions page.

- [x] **16.3.3** What-if bütçe (AI’sız)  
  **Açıklama:** “Şu aboneliği kaldır / yearly yap” → yeni monthly total ve bütçe kalanı (client veya küçük query).  
  **Öncelik:** P2 · **Bağımlı:** reports/budget, 4.3 math · **Tamamlandı:** 2026-08-02  
  **Not:** `computeWhatIf` + Reports Budget tab exclude/yearly checkboxes.

### 16.4 Fiyat geçmişi & uyarılar

- [x] **16.4.1** Abonelik fiyat geçmişi modeli  
  **Açıklama:** Fiyat/currency değişince history satırı; soft audit.  
  **Öncelik:** P2 · **Bağımlı:** 4.1 update · **Tamamlandı:** 2026-08-02  
  **Not:** `SubscriptionPriceHistory` + migration; write on Update price/currency; DTO on list/detail; test.

- [x] **16.4.2** “Zam” sinyali UI  
  **Açıklama:** Son değişiklik badge / rapor satırı; opsiyonel e-posta (SMTP) sonra.  
  **Öncelik:** P3 · **Bağımlı:** 16.4.1 · **Tamamlandı:** 2026-08-02  
  **Not:** List badge (Zam/İndirim); detail price history panel. Email optional later.

### 16.5 Aile / multi-user (opt-in, privacy)

- [ ] **16.5.1** Aile bütçe özeti (opt-in) — **İPTAL**  
  **Açıklama:** PRD §4.2 “ileride”. Kullanıcılar açık rıza ile aggregate monthly (detay abonelik yok). SuperAdmin ayarı + user opt-in.  
  **Öncelik:** P3 · **Bağımlı:** 7.1, 4.3 · **İptal:** 2026-08-02  
  **Not:** Kaldırıldı — domain flag’ler, API, UI, migration drop (`DropFamilyBudgetFlags`).

- [ ] **16.5.2** Paylaşım matrisi (opsiyonel) — **İPTAL**  
  **Açıklama:** `shared_with_count` ötesi: instance user’a “paylaşılan kişiler” (isim) — finansal detay sızdırmaz.  
  **Öncelik:** P3 · **Bağımlı:** 16.5.1 · **İptal:** 2026-08-02  
  **Not:** 16.5.1 ile birlikte kaldırıldı.

### 16.6 Self-host ops & community

- [x] **16.6.1** SuperAdmin instance health paneli (MVP)  
  **Açıklama:** setup flags, son FX sync, email send success/fail özeti, AI key configured, job enabled.  
  **Öncelik:** P2 · **Bağımlı:** health, 6.2, 15.x logs · **Tamamlandı:** 2026-08-02  
  **Not:** Admin settings → **Sağlık / Ops** sekmesi: FX snapshot/stale, AI/SMTP ready; banner → `?tab=ops`. Full job metrics later.

- [x] **16.6.2** Yedekleme UX / dokümantasyon derinleştirme  
  **Açıklama:** Admin’de “nasıl yedek alınır” + opsiyonel scheduled dump notu (OPS); tam UI restore P3.  
  **Öncelik:** P2 · **Bağımlı:** 11.2 OPS · **Tamamlandı:** 2026-08-02  
  **Not:** Ops kartı (komut + cron kopyala); `docker/scripts/backup-postgres.sh` / `restore-postgres.sh`; OPS.md + docker/README.

- [x] **16.6.3** Provider katalog import (JSON/seed PR)  
  **Açıklama:** Topluluk provider listesi; SuperAdmin import; logo URL opsiyonel.  
  **Öncelik:** P3 · **Bağımlı:** 5.2, 7.x · **Tamamlandı:** 2026-08-02  
  **Not:** `POST /api/admin/providers/import` (create/skip/updateExisting); sample `data/provider-catalog.sample.json`; Ops UI; tests.

- [ ] **16.6.4** Runtime resources / dil paketi (web hibrit)  
  **Açıklama:** ADR-001; admin resource + client delta (MVP static kalabilir).  
  **Öncelik:** P3 · **Bağımlı:** 6.3  
  **Not:** v1 web `messages.ts` static; API resources endpoints zaten var — hibrit client sonra.

- [ ] **16.6.5** OpenTelemetry (opsiyonel)  
  **Açıklama:** LOGGING_MONITORING; v1 zorunlu değil.  
  **Öncelik:** P3  
  **Not:** Bilinçli erteleme — Serilog + health yeterli self-host v1.

### 16.7 Etiket / sınıflama

- [ ] **16.7.1** Abonelik etiketleri (tags)  
  **Açıklama:** iş / ev / çocuk vb.; filtre + rapor dilimi (opsiyonel).  
  **Öncelik:** P3 · **Bağımlı:** 4.x

---

## Önerilen uygulama sırası (numara rehberi)

| Sıra | Paket | Task aralığı |
| ---- | ----- | ------------ |
| 1 | Pipeline + health + error OS | `1.2.x` (kritikler) |
| 2 | Domain + EF + migrate/seed | `2.1` → `2.3` |
| 3 | Auth + SuperAdmin + change/reset password (admin) | `3.1`–`3.4`, `3.2.14`, `3.2.15` |
| 4 | **First-run Setup Wizard** | **`3S.*`** + web `3S.8` |
| 5 | Subscription core | `4.x` |
| 6 | Categories + Profile + Activity | `5.x` |
| 7 | Admin + Settings (SMTP kaydet, AI) | `7.x` |
| 8 | Reports + FX | `6.x` |
| 9 | AI | `9.x` |
| 10 | Web app shell + dashboard + subs | `10.1`–`10.5` |
| 11 | Web admin + profile | `10.6`–`10.9` |
| 12 | Docker release | `11.x` |
| 13 | Tests | `12.x` |
| 14 | Flutter | `13.x` |
| 15 | Polish | `14.x` |
| 16 | EmailSend + forgot-mail + reminders | `15.x` |
| **17** | **v1+ genişletme (FX UX önce)** | **`16.1` → `16.2` → `16.3` → …** |

---

## Hızlı referans: P0 “MVP dikey dilim” minimum set

1. `1.2.1`, `1.2.3`, `1.2.7`, `1.2.12`
2. `2.1.1`, `2.1.2`, `2.1.5` (settings model), `2.2.1`–`2.2.5`, `2.3.2`–`2.3.5`, `2.3.10`, `2.3.11`
3. `3.1.3`, `3.2.3`, `3.2.4`, `3.2.9`, `3.2.10`, `3.2.14`, `3.2.15`, `3.3.1`–`3.3.4`, `3.3.6`
4. **Setup:** `3S.1.1`–`3S.1.3`, `3S.2.1`, `3S.3.1`, `3S.7.1`, `3S.8.1`, `3S.8.3`, `3S.8.4`, `3S.8.8`–`3S.8.10`
5. `4.1.1`–`4.1.10`, `4.2.1`–`4.2.6`, `4.3.1`–`4.3.3`
6. `5.1.1`–`5.1.3`, `5.3.1`–`5.3.2`
7. `7.1.1`–`7.1.2`, `7.3.1`–`7.3.2`, `7.5.1`
8. `10.1.5`–`10.1.8`, `10.2.1`, `10.2.3`, `10.3.1`, `10.4.1`, `10.4.3`, `10.5.1`–`10.5.5`, `10.9.1`, `10.9.3`
9. `11.1.3`, `11.1.4`, `11.1.7`, `11.2.1`
10. `12.2.3`

**Setup sonrası opsiyonel adımlar (MVP nice-to-have):** `3S.4`, `3S.5` (SMTP kaydet), `3S.6` (AI kaydet).

---

*Bu dosya Subify OS geliştirme sırasının tek operasyonel task listesidir (sürüm 1.4).*  
*Çelişkide: (1) Bu listedeki ürün kararları · (2) Manifesto · (3) PRD · (4) legacy docs.*  
*Özet: **Confirm yok (kalıcı)** · **SMTP BYOK (Faz 15)** · **v1+ backlog Faz 16** (FX dual display + menü kurları + docs fikirleri).*  
*Cloud/SaaS: [SUBIFY_SAAS_TRANSITION_PRD.md](./SUBIFY_SAAS_TRANSITION_PRD.md) + [SUBIFY_SAAS_TRANSITION_TASK_LIST.md](./SUBIFY_SAAS_TRANSITION_TASK_LIST.md) — OS listesine billing task eklenmez.*
